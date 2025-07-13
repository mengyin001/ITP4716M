using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(PhotonView))]
public class Enemy : Character, IPunObservable
{
    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnAttack;

    [Header("Chase Settings")]
    [SerializeField] private float chaseDistance = 5f; // 扩大追击范围，适应多玩家
    [SerializeField] private float attackDistance = 0.8f;

    [Header("Patrol Settings")]
    [SerializeField] public bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float meleeAttackDamage; // 修正拼写错误（原meleet→melee）
    public LayerMask playerLayer;
    public float AttackCooldownDuration = 2f;

    [Header("Target Selection")]
    [SerializeField] private float targetDetectionRadius = 10f; // 检测玩家的最大范围
    [SerializeField] private bool prioritizeThreats = false; // 是否优先攻击威胁最大的玩家

    [Header("Behavior Override")]
    public bool forceChaseMode = false;
    public bool publicMode = true;

    // 核心组件
    private Seeker seeker;
    private Animator animator;
    private SpriteRenderer sr;
    private List<Vector3> pathPointList;

    // 状态变量
    private bool isAlive = true;
    private bool isAttack = true; // 攻击冷却状态
    private bool isAttacking = false; // 攻击动画状态（需同步）
    private bool isChasing = false;
    private bool isPatrolling = true;
    private bool isWaitingAtPoint = false;

    // 路径与巡逻变量
    private int currentIndex = 0;
    private int currentPatrolPointIndex = 0;
    private float pathGenerateInterval = 0.5f;
    private float pathGenerateTimer = 0f;
    private float waitTimer = 0f;
    private int patrolDirection = 1;

    // 玩家目标变量（多玩家支持）
    private List<Transform> allAlivePlayers = new List<Transform>(); // 所有存活玩家
    private Transform currentTarget; // 当前选中的目标
    private float targetUpdateInterval = 0.5f; // 目标更新频率（比玩家检测更频繁）
    private float targetUpdateTimer = 0f;

    // 网络同步变量
    private Vector2 networkMovementDirection;
    private bool networkIsAttacking;
    private Vector2 currentMovementDirection;

    // 其他引用
    public PickupSpawner pickupSpawner;
    private Transform player;


    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (shouldPatrol && patrolPoints.Count > 0)
            {
                GeneratePath(transform.position, patrolPoints[currentPatrolPointIndex].position);
            }
        }
    }

    private void Update()
    {
        if (!isAlive) return;

        // 仅主机执行AI逻辑
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateAlivePlayersList(); // 定期更新存活玩家列表
            UpdateCurrentTarget(); // 选择最优目标

            if (currentTarget == null)
            {
                // 无目标时回到巡逻
                HandlePatrolBehavior();
                return;
            }

            // 有目标时执行追击/攻击
            HandleTargetBehavior();
        }
        else
        {
            // 客户端仅同步状态
            ApplyNetworkState();
        }
    }
    public void SetTarget(Transform target)
    {
        player = target;
        // 可以添加其他逻辑，例如当目标变更时重置路径
        currentTarget = target;
        pathPointList = null;
    }

    #region 多玩家检测与目标选择
    // 更新所有存活玩家列表
    private void UpdateAlivePlayersList()
    {
        targetUpdateTimer += Time.deltaTime;
        if (targetUpdateTimer < targetUpdateInterval) return;

        targetUpdateTimer = 0f;
        allAlivePlayers.Clear();

        // 检测范围内所有玩家
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, targetDetectionRadius, playerLayer);
        foreach (var collider in hitColliders)
        {
            HealthSystem health = collider.GetComponent<HealthSystem>();
            // 筛选存活的玩家
            if (health != null && health.currentHealth > 0)
            {
                allAlivePlayers.Add(collider.transform);
            }
        }
    }

    // 选择当前目标（最近或威胁最大）
    private void UpdateCurrentTarget()
    {
        if (allAlivePlayers.Count == 0)
        {
            currentTarget = null;
            return;
        }

        if (prioritizeThreats)
        {
            // 优先选择威胁最大的玩家（此处简化为最近攻击过敌人的玩家，可扩展）
            currentTarget = GetHighestThreatPlayer();
        }
        else
        {
            // 优先选择最近的玩家
            currentTarget = GetNearestPlayer();
        }
    }

    // 获取最近的玩家
    private Transform GetNearestPlayer()
    {
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var player in allAlivePlayers)
        {
            float distance = Vector2.Distance(transform.position, GetPlayerCenterPosition(player));
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = player;
            }
        }
        return nearest;
    }

    // 获取威胁最大的玩家（示例：最近攻击过敌人的玩家，需额外逻辑支持）
    private Transform GetHighestThreatPlayer()
    {
        // 简化实现：如果没有威胁数据，默认返回最近的玩家
        return GetNearestPlayer();

        // 扩展提示：可添加字典记录玩家威胁值（如最近造成伤害的玩家威胁值最高）
        // 例如：Dictionary<Transform, float> playerThreats = new Dictionary<Transform, float>();
        // 每次被攻击时更新对应玩家的威胁值，此处返回威胁值最高的玩家
    }
    #endregion

    #region 行为逻辑（追击/攻击/巡逻）
    // 处理有目标时的行为
    private void HandleTargetBehavior()
    {
        float distanceToTarget = Vector2.Distance(transform.position, GetPlayerCenterPosition(currentTarget));

        if (forceChaseMode || distanceToTarget < chaseDistance)
        {
            isChasing = true;
            isPatrolling = false;

            // 攻击范围内：执行攻击
            if (distanceToTarget <= attackDistance)
            {
                HandleAttackBehavior();
            }
            // 追击范围内：执行追击
            else
            {
                HandleChaseMovement();
            }
        }
        else
        {
            // 超出追击范围：回到巡逻
            isChasing = false;
            isPatrolling = shouldPatrol;
            HandlePatrolBehavior();
        }
    }

    // 追击移动逻辑
    private void HandleChaseMovement()
    {
        UpdatePathToTarget();
        MoveAlongPath();
    }

    // 攻击逻辑
    private void HandleAttackBehavior()
    {
        currentMovementDirection = Vector2.zero;
        OnMovementInput?.Invoke(Vector2.zero);

        if (isAttack)
        {
            isAttack = false;
            isAttacking = true;
            OnAttack?.Invoke();
            StartCoroutine(AttackCooldownCoroutine());
        }

        // 面向目标
        sr.flipX = GetPlayerCenterPosition(currentTarget).x > transform.position.x;
    }

    // 巡逻行为逻辑
    private void HandlePatrolBehavior()
    {
        if (!shouldPatrol || patrolPoints.Count == 0)
        {
            currentMovementDirection = Vector2.zero;
            OnMovementInput?.Invoke(Vector2.zero);
            return;
        }

        if (isWaitingAtPoint)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaitingAtPoint = false;
                waitTimer = 0f;
                MoveToNextPatrolPoint();
                GeneratePath(transform.position, patrolPoints[currentPatrolPointIndex].position);
            }
            return;
        }

        if (pathPointList == null || pathPointList.Count <= 0)
        {
            GeneratePath(transform.position, patrolPoints[currentPatrolPointIndex].position);
            return;
        }

        MoveAlongPath();
    }
    #endregion

    #region 路径与移动辅助
    // 生成到目标的路径
    private void UpdatePathToTarget()
    {
        if (currentTarget == null) return;

        pathGenerateTimer += Time.deltaTime;
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(transform.position, GetPlayerCenterPosition(currentTarget));
            pathGenerateTimer = 0f;
        }
    }

    // 沿路径移动
    private void MoveAlongPath()
    {
        if (pathPointList == null || pathPointList.Count == 0) return;

        if (currentIndex >= pathPointList.Count)
        {
            currentIndex = pathPointList.Count - 1;
        }

        Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
        currentMovementDirection = direction;
        OnMovementInput?.Invoke(direction);
        sr.flipX = direction.x > 0;

        if (Vector2.Distance(transform.position, pathPointList[currentIndex]) < 0.1f)
        {
            currentIndex++;
        }
    }

    // 生成路径
    private void GeneratePath(Vector3 start, Vector3 end)
    {
        currentIndex = 0;
        seeker.StartPath(start, end, OnPathGenerated);
    }

    // 路径生成回调
    private void OnPathGenerated(Path path)
    {
        if (!path.error)
        {
            pathPointList = path.vectorPath;
        }
    }

    // 移动到下一个巡逻点
    private void MoveToNextPatrolPoint()
    {
        currentPatrolPointIndex += patrolDirection;
        if (currentPatrolPointIndex >= patrolPoints.Count)
        {
            currentPatrolPointIndex = patrolPoints.Count - 1;
            patrolDirection = -1;
        }
        else if (currentPatrolPointIndex < 0)
        {
            currentPatrolPointIndex = 0;
            patrolDirection = 1;
        }
    }
    #endregion

    #region 攻击冷却与动画事件
    // 攻击冷却协程
    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);
        isAttack = true;
        isAttacking = false;
    }

    // 攻击动画事件（由动画帧调用）
    public void MeleeAttackEvent()
    {
        if (!PhotonNetwork.IsMasterClient || currentTarget == null) return;

        // 使用敌人自身位置作为检测中心
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            transform.position, // 使用敌人位置而非玩家位置
            attackDistance,
            playerLayer
        );

        foreach (var collider in hitColliders)
        {
            if (collider.transform == currentTarget)
            {
                HealthSystem health = collider.GetComponent<HealthSystem>();
                if (health != null)
                {
                    // 调用RPC方法造成伤害
                    health.photonView.RPC("RPC_TakeDamage", RpcTarget.All, meleeAttackDamage);
                    Debug.Log($"Enemy dealt {meleeAttackDamage} damage to {currentTarget.name}");
                    break;
                }
            }
        }
    }

    // 攻击动画结束事件
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }
    #endregion

    #region 死亡与网络同步
    [PunRPC]
    public override void DieRPC()
    {
        base.DieRPC();
        isAlive = false;
        currentMovementDirection = Vector2.zero;
        OnMovementInput?.Invoke(Vector2.zero);

        if (PhotonNetwork.IsMasterClient && pickupSpawner != null)
        {
            pickupSpawner.DropItems();
        }
    }

    // 网络同步
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 主机发送移动方向和攻击状态
            stream.SendNext(currentMovementDirection);
            stream.SendNext(isAttacking);
            // 同步当前目标的ViewID（用于客户端显示目标相关逻辑）
            stream.SendNext(currentTarget != null ? currentTarget.GetComponent<PhotonView>().ViewID : -1);
        }
        else
        {
            // 客户端接收
            networkMovementDirection = (Vector2)stream.ReceiveNext();
            networkIsAttacking = (bool)stream.ReceiveNext();

            // 同步目标（仅用于显示，不影响AI逻辑）
            int targetViewID = (int)stream.ReceiveNext();
            if (targetViewID != -1)
            {
                PhotonView targetView = PhotonView.Find(targetViewID);
                currentTarget = targetView != null ? targetView.transform : null;
            }
            else
            {
                currentTarget = null;
            }
        }
    }

    // 应用网络同步的状态
    private void ApplyNetworkState()
    {
        OnMovementInput?.Invoke(networkMovementDirection);

        if (networkMovementDirection.x != 0)
        {
            sr.flipX = networkMovementDirection.x > 0;
        }

        if (animator != null)
        {
            animator.SetBool("isWalk", networkMovementDirection != Vector2.zero);
            animator.SetBool("isAttack", networkIsAttacking);
        }
    }
    #endregion

    #region 辅助方法
    private Vector3 GetPlayerCenterPosition(Transform playerTransform)
    {
        if (playerTransform == null) return transform.position;

        Collider2D collider = playerTransform.GetComponent<Collider2D>();
        return collider != null ? collider.bounds.center : playerTransform.position;
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // 绘制追击范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        // 绘制玩家检测范围
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, targetDetectionRadius);

        // 绘制巡逻点
        if (shouldPatrol && patrolPoints.Count > 0)
        {
            Gizmos.color = Color.blue;
            foreach (var point in patrolPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.2f);
                }
            }
        }

        // 绘制当前目标连线
        if (currentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
    #endregion
}
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
    [SerializeField] private float chaseDistance = 3f;
    [SerializeField] private float attackDistance = 0.8f;

    [Header("Patrol Settings")]
    [SerializeField] public bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float meleetAttackDamage;
    public LayerMask playerLayer;
    public float AttackCooldownDuration = 2f;

    [Header("Behavior Override")]
    public bool forceChaseMode = false;
    public bool publicMode = true;

    private Seeker seeker;
    private List<Vector3> pathPointList;
    private int currentIndex = 0;
    private float pathGenerateInterval = 0.5f;
    private float pathGenerateTimer = 0f;

    private Animator animator;
    private bool isAttackReady = true; // 是否可以攻击（冷却状态）
    private bool isAttacking = false; // 是否正在攻击（动画状态，需同步）
    private bool isChasing = false;
    private int currentPatrolPointIndex = 0;
    private bool isWaitingAtPoint = false;
    private float waitTimer = 0f;
    private SpriteRenderer sr;

    private bool isAlive = true;
    private bool isPatrolling = true;
    private int patrolDirection = 1;

    public PickupSpawner pickupSpawner;

    // 改为追踪最近的玩家（核心修改）
    private Transform nearestPlayer;
    private float playerUpdateInterval = 0.3f; // 缩短更新间隔，提高响应速度
    private float playerUpdateTimer = 0f;

    // 网络同步变量
    private Vector2 networkMovementDirection;
    private bool networkIsAttacking;
    private Vector2 currentMovementDirection; // 记录当前移动方向（主机用）

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        UpdateNearestPlayer(); // 初始化最近玩家检测
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
        if (!isAlive)
            return;

        // 仅主机执行AI逻辑（Photon网络逻辑）
        if (PhotonNetwork.IsMasterClient)
        {
            // 定期更新最近玩家（所有状态下都执行）
            playerUpdateTimer += Time.deltaTime;
            if (playerUpdateTimer >= playerUpdateInterval)
            {
                UpdateNearestPlayer();
                playerUpdateTimer = 0f;
            }

            // 没有玩家时停止移动
            if (nearestPlayer == null)
            {
                currentMovementDirection = Vector2.zero;
                OnMovementInput?.Invoke(Vector2.zero);
                return;
            }

            // 强制追逐模式优先
            if (forceChaseMode)
            {
                HandleForceChaseBehavior();
                return;
            }

            // 公共模式下的行为逻辑（巡逻/追逐切换）
            if (publicMode)
            {
                float distanceToNearestPlayer = Vector2.Distance(
                    GetPlayerCenterPosition(nearestPlayer),
                    transform.position
                );

                // 根据距离切换状态（巡逻→追逐）
                if (distanceToNearestPlayer < chaseDistance)
                {
                    isChasing = true;
                    isPatrolling = false;
                    HandleChaseBehavior(distanceToNearestPlayer);
                }
                else
                {
                    // 追逐→巡逻的切换逻辑
                    if (isChasing)
                    {
                        isChasing = false;
                        pathPointList = null;
                        isPatrolling = shouldPatrol;
                        if (isPatrolling && patrolPoints.Count > 0)
                        {
                            GeneratePath(transform.position, patrolPoints[currentPatrolPointIndex].position);
                        }
                    }

                    // 巡逻状态（仍会检测最近玩家，确保进入范围时立即切换）
                    if (isPatrolling && patrolPoints.Count > 0)
                    {
                        Patrol();
                    }
                    else
                    {
                        currentMovementDirection = Vector2.zero;
                        OnMovementInput?.Invoke(Vector2.zero);
                    }
                }
            }
        }
        else
        {
            // 客户端仅同步状态
            ApplyNetworkState();
        }
    }

    /// <summary>
    /// 更新最近的玩家引用（所有状态下都会调用）
    /// 遍历所有玩家，筛选出最近的存活玩家
    /// </summary>
    private void UpdateNearestPlayer()
    {
        // 获取场景中所有玩家对象
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (allPlayers.Length == 0)
        {
            nearestPlayer = null;
            return;
        }

        Transform closestPlayer = null;
        float closestDistance = Mathf.Infinity;
        Vector3 enemyPos = transform.position;

        foreach (GameObject playerObj in allPlayers)
        {
            // 跳过已死亡的玩家（假设玩家有HealthSystem组件）
            HealthSystem health = playerObj.GetComponent<HealthSystem>();
            if (health != null && !health.IsAlive)
                continue;

            // 计算与当前敌人的距离
            Transform playerTrans = playerObj.transform;
            float distance = Vector2.Distance(
                GetPlayerCenterPosition(playerTrans),
                enemyPos
            );

            // 更新最近玩家
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = playerTrans;
            }
        }

        nearestPlayer = closestPlayer;
    }

    /// <summary>
    /// 强制追逐模式行为（直接追击最近玩家，忽略巡逻）
    /// </summary>
    private void HandleForceChaseBehavior()
    {
        if (nearestPlayer == null) return;

        // 定期生成路径
        pathGenerateTimer += Time.deltaTime;
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(transform.position, GetPlayerCenterPosition(nearestPlayer));
            pathGenerateTimer = 0;
        }

        float distanceToPlayer = Vector2.Distance(
            GetPlayerCenterPosition(nearestPlayer),
            transform.position
        );

        // 攻击范围内：停止移动并攻击
        if (distanceToPlayer <= attackDistance)
        {
            currentMovementDirection = Vector2.zero;
            OnMovementInput?.Invoke(Vector2.zero);

            if (isAttackReady)
            {
                isAttackReady = false;
                isAttacking = true;
                OnAttack?.Invoke();
                StartCoroutine(AttackCooldownCoroutine());
            }
            // 朝向最近玩家
            sr.flipX = GetPlayerCenterPosition(nearestPlayer).x > transform.position.x;
        }
        // 追逐范围内：移动到最近玩家
        else
        {
            if (pathPointList != null && pathPointList.Count > 0)
            {
                if (currentIndex >= pathPointList.Count)
                {
                    currentIndex = pathPointList.Count - 1;
                }

                // 向路径点移动
                Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                currentMovementDirection = direction;
                OnMovementInput?.Invoke(direction);
                sr.flipX = direction.x > 0;

                // 到达当前路径点后，切换到下一个
                if (Vector2.Distance(transform.position, pathPointList[currentIndex]) < 0.1f)
                {
                    currentIndex++;
                }
            }
        }
    }

    /// <summary>
    /// 追逐状态行为（追击最近玩家）
    /// </summary>
    private void HandleChaseBehavior(float distanceToPlayer)
    {
        if (nearestPlayer == null) return;

        AutoPath(); // 自动更新路径
        if (pathPointList == null) return;

        // 攻击范围：停止移动并攻击最近玩家
        if (distanceToPlayer <= attackDistance)
        {
            currentMovementDirection = Vector2.zero;
            OnMovementInput?.Invoke(Vector2.zero);

            if (isAttackReady)
            {
                isAttackReady = false;
                isAttacking = true;
                OnAttack?.Invoke();
                StartCoroutine(AttackCooldownCoroutine());
            }
            // 朝向最近玩家
            sr.flipX = GetPlayerCenterPosition(nearestPlayer).x > transform.position.x;
        }
        // 追逐范围：向最近玩家移动
        else
        {
            if (currentIndex >= 0 && currentIndex < pathPointList.Count)
            {
                Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                currentMovementDirection = direction;
                OnMovementInput?.Invoke(direction);
                sr.flipX = direction.x > 0;
            }
            else
            {
                currentIndex = 0;
            }
        }
    }

    /// <summary>
    /// 自动路径更新（追逐时以最近玩家为目标，巡逻时以巡逻点为目标）
    /// </summary>
    private void AutoPath()
    {
        if (nearestPlayer == null) return;

        pathGenerateTimer += Time.deltaTime;
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            // 目标：追逐时为最近玩家，巡逻时为巡逻点
            Vector3 target = isChasing
                ? GetPlayerCenterPosition(nearestPlayer)
                : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(transform.position, target);
            pathGenerateTimer = 0;
        }

        // 路径为空时重新生成
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            Vector3 target = isChasing
                ? GetPlayerCenterPosition(nearestPlayer)
                : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(transform.position, target);
        }
        // 到达当前路径点后切换到下一个
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
            {
                currentIndex = 0;
                if (!isChasing) // 巡逻时到达终点则等待
                {
                    isWaitingAtPoint = true;
                    waitTimer = 0f;
                }
            }
        }
    }

    /// <summary>
    /// 生成路径（使用A*寻路）
    /// </summary>
    private void GeneratePath(Vector3 start, Vector3 end)
    {
        currentIndex = 0;
        seeker.StartPath(start, end, OnPathGenerated);
    }

    /// <summary>
    /// 路径生成回调
    /// </summary>
    private void OnPathGenerated(Path path)
    {
        if (!path.error)
        {
            pathPointList = path.vectorPath;
        }
    }

    /// <summary>
    /// 巡逻行为（巡逻时仍会检测最近玩家，进入范围立即切换为追逐）
    /// </summary>
    private void Patrol()
    {
        if (isWaitingAtPoint)
        {
            // 在巡逻点等待
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaitingAtPoint = false;
                waitTimer = 0f;
                // 随机选择下一个巡逻点（不重复当前点）
                int nextIndex;
                do
                {
                    nextIndex = Random.Range(0, patrolPoints.Count);
                } while (nextIndex == currentPatrolPointIndex && patrolPoints.Count > 1);

                GeneratePath(patrolPoints[currentPatrolPointIndex].position, patrolPoints[nextIndex].position);
                currentPatrolPointIndex = nextIndex;
            }
            return;
        }

        // 路径为空时生成路径
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            int randomIndex = Random.Range(0, patrolPoints.Count);
            GeneratePath(patrolPoints[currentPatrolPointIndex].position, patrolPoints[randomIndex].position);
            return;
        }

        // 向巡逻点移动
        if (currentIndex >= 0 && currentIndex < pathPointList.Count)
        {
            Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
            currentMovementDirection = direction;
            OnMovementInput?.Invoke(direction);
            sr.flipX = direction.x > 0;
        }

        // 到达巡逻点后等待
        if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
            {
                currentIndex = 0;
                isWaitingAtPoint = true;
            }
        }
    }

    /// <summary>
    /// 攻击动画事件（实际造成伤害的逻辑）
    /// </summary>
    private void MeleeAttackEvent()
    {
        if (!PhotonNetwork.IsMasterClient || nearestPlayer == null) return;

        // 检测攻击范围内的玩家（确保是最近的玩家）
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            transform.position,
            attackDistance,
            playerLayer
        );
        foreach (Collider2D hit in hitColliders)
        {
            // 只对最近的玩家造成伤害
            if (hit.transform == nearestPlayer)
            {
                hit.GetComponent<HealthSystem>()?.RPC_TakeDamage(meleetAttackDamage);
                break;
            }
        }
    }

    /// <summary>
    /// 攻击冷却协程
    /// </summary>
    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);
        isAttackReady = true;
    }

    /// <summary>
    /// 攻击动画结束事件（更精准地重置攻击状态）
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }

    /// <summary>
    /// Gizmos绘制（辅助调试）
    /// </summary>
    public void OnDrawGizmosSelected()
    {
        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // 追逐范围
        if (!forceChaseMode)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseDistance);
        }

        // 巡逻路径
        if (shouldPatrol && patrolPoints.Count > 1)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] == null) continue;

                Gizmos.DrawSphere(patrolPoints[i].position, 0.2f);
                if (i < patrolPoints.Count - 1 && patrolPoints[i + 1] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                }
                else if (i == patrolPoints.Count - 1 && patrolPoints[0] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                }
            }
        }
    }

    /// <summary>
    /// 死亡RPC（网络同步死亡状态）
    /// </summary>
    [PunRPC]
    public override void DieRPC()
    {
        base.DieRPC();
        isAlive = false;
        if (PhotonNetwork.IsMasterClient && pickupSpawner != null)
        {
            pickupSpawner.DropItems();
        }
    }

    /// <summary>
    /// 获取玩家碰撞体中心位置（更精准的目标点）
    /// </summary>
    private Vector3 GetPlayerCenterPosition(Transform targetPlayer)
    {
        if (targetPlayer == null) return transform.position;

        Collider2D collider = targetPlayer.GetComponent<Collider2D>();
        return collider != null ? collider.bounds.center : targetPlayer.position;
    }

    /// <summary>
    /// Photon网络同步
    /// </summary>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 主机发送移动方向和攻击状态
            stream.SendNext(currentMovementDirection);
            stream.SendNext(isAttacking);
        }
        else
        {
            // 客户端接收同步数据
            networkMovementDirection = (Vector2)stream.ReceiveNext();
            networkIsAttacking = (bool)stream.ReceiveNext();
        }
    }

    /// <summary>
    /// 客户端应用同步状态
    /// </summary>
    private void ApplyNetworkState()
    {
        // 应用移动方向
        OnMovementInput?.Invoke(networkMovementDirection);

        // 同步朝向
        if (networkMovementDirection.x != 0)
        {
            sr.flipX = networkMovementDirection.x > 0;
        }

        // 同步动画
        if (animator != null)
        {
            animator.SetBool("IsMoving", networkMovementDirection != Vector2.zero);
            animator.SetBool("IsAttacking", networkIsAttacking);
        }
    }
}
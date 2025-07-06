using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;
using Photon.Pun;
using Photon.Realtime;

public class Enemy : Character, IPunObservable
{
    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnAttack;

    [Header("Chase Settings")]
    [SerializeField] private float chaseDistance = 3f; // 追击距离
    [SerializeField] private float attackDistance = 0.8f; // 攻击距离

    [Header("Patrol Settings")]
    [SerializeField] public bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float meleetAttackDamage; // 近战攻击伤害
    public LayerMask playerLayer; // 表示玩家图层
    public float AttackCooldownDuration = 2f; // 冷却时间

    [Header("Behavior Override")]
    public bool forceChaseMode = false;
    public bool publicMode = true;

    private Seeker seeker;
    private List<Vector3> pathPointList; // 路径点列表
    private int currentIndex = 0; // 路径点的索引
    private float pathGenerateInterval = 0.5f; // 每 0.5 秒生成一次路径
    private float pathGenerateTimer = 0f; // 计时器

    private Animator animator;
    private bool isAttack = true;
    private bool isChasing = false;
    private int currentPatrolPointIndex = 0;
    private bool isWaitingAtPoint = false;
    private float waitTimer = 0f;
    private SpriteRenderer sr;

    // 新增标志位，表示敌人是否存活
    private bool isAlive = true;
    // 新增标志位，表示当前是否在巡逻
    private bool isPatrolling = true;
    // 新增方向变量，1 表示正向， -1 表示反向
    private int patrolDirection = 1;

    // 新增：引用道具生成器
    public PickupSpawner pickupSpawner;

    // 玩家引用相关
    public Transform player;
    private float playerUpdateInterval = 2f; // 每2秒更新一次玩家引用
    private float playerUpdateTimer = 0f;

    // 网络同步相关
    private Vector2 networkMovementDirection;
    private bool networkIsAttacking;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // 初始化时查找玩家
        UpdatePlayerReference();
    }

    private void Start()
    {
        // 如果是主机，初始化巡逻路径
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

        // 定期更新玩家引用
        playerUpdateTimer += Time.deltaTime;
        if (playerUpdateTimer >= playerUpdateInterval)
        {
            UpdatePlayerReference();
            playerUpdateTimer = 0f;
        }

        // 只有主机执行AI逻辑
        if (PhotonNetwork.IsMasterClient)
        {
            if (player == null)
            {
                OnMovementInput?.Invoke(Vector2.zero);
                return;
            }

            if (forceChaseMode)
            {
                // 强制追击模式下，始终追击玩家
                HandleForceChaseBehavior();
                return;
            }

            if (publicMode)
            {
                float distanceToPlayer = Vector2.Distance(GetPlayerCenterPosition(), transform.position);

                // 检查玩家是否在追击范围内
                if (distanceToPlayer < chaseDistance)
                {
                    isChasing = true;
                    isPatrolling = false; // 停止巡逻
                    HandleChaseBehavior(distanceToPlayer);
                }
                else
                {
                    // 如果之前在追击，但现在玩家超出范围
                    if (isChasing)
                    {
                        isChasing = false; // 停止追击
                        pathPointList = null; // 清除当前路径

                        // 恢复巡逻状态
                        isPatrolling = shouldPatrol;
                        if (isPatrolling && patrolPoints.Count > 0)
                        {
                            currentPatrolPointIndex = 0; // 重置巡逻点索引
                            GeneratePath(transform.position, patrolPoints[currentPatrolPointIndex].position); // 生成巡逻路径
                        }
                    }

                    // 进行巡逻
                    if (isPatrolling && patrolPoints.Count > 0)
                    {
                        Patrol();
                    }
                    else
                    {
                        OnMovementInput?.Invoke(Vector2.zero); // 停止移动
                    }
                }
            }
        }
        else
        {
            // 客户端只执行表现逻辑
            ApplyNetworkState();
        }
    }

    private void UpdatePlayerReference()
    {
        // 查找本地玩家
        GameObject localPlayerObj = null;
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.IsLocal)
            {
                GameObject playerObj = PhotonView.Find(player.ActorNumber)?.gameObject;
                if (playerObj != null && playerObj.CompareTag("Player"))
                {
                    localPlayerObj = playerObj;
                    break;
                }
            }
        }

        if (localPlayerObj != null)
        {
            player = localPlayerObj.transform;
        }
        else
        {
            // 如果找不到本地玩家，尝试查找任意玩家
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                player = null;
                Debug.LogWarning("找不到玩家对象！");
            }
        }
    }

    private void HandleForceChaseBehavior()
    {
        // 强制追击模式下，始终生成路径追击玩家
        pathGenerateTimer += Time.deltaTime;
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(transform.position, GetPlayerCenterPosition());
            pathGenerateTimer = 0;
        }

        float distanceToPlayer = Vector2.Distance(GetPlayerCenterPosition(), transform.position);

        if (distanceToPlayer <= attackDistance)
        {
            // 停止移动并进行攻击
            OnMovementInput?.Invoke(Vector2.zero);
            if (isAttack)
            {
                isAttack = false;
                OnAttack?.Invoke();
                StartCoroutine(nameof(AttackCooldownCoroutine));
            }

            // 根据玩家位置翻转精灵
            sr.flipX = GetPlayerCenterPosition().x > transform.position.x;
        }
        else
        {
            // 追击玩家
            if (pathPointList != null && pathPointList.Count > 0)
            {
                // 找到最近的路径点
                if (currentIndex >= pathPointList.Count)
                {
                    currentIndex = pathPointList.Count - 1;
                }

                Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                OnMovementInput?.Invoke(direction);

                // 根据移动方向翻转精灵
                sr.flipX = direction.x > 0;

                // 检查是否到达当前路径点
                if (Vector2.Distance(transform.position, pathPointList[currentIndex]) < 0.1f)
                {
                    currentIndex++;
                }
            }
        }
    }

    private void HandleChaseBehavior(float distanceToPlayer)
    {
        AutoPath();
        if (pathPointList == null)
            return;

        if (distanceToPlayer <= attackDistance)
        {
            // 停止移动并进行攻击
            OnMovementInput?.Invoke(Vector2.zero);
            if (isAttack)
            {
                isAttack = false;
                OnAttack?.Invoke();
                StartCoroutine(nameof(AttackCooldownCoroutine));
            }

            // 根据玩家位置翻转精灵
            sr.flipX = GetPlayerCenterPosition().x > transform.position.x;
        }
        else
        {
            // Chase player
            if (currentIndex >= 0 && currentIndex < pathPointList.Count)
            {
                Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                OnMovementInput?.Invoke(direction);
            }
            else
            {
                currentIndex = 0;
            }
        }
    }

    private void AutoPath()
    {
        pathGenerateTimer += Time.deltaTime;

        // 间隔一定时间来获取路径点
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            Vector3 target = isChasing ? GetPlayerCenterPosition() : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(transform.position, target);
            pathGenerateTimer = 0; // 重置计时器
        }

        // 当路径点列表为空时，进行路径计算
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            Vector3 target = isChasing ? GetPlayerCenterPosition() : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(transform.position, target);
        }
        // 当敌人到达当前路径点时，递增索引 currentIndex 并进行路径计算
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
            {
                currentIndex = 0;
                // 如果不是在追击状态，则设置为等待状态
                if (!isChasing)
                {
                    isWaitingAtPoint = true;
                    waitTimer = 0f;
                }
            }
        }
    }

    // 获取路径点，重载方法，接受起点和终点
    private void GeneratePath(Vector3 start, Vector3 end)
    {
        currentIndex = 0;
        seeker.StartPath(start, end, Path =>
        {
            pathPointList = Path.vectorPath;
        });
    }

    private void Patrol()
    {
        if (isWaitingAtPoint)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaitingAtPoint = false;
                waitTimer = 0f;
                MoveToNextPatrolPoint(); // 移动到下一个巡逻点

                // 随机选择下一个路径点
                int nextIndex;
                do
                {
                    nextIndex = Random.Range(0, patrolPoints.Count);
                } while (nextIndex == currentPatrolPointIndex);

                GeneratePath(patrolPoints[currentPatrolPointIndex].position, patrolPoints[nextIndex].position);
                currentPatrolPointIndex = nextIndex;
            }
            return;
        }

        // 如果路径点列表为空，生成路径
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            // 随机选择路径点
            int randomIndex = Random.Range(0, patrolPoints.Count);
            GeneratePath(patrolPoints[currentPatrolPointIndex].position, patrolPoints[randomIndex].position);
            return;
        }

        // 移动到当前路径点
        if (currentIndex >= 0 && currentIndex < pathPointList.Count)
        {
            Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
            OnMovementInput?.Invoke(direction);

            // 根据移动方向翻转精灵
            sr.flipX = direction.x > 0;
        }

        // 检查是否到达当前路径点
        if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            // 如果到达路径末尾，切换到下一个巡逻点
            if (currentIndex >= pathPointList.Count)
            {
                currentIndex = 0; // 重置索引
                isWaitingAtPoint = true; // 设置为等待状态
            }
        }
    }

    private void MoveToNextPatrolPoint()
    {
        currentPatrolPointIndex += patrolDirection;

        // 到达路径终点，改变方向
        if (currentPatrolPointIndex >= patrolPoints.Count)
        {
            currentPatrolPointIndex = patrolPoints.Count - 1; // 保持在最后一个点
            patrolDirection = -1; // 反向巡逻
        }
        // 到达路径起点，改变方向
        else if (currentPatrolPointIndex <= 0)
        {
            currentPatrolPointIndex = 0; // 保持在第一个点
            patrolDirection = 1; // 正向巡逻
        }
    }

    private void MeleeAttackEvent()
    {
        // 只有主机执行攻击判定
        if (!PhotonNetwork.IsMasterClient)
            return;

        // 检测碰撞
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(GetPlayerCenterPosition(), attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<HealthSystem>()?.RPC_TakeDamage(meleetAttackDamage);
        }
    }

    // 攻击冷却时间
    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration); // 等待冷却时间
        isAttack = true; // 重置攻击状态
    }

    public void OnDrawGizmosSelected()
    {
        // 攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // 追击范围（仅在非强制追击模式下显示）
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

    [PunRPC]
    public override void DieRPC()
    {
        // 先执行基类死亡逻辑
        base.DieRPC();

        // 敌人特有逻辑
        isAlive = false;

        if (PhotonNetwork.IsMasterClient && pickupSpawner != null)
        {
            pickupSpawner.DropItems();
        }
    }

    // 获取玩家的中心位置
    private Vector3 GetPlayerCenterPosition()
    {
        if (player == null)
            return transform.position;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            return playerCollider.bounds.center;
        }
        return player.position;
    }

    // 网络同步实现
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 主机发送移动和攻击状态
            stream.SendNext(OnMovementInput.GetPersistentEventCount() > 0 ? OnMovementInput.GetPersistentTarget(0) : Vector2.zero);
            stream.SendNext(isAttack);
        }
        else
        {
            // 客户端接收状态
            networkMovementDirection = (Vector2)stream.ReceiveNext();
            networkIsAttacking = (bool)stream.ReceiveNext();
        }
    }

    private void ApplyNetworkState()
    {
        // 应用网络同步的移动和攻击状态
        if (OnMovementInput.GetPersistentEventCount() > 0)
        {
            OnMovementInput?.Invoke(networkMovementDirection);
        }

        // 根据移动方向翻转精灵
        if (networkMovementDirection.x != 0)
        {
            sr.flipX = networkMovementDirection.x > 0;
        }

        // 同步攻击动画
        if (animator != null)
        {
            animator.SetBool("IsAttacking", !networkIsAttacking);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;

public class LongRangeEnemy : Character
{
    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnAttack;

    [Header("Chase Settings")]
    [SerializeField] public Transform player;
    [SerializeField] private float chaseDistance = 3f; // 追击距离
    [SerializeField] private float attackDistance = 3f; // 远程攻击距离

    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float rangedAttackDamage; // 远程攻击伤害
    public LayerMask playerLayer; // 表示玩家图层
    public float fireRate = 1f; // 射击频率，每秒发射的子弹数
    public GameObject enemyBulletPrefab; // 敌人子弹预制体
    public float bulletSpeed = 10f; // 子弹速度

    [Header("Bullet Pool Settings")]
    [SerializeField] private int initialPoolSize = 20; // 初始池大小
    [SerializeField] private int poolExpandAmount = 5; // 池不足时每次扩展数量

    [Header("Bullet Spawn Point")]
    [SerializeField] private Transform bulletSpawnPoint; // 新增：子弹发射点

    private Seeker seeker;
    private List<Vector3> pathPointList; // 路径点列表
    private int currentIndex = 0; // 路径点的索引
    private float pathGenerateInterval = 0.5f; // 每 0.5 秒生成一次路径
    private float pathGenerateTimer = 0f; // 计时器
    private float fireTimer = 0f; // 射击计时器

    private Animator animator;
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

    // 每个敌人拥有自己的子弹池
    private EnemyBulletPool bulletPool;
    // 新增：引用道具生成器
    public PickupSpawner pickupSpawner;

    private void Start()
    {
        // 初始化子弹池
        InitializeBulletPool();
        if (shouldPatrol && patrolPoints.Count > 0)
        {
            // 随机选择开始路径点和终点
            int startIndex = Random.Range(0, patrolPoints.Count);
            int endIndex;
            do
            {
                endIndex = Random.Range(0, patrolPoints.Count);
            } while (endIndex == startIndex);

            GeneratePath(patrolPoints[startIndex].position, patrolPoints[endIndex].position);
            currentPatrolPointIndex = startIndex;
        }
    }

    private void InitializeBulletPool()
    {
        GameObject poolObj = new GameObject("BulletPool");
        poolObj.transform.parent = transform;
        bulletPool = poolObj.AddComponent<EnemyBulletPool>();
        bulletPool.bulletPrefab = enemyBulletPrefab;
        bulletPool.poolSize = initialPoolSize;
        bulletPool.InitializePool();
    }

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // 查找带有 "Player" 标签的游戏对象并赋值给 player 变量
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("找不到带有 'Player' 标签的游戏对象！");
        }
    }

    private void Update()
    {
        // 如果敌人死亡或者玩家不存在，不执行后续逻辑
        if (!isAlive || player == null)
            return;

        float distanceToPlayer = Vector2.Distance(player.position, transform.position);

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
                    // 随机选择开始路径点和终点
                    int startIndex = Random.Range(0, patrolPoints.Count);
                    int endIndex;
                    do
                    {
                        endIndex = Random.Range(0, patrolPoints.Count);
                    } while (endIndex == startIndex);

                    GeneratePath(patrolPoints[startIndex].position, patrolPoints[endIndex].position);
                    currentPatrolPointIndex = startIndex;
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

    private void HandleChaseBehavior(float distanceToPlayer)
    {
        // 如果敌人死亡，不执行后续逻辑
        if (!isAlive)
            return;

        AutoPath();
        if (pathPointList == null)
            return;

        if (distanceToPlayer <= attackDistance)
        {
            // 停止移动并进行攻击
            OnMovementInput?.Invoke(Vector2.zero);
            Fire();

            // Flip sprite based on player position
            float x = player.position.x - transform.position.x;
            if (x > 0)
            {
                if (!sr.flipX)
                {
                    sr.flipX = true;
                    FlipBulletSpawnPoint();
                }
            }
            else
            {
                if (sr.flipX)
                {
                    sr.flipX = false;
                    FlipBulletSpawnPoint();
                }
            }
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

    // 自动寻路
    private void AutoPath()
    {
        // 如果敌人死亡，不执行后续逻辑
        if (!isAlive)
            return;

        pathGenerateTimer += Time.deltaTime;

        // 间隔一定时间来获取路径点
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            Vector3 target = isChasing ? player.position : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(target);
            pathGenerateTimer = 0; // 重置计时器
        }

        // 当路径点列表为空时，进行路径计算
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            Vector3 target = isChasing ? player.position : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(target);
        }
        // 当敌人到达当前路径点时，递增索引 currentIndex 并进行路径计算
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
            {
                if (isChasing)
                {
                    GeneratePath(player.position);
                }
                else
                {
                    if (Vector2.Distance(transform.position, patrolPoints[currentPatrolPointIndex].position) <= patrolPointReachedDistance)
                    {
                        isWaitingAtPoint = true;
                        waitTimer = 0f;
                    }
                    else
                    {
                        GeneratePath(patrolPoints[currentPatrolPointIndex].position);
                    }
                }
                currentIndex = 0;
            }
        }
    }

    // 获取路径点
    private void GeneratePath(Vector3 target)
    {
        // 如果敌人死亡，不执行后续逻辑
        if (!isAlive)
            return;

        currentIndex = 0;
        // 三个参数：起点、终点、回调函数
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath; // Path.vectorPath 包含了从起点到终点的完整路径
        });
    }

    // 重载 GeneratePath 方法，接受起点和终点
    private void GeneratePath(Vector3 start, Vector3 end)
    {
        // 如果敌人死亡，不执行后续逻辑
        if (!isAlive)
            return;

        currentIndex = 0;
        seeker.StartPath(start, end, Path =>
        {
            pathPointList = Path.vectorPath;
        });
    }

    private void Patrol()
    {
        // 如果敌人死亡，不执行后续逻辑
        if (!isAlive) return;

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
            if (direction.x > 0 && !sr.flipX)
            {
                sr.flipX = true;
                FlipBulletSpawnPoint();
            }
            else if (direction.x < 0 && sr.flipX)
            {
                sr.flipX = false;
                FlipBulletSpawnPoint();
            }
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
        // 如果敌人死亡，不执行后续逻辑
        if (!isAlive) return;

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

    private void Fire()
    {
        // 如果敌人死亡，不执行后续逻辑
        if (!isAlive)
            return;

        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / fireRate)
        {
            fireTimer = 0f;
            OnAttack?.Invoke();
            ShootBullet();
        }
    }

    // 发射子弹的方法
    private void ShootBullet()
    {
        if (!isAlive || enemyBulletPrefab == null || bulletPool == null)
            return;

        // 确保玩家位置是准确的中心位置
        Vector3 playerCenter = player.position;

        // 考虑玩家的碰撞体中心（如果有必要）
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerCenter = playerCollider.bounds.center;
        }

        Vector2 direction = ((Vector2)playerCenter - (Vector2)bulletSpawnPoint.position).normalized;

        // 使用对象池获取子弹
        GameObject bullet = bulletPool.GetBullet();
        bullet.transform.position = bulletSpawnPoint.position;

        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Setup(rangedAttackDamage, playerLayer, bulletSpeed, direction);
        }
    }

    private void FlipBulletSpawnPoint()
    {
        if (bulletSpawnPoint != null)
        {
            bulletSpawnPoint.localPosition = new Vector3(-bulletSpawnPoint.localPosition.x, bulletSpawnPoint.localPosition.y, bulletSpawnPoint.localPosition.z);
        }
    }

    public void OnDrawGizmosSelected()
    {
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Chase range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        // Patrol path
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

    // 重写 Die 方法，在敌人死亡时更新标志位
    public override void Die()
    {
        base.Die();
        isAlive = false;
        // 可以在这里添加播放死亡动画等逻辑
        if (pickupSpawner != null)
        {
            pickupSpawner.DropItems();
        }
    }
}    
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;
using Photon.Pun;

public class LongRangeEnemy : Character
{
    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnAttack;

    [Header("Chase Settings")]
    [SerializeField] public Transform player;
    [SerializeField] private float chaseDistance = 3f;
    [SerializeField] private float attackDistance = 3f;

    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float rangedAttackDamage;
    public LayerMask playerLayer;
    public float fireRate = 1f;
    public GameObject enemyBulletPrefab;
    public float bulletSpeed = 10f;

    [Header("Bullet Pool Settings")]
    [SerializeField] private int initialPoolSize = 20;

    [Header("Bullet Spawn Point")]
    [SerializeField] private Transform bulletSpawnPoint;

    private Seeker seeker;
    private List<Vector3> pathPointList;
    private int currentIndex = 0;
    private float pathGenerateInterval = 0.5f;
    private float pathGenerateTimer = 0f;
    private float fireTimer = 0f;

    private Animator animator;
    private bool isChasing = false;
    private int currentPatrolPointIndex = 0;
    private bool isWaitingAtPoint = false;
    private float waitTimer = 0f;
    private SpriteRenderer sr;

    private bool isAlive = true;
    private bool isPatrolling = true;
    private int patrolDirection = 1;

    private EnemyBulletPool bulletPool;
    public PickupSpawner pickupSpawner;

    protected override void Awake()
    {
        base.Awake(); // 调用基类的Awake方法

        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // 设置敌人标识
        isEnemy = true;

        // 初始化子弹池
        InitializeBulletPool();

        // 只在主客户端查找玩家
        if (PhotonNetwork.IsMasterClient)
        {
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
    }

    private void Start()
    {
        // 只在主客户端初始化巡逻路径
        if (PhotonNetwork.IsMasterClient && shouldPatrol && patrolPoints.Count > 0)
        {
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
        // 子弹池应该在所有客户端都存在
        GameObject poolObj = new GameObject("BulletPool");
        poolObj.transform.parent = transform;
        bulletPool = poolObj.AddComponent<EnemyBulletPool>();
        bulletPool.bulletPrefab = enemyBulletPrefab;
        bulletPool.poolSize = initialPoolSize;
        bulletPool.InitializePool();
    }

    private void Update()
    {
        // 只在主客户端运行AI逻辑
        if (!PhotonNetwork.IsMasterClient) return;
        if (!isAlive || player == null) return;

        float distanceToPlayer = Vector2.Distance(player.position, transform.position);

        if (distanceToPlayer < chaseDistance)
        {
            isChasing = true;
            isPatrolling = false;
            HandleChaseBehavior(distanceToPlayer);
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                pathPointList = null;

                isPatrolling = shouldPatrol;
                if (isPatrolling && patrolPoints.Count > 0)
                {
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

            if (isPatrolling && patrolPoints.Count > 0)
            {
                Patrol();
            }
            else
            {
                OnMovementInput?.Invoke(Vector2.zero);
            }
        }
    }

    private void HandleChaseBehavior(float distanceToPlayer)
    {
        if (!isAlive) return;

        AutoPath();
        if (pathPointList == null) return;

        if (distanceToPlayer <= attackDistance)
        {
            OnMovementInput?.Invoke(Vector2.zero);
            Fire();

            float x = player.position.x - transform.position.x;
            bool shouldFlip = x > 0;

            // 如果方向需要改变，同步到所有客户端
            if (shouldFlip != sr.flipX)
            {
                photonView.RPC("RPC_UpdateFlip", RpcTarget.All, shouldFlip);
            }
        }
        else
        {
            if (currentIndex >= 0 && currentIndex < pathPointList.Count)
            {
                Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                OnMovementInput?.Invoke(direction);

                // 同步移动方向
                photonView.RPC("RPC_UpdateFlip", RpcTarget.All, direction.x > 0);
            }
            else
            {
                currentIndex = 0;
            }
        }
    }

    private void AutoPath()
    {
        if (!isAlive) return;

        pathGenerateTimer += Time.deltaTime;

        if (pathGenerateTimer >= pathGenerateInterval)
        {
            Vector3 target = isChasing ? player.position : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(target);
            pathGenerateTimer = 0;
        }

        if (pathPointList == null || pathPointList.Count <= 0)
        {
            Vector3 target = isChasing ? player.position : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(target);
        }
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

    private void GeneratePath(Vector3 target)
    {
        if (!isAlive) return;

        currentIndex = 0;
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath;
        });
    }

    private void GeneratePath(Vector3 start, Vector3 end)
    {
        if (!isAlive) return;

        currentIndex = 0;
        seeker.StartPath(start, end, Path =>
        {
            pathPointList = Path.vectorPath;
        });
    }

    private void Patrol()
    {
        if (!isAlive) return;

        if (isWaitingAtPoint)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaitingAtPoint = false;
                waitTimer = 0f;
                MoveToNextPatrolPoint();
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

        if (pathPointList == null || pathPointList.Count <= 0)
        {
            int randomIndex = Random.Range(0, patrolPoints.Count);
            GeneratePath(patrolPoints[currentPatrolPointIndex].position, patrolPoints[randomIndex].position);
            return;
        }

        if (currentIndex >= 0 && currentIndex < pathPointList.Count)
        {
            Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
            OnMovementInput?.Invoke(direction);

            // 同步移动方向
            photonView.RPC("RPC_UpdateFlip", RpcTarget.All, direction.x > 0);
        }

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

    private void MoveToNextPatrolPoint()
    {
        if (!isAlive) return;

        currentPatrolPointIndex += patrolDirection;

        if (currentPatrolPointIndex >= patrolPoints.Count)
        {
            currentPatrolPointIndex = patrolPoints.Count - 1;
            patrolDirection = -1;
        }
        else if (currentPatrolPointIndex <= 0)
        {
            currentPatrolPointIndex = 0;
            patrolDirection = 1;
        }
    }

    private void Fire()
    {
        if (!isAlive) return;

        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / fireRate)
        {
            fireTimer = 0f;
            OnAttack?.Invoke();

            // 同步攻击动作
            photonView.RPC("RPC_ShootBullet", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_ShootBullet()
    {
        // 所有客户端播放攻击动画
        animator.SetTrigger("Attack");

        // 只在主客户端实际发射子弹（避免重复生成）
        if (PhotonNetwork.IsMasterClient)
        {
            ShootBullet();
        }
    }

    private void ShootBullet()
    {
        if (!isAlive || enemyBulletPrefab == null || bulletPool == null)
            return;

        Vector3 playerCenter = player.position;
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerCenter = playerCollider.bounds.center;
        }

        Vector2 direction = ((Vector2)playerCenter - (Vector2)bulletSpawnPoint.position).normalized;

        GameObject bullet = bulletPool.GetBullet();
        bullet.transform.position = bulletSpawnPoint.position;

        EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
        if (bulletScript != null)
        {
            bulletScript.Setup(rangedAttackDamage, playerLayer, bulletSpeed, direction);
            bulletScript.photonView.RPC("RPC_Initialize", RpcTarget.All, direction, bulletSpeed);
        }
    }

    [PunRPC]
    private void RPC_UpdateFlip(bool flipRight)
    {
        sr.flipX = flipRight;
        FlipBulletSpawnPoint();
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

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

    // 重写RPC_Die方法
    [PunRPC]
    public override void RPC_Die()
    {
        base.RPC_Die(); // 调用基类死亡逻辑
        isAlive = false;

        // 只在主客户端生成掉落物
        if (PhotonNetwork.IsMasterClient && pickupSpawner != null)
        {
            pickupSpawner.DropItems();
        }

        // 销毁敌人对象
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
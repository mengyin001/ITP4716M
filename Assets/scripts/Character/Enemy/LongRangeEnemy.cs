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
    [SerializeField] private float chaseDistance = 3f; // ׷������
    [SerializeField] private float attackDistance = 3f; // Զ�̹�������

    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float rangedAttackDamage; // Զ�̹����˺�
    public LayerMask playerLayer; // ��ʾ���ͼ��
    public float fireRate = 1f; // ���Ƶ�ʣ�ÿ�뷢����ӵ���
    public GameObject enemyBulletPrefab; // �����ӵ�Ԥ����
    public float bulletSpeed = 10f; // �ӵ��ٶ�

    [Header("Bullet Pool Settings")]
    [SerializeField] private int initialPoolSize = 20; // ��ʼ�ش�С
    [SerializeField] private int poolExpandAmount = 5; // �ز���ʱÿ����չ����

    [Header("Bullet Spawn Point")]
    [SerializeField] private Transform bulletSpawnPoint; // �������ӵ������

    private Seeker seeker;
    private List<Vector3> pathPointList; // ·�����б�
    private int currentIndex = 0; // ·���������
    private float pathGenerateInterval = 0.5f; // ÿ 0.5 ������һ��·��
    private float pathGenerateTimer = 0f; // ��ʱ��
    private float fireTimer = 0f; // �����ʱ��

    private Animator animator;
    private bool isChasing = false;
    private int currentPatrolPointIndex = 0;
    private bool isWaitingAtPoint = false;
    private float waitTimer = 0f;
    private SpriteRenderer sr;

    // ������־λ����ʾ�����Ƿ���
    private bool isAlive = true;
    // ������־λ����ʾ��ǰ�Ƿ���Ѳ��
    private bool isPatrolling = true;
    // �������������1 ��ʾ���� -1 ��ʾ����
    private int patrolDirection = 1;

    // ÿ������ӵ���Լ����ӵ���
    private EnemyBulletPool bulletPool;
    // ���������õ���������
    public PickupSpawner pickupSpawner;

    private void Start()
    {
        // ��ʼ���ӵ���
        InitializeBulletPool();
        if (shouldPatrol && patrolPoints.Count > 0)
        {
            // ���ѡ��ʼ·������յ�
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

        // ���Ҵ��� "Player" ��ǩ����Ϸ���󲢸�ֵ�� player ����
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("�Ҳ������� 'Player' ��ǩ����Ϸ����");
        }
    }

    private void Update()
    {
        // �����������������Ҳ����ڣ���ִ�к����߼�
        if (!isAlive || player == null)
            return;

        float distanceToPlayer = Vector2.Distance(player.position, transform.position);

        // �������Ƿ���׷����Χ��
        if (distanceToPlayer < chaseDistance)
        {
            isChasing = true;
            isPatrolling = false; // ֹͣѲ��
            HandleChaseBehavior(distanceToPlayer);
        }
        else
        {
            // ���֮ǰ��׷������������ҳ�����Χ
            if (isChasing)
            {
                isChasing = false; // ֹͣ׷��
                pathPointList = null; // �����ǰ·��

                // �ָ�Ѳ��״̬
                isPatrolling = shouldPatrol;
                if (isPatrolling && patrolPoints.Count > 0)
                {
                    // ���ѡ��ʼ·������յ�
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

            // ����Ѳ��
            if (isPatrolling && patrolPoints.Count > 0)
            {
                Patrol();
            }
            else
            {
                OnMovementInput?.Invoke(Vector2.zero); // ֹͣ�ƶ�
            }
        }
    }

    private void HandleChaseBehavior(float distanceToPlayer)
    {
        // ���������������ִ�к����߼�
        if (!isAlive)
            return;

        AutoPath();
        if (pathPointList == null)
            return;

        if (distanceToPlayer <= attackDistance)
        {
            // ֹͣ�ƶ������й���
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

    // �Զ�Ѱ·
    private void AutoPath()
    {
        // ���������������ִ�к����߼�
        if (!isAlive)
            return;

        pathGenerateTimer += Time.deltaTime;

        // ���һ��ʱ������ȡ·����
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            Vector3 target = isChasing ? player.position : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(target);
            pathGenerateTimer = 0; // ���ü�ʱ��
        }

        // ��·�����б�Ϊ��ʱ������·������
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            Vector3 target = isChasing ? player.position : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(target);
        }
        // �����˵��ﵱǰ·����ʱ���������� currentIndex ������·������
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

    // ��ȡ·����
    private void GeneratePath(Vector3 target)
    {
        // ���������������ִ�к����߼�
        if (!isAlive)
            return;

        currentIndex = 0;
        // ������������㡢�յ㡢�ص�����
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath; // Path.vectorPath �����˴���㵽�յ������·��
        });
    }

    // ���� GeneratePath ���������������յ�
    private void GeneratePath(Vector3 start, Vector3 end)
    {
        // ���������������ִ�к����߼�
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
        // ���������������ִ�к����߼�
        if (!isAlive) return;

        if (isWaitingAtPoint)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaitingAtPoint = false;
                waitTimer = 0f;
                MoveToNextPatrolPoint(); // �ƶ�����һ��Ѳ�ߵ�
                // ���ѡ����һ��·����
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

        // ���·�����б�Ϊ�գ�����·��
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            // ���ѡ��·����
            int randomIndex = Random.Range(0, patrolPoints.Count);
            GeneratePath(patrolPoints[currentPatrolPointIndex].position, patrolPoints[randomIndex].position);
            return;
        }

        // �ƶ�����ǰ·����
        if (currentIndex >= 0 && currentIndex < pathPointList.Count)
        {
            Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
            OnMovementInput?.Invoke(direction);

            // �����ƶ�����ת����
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

        // ����Ƿ񵽴ﵱǰ·����
        if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            // �������·��ĩβ���л�����һ��Ѳ�ߵ�
            if (currentIndex >= pathPointList.Count)
            {
                currentIndex = 0; // ��������
                isWaitingAtPoint = true; // ����Ϊ�ȴ�״̬
            }
        }
    }


    private void MoveToNextPatrolPoint()
    {
        // ���������������ִ�к����߼�
        if (!isAlive) return;

        currentPatrolPointIndex += patrolDirection;

        // ����·���յ㣬�ı䷽��
        if (currentPatrolPointIndex >= patrolPoints.Count)
        {
            currentPatrolPointIndex = patrolPoints.Count - 1; // ���������һ����
            patrolDirection = -1; // ����Ѳ��
        }
        // ����·����㣬�ı䷽��
        else if (currentPatrolPointIndex <= 0)
        {
            currentPatrolPointIndex = 0; // �����ڵ�һ����
            patrolDirection = 1; // ����Ѳ��
        }
    }

    private void Fire()
    {
        // ���������������ִ�к����߼�
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

    // �����ӵ��ķ���
    private void ShootBullet()
    {
        if (!isAlive || enemyBulletPrefab == null || bulletPool == null)
            return;

        // ȷ�����λ����׼ȷ������λ��
        Vector3 playerCenter = player.position;

        // ������ҵ���ײ�����ģ�����б�Ҫ��
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerCenter = playerCollider.bounds.center;
        }

        Vector2 direction = ((Vector2)playerCenter - (Vector2)bulletSpawnPoint.position).normalized;

        // ʹ�ö���ػ�ȡ�ӵ�
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

    // ��д Die �������ڵ�������ʱ���±�־λ
    public override void Die()
    {
        base.Die();
        isAlive = false;
        // ������������Ӳ��������������߼�
        if (pickupSpawner != null)
        {
            pickupSpawner.DropItems();
        }
    }
}    
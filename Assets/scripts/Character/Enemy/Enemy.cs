using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;

public class Enemy : Character
{

    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnAttack;

    [Header("Chase Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseDistance = 3f;//追击距离
    [SerializeField] private float attackDistance = 0.8f;//攻击距离

    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float patrolSpeed = 1f;
    [SerializeField] private float waitTimeAtPoint = 1f;



    [Header("Attack Settings")]
    public float meleetAttackDamage;//近战攻击伤害
    public LayerMask playerLayer;//表示玩家图层
    public float AttackCooldownDuration = 2f;//冷却时间

    private Seeker seeker;
    private List<Vector3> pathPointList;//路径点列表
    private int currentIndex = 0;//路径点的索引
    private float pathGenerateInterval = 0.5f; //每0.5秒生成一次路径
    private float pathGenerateTimer = 0f;//计时器

    private Animator animator;
    private bool isDead = true;
    private bool isAttack = true;
    private bool isChasing = false;
    private int currentPatrolPointIndex = 0;
    private bool isWaitingAtPoint = false;
    private float waitTimer = 0f;
    private SpriteRenderer sr;

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distanceToPlayer = Vector2.Distance(player.position, transform.position);

        // Check if player is within chase distance
        if (distanceToPlayer < chaseDistance)
        {
            isChasing = true;
            HandleChaseBehavior(distanceToPlayer);
        }
        else
        {
            // If we were chasing but player is now out of range
            if (isChasing)
            {
                isChasing = false;
                OnMovementInput?.Invoke(Vector2.zero);
                pathPointList = null; // Clear path
            }

            // Patrol if enabled
            if (shouldPatrol && patrolPoints.Count > 0)
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
        AutoPath();
        if (pathPointList == null)
            return;

        if (distanceToPlayer <= attackDistance)
        {
            // Attack player
            OnMovementInput?.Invoke(Vector2.zero);
            if (isAttack)
            {
                isAttack = false;
                OnAttack?.Invoke();
                StartCoroutine(nameof(AttackCooldownCoroutine));
            }

            // Flip sprite based on player position
            float x = player.position.x - transform.position.x;
            sr.flipX = x > 0;
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
    //自动寻路
    private void AutoPath()
    {
        pathGenerateTimer += Time.deltaTime;

        //间隔一定时间来获取路径点
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(player.position);
            pathGenerateTimer = 0;//重置计时器
        }


        //当路径点列表为空时，进行路径计算
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            GeneratePath(player.position);
        }//当敌人到达当前路径点时，递增索引currentIndex并进行路径计算
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
                GeneratePath(player.position);
        }
    }

    //获取路径点
    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        //三个参数：起点、终点、回调函数
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath;//Path.vectorPath包含了从起点到终点的完整路径
        });
    }
    //敌人近战攻击

    private void Patrol()
    {
        if (isWaitingAtPoint)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaitingAtPoint = false;
                waitTimer = 0f;
                MoveToNextPatrolPoint();

            }
            return;
        }

        Transform currentPatrolPoint = patrolPoints[currentPatrolPointIndex];
        float distanceToPoint = Vector2.Distance(transform.position, currentPatrolPoint.position);

        if (distanceToPoint <= patrolPointReachedDistance)
        {
            // Reached patrol point
            isWaitingAtPoint = true;
            OnMovementInput?.Invoke(Vector2.zero);

        }
        else
        {
            // Move toward patrol point
            Vector2 direction = (currentPatrolPoint.position - transform.position).normalized;
            OnMovementInput?.Invoke(direction * patrolSpeed);


            // Flip sprite based on movement direction
            sr.flipX = direction.x > 0;
        }
    }

    private void MoveToNextPatrolPoint()
    {
        currentPatrolPointIndex++;
        if (currentPatrolPointIndex >= patrolPoints.Count)
        {
            currentPatrolPointIndex = 0;
        }
    }

    private void MeleeAttackEvent()
    {
        //检测碰撞
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<PlayerHealth>().TakeDamage(meleetAttackDamage);
        }
    }
    //攻击冷却时间

    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);// 等待冷却时间
        isAttack = true;// 重置攻击状态
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
}
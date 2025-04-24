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
    [SerializeField] public Transform player;
    [SerializeField] private float chaseDistance = 3f; // 追击距离
    [SerializeField] private float attackDistance = 0.8f; // 攻击距离

    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float meleetAttackDamage; // 近战攻击伤害
    public LayerMask playerLayer; // 表示玩家图层
    public float AttackCooldownDuration = 2f; // 冷却时间

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

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
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
        if (!isAlive || player == null)
            return;

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
                    GeneratePath(patrolPoints[currentPatrolPointIndex].position); // 生成巡逻路径
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
            GeneratePath(target);
            pathGenerateTimer = 0; // 重置计时器
        }

        // 当路径点列表为空时，进行路径计算
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            Vector3 target = isChasing ? GetPlayerCenterPosition() : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(target);
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

    // 获取路径点
    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath; // Path.vectorPath 包含了从起点到终点的完整路径
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
                GeneratePath(patrolPoints[currentPatrolPointIndex].position); // 生成新的路径
            }
            return;
        }

        // 如果路径点列表为空，生成路径
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            GeneratePath(patrolPoints[currentPatrolPointIndex].position);
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
        // 检测碰撞
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(GetPlayerCenterPosition(), attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<HealthSystem>().TakeDamage(meleetAttackDamage);
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

        // 追击范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

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

    // 重写 Die 方法，在敌人死亡时更新标志位
    public override void Die()
    {
        base.Die();
        isAlive = false;
        // 检查是否有道具生成器并调用 DropItems 方法
        if (pickupSpawner != null)
        {
            pickupSpawner.DropItems();
        }
        // 可以在这里添加播放死亡动画等逻辑
    }

    // 获取玩家的中心位置
    private Vector3 GetPlayerCenterPosition()
    {
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            return playerCollider.bounds.center;
        }
        return player.position;
    }
}
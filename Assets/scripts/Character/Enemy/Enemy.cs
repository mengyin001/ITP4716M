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
    [SerializeField] private float chaseDistance = 3f; // ׷������
    [SerializeField] private float attackDistance = 0.8f; // ��������

    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float meleetAttackDamage; // ��ս�����˺�
    public LayerMask playerLayer; // ��ʾ���ͼ��
    public float AttackCooldownDuration = 2f; // ��ȴʱ��

    private Seeker seeker;
    private List<Vector3> pathPointList; // ·�����б�
    private int currentIndex = 0; // ·���������
    private float pathGenerateInterval = 0.5f; // ÿ 0.5 ������һ��·��
    private float pathGenerateTimer = 0f; // ��ʱ��

    private Animator animator;
    private bool isAttack = true;
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

    private void Awake()
    {
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
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
                    currentPatrolPointIndex = 0; // ����Ѳ�ߵ�����
                    GeneratePath(patrolPoints[currentPatrolPointIndex].position); // ����Ѳ��·��
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
        AutoPath();
        if (pathPointList == null)
            return;

        if (distanceToPlayer <= attackDistance)
        {
            // ֹͣ�ƶ������й���
            OnMovementInput?.Invoke(Vector2.zero);
            if (isAttack)
            {
                isAttack = false;
                OnAttack?.Invoke();
                StartCoroutine(nameof(AttackCooldownCoroutine));
            }

            // �������λ�÷�ת����
            sr.flipX = player.position.x > transform.position.x;
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
                currentIndex = 0;
                // ���������׷��״̬��������Ϊ�ȴ�״̬
                if (!isChasing)
                {
                    isWaitingAtPoint = true;
                    waitTimer = 0f;
                }
            }
        }
    }

    // ��ȡ·����
    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath; // Path.vectorPath �����˴���㵽�յ������·��
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
                MoveToNextPatrolPoint(); // �ƶ�����һ��Ѳ�ߵ�
                GeneratePath(patrolPoints[currentPatrolPointIndex].position); // �����µ�·��
            }
            return;
        }

        // ���·�����б�Ϊ�գ�����·��
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            GeneratePath(patrolPoints[currentPatrolPointIndex].position);
            return;
        }

        // �ƶ�����ǰ·����
        if (currentIndex >= 0 && currentIndex < pathPointList.Count)
        {
            Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
            OnMovementInput?.Invoke(direction);

            // �����ƶ�����ת����
            sr.flipX = direction.x > 0;
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

    private void MeleeAttackEvent()
    {
        // �����ײ
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<HealthSystem>().TakeDamage(meleetAttackDamage);
        }
    }

    // ������ȴʱ��
    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration); // �ȴ���ȴʱ��
        isAttack = true; // ���ù���״̬
    }

    public void OnDrawGizmosSelected()
    {
        // ������Χ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // ׷����Χ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        // Ѳ��·��
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
    }
}
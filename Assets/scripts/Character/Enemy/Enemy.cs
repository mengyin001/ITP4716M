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
    [SerializeField] private float chaseDistance = 3f;//׷������
    [SerializeField] private float attackDistance = 0.8f;//��������

    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float patrolSpeed = 1f;
    [SerializeField] private float waitTimeAtPoint = 1f;



    [Header("Attack Settings")]
    public float meleetAttackDamage;//��ս�����˺�
    public LayerMask playerLayer;//��ʾ���ͼ��
    public float AttackCooldownDuration = 2f;//��ȴʱ��

    private Seeker seeker;
    private List<Vector3> pathPointList;//·�����б�
    private int currentIndex = 0;//·���������
    private float pathGenerateInterval = 0.5f; //ÿ0.5������һ��·��
    private float pathGenerateTimer = 0f;//��ʱ��

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
    //�Զ�Ѱ·
    private void AutoPath()
    {
        pathGenerateTimer += Time.deltaTime;

        //���һ��ʱ������ȡ·����
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(player.position);
            pathGenerateTimer = 0;//���ü�ʱ��
        }


        //��·�����б�Ϊ��ʱ������·������
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            GeneratePath(player.position);
        }//�����˵��ﵱǰ·����ʱ����������currentIndex������·������
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
                GeneratePath(player.position);
        }
    }

    //��ȡ·����
    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        //������������㡢�յ㡢�ص�����
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath;//Path.vectorPath�����˴���㵽�յ������·��
        });
    }
    //���˽�ս����

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
        //�����ײ
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<PlayerHealth>().TakeDamage(meleetAttackDamage);
        }
    }
    //������ȴʱ��

    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);// �ȴ���ȴʱ��
        isAttack = true;// ���ù���״̬
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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;
using Photon.Pun;

public class Enemy : Character
{
    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnAttack;

    [Header("Chase Settings")]
    [SerializeField] public Transform player;
    [SerializeField] private float chaseDistance = 3f;
    [SerializeField] private float attackDistance = 0.8f;

    [Header("Patrol Settings")]
    [SerializeField] private bool shouldPatrol = true;
    [SerializeField] public List<Transform> patrolPoints;
    [SerializeField] private float patrolPointReachedDistance = 0.5f;
    [SerializeField] private float waitTimeAtPoint = 1f;

    [Header("Attack Settings")]
    public float meleetAttackDamage;
    public LayerMask playerLayer;
    public float AttackCooldownDuration = 2f;

    private Seeker seeker;
    private List<Vector3> pathPointList;
    private int currentIndex = 0;
    private float pathGenerateInterval = 0.5f;
    private float pathGenerateTimer = 0f;

    private Animator animator;
    private bool isAttack = true;
    private bool isChasing = false;
    private int currentPatrolPointIndex = 0;
    private bool isWaitingAtPoint = false;
    private float waitTimer = 0f;
    private SpriteRenderer sr;

    private bool isAlive = true;
    private bool isPatrolling = true;
    private int patrolDirection = 1;

    public PickupSpawner pickupSpawner;

    protected override void Awake()
    {
        base.Awake();
        seeker = GetComponent<Seeker>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

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

    private void Update()
    {
        // 只在主客户端运行AI逻辑
        if (!PhotonNetwork.IsMasterClient) return;
        if (!isAlive || player == null) return;

        float distanceToPlayer = Vector2.Distance(GetPlayerCenterPosition(), transform.position);

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
                    currentPatrolPointIndex = 0;
                    GeneratePath(transform.position, patrolPoints[currentPatrolPointIndex].position);
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
        AutoPath();
        if (pathPointList == null) return;

        if (distanceToPlayer <= attackDistance)
        {
            OnMovementInput?.Invoke(Vector2.zero);
            if (isAttack)
            {
                isAttack = false;
                OnAttack?.Invoke();
                StartCoroutine(nameof(AttackCooldownCoroutine));

                // 同步攻击动画
                photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All);
            }

            sr.flipX = GetPlayerCenterPosition().x > transform.position.x;
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
        pathGenerateTimer += Time.deltaTime;

        if (pathGenerateTimer >= pathGenerateInterval)
        {
            Vector3 target = isChasing ? GetPlayerCenterPosition() : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(transform.position, target);
            pathGenerateTimer = 0;
        }

        if (pathPointList == null || pathPointList.Count <= 0)
        {
            Vector3 target = isChasing ? GetPlayerCenterPosition() : patrolPoints[currentPatrolPointIndex].position;
            GeneratePath(transform.position, target);
        }
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
            {
                currentIndex = 0;
                if (!isChasing)
                {
                    isWaitingAtPoint = true;
                    waitTimer = 0f;
                }
            }
        }
    }

    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath;
        });
    }

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

    [PunRPC]
    private void RPC_PlayAttackAnimation()
    {
        animator.SetTrigger("Attack");
        MeleeAttackEvent();
    }

    private void MeleeAttackEvent()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(GetPlayerCenterPosition(), attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            Character character = hitCollider.GetComponent<Character>();
            if (character != null)
            {
                // 使用RPC调用伤害
                character.photonView.RPC("TakeDamage", RpcTarget.All, meleetAttackDamage);
            }
        }
    }

    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);
        isAttack = true;
    }

    [PunRPC]
    private void RPC_UpdateFlip(bool flipRight)
    {
        sr.flipX = flipRight;
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
        base.RPC_Die();
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

    private Vector3 GetPlayerCenterPosition()
    {
        if (player == null) return Vector3.zero;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            return playerCollider.bounds.center;
        }
        return player.position;
    }
}
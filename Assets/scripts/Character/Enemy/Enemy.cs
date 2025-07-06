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
    private bool isAttack = true; // 是否可以攻击（冷却状态）
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

    public Transform player;
    private float playerUpdateInterval = 2f;
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
        UpdatePlayerReference();
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

        // 仅主机执行AI和玩家引用更新
        if (PhotonNetwork.IsMasterClient)
        {
            playerUpdateTimer += Time.deltaTime;
            if (playerUpdateTimer >= playerUpdateInterval)
            {
                UpdatePlayerReference();
                playerUpdateTimer = 0f;
            }

            if (player == null)
            {
                currentMovementDirection = Vector2.zero;
                OnMovementInput?.Invoke(Vector2.zero);
                return;
            }

            if (forceChaseMode)
            {
                HandleForceChaseBehavior();
                return;
            }

            if (publicMode)
            {
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
                        currentMovementDirection = Vector2.zero;
                        OnMovementInput?.Invoke(Vector2.zero);
                    }
                }
            }
        }
        else
        {
            // 客户端仅应用同步状态
            ApplyNetworkState();
        }
    }

    private void UpdatePlayerReference()
    {
        GameObject localPlayerObj = null;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (p.IsLocal)
            {
                GameObject playerObj = PhotonView.Find(p.ActorNumber)?.gameObject;
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
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            player = playerObj != null ? playerObj.transform : null;
        }
    }

    private void HandleForceChaseBehavior()
    {
        pathGenerateTimer += Time.deltaTime;
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(transform.position, GetPlayerCenterPosition());
            pathGenerateTimer = 0;
        }

        float distanceToPlayer = Vector2.Distance(GetPlayerCenterPosition(), transform.position);

        if (distanceToPlayer <= attackDistance)
        {
            currentMovementDirection = Vector2.zero;
            OnMovementInput?.Invoke(Vector2.zero);
            if (isAttack)
            {
                isAttack = false;
                isAttacking = true;
                OnAttack?.Invoke();
                StartCoroutine(AttackCooldownCoroutine());
            }
            sr.flipX = GetPlayerCenterPosition().x > transform.position.x;
        }
        else
        {
            if (pathPointList != null && pathPointList.Count > 0)
            {
                if (currentIndex >= pathPointList.Count)
                {
                    currentIndex = pathPointList.Count - 1;
                }

                Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                currentMovementDirection = direction;
                OnMovementInput?.Invoke(direction);
                sr.flipX = direction.x > 0;

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
            currentMovementDirection = Vector2.zero;
            OnMovementInput?.Invoke(Vector2.zero);
            if (isAttack)
            {
                isAttack = false;
                isAttacking = true;
                OnAttack?.Invoke();
                StartCoroutine(AttackCooldownCoroutine());
            }
            sr.flipX = GetPlayerCenterPosition().x > transform.position.x;
        }
        else
        {
            if (currentIndex >= 0 && currentIndex < pathPointList.Count)
            {
                Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                currentMovementDirection = direction;
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
            currentMovementDirection = direction;
            OnMovementInput?.Invoke(direction);
            sr.flipX = direction.x > 0;
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

    private void MeleeAttackEvent()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(GetPlayerCenterPosition(), attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<HealthSystem>()?.RPC_TakeDamage(meleetAttackDamage);
        }
    }

    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackCooldownDuration);
        isAttack = true;
        isAttacking = false; // 冷却结束后重置攻击状态
    }

    // 动画事件调用：攻击动画结束时重置状态（更精准）
    public void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        if (!forceChaseMode)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, chaseDistance);
        }

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
        base.DieRPC();
        isAlive = false;
        if (PhotonNetwork.IsMasterClient && pickupSpawner != null)
        {
            pickupSpawner.DropItems();
        }
    }

    private Vector3 GetPlayerCenterPosition()
    {
        if (player == null)
            return transform.position;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        return playerCollider != null ? playerCollider.bounds.center : player.position;
    }

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
            // 客户端接收
            networkMovementDirection = (Vector2)stream.ReceiveNext();
            networkIsAttacking = (bool)stream.ReceiveNext();
        }
    }

    private void ApplyNetworkState()
    {
        // 应用移动方向
        OnMovementInput?.Invoke(networkMovementDirection);

        // 同步翻转方向
        if (networkMovementDirection.x != 0)
        {
            sr.flipX = networkMovementDirection.x > 0;
        }

        // 同步动画状态
        if (animator != null)
        {
            animator.SetBool("IsMoving", networkMovementDirection != Vector2.zero);
            animator.SetBool("IsAttacking", networkIsAttacking);
        }
    }
}
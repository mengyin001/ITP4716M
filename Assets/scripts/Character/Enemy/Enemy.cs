using UnityEngine;
using UnityEngine.Events;
using Pathfinding;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Seeker), typeof(SpriteRenderer), typeof(Animator))]
public class Enemy : Character
{
    [Header("Chase Settings")]
    [SerializeField] private Transform _playerTarget;
    [SerializeField] private float _chaseRadius = 3f;
    [SerializeField] private float _attackRadius = 0.8f;
    [SerializeField] private float _pathUpdateInterval = 0.5f;

    [Header("Patrol Settings")]
    [SerializeField] private bool _enablePatrol = true;
    [SerializeField] private float _patrolSpeed = 1f;
    [SerializeField] private float _waypointReachThreshold = 0.5f;
    [SerializeField] private float _waypointWaitTime = 1f;
    [SerializeField] private PatrolPath _patrolPath;

    [Header("Combat Settings")]
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _attackCooldown = 2f;

    // Events
    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnAttack;

    // Components
    private Seeker _pathSeeker;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;

    // Pathfinding
    private Path _currentPath;
    private int _currentWaypointIndex;
    private float _pathUpdateTimer;

    // State tracking
    private bool _isChasing;
    private bool _canAttack = true;
    private bool _isWaitingAtWaypoint;
    private float _waypointWaitTimer;
    private int _currentPatrolIndex;

    private bool _isDead;

    private void Awake()
    {
        _pathSeeker = GetComponent<Seeker>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (_playerTarget == null) return;

        UpdateAIState();
        HandleCurrentState();
    }

    private void UpdateAIState()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, _playerTarget.position);
        _isChasing = distanceToPlayer < _chaseRadius;

        if (!_isChasing && _enablePatrol)
        {
            PatrolBehavior();
        }
    }

    private void HandleCurrentState()
    {
        if (_isChasing)
        {
            ChaseBehavior();
        }
        else
        {
            OnMovementInput?.Invoke(Vector2.zero);
        }
    }

    #region Chase Logic
    private void ChaseBehavior()
    {
        UpdatePathToTarget();
        HandleTargetDistance();

        if (_currentPath == null) return;

        MoveAlongPath();
        UpdateSpriteOrientation(_playerTarget.position);
    }

    private void UpdatePathToTarget()
    {
        _pathUpdateTimer += Time.deltaTime;

        if (_pathUpdateTimer >= _pathUpdateInterval)
        {
            _pathSeeker.StartPath(transform.position, _playerTarget.position, OnPathGenerated);
            _pathUpdateTimer = 0;
        }
    }

    private void OnPathGenerated(Path path)
    {
        if (!path.error)
        {
            _currentPath = path;
            _currentWaypointIndex = 0;
        }
    }

    private void HandleTargetDistance()
    {
        float distance = Vector2.Distance(transform.position, _playerTarget.position);

        if (distance <= _attackRadius && _canAttack)
        {
            ExecuteAttack();
        }
    }

    private void MoveAlongPath()
    {
        if (_currentWaypointIndex >= _currentPath.vectorPath.Count)
        {
            _currentPath = null;
            return;
        }

        Vector2 direction = ((Vector2)_currentPath.vectorPath[_currentWaypointIndex] - (Vector2)transform.position).normalized;
        OnMovementInput?.Invoke(direction);

        if (Vector2.Distance(transform.position, _currentPath.vectorPath[_currentWaypointIndex]) < 0.1f)
        {
            _currentWaypointIndex++;
        }
    }
    #endregion

    #region Combat Logic
    private void ExecuteAttack()
    {
        _canAttack = false;
        OnAttack?.Invoke();
        StartCoroutine(AttackCooldown());
    }

    // Animation Event
    private void ApplyMeleeDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _attackRadius, _playerLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<PlayerHealth>(out var health))
            {
                health.TakeDamage(_attackDamage);
            }
        }
    }

    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(_attackCooldown);
        _canAttack = true;
    }
    #endregion

    #region Patrol Logic
    private void PatrolBehavior()
    {
        if (_patrolPath == null || _patrolPath.Waypoints.Count == 0) return;

        HandleWaypointWaiting();
        MoveToCurrentWaypoint();
    }

    private void HandleWaypointWaiting()
    {
        if (_isWaitingAtWaypoint)
        {
            _waypointWaitTimer += Time.deltaTime;

            if (_waypointWaitTimer >= _waypointWaitTime)
            {
                _isWaitingAtWaypoint = false;
                _waypointWaitTimer = 0;
                UpdatePatrolWaypoint();
            }
        }
    }

    private void MoveToCurrentWaypoint()
    {
        Vector2 currentWaypoint = _patrolPath.Waypoints[_currentPatrolIndex].position;
        float distance = Vector2.Distance(transform.position, currentWaypoint);

        if (distance <= _waypointReachThreshold)
        {
            _isWaitingAtWaypoint = true;
            OnMovementInput?.Invoke(Vector2.zero);
        }
        else
        {
            Vector2 direction = (currentWaypoint - (Vector2)transform.position).normalized;
            OnMovementInput?.Invoke(direction * _patrolSpeed);
            UpdateSpriteOrientation(currentWaypoint);
        }
    }

    private void UpdatePatrolWaypoint()
    {
        _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPath.Waypoints.Count;
    }
    #endregion

    private void UpdateSpriteOrientation(Vector3 targetPosition)
    {
        _spriteRenderer.flipX = targetPosition.x > transform.position.x;
    }

    public override void Die()
    {
        if (_isDead) return; // 防止重复执行
        _isDead = true;
        base.Die();
        // 延迟关闭 Collider（可选）
        StartCoroutine(DisableColliderAfterDeath());
        _animator.SetTrigger("isDie");
        enabled = false;
    }

    private IEnumerator DisableColliderAfterDeath()
    {
        // 等待一帧确保动画播放
        yield return new WaitForEndOfFrame();

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw chase radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _chaseRadius);

        // Draw attack radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRadius);

        // Draw patrol path
        if (_patrolPath != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < _patrolPath.Waypoints.Count; i++)
            {
                if (_patrolPath.Waypoints[i] == null) continue;

                Gizmos.DrawSphere(_patrolPath.Waypoints[i].position, 0.2f);
                if (i < _patrolPath.Waypoints.Count - 1 && _patrolPath.Waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(_patrolPath.Waypoints[i].position, _patrolPath.Waypoints[i + 1].position);
                }
            }
        }
    }
}

[System.Serializable]
public class PatrolPath
{
    public List<Transform> Waypoints = new List<Transform>();
}
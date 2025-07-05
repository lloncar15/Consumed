using UnityEngine;

public class FlyingMonster : MonsterBase
{
    [Header("Flying Movement")]
    [Tooltip("How quickly monster accelerates to target speed")]
    [SerializeField] private float acceleration = 10f;
    [Tooltip("How quickly monster decelerates when stopping")]
    [SerializeField] private float deceleration = 15f;
    [Tooltip("Maximum distance from level bounds before turning around")]
    [SerializeField] private float boundaryBuffer = 2f;
    
    [Header("Flight Patterns")]
    [Tooltip("Radius of circular flight pattern when idle")]
    [SerializeField] private float idleFlightRadius = 3f;
    [Tooltip("Speed of circular flight pattern")]
    [SerializeField] private float idleFlightSpeed = 2f;
    [Tooltip("How close monster tries to get to player when chasing")]
    [SerializeField] private float chaseStopDistance = 1.5f;
    
    [Header("Player Tracking")]
    [Tooltip("How quickly monster turns toward player")]
    [SerializeField] private float turnSpeed = 5f;
    [Tooltip("Distance ahead of player to predict movement")]
    [SerializeField] private float predictionDistance = 1f;
    
    // Movement state
    private Vector2 _currentVelocity;
    private Vector2 _targetVelocity;
    private Vector2 _idleCenter;
    private float _idleFlightAngle;
    private Vector2 _lastPlayerPosition;
    private Vector2 _predictedPlayerPosition;
    
    // Level bounds
    private Vector2 _levelBounds;
    
    protected override void Awake()
    {
        base.Awake();
        monsterType = MonsterType.Flying;
        
        // Flying monsters don't need rigidbody physics
        if (MonsterRigidbody)
        {
            MonsterRigidbody.gravityScale = 0f;
            MonsterRigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
    }
    
    public override void Initialize()
    {
        base.Initialize();
        _idleCenter = transform.position;
        _idleFlightAngle = Random.Range(0f, 360f);
        _currentVelocity = Vector2.zero;
        _targetVelocity = Vector2.zero;
        _lastPlayerPosition = Vector2.zero;
        
        // Get level bounds for boundary detection
        if (GameManager && GameManager.GetComponent<BubbleSystem>())
        {
            var bubbleSystem = GameManager.GetComponent<BubbleSystem>();
            if (bubbleSystem.MovementSettings)
            {
                _levelBounds = bubbleSystem.MovementSettings.GetLevelBounds();
            }
        }
        
        // Fallback bounds if no bubble system found
        if (_levelBounds == Vector2.zero)
        {
            _levelBounds = new Vector2(15f, 10f);
        }
    }
    
    protected override void UpdateMonster()
    {
        UpdatePlayerPrediction();
        HandleFlightMovement();
        CheckBoundaries();
        CheckForPlayerToAttack();
        ApplyMovement();
    }
    
    private void UpdatePlayerPrediction()
    {
        if (!GameManager || !GameManager.IsPlayerValid()) return;
        
        Vector2 currentPlayerPos = GetPlayerPosition();
        
        // Calculate player velocity
        Vector2 playerVelocity = Vector2.zero;
        if (_lastPlayerPosition != Vector2.zero)
        {
            playerVelocity = (currentPlayerPos - _lastPlayerPosition) / Time.deltaTime;
        }
        
        // Predict where player will be
        _predictedPlayerPosition = currentPlayerPos + playerVelocity * predictionDistance;
        _lastPlayerPosition = currentPlayerPos;
    }
    
    private void HandleFlightMovement()
    {
        switch (currentState)
        {
            case MonsterState.Idle:
            case MonsterState.Patrolling:
                HandleIdleFlight();
                break;
            case MonsterState.Chasing:
                HandleChaseFlight();
                break;
            case MonsterState.Attacking:
                HandleAttackFlight();
                break;
            default:
                _targetVelocity = Vector2.zero;
                break;
        }
    }
    
    private void HandleIdleFlight()
    {
        // Circular flight pattern around spawn point
        _idleFlightAngle += idleFlightSpeed * Time.deltaTime;
        if (_idleFlightAngle >= 360f) _idleFlightAngle -= 360f;
        
        Vector2 circleOffset = new Vector2(
            Mathf.Cos(_idleFlightAngle * Mathf.Deg2Rad),
            Mathf.Sin(_idleFlightAngle * Mathf.Deg2Rad)
        ) * idleFlightRadius;
        
        Vector2 targetPosition = _idleCenter + circleOffset;
        Vector2 directionToTarget = (targetPosition - (Vector2)transform.position).normalized;
        
        _targetVelocity = directionToTarget * moveSpeed;
    }
    
    private void HandleChaseFlight()
    {
        if (!GameManager || !GameManager.IsPlayerValid())
        {
            TransitionToState(MonsterState.Patrolling);
            return;
        }
        
        Vector2 directionToPlayer = (_predictedPlayerPosition - (Vector2)transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // Stop chasing if too close (to avoid hovering directly on player)
        if (distanceToPlayer <= chaseStopDistance)
        {
            _targetVelocity = Vector2.zero;
        }
        else
        {
            // Move toward predicted player position
            directionToPlayer.Normalize();
            _targetVelocity = directionToPlayer * chaseSpeed;
        }
    }
    
    private void HandleAttackFlight()
    {
        // Stop moving during attack
        _targetVelocity = Vector2.zero;
    }
    
    private void CheckBoundaries()
    {
        Vector2 position = transform.position;
        Vector2 pushForce = Vector2.zero;
        
        // Check horizontal boundaries
        if (position.x > _levelBounds.x - boundaryBuffer)
        {
            pushForce.x = -1f;
        }
        else if (position.x < -_levelBounds.x + boundaryBuffer)
        {
            pushForce.x = 1f;
        }
        
        // Check vertical boundaries
        if (position.y > _levelBounds.y - boundaryBuffer)
        {
            pushForce.y = -1f;
        }
        else if (position.y < -_levelBounds.y + boundaryBuffer)
        {
            pushForce.y = 1f;
        }
        
        // Apply boundary avoidance
        if (pushForce != Vector2.zero)
        {
            _targetVelocity += pushForce.normalized * (chaseSpeed * 0.5f);
        }
    }
    
    private void CheckForPlayerToAttack()
    {
        if (!GameManager || !GameManager.IsPlayerValid() || currentState == MonsterState.Attacking)
            return;
        
        // Check if player is in attack range and we can attack
        if (IsPlayerInRange(attackRange) && CanAttack)
        {
            TransitionToState(MonsterState.Attacking);
        }
    }
    
    private void ApplyMovement()
    {
        // Smooth velocity changes
        float accelRate = _targetVelocity.magnitude > 0.1f ? acceleration : deceleration;
        _currentVelocity = Vector2.MoveTowards(_currentVelocity, _targetVelocity, accelRate * Time.deltaTime);
        
        // Clamp to max speed
        if (_currentVelocity.magnitude > chaseSpeed)
        {
            _currentVelocity = _currentVelocity.normalized * chaseSpeed;
        }
        
        // Apply movement
        transform.position += (Vector3)_currentVelocity * Time.deltaTime;
        
        // Handle sprite flipping based on movement direction
        if (_currentVelocity.x != 0 && SpriteRenderer)
        {
            SpriteRenderer.flipX = _currentVelocity.x < 0;
        }
    }
    
    protected override void HandleIdleState()
    {
        if (IsPlayerInRange(detectionRange))
        {
            TransitionToState(MonsterState.Chasing);
        }
        else if (StateTimer >= idleWaitTime)
        {
            TransitionToState(MonsterState.Patrolling);
        }
    }
    
    protected override void HandlePatrollingState()
    {
        if (IsPlayerInRange(detectionRange))
        {
            TransitionToState(MonsterState.Chasing);
        }
    }
    
    protected override void HandleChasingState()
    {
        float loseTargetRange = detectionRange * loseTargetRangeMultiplier;
        
        if (!IsPlayerInRange(loseTargetRange))
        {
            TransitionToState(MonsterState.Patrolling);
        }
        else if (IsPlayerInRange(attackRange) && CanAttack)
        {
            TransitionToState(MonsterState.Attacking);
        }
    }
    
    protected override void OnAttackStart()
    {
        base.OnAttackStart();
        
        // Flying monster dive attack or chomp
        Debug.Log($"{name} starts aerial chomp attack!");
        
        // Spawn attack effect if assigned
        if (attackEffect)
        {
            GameObject effect = Instantiate(attackEffect, transform.position, Quaternion.identity);
            Destroy(effect, attackWindupTime + attackDuration);
        }
        
        // Optional: Quick dash toward player during windup
        if (GameManager && GameManager.IsPlayerValid())
        {
            Vector2 dashDirection = DirectionToPlayer;
            _targetVelocity = dashDirection * chaseSpeed * 0.5f; // Half speed dash
        }
    }
    
    protected override void OnAttackComplete()
    {
        base.OnAttackComplete();
        Debug.Log($"{name} finished aerial attack");
        
        // Reset to normal movement
        _targetVelocity = Vector2.zero;
    }
    
    protected override MonsterState GetStateAfterAttack()
    {
        // Flying monsters return to chasing if player still in detection range
        return IsPlayerInRange(detectionRange) ? MonsterState.Chasing : MonsterState.Patrolling;
    }
    
    protected override void OnStateEnter(MonsterState state)
    {
        base.OnStateEnter(state);
        
        switch (state)
        {
            case MonsterState.Patrolling:
                // Reset idle center to current position
                _idleCenter = transform.position;
                break;
            case MonsterState.Attacking:
                // Stop normal movement during attack (handled in OnAttackStart)
                break;
        }
    }
    
    // Debug visualization
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        // Idle flight radius
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_idleCenter, idleFlightRadius);
            
            // Current target position in idle flight
            if (currentState == MonsterState.Patrolling || currentState == MonsterState.Idle)
            {
                Vector2 circleOffset = new Vector2(
                    Mathf.Cos(_idleFlightAngle * Mathf.Deg2Rad),
                    Mathf.Sin(_idleFlightAngle * Mathf.Deg2Rad)
                ) * idleFlightRadius;
                Vector2 targetPos = _idleCenter + circleOffset;
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetPos, 0.3f);
                Gizmos.DrawLine(transform.position, targetPos);
            }
        }
        
        // Level boundaries
        Gizmos.color = Color.white;
        Vector2 bounds = _levelBounds - Vector2.one * boundaryBuffer;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(bounds.x * 2, bounds.y * 2, 0));
        
        // Current velocity
        if (Application.isPlaying && _currentVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)_currentVelocity);
        }
        
        // Predicted player position
        if (Application.isPlaying && GameManager && GameManager.IsPlayerValid())
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_predictedPlayerPosition, 0.2f);
            Gizmos.DrawLine(GetPlayerPosition(), _predictedPlayerPosition);
        }
        
        // Chase stop distance
        if (Application.isPlaying && currentState == MonsterState.Chasing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, chaseStopDistance);
        }
    }
}
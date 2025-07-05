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
    private Vector2 currentVelocity;
    private Vector2 targetVelocity;
    private Vector2 idleCenter;
    private float idleFlightAngle;
    private Vector2 lastPlayerPosition;
    private Vector2 predictedPlayerPosition;
    
    // Level bounds
    private Vector2 levelBounds;
    
    protected override void Awake()
    {
        base.Awake();
        monsterType = MonsterType.Flying;
        
        // Flying monsters don't need rigidbody physics
        if (monsterRigidbody)
        {
            monsterRigidbody.gravityScale = 0f;
            monsterRigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
    }
    
    public override void Initialize()
    {
        base.Initialize();
        idleCenter = transform.position;
        idleFlightAngle = Random.Range(0f, 360f);
        currentVelocity = Vector2.zero;
        targetVelocity = Vector2.zero;
        lastPlayerPosition = Vector2.zero;
        
        // Get level bounds for boundary detection
        if (gameManager && gameManager.GetComponent<BubbleSystem>())
        {
            var bubbleSystem = gameManager.GetComponent<BubbleSystem>();
            if (bubbleSystem.MovementSettings)
            {
                levelBounds = bubbleSystem.MovementSettings.GetLevelBounds();
            }
        }
        
        // Fallback bounds if no bubble system found
        if (levelBounds == Vector2.zero)
        {
            levelBounds = new Vector2(15f, 10f);
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
        if (!gameManager || !gameManager.IsPlayerValid()) return;
        
        Vector2 currentPlayerPos = GetPlayerPosition();
        
        // Calculate player velocity
        Vector2 playerVelocity = Vector2.zero;
        if (lastPlayerPosition != Vector2.zero)
        {
            playerVelocity = (currentPlayerPos - lastPlayerPosition) / Time.deltaTime;
        }
        
        // Predict where player will be
        predictedPlayerPosition = currentPlayerPos + playerVelocity * predictionDistance;
        lastPlayerPosition = currentPlayerPos;
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
                targetVelocity = Vector2.zero;
                break;
        }
    }
    
    private void HandleIdleFlight()
    {
        // Circular flight pattern around spawn point
        idleFlightAngle += idleFlightSpeed * Time.deltaTime;
        if (idleFlightAngle >= 360f) idleFlightAngle -= 360f;
        
        Vector2 circleOffset = new Vector2(
            Mathf.Cos(idleFlightAngle * Mathf.Deg2Rad),
            Mathf.Sin(idleFlightAngle * Mathf.Deg2Rad)
        ) * idleFlightRadius;
        
        Vector2 targetPosition = idleCenter + circleOffset;
        Vector2 directionToTarget = (targetPosition - (Vector2)transform.position).normalized;
        
        targetVelocity = directionToTarget * moveSpeed;
    }
    
    private void HandleChaseFlight()
    {
        if (!gameManager || !gameManager.IsPlayerValid())
        {
            TransitionToState(MonsterState.Patrolling);
            return;
        }
        
        Vector2 directionToPlayer = (predictedPlayerPosition - (Vector2)transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // Stop chasing if too close (to avoid hovering directly on player)
        if (distanceToPlayer <= chaseStopDistance)
        {
            targetVelocity = Vector2.zero;
        }
        else
        {
            // Move toward predicted player position
            directionToPlayer.Normalize();
            targetVelocity = directionToPlayer * chaseSpeed;
        }
    }
    
    private void HandleAttackFlight()
    {
        // Stop moving during attack
        targetVelocity = Vector2.zero;
    }
    
    private void CheckBoundaries()
    {
        Vector2 position = transform.position;
        Vector2 pushForce = Vector2.zero;
        
        // Check horizontal boundaries
        if (position.x > levelBounds.x - boundaryBuffer)
        {
            pushForce.x = -1f;
        }
        else if (position.x < -levelBounds.x + boundaryBuffer)
        {
            pushForce.x = 1f;
        }
        
        // Check vertical boundaries
        if (position.y > levelBounds.y - boundaryBuffer)
        {
            pushForce.y = -1f;
        }
        else if (position.y < -levelBounds.y + boundaryBuffer)
        {
            pushForce.y = 1f;
        }
        
        // Apply boundary avoidance
        if (pushForce != Vector2.zero)
        {
            targetVelocity += pushForce.normalized * chaseSpeed * 0.5f;
        }
    }
    
    private void CheckForPlayerToAttack()
    {
        if (!gameManager || !gameManager.IsPlayerValid() || currentState == MonsterState.Attacking)
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
        float accelRate = targetVelocity.magnitude > 0.1f ? acceleration : deceleration;
        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, accelRate * Time.deltaTime);
        
        // Clamp to max speed
        if (currentVelocity.magnitude > chaseSpeed)
        {
            currentVelocity = currentVelocity.normalized * chaseSpeed;
        }
        
        // Apply movement
        transform.position += (Vector3)currentVelocity * Time.deltaTime;
        
        // Handle sprite flipping based on movement direction
        if (currentVelocity.x != 0 && spriteRenderer)
        {
            spriteRenderer.flipX = currentVelocity.x < 0;
        }
    }
    
    protected override void HandleIdleState()
    {
        if (IsPlayerInRange(detectionRange))
        {
            TransitionToState(MonsterState.Chasing);
        }
        else if (stateTimer >= idleWaitTime)
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
        if (gameManager && gameManager.IsPlayerValid())
        {
            Vector2 dashDirection = DirectionToPlayer;
            targetVelocity = dashDirection * chaseSpeed * 0.5f; // Half speed dash
        }
    }
    
    protected override void OnAttackComplete()
    {
        base.OnAttackComplete();
        Debug.Log($"{name} finished aerial attack");
        
        // Reset to normal movement
        targetVelocity = Vector2.zero;
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
                idleCenter = transform.position;
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
            Gizmos.DrawWireSphere(idleCenter, idleFlightRadius);
            
            // Current target position in idle flight
            if (currentState == MonsterState.Patrolling || currentState == MonsterState.Idle)
            {
                Vector2 circleOffset = new Vector2(
                    Mathf.Cos(idleFlightAngle * Mathf.Deg2Rad),
                    Mathf.Sin(idleFlightAngle * Mathf.Deg2Rad)
                ) * idleFlightRadius;
                Vector2 targetPos = idleCenter + circleOffset;
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetPos, 0.3f);
                Gizmos.DrawLine(transform.position, targetPos);
            }
        }
        
        // Level boundaries
        Gizmos.color = Color.white;
        Vector2 bounds = levelBounds - Vector2.one * boundaryBuffer;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(bounds.x * 2, bounds.y * 2, 0));
        
        // Current velocity
        if (Application.isPlaying && currentVelocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)currentVelocity);
        }
        
        // Predicted player position
        if (Application.isPlaying && gameManager && gameManager.IsPlayerValid())
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(predictedPlayerPosition, 0.2f);
            Gizmos.DrawLine(GetPlayerPosition(), predictedPlayerPosition);
        }
        
        // Chase stop distance
        if (Application.isPlaying && currentState == MonsterState.Chasing)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, chaseStopDistance);
        }
    }
}
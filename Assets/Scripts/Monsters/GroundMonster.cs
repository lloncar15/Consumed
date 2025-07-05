using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlatformDetector))]
public class GroundMonster : MonsterBase
{
    [Header("Ground Movement")]
    [Tooltip("Force applied when falling from bubble")]
    [SerializeField] private float fallGravityScale = 2f;
    [Tooltip("How quickly monster changes direction when patrolling")]
    [SerializeField] private float directionChangeSpeed = 5f;
    [Tooltip("Time to pause when reaching platform edge")]
    [SerializeField] private float edgePauseTime = 0.5f;
    
    [Header("Patrol Behavior")]
    [Tooltip("Distance to patrol from spawn point")]
    [SerializeField] private float patrolDistance = 5f;
    
    // Movement state
    private Vector2 patrolCenter;
    private float patrolDirection = 1f; // 1 for right, -1 for left
    private float edgePauseTimer;
    private bool isAtEdge;
    private bool hasTouchedGround;
    
    // Components
    private PlatformDetector platformDetector;
    
    protected override void Awake()
    {
        base.Awake();
        monsterType = MonsterType.Ground;
        platformDetector = GetComponent<PlatformDetector>();
        
        // Configure rigidbody for ground movement
        if (monsterRigidbody)
        {
            monsterRigidbody.gravityScale = fallGravityScale;
            monsterRigidbody.freezeRotation = true;
        }
    }
    
    public override void Initialize()
    {
        base.Initialize();
        patrolCenter = transform.position;
        patrolDirection = Random.value > 0.5f ? 1f : -1f; // Random initial direction
        hasTouchedGround = false;
        isAtEdge = false;
        edgePauseTimer = 0f;
    }
    
    protected override void UpdateMonster()
    {
        HandleGroundDetection();
        HandleMovement();
        CheckForPlayerToEat();
        UpdatePatrolCenter();
    }
    
    private void HandleGroundDetection()
    {
        // Check if we've just landed
        if (!hasTouchedGround && platformDetector.IsGrounded)
        {
            hasTouchedGround = true;
            patrolCenter = transform.position;
            
            // Reduce gravity once grounded
            if (monsterRigidbody)
            {
                monsterRigidbody.gravityScale = 1f;
            }
        }
    }
    
    private void HandleMovement()
    {
        if (!hasTouchedGround) return; // Still falling
        
        Vector2 velocity = monsterRigidbody.linearVelocity;
        
        switch (currentState)
        {
            case MonsterState.Patrolling:
            case MonsterState.Idle:
                HandlePatrolMovement(ref velocity);
                break;
            case MonsterState.Attacking:
                // Stop moving during attack
                velocity.x = 0;
                break;
            default:
                // Stop moving in other states
                velocity.x = 0;
                break;
        }
        
        // Apply safe movement (respects platform edges)
        velocity = platformDetector.GetSafeMovementDirection(velocity);
        monsterRigidbody.linearVelocity = velocity;
    }
    
    private void HandlePatrolMovement(ref Vector2 velocity)
    {
        // Handle edge pausing
        if (isAtEdge)
        {
            edgePauseTimer += Time.deltaTime;
            velocity.x = 0;
            
            if (edgePauseTimer >= edgePauseTime)
            {
                // Change direction and stop being at edge
                patrolDirection *= -1f;
                isAtEdge = false;
                edgePauseTimer = 0f;
                FlipSprite();
            }
            return;
        }
        
        // Check for edges
        bool canContinue = (patrolDirection > 0 && platformDetector.CanMoveRight) ||
                          (patrolDirection < 0 && platformDetector.CanMoveLeft);
        
        if (!canContinue)
        {
            isAtEdge = true;
            velocity.x = 0;
            return;
        }
        
        // Check patrol distance limits
        float distanceFromCenter = transform.position.x - patrolCenter.x;
        if (Mathf.Abs(distanceFromCenter) > patrolDistance)
        {
            // Turn around if we've gone too far
            patrolDirection = distanceFromCenter > 0 ? -1f : 1f;
            FlipSprite();
        }
        
        // Apply patrol movement
        float targetSpeed = moveSpeed * patrolDirection;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, directionChangeSpeed * Time.deltaTime);
    }
    
    private void CheckForPlayerToEat()
    {
        if (!gameManager || !gameManager.IsPlayerValid() || currentState == MonsterState.Attacking)
            return;
        
        // Check if player is in attack range and we can attack (cooldown finished)
        if (!IsPlayerInRange(attackRange) || !CanAttack)
            return;
        
        Vector2 playerPos = GetPlayerPosition();
        Vector2 monsterPos = transform.position;
        Vector2 directionToPlayer = (playerPos - monsterPos).normalized;
        
        // Check if player is behind the monster
        bool playerIsBehind = (patrolDirection > 0 && directionToPlayer.x < 0) ||
                             (patrolDirection < 0 && directionToPlayer.x > 0);
        
        if (playerIsBehind)
        {
            // Turn around to face the player
            patrolDirection *= -1f;
            FlipSprite();
        }
        
        // Attempt to eat the player
        TransitionToState(MonsterState.Attacking);
    }
    
    private void UpdatePatrolCenter()
    {
        // Update patrol center when grounded to current position
        // This helps when monster falls to a new platform
        if (hasTouchedGround && platformDetector.IsGrounded && 
            (currentState == MonsterState.Idle || currentState == MonsterState.Spawning))
        {
            patrolCenter = transform.position;
        }
    }
    
    private void FlipSprite()
    {
        if (spriteRenderer)
        {
            spriteRenderer.flipX = patrolDirection < 0;
        }
    }
    
    protected override void HandleIdleState()
    {
        // Go straight to patrolling after spawn
        if (stateTimer >= idleWaitTime)
        {
            TransitionToState(MonsterState.Patrolling);
        }
    }
    
    protected override void HandlePatrollingState()
    {
        // Ground monsters don't chase, they just patrol
        // Player detection and eating is handled in CheckForPlayerToEat()
    }
    
    protected override void HandleChasingState()
    {
        // Ground monsters don't chase, redirect to patrolling
        TransitionToState(MonsterState.Patrolling);
    }
    
    protected override void OnStateEnter(MonsterState state)
    {
        base.OnStateEnter(state);
        
        switch (state)
        {
            case MonsterState.Patrolling:
                isAtEdge = false;
                edgePauseTimer = 0f;
                break;
            case MonsterState.Attacking:
                // Monster stops and attacks
                break;
        }
    }
    
    protected override void OnStateExit(MonsterState state)
    {
        base.OnStateExit(state);
        
        switch (state)
        {
            case MonsterState.Attacking:
                // After attacking, continue patrolling in current direction
                break;
        }
    }
    
    // Debug visualization
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        
        // Patrol area
        if (Application.isPlaying && hasTouchedGround)
        {
            Gizmos.color = Color.cyan;
            Vector3 leftBound = new Vector3(patrolCenter.x - patrolDistance, patrolCenter.y, 0);
            Vector3 rightBound = new Vector3(patrolCenter.x + patrolDistance, patrolCenter.y, 0);
            Gizmos.DrawLine(leftBound, rightBound);
            Gizmos.DrawWireSphere(patrolCenter, 0.2f);
        }
        
        // Current direction
        if (Application.isPlaying && patrolDirection != 0)
        {
            Gizmos.color = Color.magenta;
            Vector3 directionIndicator = transform.position + Vector3.right * patrolDirection * 0.5f;
            Gizmos.DrawLine(transform.position, directionIndicator);
        }
        
        // Edge state indicator
        if (Application.isPlaying && isAtEdge)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, Vector3.one * 0.3f);
        }
        
        // Attack cooldown indicator
        if (Application.isPlaying && !CanAttack)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.3f);
        }
    }
}
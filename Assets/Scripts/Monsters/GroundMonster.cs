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
    private Vector2 _patrolCenter;
    private float _patrolDirection = 1f; // 1 for right, -1 for left
    private float _edgePauseTimer;
    private bool _isAtEdge;
    private bool _hasTouchedGround;
    
    // Components
    private PlatformDetector _platformDetector;
    
    protected override void Awake()
    {
        base.Awake();
        monsterType = MonsterType.Ground;
        _platformDetector = GetComponent<PlatformDetector>();
        
        // Configure rigidbody for ground movement
        if (MonsterRigidbody)
        {
            MonsterRigidbody.gravityScale = fallGravityScale;
            MonsterRigidbody.freezeRotation = true;
        }
    }
    
    public override void Initialize()
    {
        base.Initialize();
        _patrolCenter = transform.position;
        _patrolDirection = Random.value > 0.5f ? 1f : -1f; // Random initial direction
        _hasTouchedGround = false;
        _isAtEdge = false;
        _edgePauseTimer = 0f;
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
        if (!_hasTouchedGround && _platformDetector.IsGrounded)
        {
            _hasTouchedGround = true;
            _patrolCenter = transform.position;
            
            // Reduce gravity once grounded
            if (MonsterRigidbody)
            {
                MonsterRigidbody.gravityScale = 1f;
            }
        }
    }
    
    private void HandleMovement()
    {
        if (!_hasTouchedGround) return; // Still falling
        
        Vector2 velocity = MonsterRigidbody.linearVelocity;
        
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
        velocity = _platformDetector.GetSafeMovementDirection(velocity);
        MonsterRigidbody.linearVelocity = velocity;
    }
    
    private void HandlePatrolMovement(ref Vector2 velocity)
    {
        // Handle edge pausing
        if (_isAtEdge)
        {
            _edgePauseTimer += Time.deltaTime;
            velocity.x = 0;
            
            if (_edgePauseTimer >= edgePauseTime)
            {
                // Change direction and stop being at edge
                _patrolDirection *= -1f;
                _isAtEdge = false;
                _edgePauseTimer = 0f;
                FlipSprite();
            }
            return;
        }
        
        // Check for edges
        bool canContinue = (_patrolDirection > 0 && _platformDetector.CanMoveRight) ||
                          (_patrolDirection < 0 && _platformDetector.CanMoveLeft);
        
        if (!canContinue)
        {
            _isAtEdge = true;
            velocity.x = 0;
            return;
        }
        
        // Check patrol distance limits
        float distanceFromCenter = transform.position.x - _patrolCenter.x;
        if (Mathf.Abs(distanceFromCenter) > patrolDistance)
        {
            // Turn around if we've gone too far
            _patrolDirection = distanceFromCenter > 0 ? -1f : 1f;
            FlipSprite();
        }
        
        // Apply patrol movement
        float targetSpeed = moveSpeed * _patrolDirection;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, directionChangeSpeed * Time.deltaTime);
    }
    
    private void CheckForPlayerToEat()
    {
        if (!GameManager || !GameManager.IsPlayerValid() || currentState == MonsterState.Attacking)
            return;
        
        // Check if player is in attack range and we can attack (cooldown finished)
        if (!IsPlayerInRange(attackRange) || !CanAttack)
            return;
        
        Vector2 playerPos = GetPlayerPosition();
        Vector2 monsterPos = transform.position;
        Vector2 directionToPlayer = (playerPos - monsterPos).normalized;
        
        // Check if player is behind the monster
        bool playerIsBehind = (_patrolDirection > 0 && directionToPlayer.x < 0) ||
                             (_patrolDirection < 0 && directionToPlayer.x > 0);
        
        if (playerIsBehind)
        {
            // Turn around to face the player
            _patrolDirection *= -1f;
            FlipSprite();
        }
        
        // Attempt to eat the player
        TransitionToState(MonsterState.Attacking);
    }
    
    private void UpdatePatrolCenter()
    {
        // Update patrol center when grounded to current position
        // This helps when monster falls to a new platform
        if (_hasTouchedGround && _platformDetector.IsGrounded && 
            (currentState == MonsterState.Idle || currentState == MonsterState.Spawning))
        {
            _patrolCenter = transform.position;
        }
    }
    
    private void FlipSprite()
    {
        if (SpriteRenderer)
        {
            SpriteRenderer.flipX = _patrolDirection < 0;
        }
    }
    
    protected override void HandleIdleState()
    {
        // Go straight to patrolling after spawn
        if (StateTimer >= idleWaitTime)
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
                _isAtEdge = false;
                _edgePauseTimer = 0f;
                break;
            case MonsterState.Attacking:
                // Monster stops and attacks
                break;
        }
    }
    
    protected override void OnAttackStart()
    {
        base.OnAttackStart();
        
        // Play chomp animation, sound effect, or visual feedback
        Debug.Log($"{name} starts chomping!");
        
        // Spawn attack effect if assigned
        if (attackEffect)
        {
            GameObject effect = Instantiate(attackEffect, transform.position, Quaternion.identity);
            Destroy(effect, attackWindupTime + attackDuration);
        }
    }
    
    protected override void OnAttackComplete()
    {
        base.OnAttackComplete();
        Debug.Log($"{name} finished chomping attack");
    }
    
    protected override MonsterState GetStateAfterAttack()
    {
        // Ground monsters always return to patrolling after attack
        return MonsterState.Patrolling;
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
        if (Application.isPlaying && _hasTouchedGround)
        {
            Gizmos.color = Color.cyan;
            Vector3 leftBound = new Vector3(_patrolCenter.x - patrolDistance, _patrolCenter.y, 0);
            Vector3 rightBound = new Vector3(_patrolCenter.x + patrolDistance, _patrolCenter.y, 0);
            Gizmos.DrawLine(leftBound, rightBound);
            Gizmos.DrawWireSphere(_patrolCenter, 0.2f);
        }
        
        // Current direction
        if (Application.isPlaying && _patrolDirection != 0)
        {
            Gizmos.color = Color.magenta;
            Vector3 directionIndicator = transform.position + Vector3.right * _patrolDirection * 0.5f;
            Gizmos.DrawLine(transform.position, directionIndicator);
        }
        
        // Edge state indicator
        if (Application.isPlaying && _isAtEdge)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 1.5f, Vector3.one * 0.3f);
        }
        
        // Attack cooldown indicator
        if (Application.isPlaying && !CanAttack)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.3f);
        }
    }
}
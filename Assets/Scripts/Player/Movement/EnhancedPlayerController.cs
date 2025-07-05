using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class EnhancedPlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnhancedPlayerMovementStats stats;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private LayerMask wallLayer;
    
    // Components
    private Rigidbody2D _rb;
    private CapsuleCollider2D _col;
    private PlayerInputActions _inputActions;
    
    // Input
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private bool _jumpHeld;
    private bool _dashPressed;
    private bool _crouchPressed;
    
    // State flags
    private bool _isGrounded;
    private bool _isCeilingDetected;
    private bool _isFacingRight = true;
    private bool _isDashing;
    private bool _canDash = true;
    private bool _isJumping;
    private bool _isCrouching;
    private bool _isOnSlope;
    private bool _isNearApex;
    private bool _endedJumpEarly;
    
    // Collision info
    private RaycastHit2D _groundHit;
    private float _slopeAngle;
    private Vector2 _slopeNormal;
    
    // Timers
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _jumpHoldCounter;
    private float _dashCooldownCounter;
    private float _lastGroundedTime;
    
    // Physics
    private Vector2 _velocity;
    private Vector2 _externalForce;
    private float _baseGravityScale;
    
    // Platform handling
    private Transform _currentPlatform;
    private Vector2 _platformVelocity;
    
    // Events
    public event Action<AbilitySlot> OnAbilityUsed;
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnDash;
    public event Action<bool> OnCrouch;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CapsuleCollider2D>();
        
        // Setup input
        _inputActions = new PlayerInputActions();
        SetupInputCallbacks();
        
        // Set initial gravity
        _baseGravityScale = stats.gravityScale;
        _rb.gravityScale = _baseGravityScale;
        
        // Configure rigidbody
        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
    
    private void OnEnable()
    {
        _inputActions.Enable();
    }
    
    private void OnDisable()
    {
        _inputActions.Disable();
    }
    
    private void SetupInputCallbacks()
    {
        _inputActions.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _inputActions.Player.Move.canceled += ctx => _moveInput = Vector2.zero;
        
        _inputActions.Player.Jump.started += ctx => OnJumpInput(true);
        _inputActions.Player.Jump.canceled += ctx => OnJumpInput(false);
        
        _inputActions.Player.Dash.started += ctx => _dashPressed = true;
        
        _inputActions.Player.Ability1.started += ctx => OnAbilityUsed?.Invoke(AbilitySlot.Ability1);
        _inputActions.Player.Ability2.started += ctx => OnAbilityUsed?.Invoke(AbilitySlot.Ability2);
        _inputActions.Player.Ability3.started += ctx => OnAbilityUsed?.Invoke(AbilitySlot.Ability3);
        _inputActions.Player.Ability4.started += ctx => OnAbilityUsed?.Invoke(AbilitySlot.Ability4);
    }
    
    private void Update()
    {
        UpdateTimers();
        GatherInput();
        CheckCollisions();
        
        // Handle input-based actions
        HandleJumpBuffer();
        HandleDash();
        HandleCrouch();
    }
    
    private void FixedUpdate()
    {
        if (!_isDashing)
        {
            HandleMovement();
            HandleJump();
            HandleGravity();
            HandleExternalForces();
        }
        
        ApplyVelocity();
    }
    
    private void GatherInput()
    {
        // Apply dead zone to horizontal input
        if (Mathf.Abs(_moveInput.x) < stats.horizontalDeadZone)
            _moveInput.x = 0;
    }
    
    private void UpdateTimers()
    {
        float deltaTime = Time.deltaTime;
        
        // Coyote time
        if (_isGrounded)
        {
            _coyoteTimeCounter = stats.coyoteTime;
            _lastGroundedTime = Time.time;
        }
        else
            _coyoteTimeCounter -= deltaTime;
        
        // Jump buffer
        if (_jumpPressed)
        {
            _jumpBufferCounter = stats.jumpBufferTime;
            _jumpPressed = false;
        }
        else
            _jumpBufferCounter -= deltaTime;
        
        // Jump hold
        if (_isJumping && _jumpHeld)
            _jumpHoldCounter += deltaTime;
        
        // Dash cooldown
        if (!_canDash)
        {
            _dashCooldownCounter -= deltaTime;
            if (_dashCooldownCounter <= 0)
                _canDash = true;
        }
    }
    
    private void CheckCollisions()
    {
        // Store previous grounded state
        bool wasGrounded = _isGrounded;
        
        // Ground check using CapsuleCast for better accuracy
        Vector2 capsuleBottom = new Vector2(_col.bounds.center.x, _col.bounds.min.y);
        _groundHit = Physics2D.CapsuleCast(
            _col.bounds.center, 
            _col.size, 
            _col.direction, 
            0, 
            Vector2.down, 
            stats.groundCheckDistance, 
            groundLayer | platformLayer
        );
        
        _isGrounded = _groundHit.collider;
        
        // Ceiling check
        _isCeilingDetected = Physics2D.CapsuleCast(
            _col.bounds.center, 
            _col.size, 
            _col.direction, 
            0, 
            Vector2.up, 
            stats.ceilingCheckDistance, 
            groundLayer
        );
        
        // Slope detection
        if (_isGrounded)
        {
            _slopeNormal = _groundHit.normal;
            _slopeAngle = Vector2.Angle(_slopeNormal, Vector2.up);
            _isOnSlope = _slopeAngle > 0.1f && _slopeAngle <= stats.maxSlopeAngle;
        }
        else
        {
            _isOnSlope = false;
        }
        
        // Landing event
        if (!wasGrounded && _isGrounded)
        {
            _isJumping = false;
            _endedJumpEarly = false;
            OnLand?.Invoke();
        }
        
        // Platform handling
        HandlePlatform();
    }
    
    private void HandlePlatform()
    {
        if (_isGrounded && _groundHit.collider)
        {
            if (_groundHit.collider.CompareTag("MovingPlatform"))
            {
                Transform platform = _groundHit.collider.transform;
                if (_currentPlatform != platform)
                {
                    _currentPlatform = platform;
                    transform.SetParent(platform);
                }
            }
            else if (_currentPlatform)
            {
                transform.SetParent(null);
                _currentPlatform = null;
            }
        }
        else if (_currentPlatform)
        {
            transform.SetParent(null);
            _currentPlatform = null;
        }
    }
    
    private void HandleMovement()
    {
        // Calculate target speed based on apex state
        float targetSpeed = _moveInput.x * (_isNearApex ? stats.GetApexSpeed() : stats.CurrentMaxSpeed);
        
        // Apply slope speed modifiers
        if (_isOnSlope && _isGrounded)
        {
            float slopeModifier = _moveInput.x * _slopeNormal.x > 0 ? 
                stats.slopeSpeedDecrease : stats.slopeSpeedIncrease;
            targetSpeed *= slopeModifier;
        }
        
        // Apply crouch speed modifier
        if (_isCrouching)
            targetSpeed *= stats.crouchSpeedMultiplier;
        
        // Calculate acceleration
        float acceleration = _isGrounded ? stats.runAcceleration : stats.runAcceleration * stats.airControlMultiplier;
        float deceleration = _isGrounded ? stats.groundDeceleration : stats.airDeceleration;
        
        // Smooth direction changes
        if (!Mathf.Approximately(Mathf.Sign(targetSpeed), Mathf.Sign(_velocity.x)) && Mathf.Abs(_velocity.x) > stats.idleThreshold)
        {
            deceleration *= (1f + stats.turnSmoothness * 10f); // Faster deceleration for direction changes
        }
        
        // Apply acceleration/deceleration
        if (Mathf.Abs(targetSpeed) > stats.idleThreshold)
        {
            _velocity.x = Mathf.MoveTowards(_velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            _velocity.x = Mathf.MoveTowards(_velocity.x, 0, deceleration * Time.fixedDeltaTime);
            
            // Apply friction when grounded with no input
            if (_isGrounded && Mathf.Abs(_velocity.x) < stats.idleThreshold)
            {
                _velocity.x = 0;
            }
        }
        
        // Handle sprite flipping
        if (_moveInput.x != 0 && !_isDashing)
        {
            bool shouldFaceRight = _moveInput.x > 0;
            if (shouldFaceRight != _isFacingRight)
                Flip();
        }
    }
    
    private void HandleJumpBuffer()
    {
        if (_jumpBufferCounter > 0 && (_coyoteTimeCounter > 0 || _isGrounded) && !_isJumping)
        {
            Jump();
        }
    }
    
    private void HandleJump()
    {
        // Variable jump height
        if (_isJumping && !_jumpHeld && _rb.linearVelocity.y > 0)
        {
            _endedJumpEarly = true;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * stats.jumpCutMultiplier);
            _velocity.y = _rb.linearVelocity.y;
            _isJumping = false;
        }
        
        // Max jump hold duration
        if (_jumpHoldCounter > stats.jumpHoldDuration)
        {
            _isJumping = false;
        }
    }
    
    private void HandleGravity()
    {
        if (_isDashing) return;
        
        // Check if near apex
        _isNearApex = stats.IsNearApex(_velocity.y) && !_isGrounded;
        
        // Grounding force
        if (_isGrounded && _velocity.y <= 0)
        {
            _velocity.y = stats.groundingForce;
        }
        else
        {
            // Calculate gravity multiplier
            float gravityMultiplier = 1f;
            
            if (_isNearApex)
            {
                // Anti-gravity at apex for floaty feel
                gravityMultiplier = stats.apexGravityMultiplier;
            }
            else if (_velocity.y < 0)
            {
                // Falling
                gravityMultiplier = stats.fallGravityMultiplier;
            }
            else if (_velocity.y > 0 && _endedJumpEarly)
            {
                // Rising but jump cut early
                gravityMultiplier = stats.quickFallGravityMultiplier;
            }
            
            _rb.gravityScale = _baseGravityScale * gravityMultiplier;
            
            // Let physics handle the velocity
            _velocity.y = _rb.linearVelocity.y;
            
            // Clamp fall speed
            if (_velocity.y < -stats.maxFallSpeed)
                _velocity.y = -stats.maxFallSpeed;
        }
    }
    
    private void HandleExternalForces()
    {
        // Decay external forces
        _externalForce = Vector2.MoveTowards(_externalForce, Vector2.zero, stats.externalForceDecay * Time.fixedDeltaTime);
        
        // Apply external forces to velocity
        _velocity += _externalForce * Time.fixedDeltaTime;
    }
    
    private void HandleDash()
    {
        if (_dashPressed && _canDash && stats.canDash && !_isCrouching)
        {
            StartCoroutine(DashCoroutine());
            _dashPressed = false;
        }
    }
    
    private void HandleCrouch()
    {
        if (stats.canCrouch && _isGrounded)
        {
            bool shouldCrouch = _moveInput.y < -0.5f;
            if (shouldCrouch != _isCrouching)
            {
                _isCrouching = shouldCrouch;
                OnCrouch?.Invoke(_isCrouching);
                
                // Adjust collider size
                if (_isCrouching)
                {
                    _col.size = new Vector2(_col.size.x, _col.size.y * 0.5f);
                    _col.offset = new Vector2(_col.offset.x, _col.offset.y - _col.size.y * 0.25f);
                }
                else if (!_isCeilingDetected) // Only stand if no ceiling
                {
                    _col.size = new Vector2(_col.size.x, _col.size.y * 2f);
                    _col.offset = new Vector2(_col.offset.x, _col.offset.y + _col.size.y * 0.125f);
                }
            }
        }
    }
    
    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        _canDash = false;
        _dashCooldownCounter = stats.dashCooldown;
        
        OnDash?.Invoke();
        
        // Disable gravity during dash
        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0;
        
        // Dash direction
        Vector2 dashDirection = _moveInput.normalized;
        if (dashDirection == Vector2.zero)
            dashDirection = new Vector2(_isFacingRight ? 1 : -1, 0);
        
        // Apply dash velocity
        _velocity = dashDirection * stats.CurrentDashSpeed;
        _rb.linearVelocity = _velocity;
        
        yield return new WaitForSeconds(stats.dashDuration);
        
        // End dash
        _rb.gravityScale = originalGravity;
        _isDashing = false;
        
        // Preserve momentum if enabled
        if (stats.preserveMomentumAfterDash)
        {
            _velocity = _rb.linearVelocity * 0.5f;
        }
        else
        {
            _velocity = new Vector2(0, _rb.linearVelocity.y);
        }
    }
    
    private void ApplyVelocity()
    {
        if (!_isDashing)
        {
            // Only set X velocity, let physics handle Y
            _rb.linearVelocity = new Vector2(_velocity.x, _rb.linearVelocity.y);
            
            // Apply Y velocity only when we're explicitly setting it (jump, dash, external forces)
            if (Mathf.Abs(_velocity.y - _rb.linearVelocity.y) > 0.1f)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _velocity.y);
            }
        }
        
        // Sync velocity with rigidbody
        _velocity = _rb.linearVelocity;
    }
    
    private void Jump()
    {
        _isJumping = true;
        _jumpBufferCounter = 0;
        _coyoteTimeCounter = 0;
        _jumpHoldCounter = 0;
        _endedJumpEarly = false;
        
        // Set velocity directly on rigidbody
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, stats.CurrentJumpForce);
        _velocity.y = stats.CurrentJumpForce;
        
        OnJump?.Invoke();
    }
    
    private void OnJumpInput(bool pressed)
    {
        if (pressed)
        {
            _jumpPressed = true;
            _jumpHeld = true;
        }
        else
        {
            _jumpHeld = false;
        }
    }
    
    private void Flip()
    {
        _isFacingRight = !_isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (_isFacingRight ? 1 : -1);
        transform.localScale = scale;
    }
    
    // Public methods
    public void AddExternalForce(Vector2 force)
    {
        _externalForce += Vector2.ClampMagnitude(force, stats.maxExternalForce);
    }
    
    public void SetVelocity(Vector2 newVelocity)
    {
        _velocity = newVelocity;
        _rb.linearVelocity = _velocity;
    }
    
    public void ApplySpeedMultiplier(float multiplier)
    {
        stats.speedMultiplier = multiplier;
    }
    
    public void ApplyJumpMultiplier(float multiplier)
    {
        stats.jumpMultiplier = multiplier;
    }
    
    public void ApplyDashSpeedMultiplier(float multiplier)
    {
        stats.dashSpeedMultiplier = multiplier;
    }
    
    // Getters
    public bool IsGrounded => _isGrounded;
    public bool IsDashing => _isDashing;
    public bool IsJumping => _isJumping;
    public bool IsCrouching => _isCrouching;
    public bool IsNearApex => _isNearApex;
    public Vector2 Velocity => _velocity;
    
    // Debug
    private void OnDrawGizmos()
    {
        if (_col == null) return;
        
        // Ground check
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 groundCheckPos = new Vector3(_col.bounds.center.x, _col.bounds.min.y - stats.groundCheckDistance, 0);
        Gizmos.DrawWireCube(groundCheckPos, new Vector3(_col.bounds.size.x, 0.05f, 0));
        
        // Ceiling check
        Gizmos.color = _isCeilingDetected ? Color.red : Color.yellow;
        Vector3 ceilingCheckPos = new Vector3(_col.bounds.center.x, _col.bounds.max.y + stats.ceilingCheckDistance, 0);
        Gizmos.DrawWireCube(ceilingCheckPos, new Vector3(_col.bounds.size.x, 0.05f, 0));
        
        // Slope normal
        if (_isOnSlope && Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, _slopeNormal);
        }
    }
}
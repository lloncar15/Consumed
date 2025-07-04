using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovementStats stats;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Collision")]
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private float ceilingCheckRadius = 0.2f;
    
    // Components
    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private PlayerInputActions _inputActions;
    
    // Input
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private bool _jumpHeld;
    private bool _dashPressed;
    
    // State
    private bool _isGrounded;
    private bool _isFacingRight = true;
    private bool _isDashing;
    private bool _canDash = true;
    private bool _isJumping;
    
    // Timers
    private float _coyoteTimeCounter;
    private float _jumpBufferCounter;
    private float _jumpHoldCounter;
    private float _dashCooldownCounter;
    
    // Events
    public event Action<AbilitySlot> OnAbilityUsed;
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnDash;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();
        
        // Setup input
        _inputActions = new PlayerInputActions();
        SetupInputCallbacks();
        
        _rb.gravityScale = stats.gravityScale;
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
        
        _inputActions.Player.Interact.started += ctx => OnInteraction();
        
        _inputActions.Player.Ability1.started += ctx => OnAbilityUsed?.Invoke(AbilitySlot.Ability1);
        _inputActions.Player.Ability2.started += ctx => OnAbilityUsed?.Invoke(AbilitySlot.Ability2);
        _inputActions.Player.Ability3.started += ctx => OnAbilityUsed?.Invoke(AbilitySlot.Ability3);
        _inputActions.Player.Ability4.started += ctx => OnAbilityUsed?.Invoke(AbilitySlot.Ability4);
    }
    
    private void Update()
    {
        UpdateTimers();
        CheckGrounded();
        HandleJumpBuffer();
        HandleDash();
    }
    
    private void FixedUpdate()
    {
        if (!_isDashing)
        {
            HandleRun();
            HandleJump();
            HandleGravity();
        }
    }
    
    private void UpdateTimers()
    {
        // Coyote time
        if (_isGrounded)
            _coyoteTimeCounter = stats.coyoteTime;
        else
            _coyoteTimeCounter -= Time.deltaTime;
        
        // Jump buffer
        if (_jumpPressed)
        {
            _jumpBufferCounter = stats.jumpBufferTime;
            _jumpPressed = false;
        }
        else
            _jumpBufferCounter -= Time.deltaTime;
        
        // Jump hold
        if (_isJumping && _jumpHeld)
            _jumpHoldCounter += Time.deltaTime;
        
        // Dash cooldown
        if (!_canDash)
        {
            _dashCooldownCounter -= Time.deltaTime;
            if (_dashCooldownCounter <= 0)
                _canDash = true;
        }
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void CheckGrounded()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        // Landing
        if (wasGrounded || !_isGrounded) return;
        
        _isJumping = false;
        OnLand?.Invoke();
    }
    
    private void HandleRun()
    {
        float targetSpeed = _moveInput.x * stats.CurrentMaxSpeed;
        float speedDif = targetSpeed - _rb.linearVelocity.x;
        float accelRate = (Mathf.Abs(targetSpeed) > stats.idleThreshold) ? 
            stats.runAcceleration : stats.runDecceleration;
        
        // Apply acceleration/deceleration
        float movement = speedDif * accelRate;
        
        // Apply friction when no input
        if (Mathf.Abs(_moveInput.x) < stats.idleThreshold && _isGrounded)
        {
            float frictionForce = Mathf.Min(Mathf.Abs(_rb.linearVelocity.x), stats.frictionAmount);
            frictionForce *= Mathf.Sign(_rb.linearVelocity.x);
            _rb.AddForce(Vector2.right * -frictionForce, ForceMode2D.Impulse);
        }
        
        // Apply air drag
        if (!_isGrounded)
            movement *= stats.airDragMultiplier;
        
        _rb.AddForce(movement * Vector2.right, ForceMode2D.Force);
        
        // Flip sprite
        if (_moveInput.x == 0) return;
        
        bool shouldFaceRight = _moveInput.x > 0;
        if (shouldFaceRight != _isFacingRight)
            Flip();
    }
    
    private void HandleJumpBuffer()
    {
        if (_jumpBufferCounter > 0 && _coyoteTimeCounter > 0 && !_isJumping)
        {
            Jump();
        }
    }
    
    private void HandleJump()
    {
        // Variable jump height
        if (!_jumpHeld && _isJumping && _rb.linearVelocity.y > 0)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * stats.jumpCutMultiplier);
            _isJumping = false;
        }
        
        // Max jump hold
        if (_jumpHoldCounter > stats.jumpHoldDuration)
        {
            _isJumping = false;
        }
    }
    
    private void HandleGravity() {
        if (_isDashing) return;

        // Apply different gravity scales based on movement state
        if (_rb.linearVelocity.y < 0)
        {
            // Falling - apply heavier gravity for better game feel
            _rb.gravityScale = stats.gravityScale * stats.fallGravityMultiplier;  // 3 * 4 = 12
        }
        else if (_rb.linearVelocity.y > 0 && !_jumpHeld)
        {
            // Rising but not holding jump - cut jump short
            _rb.gravityScale = stats.gravityScale * stats.quickFallGravityMultiplier;  // 3 * 6 = 18
        }
        else
        {
            // Normal gravity
            _rb.gravityScale = stats.gravityScale;  // 3
        }
    }
    
    private void HandleDash() {
        if (!_dashPressed || !_canDash || !stats.canDash) return;
        
        StartCoroutine(DashCoroutine());
        _dashPressed = false;
    }
    
    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        _canDash = false;
        _dashCooldownCounter = stats.dashCooldown;
        
        OnDash?.Invoke();
        
        // Store original gravity
        float originalGravity = _rb.gravityScale;
        _rb.gravityScale = 0;
        
        // Dash direction (facing direction or input direction)
        Vector2 dashDirection = _moveInput.x != 0 ? 
            new Vector2(Mathf.Sign(_moveInput.x), 0) : 
            new Vector2(_isFacingRight ? 1 : -1, 0);
        
        // Apply dash velocity
        _rb.linearVelocity = dashDirection * stats.CurrentDashSpeed;
        
        yield return new WaitForSeconds(stats.dashDuration);
        
        // End dash
        _rb.gravityScale = stats.gravityScale;
        _isDashing = false;
        
        // Maintain some momentum
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x * 0.5f, _rb.linearVelocity.y);
    }
    
    private void Jump()
    {
        _isJumping = true;
        _jumpBufferCounter = 0;
        _coyoteTimeCounter = 0;
        _jumpHoldCounter = 0;
        
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
        _rb.AddForce(Vector2.up * stats.CurrentJumpForce, ForceMode2D.Impulse);
        
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
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnInteraction() {
        
    }
    
    // Public methods for stat upgrades
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
    
    // Debug
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        
        if (ceilingCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ceilingCheck.position, ceilingCheckRadius);
        }
    }
}
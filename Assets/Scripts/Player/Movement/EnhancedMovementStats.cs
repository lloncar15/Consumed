using UnityEngine;

[CreateAssetMenu(fileName = "EnhancedPlayerMovementStats", menuName = "Player/Enhanced Movement Stats")]
public class EnhancedPlayerMovementStats : ScriptableObject {
    [Header("Run")]
    [Range(1f, 100f)] public float maxRunSpeed = 14f;
    [Range(0.01f, 150f)] public float runAcceleration = 120f;
    [Range(0.01f, 150f)] public float groundDeceleration = 60f;
    [Range(0.01f, 150f)] public float airDeceleration = 30f;
    [Range(0.01f, 1f)] public float frictionAmount = 0.2f;
    [Range(0.01f, 0.99f)] public float horizontalDeadZone = 0.1f;
    
    [Header("Jump")]
    [Range(1f, 50f)] public float jumpForce = 36f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Range(0f, 1f)] public float jumpHoldDuration = 0.15f;
    [Range(1f, 10f)] public float gravityScale = 3f;
    [Range(1f, 20f)] public float fallGravityMultiplier = 1.5f;
    [Range(1f, 20f)] public float quickFallGravityMultiplier = 2f;
    [Range(0f, 10f)] public float maxFallSpeed = 40f;
    
    [Header("Apex Modifiers - Game Feel")]
    [Tooltip("Horizontal speed multiplier at jump apex"), Range(1f, 2f)] 
    public float apexSpeedMultiplier = 1.25f;
    [Tooltip("Gravity reduction at jump apex"), Range(0f, 1f)] 
    public float apexGravityMultiplier = 0.4f;
    [Tooltip("Velocity threshold to be considered at apex"), Range(0.1f, 5f)]
    public float apexThreshold = 2f;
    
    [Header("Assists")]
    [Range(0f, 0.5f)] public float coyoteTime = 0.1f;
    [Range(0f, 0.5f)] public float jumpBufferTime = 0.1f;
    [Tooltip("Grace period for landing detection"), Range(0f, 0.5f)]
    public float jumpGracePeriod = 0.05f;
    
    [Header("Dash")]
    public bool canDash = true;
    [Range(1f, 50f)] public float dashSpeed = 30f;
    [Range(0.1f, 1f)] public float dashDuration = 0.2f;
    [Range(0.1f, 5f)] public float dashCooldown = 1f;
    public bool dashThroughEnemies = true;
    public bool preserveMomentumAfterDash = true;
    
    [Header("Collision Detection")]
    [Tooltip("Distance to check for ground"), Range(0.01f, 0.5f)]
    public float groundCheckDistance = 0.05f;
    [Tooltip("Distance to check for ceiling"), Range(0.01f, 0.5f)]
    public float ceilingCheckDistance = 0.05f;
    [Tooltip("Constant downward force while grounded"), Range(-10f, 0f)]
    public float groundingForce = -1.5f;
    
    [Header("Slopes")]
    [Tooltip("Maximum angle the player can walk on"), Range(0f, 90f)]
    public float maxSlopeAngle = 45f;
    [Tooltip("Extra speed when going down slopes"), Range(0f, 5f)]
    public float slopeSpeedIncrease = 1.5f;
    [Tooltip("Speed reduction when going up slopes"), Range(0f, 1f)]
    public float slopeSpeedDecrease = 0.5f;
    
    [Header("External Forces")]
    [Tooltip("How quickly external forces decay"), Range(0f, 10f)]
    public float externalForceDecay = 5f;
    [Tooltip("Maximum external force that can be applied"), Range(0f, 100f)]
    public float maxExternalForce = 50f;
    
    [Header("Advanced Movement")]
    public bool canWallSlide = true;
    [Range(0f, 10f)] public float wallSlideSpeed = 3f;
    public bool canLedgeGrab = true;
    public bool canCrouch = true;
    [Range(0f, 1f)] public float crouchSpeedMultiplier = 0.5f;
    
    [Header("Feel")]
    [Range(0.01f, 0.5f)] public float idleThreshold = 0.01f;
    [Range(0f, 2f)] public float airControlMultiplier = 0.85f;
    [Tooltip("Smoothing for direction changes"), Range(0f, 1f)]
    public float turnSmoothness = 0.1f;
    
    // Runtime stat modifiers (for upgrades)
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float jumpMultiplier = 1f;
    [HideInInspector] public float dashSpeedMultiplier = 1f;
    
    // Calculated properties
    public float CurrentMaxSpeed => maxRunSpeed * speedMultiplier;
    public float CurrentJumpForce => jumpForce * jumpMultiplier;
    public float CurrentDashSpeed => dashSpeed * dashSpeedMultiplier;
    
    // Helper methods for apex detection
    public bool IsNearApex(float verticalVelocity) => Mathf.Abs(verticalVelocity) < apexThreshold;
    public float GetApexGravity() => gravityScale * apexGravityMultiplier;
    public float GetApexSpeed() => CurrentMaxSpeed * apexSpeedMultiplier;
}
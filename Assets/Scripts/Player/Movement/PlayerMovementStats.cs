using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "Player/Movement Stats")]
public class PlayerMovementStats : ScriptableObject {
    [Header("Run")]
    [Range(1f, 100f)] public float maxRunSpeed = 12f;
    [Range(0.01f, 5f)] public float runAcceleration = 2.5f;
    [Range(0.01f, 5f)] public float runDecceleration = 2f;
    [Range(0.01f, 1f)] public float frictionAmount = 0.2f;
    
    [Header("Jump")]
    [Range(1f, 50f)] public float jumpForce = 25f;
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Range(0f, 1f)] public float jumpHoldDuration = 0.15f;
    [Range(1f, 10f)] public float gravityScale = 3f;
    [Range(1f, 20f)] public float fallGravityMultiplier = 4f;
    [Range(1f, 20f)] public float quickFallGravityMultiplier = 6f;
    
    [Header("Assists")]
    [Range(0f, 0.5f)] public float coyoteTime = 0.1f;
    [Range(0f, 0.5f)] public float jumpBufferTime = 0.1f;
    
    [Header("Dash")]
    public bool canDash = true;
    [Range(1f, 50f)] public float dashSpeed = 30f;
    [Range(0.1f, 1f)] public float dashDuration = 0.2f;
    [Range(0.1f, 5f)] public float dashCooldown = 1f;
    
    [Header("Feel")]
    [Range(0.01f, 0.5f)] public float idleThreshold = 0.01f;
    [Range(0f, 2f)] public float airDragMultiplier = 0.85f;
    
    // Runtime stat modifiers (for upgrades)
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float jumpMultiplier = 1f;
    [HideInInspector] public float dashSpeedMultiplier = 1f;
    
    // Calculated properties
    public float CurrentMaxSpeed => maxRunSpeed * speedMultiplier;
    public float CurrentJumpForce => jumpForce * jumpMultiplier;
    public float CurrentDashSpeed => dashSpeed * dashSpeedMultiplier;
}
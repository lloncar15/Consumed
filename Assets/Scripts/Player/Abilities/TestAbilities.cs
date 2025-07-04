using UnityEngine;
using System.Collections;

// Example Ability 1: Double Jump
[CreateAssetMenu(fileName = "DoubleJumpAbility", menuName = "Player/Abilities/Double Jump")]
public class DoubleJumpAbility : PlayerAbility
{
    [Header("Double Jump Settings")]
    [SerializeField] private float jumpForce = 20f;
    [SerializeField] private int maxExtraJumps = 1;
    
    private int _jumpsRemaining;
    private bool _wasGrounded;
    
    public override void Initialize(EnhancedPlayerController playerController)
    {
        base.Initialize(playerController);
        _jumpsRemaining = maxExtraJumps;
        
        // Subscribe to events
        Player.OnLand += ResetJumps;
    }
    
    public override bool CanActivate()
    {
        return base.CanActivate() && _jumpsRemaining > 0 && !IsGrounded();
    }
    
    protected override void OnActivate()
    {
        _jumpsRemaining--;
        
        // Reset vertical velocity and apply jump
        Rigidbody2D rb = Player.GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        
        // You can add effects here
        // CreateJumpEffect();
    }
    
    private void ResetJumps()
    {
        _jumpsRemaining = maxExtraJumps;
    }
    
    private bool IsGrounded()
    {
        // Simple ground check - in production, use the player's ground check
        return Physics2D.Raycast(Player.transform.position, Vector2.down, 1.1f);
    }
}

// Example Ability 2: Air Dash
[CreateAssetMenu(fileName = "AirDashAbility", menuName = "Player/Abilities/Air Dash")]
public class AirDashAbility : PlayerAbility
{
    [Header("Air Dash Settings")]
    [SerializeField] private float dashSpeed = 25f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private bool resetVelocity = true;
    [SerializeField] private bool canDashInAllDirections = true;
    
    private Coroutine _dashCoroutine;
    
    public override bool CanActivate()
    {
        return base.CanActivate() && !IsGrounded() && _dashCoroutine == null;
    }
    
    protected override void OnActivate()
    {
        _dashCoroutine = Player.StartCoroutine(PerformDash());
    }
    
    private IEnumerator PerformDash()
    {
        Rigidbody2D rb = Player.GetComponent<Rigidbody2D>();
        float originalGravity = rb.gravityScale;
        
        // Disable gravity during dash
        rb.gravityScale = 0;
        
        // Get dash direction
        Vector2 dashDir = GetDashDirection();
        
        // Reset velocity if needed
        if (resetVelocity)
            rb.linearVelocity = Vector2.zero;
        
        // Apply dash velocity
        rb.linearVelocity = dashDir * dashSpeed;
        
        yield return new WaitForSeconds(dashDuration);
        
        // Restore gravity
        rb.gravityScale = originalGravity;
        
        // Maintain some momentum
        rb.linearVelocity *= 0.7f;
        
        _dashCoroutine = null;
    }
    
    private Vector2 GetDashDirection()
    {
        // Get input direction
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = canDashInAllDirections ? Input.GetAxisRaw("Vertical") : 0;
        
        Vector2 direction = new Vector2(horizontal, vertical).normalized;
        
        // If no input, dash in facing direction
        if (direction == Vector2.zero)
        {
            direction = Player.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }
        
        return direction;
    }
    
    private bool IsGrounded()
    {
        return Physics2D.Raycast(Player.transform.position, Vector2.down, 1.1f);
    }
}

// Example Ability 3: Ground Pound
[CreateAssetMenu(fileName = "GroundPoundAbility", menuName = "Player/Abilities/Ground Pound")]
public class GroundPoundAbility : PlayerAbility
{
    [Header("Ground Pound Settings")]
    [SerializeField] private float poundSpeed = 40f;
    [SerializeField] private float impactRadius = 3f;
    [SerializeField] private float impactForce = 10f;
    [SerializeField] private LayerMask enemyLayer;
    
    private bool _isPounding;
    
    public override void Initialize(EnhancedPlayerController playerController)
    {
        base.Initialize(playerController);
        Player.OnLand += CheckGroundPoundImpact;
    }
    
    public override bool CanActivate()
    {
        return base.CanActivate() && !IsGrounded() && !_isPounding;
    }
    
    protected override void OnActivate()
    {
        _isPounding = true;
        
        Rigidbody2D rb = Player.GetComponent<Rigidbody2D>();
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, -poundSpeed);
    }
    
    private void CheckGroundPoundImpact()
    {
        if (!_isPounding) return;
        
        _isPounding = false;
        
        // Create impact
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            Player.transform.position, 
            impactRadius, 
            enemyLayer
        );
        
        foreach (var enemy in enemies)
        {
            // Apply force to enemies
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb)
            {
                Vector2 forceDir = (enemy.transform.position - Player.transform.position).normalized;
                enemyRb.AddForce(forceDir * impactForce, ForceMode2D.Impulse);
            }
            
            // Deal damage if enemy has health component
            // enemy.GetComponent<Health>()?.TakeDamage(damage);
        }
        
        // Create visual effect
        // CreateImpactEffect();
    }
    
    private bool IsGrounded()
    {
        return Physics2D.Raycast(Player.transform.position, Vector2.down, 1.1f);
    }
}

// Example Ability 4: Teleport
[CreateAssetMenu(fileName = "TeleportAbility", menuName = "Player/Abilities/Teleport")]
public class TeleportAbility : PlayerAbility
{
    [Header("Teleport Settings")]
    [SerializeField] private float teleportDistance = 5f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private bool maintainMomentum = true;
    
    protected override void OnActivate()
    {
        Vector2 teleportDirection = GetTeleportDirection();
        Vector2 targetPosition = (Vector2)Player.transform.position + (teleportDirection * teleportDistance);
        
        // Check if path is clear
        RaycastHit2D hit = Physics2D.Raycast(
            Player.transform.position, 
            teleportDirection, 
            teleportDistance, 
            obstacleLayer
        );
        
        if (hit.collider != null)
        {
            // Teleport to just before the obstacle
            targetPosition = hit.point - (teleportDirection * 0.5f);
        }
        
        // Perform teleport
        Player.transform.position = targetPosition;
        
        // Maintain momentum if enabled
        if (!maintainMomentum)
        {
            Rigidbody2D rb = Player.GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
        }
        
        // Create teleport effect
        // CreateTeleportEffect();
    }
    
    private Vector2 GetTeleportDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector2 direction = new Vector2(horizontal, vertical).normalized;
        
        // If no input, teleport in facing direction
        if (direction == Vector2.zero)
        {
            direction = Player.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }
        
        return direction;
    }
}
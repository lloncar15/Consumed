using UnityEngine;

public class PlatformDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("Layer mask for platforms and ground")]
    [SerializeField] private LayerMask platformLayerMask = -1;
    [Tooltip("Distance to check for ground below")]
    [SerializeField] private float groundCheckDistance = 1f;
    [Tooltip("Distance to check for platform edges")]
    [SerializeField] private float edgeCheckDistance = 0.5f;
    [Tooltip("Offset from center for edge detection")]
    [SerializeField] private Vector2 edgeCheckOffset = new Vector2(0.5f, 0f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    // Detection results
    private bool isGrounded;
    private bool hasLeftEdge;
    private bool hasRightEdge;
    private RaycastHit2D groundHit;
    private RaycastHit2D leftEdgeHit;
    private RaycastHit2D rightEdgeHit;
    
    // Properties
    public bool IsGrounded => isGrounded;
    public bool HasLeftEdge => hasLeftEdge;
    public bool HasRightEdge => hasRightEdge;
    public bool CanMoveLeft => hasLeftEdge;
    public bool CanMoveRight => hasRightEdge;
    public Vector2 GroundNormal => groundHit.normal;
    public float GroundDistance => groundHit.distance;
    
    private void Update()
    {
        PerformDetection();
    }
    
    private void PerformDetection()
    {
        Vector2 position = transform.position;
        
        // Ground detection
        groundHit = Physics2D.Raycast(position, Vector2.down, groundCheckDistance, platformLayerMask);
        isGrounded = groundHit.collider != null;
        
        // Edge detection for left side
        Vector2 leftCheckPos = position + new Vector2(-edgeCheckOffset.x, edgeCheckOffset.y);
        leftEdgeHit = Physics2D.Raycast(leftCheckPos, Vector2.down, edgeCheckDistance, platformLayerMask);
        hasLeftEdge = leftEdgeHit.collider != null;
        
        // Edge detection for right side
        Vector2 rightCheckPos = position + new Vector2(edgeCheckOffset.x, edgeCheckOffset.y);
        rightEdgeHit = Physics2D.Raycast(rightCheckPos, Vector2.down, edgeCheckDistance, platformLayerMask);
        hasRightEdge = rightEdgeHit.collider != null;
    }
    
    public bool IsOnSamePlatform(Vector2 targetPosition)
    {
        if (!isGrounded) return false;
        
        RaycastHit2D targetHit = Physics2D.Raycast(targetPosition, Vector2.down, groundCheckDistance * 2f, platformLayerMask);
        return targetHit.collider != null && targetHit.collider == groundHit.collider;
    }
    
    public Vector2 GetSafeMovementDirection(Vector2 currentVelocity)
    {
        if (!isGrounded) return currentVelocity;
        
        Vector2 safeVelocity = currentVelocity;
        
        // Block movement if approaching edge
        if (currentVelocity.x < 0 && !hasLeftEdge)
        {
            safeVelocity.x = 0;
        }
        else if (currentVelocity.x > 0 && !hasRightEdge)
        {
            safeVelocity.x = 0;
        }
        
        return safeVelocity;
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        Vector2 position = transform.position;
        
        // Ground check ray
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(position, position + Vector2.down * groundCheckDistance);
        
        // Left edge check
        Vector2 leftCheckPos = position + new Vector2(-edgeCheckOffset.x, edgeCheckOffset.y);
        Gizmos.color = hasLeftEdge ? Color.green : Color.red;
        Gizmos.DrawLine(leftCheckPos, leftCheckPos + Vector2.down * edgeCheckDistance);
        
        // Right edge check
        Vector2 rightCheckPos = position + new Vector2(edgeCheckOffset.x, edgeCheckOffset.y);
        Gizmos.color = hasRightEdge ? Color.green : Color.red;
        Gizmos.DrawLine(rightCheckPos, rightCheckPos + Vector2.down * edgeCheckDistance);
        
        // Edge check positions
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(leftCheckPos, 0.1f);
        Gizmos.DrawWireSphere(rightCheckPos, 0.1f);
    }
}
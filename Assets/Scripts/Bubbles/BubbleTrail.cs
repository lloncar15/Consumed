using UnityEngine;

[System.Serializable]
public class TrailBubble
{
    [Tooltip("Transform of the trail bubble")]
    public Transform transform;
    [Tooltip("How far behind the main bubble this trail bubble follows")]
    public float followDistance = 1f;
    [Tooltip("How quickly this trail bubble follows the main bubble")]
    public float followSpeed = 5f;
    
    [HideInInspector]
    public Vector3 targetPosition;
    [HideInInspector]
    public Vector3 velocity;
}

public class BubbleTrail : MonoBehaviour
{
    [Header("Trail Configuration")]
    [Tooltip("Array of trail bubbles (should be 3 for comic effect)")]
    [SerializeField] private TrailBubble[] trailBubbles = new TrailBubble[3];
    
    [Tooltip("Base follow distance between trail bubbles")]
    [SerializeField] private float baseFollowDistance = 0.8f;
    
    [Tooltip("Base follow speed for all trail bubbles")]
    [SerializeField] private float baseFollowSpeed = 6f;
    
    [Tooltip("Minimum distance main bubble must move before trail updates")]
    [SerializeField] private float movementThreshold = 0.1f;
    
    [Header("Visual Effects")]
    [Tooltip("How much trail bubbles sway independently")]
    [SerializeField] private float swayAmount = 0.1f;
    
    [Tooltip("Speed of independent sway animation")]
    [SerializeField] private float swaySpeed = 2f;
    
    [Tooltip("Random offset for each trail bubble's sway")]
    [SerializeField] private bool randomizeSwayOffset = true;
    
    // State tracking
    private Vector3 _lastMainBubblePosition;
    private Vector3[] _trailPositions;
    private float[] _swayOffsets;
    private bool _isInitialized = false;
    
    // Main bubble reference (this script should be on the main bubble)
    private Transform _mainBubbleTransform;
    
    private void Awake()
    {
        _mainBubbleTransform = transform;
        InitializeTrail();
    }
    
    private void InitializeTrail()
    {
        if (trailBubbles == null || trailBubbles.Length == 0)
        {
            Debug.LogWarning($"BubbleTrail on {name}: No trail bubbles assigned!");
            return;
        }
        
        _trailPositions = new Vector3[trailBubbles.Length];
        _swayOffsets = new float[trailBubbles.Length];
        
        // Initialize trail bubble settings and positions
        for (int i = 0; i < trailBubbles.Length; i++)
        {
            if (trailBubbles[i].transform == null)
            {
                Debug.LogWarning($"BubbleTrail on {name}: Trail bubble {i} transform is null!");
                continue;
            }
            
            // Set up follow distance (each bubble further behind)
            trailBubbles[i].followDistance = baseFollowDistance * (i + 1);
            trailBubbles[i].followSpeed = baseFollowSpeed;
            
            // Initialize positions
            _trailPositions[i] = _mainBubbleTransform.position - Vector3.right * trailBubbles[i].followDistance;
            trailBubbles[i].transform.position = _trailPositions[i];
            
            // Random sway offset
            if (randomizeSwayOffset)
            {
                _swayOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            }
            else
            {
                _swayOffsets[i] = i * 0.5f; // Slight offset between bubbles
            }
        }
        
        _lastMainBubblePosition = _mainBubbleTransform.position;
        _isInitialized = true;
    }
    
    private void Update()
    {
        if (!_isInitialized) return;
        
        UpdateTrailPositions();
        ApplySwayEffect();
    }
    
    private void UpdateTrailPositions()
    {
        Vector3 currentMainPosition = _mainBubbleTransform.position;
        
        // Only update if main bubble moved significantly
        if (Vector3.Distance(currentMainPosition, _lastMainBubblePosition) < movementThreshold)
            return;
        
        // Update each trail bubble to follow the previous one
        Vector3 targetPosition = currentMainPosition;
        
        for (int i = 0; i < trailBubbles.Length; i++)
        {
            if (trailBubbles[i].transform == null) continue;
            
            // Calculate where this trail bubble should be
            Vector3 directionToTarget = (targetPosition - _trailPositions[i]).normalized;
            Vector3 desiredPosition = targetPosition - directionToTarget * trailBubbles[i].followDistance;
            
            // Smoothly move towards desired position
            _trailPositions[i] = Vector3.SmoothDamp(
                _trailPositions[i],
                desiredPosition,
                ref trailBubbles[i].velocity,
                1f / trailBubbles[i].followSpeed
            );
            
            // Update the next bubble's target to this bubble's position
            targetPosition = _trailPositions[i];
        }
        
        _lastMainBubblePosition = currentMainPosition;
    }
    
    private void ApplySwayEffect()
    {
        // Apply sway animation to each trail bubble
        for (int i = 0; i < trailBubbles.Length; i++)
        {
            if (trailBubbles[i].transform == null) continue;
            
            // Calculate sway offset
            float swayOffset = Mathf.Sin(Time.time * swaySpeed + _swayOffsets[i]) * swayAmount;
            Vector3 swayDirection = Vector3.up; // Sway vertically
            
            // Apply final position with sway
            Vector3 finalPosition = _trailPositions[i] + swayDirection * swayOffset;
            trailBubbles[i].transform.position = finalPosition;
        }
    }
    
    // Public method to refresh trail setup (useful for runtime changes)
    public void RefreshTrail()
    {
        InitializeTrail();
    }
    
    // Method to enable/disable trail effect
    public void SetTrailActive(bool active)
    {
        for (int i = 0; i < trailBubbles.Length; i++)
        {
            if (trailBubbles[i].transform != null)
            {
                trailBubbles[i].transform.gameObject.SetActive(active);
            }
        }
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !_isInitialized) return;
        
        // Draw connections between bubbles
        Gizmos.color = Color.cyan;
        Vector3 previousPos = _mainBubbleTransform.position;
        
        for (int i = 0; i < trailBubbles.Length; i++)
        {
            if (trailBubbles[i].transform == null) continue;
            
            Vector3 currentPos = trailBubbles[i].transform.position;
            Gizmos.DrawLine(previousPos, currentPos);
            
            // Draw follow distance indicators
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentPos, 0.1f);
            
            previousPos = currentPos;
        }
        
        // Draw movement threshold around main bubble
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_mainBubbleTransform.position, movementThreshold);
    }
    
    // Editor helper methods
    #if UNITY_EDITOR
    [ContextMenu("Auto-Setup Trail Bubbles")]
    private void AutoSetupTrailBubbles()
    {
        // Find child objects that could be trail bubbles
        Transform[] children = GetComponentsInChildren<Transform>();
        int trailIndex = 0;
        
        for (int i = 0; i < children.Length && trailIndex < trailBubbles.Length; i++)
        {
            if (children[i] != transform && children[i].name.ToLower().Contains("trail"))
            {
                if (trailBubbles[trailIndex] == null)
                    trailBubbles[trailIndex] = new TrailBubble();
                
                trailBubbles[trailIndex].transform = children[i];
                trailIndex++;
            }
        }
        
        Debug.Log($"Auto-setup found {trailIndex} trail bubbles");
    }
    #endif
}
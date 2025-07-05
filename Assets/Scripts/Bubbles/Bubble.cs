using System;
using UnityEngine;

[RequireComponent(typeof(EdgeCollider2D))]
public class Bubble : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BubbleMovementSettings settings;
    [SerializeField] private BubbleTypeDefinition bubbleTypeDefinition;
    [SerializeField] private GameObject burstEffect;
    
    // Movement state
    private Vector2 _velocity;
    private Vector2 _externalForce;
    private float _lifetime;
    private float _maxLifetime;
    private float _timeAlive;
    
    // Noise sampling
    private float _perlinSeedX;
    private float _perlinSeedY;
    private float _bobSeed;
    
    // Components
    private EdgeCollider2D _bubbleCollider;
    private Transform _bubbleTransform;
    
    // Events
    public static event Action<Bubble, Vector2> OnBubbleBurst;
    public static event Action<Bubble> OnBubbleDestroyed;
    
    // Properties
    public BubbleTypeDefinition TypeDefinition => bubbleTypeDefinition;
    public BubbleCategory Category => bubbleTypeDefinition?.category ?? BubbleCategory.Bad;
    public BubbleSubtype Subtype => bubbleTypeDefinition?.subtype ?? BubbleSubtype.GroundMonster;
    public Vector2 Position => _bubbleTransform.position;
    public bool IsActive { get; private set; }
    
    private void Awake()
    {
        _bubbleCollider = GetComponent<EdgeCollider2D>();
        _bubbleTransform = transform;
        
        // Configure collider as trigger
        _bubbleCollider.isTrigger = true;
    }
    
    private void Update()
    {
        if (!IsActive) return;
        
        UpdateMovement();
        UpdateBoundaries();
        UpdateLifetime();
    }
    
    public void Initialize(BubbleMovementSettings movementSettings, BubbleTypeDefinition typeDefinition, Vector2 startPosition)
    {
        settings = movementSettings;
        bubbleTypeDefinition = typeDefinition;
        _bubbleTransform.position = startPosition;
        
        // Initialize movement state
        _velocity = Vector2.zero;
        _externalForce = Vector2.zero;
        _timeAlive = 0f;
        _maxLifetime = UnityEngine.Random.Range(settings.minLifetime, settings.maxLifetime);
        
        // Initialize noise seeds for unique movement
        _perlinSeedX = UnityEngine.Random.Range(0f, 1000f);
        _perlinSeedY = UnityEngine.Random.Range(0f, 1000f);
        _bobSeed = UnityEngine.Random.Range(0f, 1000f);
        
        IsActive = true;
        gameObject.SetActive(true);
    }
    
    private void UpdateMovement()
    {
        float deltaTime = Time.deltaTime;
        
        // Base upward movement
        _velocity.y = settings.CurrentUpwardSpeed;
        
        // Perlin noise for horizontal drift
        float perlinTime = (Time.time + settings.perlinTimeOffset) * settings.perlinFrequency;
        float perlinX = Mathf.PerlinNoise(_perlinSeedX + perlinTime, 0f) - 0.5f;
        float perlinY = Mathf.PerlinNoise(_perlinSeedY + perlinTime, 0f) - 0.5f;
        
        Vector2 perlinDrift = new Vector2(perlinX, perlinY) * settings.CurrentPerlinAmplitude;
        _velocity += perlinDrift;
        
        // Sine wave bobbing
        float bobTime = (Time.time + settings.bobTimeOffset + _bobSeed) * settings.bobFrequency;
        float bobX = Mathf.Sin(bobTime) * settings.CurrentBobAmplitude;
        float bobY = Mathf.Sin(bobTime * 0.7f) * settings.CurrentBobAmplitude * 0.5f;
        
        Vector2 bobMovement = new Vector2(bobX, bobY);
        _velocity += bobMovement;
        
        // Apply external forces
        _velocity += _externalForce * settings.externalForceSensitivity;
        
        // Clamp velocity to max speed
        if (_velocity.magnitude > settings.CurrentMaxSpeed)
        {
            _velocity = _velocity.normalized * settings.CurrentMaxSpeed;
        }
        
        // Update position
        _bubbleTransform.position += (Vector3)_velocity * deltaTime;
        
        // Decay external forces
        _externalForce = Vector2.MoveTowards(_externalForce, Vector2.zero, settings.forceDecayRate * deltaTime);
    }
    
    private void UpdateBoundaries()
    {
        Vector2 position = _bubbleTransform.position;
        Vector2 levelBounds = settings.GetLevelBounds();
        Vector2 safeBounds = settings.GetSafeBounds();
        
        // Calculate boundary push forces
        Vector2 pushForce = Vector2.zero;
        
        // Horizontal boundaries
        if (position.x > safeBounds.x)
        {
            float distance = position.x - safeBounds.x;
            float fadeAmount = Mathf.Clamp01(distance / settings.boundaryFadeDistance);
            pushForce.x = -settings.boundaryPushStrength * fadeAmount;
        }
        else if (position.x < -safeBounds.x)
        {
            float distance = -safeBounds.x - position.x;
            float fadeAmount = Mathf.Clamp01(distance / settings.boundaryFadeDistance);
            pushForce.x = settings.boundaryPushStrength * fadeAmount;
        }
        
        // Vertical boundaries (only bottom, bubbles naturally go up)
        if (position.y < -safeBounds.y)
        {
            float distance = -safeBounds.y - position.y;
            float fadeAmount = Mathf.Clamp01(distance / settings.boundaryFadeDistance);
            pushForce.y = settings.boundaryPushStrength * fadeAmount;
        }
        
        // Apply boundary forces directly to velocity (gradual pushback)
        if (pushForce != Vector2.zero)
        {
            _velocity += pushForce * Time.deltaTime;
        }
        
        // Hard boundary check - destroy if way outside bounds
        if (Mathf.Abs(position.x) > levelBounds.x + 2f || 
            position.y < -levelBounds.y - 2f || 
            position.y > levelBounds.y + 2f)
        {
            DestroyBubble();
        }
    }
    
    private void UpdateLifetime()
    {
        _timeAlive += Time.deltaTime;
        
        if (_timeAlive >= _maxLifetime)
        {
            DestroyBubble();
        }
    }
    
    public void AddExternalForce(Vector2 force)
    {
        _externalForce += force;
    }
    
    public void BurstBubble()
    {
        if (!IsActive) return;
        
        // Spawn burst effect
        if (burstEffect != null)
        {
            GameObject effect = Instantiate(burstEffect, _bubbleTransform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Notify listeners
        OnBubbleBurst?.Invoke(this, _bubbleTransform.position);
        
        // Deactivate bubble
        DeactivateBubble();
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    public void DestroyBubble()
    {
        if (!IsActive) return;
        
        OnBubbleDestroyed?.Invoke(this);
        DeactivateBubble();
    }
    
    private void DeactivateBubble()
    {
        IsActive = false;
        gameObject.SetActive(false);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsActive) return;
        
        // Check if player collided with bubble
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player collided with bubble: " + bubbleTypeDefinition.displayName);
            BurstBubble();
        }
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!IsActive) return;
        
        // Draw velocity direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)_velocity);
        
        // Draw external force
        if (_externalForce.magnitude > 0.1f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)_externalForce);
        }
    }
}
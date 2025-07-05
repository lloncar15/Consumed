using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Bubble : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private BubbleMovementSettings settings;
    [SerializeField] private BubbleTypeDefinition bubbleTypeDefinition;
    [SerializeField] private GameObject burstEffect;
    
    // Movement state
    private Vector2 velocity;
    private Vector2 externalForce;
    private float lifetime;
    private float maxLifetime;
    private float timeAlive;
    
    // Noise sampling
    private float perlinSeedX;
    private float perlinSeedY;
    private float bobSeed;
    
    // Components
    private CircleCollider2D bubbleCollider;
    private Transform bubbleTransform;
    
    // Events
    public static event Action<Bubble, Vector2> OnBubbleBurst;
    public static event Action<Bubble> OnBubbleDestroyed;
    
    // Properties
    public BubbleTypeDefinition TypeDefinition => bubbleTypeDefinition;
    public BubbleCategory Category => bubbleTypeDefinition?.category ?? BubbleCategory.Bad;
    public BubbleSubtype Subtype => bubbleTypeDefinition?.subtype ?? BubbleSubtype.GroundMonster;
    public Vector2 Position => bubbleTransform.position;
    public bool IsActive { get; private set; }
    
    private void Awake()
    {
        bubbleCollider = GetComponent<CircleCollider2D>();
        bubbleTransform = transform;
        
        // Configure collider as trigger
        bubbleCollider.isTrigger = true;
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
        bubbleTransform.position = startPosition;
        
        // Initialize movement state
        velocity = Vector2.zero;
        externalForce = Vector2.zero;
        timeAlive = 0f;
        maxLifetime = UnityEngine.Random.Range(settings.minLifetime, settings.maxLifetime);
        
        // Initialize noise seeds for unique movement
        perlinSeedX = UnityEngine.Random.Range(0f, 1000f);
        perlinSeedY = UnityEngine.Random.Range(0f, 1000f);
        bobSeed = UnityEngine.Random.Range(0f, 1000f);
        
        // Configure collider
        bubbleCollider.radius = settings.collisionRadius;
        
        IsActive = true;
        gameObject.SetActive(true);
    }
    
    private void UpdateMovement()
    {
        float deltaTime = Time.deltaTime;
        
        // Base upward movement
        velocity.y = settings.CurrentUpwardSpeed;
        
        // Perlin noise for horizontal drift
        float perlinTime = (Time.time + settings.perlinTimeOffset) * settings.perlinFrequency;
        float perlinX = Mathf.PerlinNoise(perlinSeedX + perlinTime, 0f) - 0.5f;
        float perlinY = Mathf.PerlinNoise(perlinSeedY + perlinTime, 0f) - 0.5f;
        
        Vector2 perlinDrift = new Vector2(perlinX, perlinY) * settings.CurrentPerlinAmplitude;
        velocity += perlinDrift;
        
        // Sine wave bobbing
        float bobTime = (Time.time + settings.bobTimeOffset + bobSeed) * settings.bobFrequency;
        float bobX = Mathf.Sin(bobTime) * settings.CurrentBobAmplitude;
        float bobY = Mathf.Sin(bobTime * 0.7f) * settings.CurrentBobAmplitude * 0.5f;
        
        Vector2 bobMovement = new Vector2(bobX, bobY);
        velocity += bobMovement;
        
        // Apply external forces
        velocity += externalForce * settings.externalForceSensitivity;
        
        // Update position
        bubbleTransform.position += (Vector3)velocity * deltaTime;
        
        // Decay external forces
        externalForce = Vector2.MoveTowards(externalForce, Vector2.zero, settings.forceDecayRate * deltaTime);
    }
    
    private void UpdateBoundaries()
    {
        Vector2 position = bubbleTransform.position;
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
        
        // Apply boundary forces
        AddExternalForce(pushForce);
        
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
        timeAlive += Time.deltaTime;
        
        if (timeAlive >= maxLifetime)
        {
            DestroyBubble();
        }
    }
    
    public void AddExternalForce(Vector2 force)
    {
        externalForce += force;
    }
    
    public void BurstBubble()
    {
        if (!IsActive) return;
        
        // Spawn burst effect
        if (burstEffect != null)
        {
            GameObject effect = Instantiate(burstEffect, bubbleTransform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Notify listeners
        OnBubbleBurst?.Invoke(this, bubbleTransform.position);
        
        // Deactivate bubble
        DeactivateBubble();
    }
    
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
            BurstBubble();
        }
    }
    
    // Debug visualization
    private void OnDrawGizmos()
    {
        if (!IsActive) return;
        
        // Draw collision radius
        Gizmos.color = Category == BubbleCategory.Good ? Color.green : Color.red;
        if (bubbleTypeDefinition != null)
            Gizmos.color = bubbleTypeDefinition.bubbleColor;
        Gizmos.DrawWireSphere(transform.position, settings?.collisionRadius ?? 0.5f);
        
        // Draw velocity direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)velocity);
        
        // Draw external force
        if (externalForce.magnitude > 0.1f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)externalForce);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class BubbleSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private BubbleMovementSettings movementSettings;
    [SerializeField] private Transform activeBubbleParent;
    
    [Header("Available Bubble Types")]
    [SerializeField] private BubbleTypeDefinition[] availableBubbleTypes;
    
    [Header("Spawning")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxActiveBubbles = 15;
    [SerializeField] private float spawnRangeX = 10f;
    [SerializeField] private float spawnY = -5f;
    
    [Header("Difficulty Scaling")]
    [SerializeField] private float currentDifficulty = 1f;
    [SerializeField] private bool scaleDifficultyOverTime = true;
    [SerializeField] private float difficultyIncreaseRate = 0.1f;
    [SerializeField] private float maxDifficulty = 3f;
    
    // Active bubble management
    private readonly HashSet<Bubble> _activeBubbles = new HashSet<Bubble>();
    private float _spawnTimer;
    private float _gameTime;
    
    // References
    private BubblePool _bubblePool;
    
    // Events
    public static event Action<BubbleTypeDefinition, Vector2> OnBubbleBurst;
    public static event Action<Bubble> OnBubbleSpawned;
    public static event Action<float> OnDifficultyChanged;
    
    // Properties
    public int ActiveBubbleCount => _activeBubbles.Count;
    public BubbleMovementSettings MovementSettings => movementSettings;
    public float CurrentDifficulty => currentDifficulty;
    public BubbleTypeDefinition[] AvailableBubbleTypes => availableBubbleTypes;
    
    private void Awake() {
        // Create active bubble parent if not assigned
        if (activeBubbleParent != null) return;
        
        activeBubbleParent = new GameObject("Active Bubbles").transform;
        activeBubbleParent.SetParent(transform);
    }
    
    private void Start()
    {
        // Get bubble pool reference
        _bubblePool = BubblePool.Instance;
        if (_bubblePool == null)
        {
            Debug.LogError("BubbleSystem: No BubblePool instance found!");
            enabled = false;
            return;
        }
        
        Debug.Log("BubbleSystem initialized");
    }
    
    private void OnEnable()
    {
        Bubble.OnBubbleDestroyed += OnBubbleDestroyed;
        Bubble.OnBubbleBurst += OnBubbleBurstInternal;
    }
    
    private void OnDisable()
    {
        Bubble.OnBubbleDestroyed -= OnBubbleDestroyed;
        Bubble.OnBubbleBurst -= OnBubbleBurstInternal;
    }
    
    private void Update()
    {
        _gameTime += Time.deltaTime;
        
        if (scaleDifficultyOverTime)
        {
            UpdateDifficulty();
        }
        
        if (autoSpawn)
        {
            HandleAutoSpawn();
        }
    }
    
    private void UpdateDifficulty()
    {
        float newDifficulty = Mathf.Min(1f + (_gameTime * difficultyIncreaseRate), maxDifficulty);
        if (Mathf.Abs(newDifficulty - currentDifficulty) > 0.01f)
        {
            currentDifficulty = newDifficulty;
            OnDifficultyChanged?.Invoke(currentDifficulty);
            
            // Update movement settings difficulty
            if (movementSettings)
            {
                movementSettings.ScaleDifficulty(currentDifficulty);
            }
        }
    }
    
    private void HandleAutoSpawn()
    {
        _spawnTimer += Time.deltaTime;

        if (!(_spawnTimer >= spawnInterval) || _activeBubbles.Count >= maxActiveBubbles) return;
        
        SpawnWeightedBubble();
        _spawnTimer = 0f;
    }
    
    public Bubble SpawnBubble(BubbleTypeDefinition bubbleType, Vector2 position)
    {
        if (!_bubblePool || _activeBubbles.Count >= maxActiveBubbles || bubbleType == null)
            return null;
        
        Bubble bubble = _bubblePool.GetBubble(bubbleType);
        if (!bubble)
            return null;
        
        // Move to active parent and initialize
        bubble.transform.SetParent(activeBubbleParent);
        bubble.Initialize(movementSettings, bubbleType, position);
        
        // Add to active set
        _activeBubbles.Add(bubble);
        
        OnBubbleSpawned?.Invoke(bubble);
        
        return bubble;
    }
    
    public void SpawnWeightedBubble()
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();
        BubbleTypeDefinition bubbleType = GetWeightedBubbleType();
        
        if (bubbleType)
        {
            SpawnBubble(bubbleType, spawnPosition);
        }
    }
    
    private BubbleTypeDefinition GetWeightedBubbleType()
    {
        // Get available bubble types at current difficulty
        var availableTypes = availableBubbleTypes.Where(bt => bt && bt.IsAvailableAtDifficulty(currentDifficulty)).ToList();
        
        if (availableTypes.Count == 0)
        {
            Debug.LogWarning("No bubble types available at current difficulty!");
            return null;
        }
        
        float totalWeight = 0f;
        
        // Calculate total weight with difficulty scaling
        foreach (var bubbleType in availableTypes)
        {
            totalWeight += bubbleType.GetCurrentWeight(currentDifficulty);
        }
        
        if (totalWeight <= 0f)
        {
            Debug.LogWarning("Total bubble weight is 0 or negative! Returning random available type.");
            return availableTypes[UnityEngine.Random.Range(0, availableTypes.Count)];
        }
        
        // Generate random value
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        // Find which bubble type to spawn
        foreach (BubbleTypeDefinition bubbleType in availableTypes)
        {
            currentWeight += bubbleType.GetCurrentWeight(currentDifficulty);
            if (randomValue <= currentWeight)
            {
                return bubbleType;
            }
        }
        
        // Fallback (shouldn't happen)
        return availableTypes[^1];
    }
    
    private Vector2 GetRandomSpawnPosition()
    {
        float x = UnityEngine.Random.Range(-spawnRangeX, spawnRangeX);
        return new Vector2(x, spawnY);
    }
    
    private void OnBubbleDestroyed(Bubble bubble)
    {
        if (_activeBubbles.Contains(bubble))
        {
            _activeBubbles.Remove(bubble);
            _bubblePool.ReturnBubble(bubble);
        }
    }
    
    private void OnBubbleBurstInternal(Bubble bubble, Vector2 position)
    {
        OnBubbleBurst?.Invoke(bubble.TypeDefinition, position);
        
        // Handle bubble burst effects here
        Debug.Log($"{bubble.TypeDefinition.displayName} bubble burst at {position}");
    }
    
    public void ReturnAllBubbles()
    {
        var bubblesToReturn = new List<Bubble>(_activeBubbles);
        foreach (Bubble bubble in bubblesToReturn)
        {
            bubble.DestroyBubble();
        }
    }
    
    public void AddExternalForceToAllBubbles(Vector2 force)
    {
        foreach (Bubble bubble in _activeBubbles)
        {
            bubble.AddExternalForce(force);
        }
    }
    
    public void AddExternalForceToNearbyBubbles(Vector2 position, float radius, Vector2 force)
    {
        foreach (Bubble bubble in _activeBubbles)
        {
            float distance = Vector2.Distance(bubble.Position, position);
            if (distance <= radius)
            {
                float falloff = 1f - (distance / radius);
                bubble.AddExternalForce(force * falloff);
            }
        }
    }
    
    public void SetMovementSettings(BubbleMovementSettings newSettings)
    {
        movementSettings = newSettings;
    }
    
    public void SetSpawnParameters(float interval, int maxBubbles)
    {
        spawnInterval = interval;
        maxActiveBubbles = maxBubbles;
    }
    
    public void SetDifficulty(float difficulty)
    {
        currentDifficulty = Mathf.Clamp(difficulty, 0.1f, maxDifficulty);
        OnDifficultyChanged?.Invoke(currentDifficulty);
        
        if (movementSettings)
        {
            movementSettings.ScaleDifficulty(currentDifficulty);
        }
    }
    
    // Runtime weight modification
    public void SetBubbleTypeWeight(BubbleTypeDefinition bubbleType, float weight)
    {
        if (bubbleType)
        {
            bubbleType.SetWeightMultiplier(weight);
        }
    }
    
    public void SetBubbleTypeEnabled(BubbleTypeDefinition bubbleType, bool enabled)
    {
        if (bubbleType)
        {
            bubbleType.SetEnabled(enabled);
        }
    }
    
    public void SetBubbleTypeDifficultyRange(BubbleTypeDefinition bubbleType, float minDifficulty, float maxDifficulty)
    {
        if (bubbleType)
        {
            bubbleType.SetDifficultyRange(minDifficulty, maxDifficulty);
        }
    }
    
    // Get bubble type by subtype
    public BubbleTypeDefinition GetBubbleType(BubbleSubtype subtype)
    {
        return availableBubbleTypes.FirstOrDefault(bt => bt != null && bt.subtype == subtype);
    }
    
    // Get all bubble types of a category
    public BubbleTypeDefinition[] GetBubbleTypesByCategory(BubbleCategory category)
    {
        return availableBubbleTypes.Where(bt => bt != null && bt.category == category).ToArray();
    }
    
    // Debug methods
    public void LogSystemStats()
    {
        Debug.Log($"BubbleSystem Stats - Active: {ActiveBubbleCount}, Max: {maxActiveBubbles}, Difficulty: {currentDifficulty:F2}");
        
        var availableTypes = availableBubbleTypes.Where(bt => bt != null && bt.IsAvailableAtDifficulty(currentDifficulty));
        foreach (BubbleTypeDefinition bubbleType in availableTypes)
        {
            float currentWeight = bubbleType.GetCurrentWeight(currentDifficulty);
            Debug.Log($"  {bubbleType.displayName}: {currentWeight:F1} (base: {bubbleType.baseWeight:F1})");
        }
        
        _bubblePool?.LogPoolStats();
    }
    
    private void OnDrawGizmos()
    {
        if (movementSettings == null) return;
        
        // Draw level boundaries
        Vector2 bounds = movementSettings.GetLevelBounds();
        Vector2 safeBounds = movementSettings.GetSafeBounds();
        
        // Level bounds
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(bounds.x * 2, bounds.y * 2, 0));
        
        // Safe bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(safeBounds.x * 2, safeBounds.y * 2, 0));
        
        // Spawn area
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(-spawnRangeX, spawnY, 0), new Vector3(spawnRangeX, spawnY, 0));
        Gizmos.DrawWireSphere(new Vector3(0, spawnY, 0), 0.5f);
    }
}
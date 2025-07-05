using UnityEngine;

public enum BubbleCategory
{
    Good,
    Bad
}

public enum BubbleSubtype
{
    // Good bubble subtypes
    SpeedBoost,
    JumpBoost,
    Shield,
    
    // Bad bubble subtypes (monsters)
    FlyingMonster,
    GroundMonster,
    ShootingMonster,
    BossMonster
}

[CreateAssetMenu(fileName = "BubbleTypeDefinition", menuName = "Bubbles/Bubble Type Definition")]
public class BubbleTypeDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Display name shown in UI and debug messages")]
    public string displayName;
    [Tooltip("Whether this is a good (powerup) or bad (monster) bubble")]
    public BubbleCategory category;
    [Tooltip("Specific type within the category (e.g., Flying Monster, Health Potion)")]
    public BubbleSubtype subtype;
    
    [Header("Visual")]
    [Tooltip("Prefab used to create this bubble type (must have Bubble component)")]
    public GameObject prefab;
    [Tooltip("Icon representing this bubble type in UI")]
    public Sprite icon;
    [Tooltip("Color used for gizmos and debug visualization")]
    public Color bubbleColor = Color.white;
    
    [Header("Spawning")]
    [Tooltip("Base spawn weight (higher = more likely to spawn)")]
    [Range(0f, 100f)] public float baseWeight = 10f;
    [Tooltip("How much difficulty affects spawn rate (1 = normal scaling)")]
    [Range(0f, 5f)] public float difficultyMultiplier = 1f;
    [Tooltip("Rarity modifier (lower = more rare, higher = more common)")]
    [Range(0f, 10f)] public float rarityMultiplier = 1f;
    
    [Header("Requirements")]
    [Tooltip("Minimum difficulty level required for this bubble to spawn")]
    public float minDifficultyLevel = 0f;
    [Tooltip("Maximum difficulty level at which this bubble can spawn")]
    public float maxDifficultyLevel = 10f;
    
    [Header("Runtime Modifiers")]
    [Tooltip("Runtime weight multiplier (modified by code during gameplay)")]
    [Range(0f, 5f)] public float currentWeightMultiplier = 1f;
    [Tooltip("Whether this bubble type can currently spawn")]
    public bool isEnabled = true;
    
    public float GetCurrentWeight(float difficultyFactor)
    {
        if (!isEnabled)
            return 0f;
            
        // Check if this bubble type should be available at current difficulty
        if (difficultyFactor < minDifficultyLevel || difficultyFactor > maxDifficultyLevel)
            return 0f;
            
        return baseWeight * (difficultyMultiplier * difficultyFactor) * rarityMultiplier * currentWeightMultiplier;
    }
    
    public bool IsAvailableAtDifficulty(float difficultyFactor)
    {
        return isEnabled && difficultyFactor >= minDifficultyLevel && difficultyFactor <= maxDifficultyLevel;
    }
    
    // Runtime modification methods
    public void SetWeightMultiplier(float multiplier)
    {
        currentWeightMultiplier = Mathf.Max(0f, multiplier);
    }
    
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }
    
    public void SetDifficultyRange(float minDifficulty, float maxDifficulty)
    {
        minDifficultyLevel = minDifficulty;
        maxDifficultyLevel = maxDifficulty;
    }
}
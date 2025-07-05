using UnityEngine;

public enum BubbleCategory
{
    Good,
    Bad
}

public enum BubbleSubtype
{
    // Good bubble subtypes
    HealthPotion,
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
    public string displayName;
    public BubbleCategory category;
    public BubbleSubtype subtype;
    
    [Header("Visual")]
    public GameObject prefab;
    public Sprite icon;
    public Color bubbleColor = Color.white;
    
    [Header("Spawning")]
    [Range(0f, 100f)] public float baseWeight = 10f;
    [Range(0f, 5f)] public float difficultyMultiplier = 1f;
    [Range(0f, 10f)] public float rarityMultiplier = 1f;
    
    [Header("Requirements")]
    public float minDifficultyLevel = 0f;
    public float maxDifficultyLevel = 10f;
    
    [Header("Runtime Modifiers")]
    [Range(0f, 5f)] public float currentWeightMultiplier = 1f;
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
using UnityEngine;

[CreateAssetMenu(fileName = "BubbleMovementSettings", menuName = "Bubbles/Movement Settings")]
public class BubbleMovementSettings : ScriptableObject
{
    [Header("Base Movement")]
    [Range(0.5f, 10f)] public float baseUpwardSpeed = 2f;
    [Range(0f, 5f)] public float horizontalDriftSpeed = 1f;
    [Range(0f, 2f)] public float verticalBobSpeed = 0.5f;
    
    [Header("Perlin Noise Movement")]
    [Range(0.1f, 5f)] public float perlinFrequency = 1f;
    [Range(0f, 3f)] public float perlinAmplitude = 1f;
    [Range(0f, 10f)] public float perlinTimeOffset = 0f;
    
    [Header("Sine Wave Bobbing")]
    [Range(0.1f, 10f)] public float bobFrequency = 2f;
    [Range(0f, 2f)] public float bobAmplitude = 0.3f;
    [Range(0f, 10f)] public float bobTimeOffset = 0f;
    
    [Header("External Forces")]
    [Range(0f, 2f)] public float externalForceSensitivity = 1f;
    [Range(0f, 10f)] public float forceDecayRate = 2f;
    
    [Header("Boundary Behavior")]
    [Range(0f, 20f)] public float levelWidth = 15f;
    [Range(0f, 20f)] public float levelHeight = 10f;
    [Range(0f, 5f)] public float boundaryMargin = 2f;
    [Range(0f, 10f)] public float boundaryPushStrength = 3f;
    [Range(0f, 5f)] public float boundaryFadeDistance = 1f;
    
    [Header("Lifetime")]
    [Range(1f, 30f)] public float minLifetime = 5f;
    [Range(1f, 30f)] public float maxLifetime = 15f;
    
    [Header("Collision")]
    [Range(0.1f, 2f)] public float collisionRadius = 0.5f;
    [Range(0f, 5f)] public float burstForce = 2f;
    
    [Header("Runtime Multipliers")]
    [Range(0.1f, 3f)] public float speedMultiplier = 1f;
    [Range(0.1f, 3f)] public float chaosMultiplier = 1f;
    [Range(0.1f, 3f)] public float difficultyMultiplier = 1f;
    
    // Calculated properties
    public float CurrentUpwardSpeed => baseUpwardSpeed * speedMultiplier * difficultyMultiplier;
    public float CurrentHorizontalDrift => horizontalDriftSpeed * chaosMultiplier * difficultyMultiplier;
    public float CurrentVerticalBob => verticalBobSpeed * chaosMultiplier;
    public float CurrentPerlinAmplitude => perlinAmplitude * chaosMultiplier * difficultyMultiplier;
    public float CurrentBobAmplitude => bobAmplitude * chaosMultiplier;
    
    // Boundary helpers
    public Vector2 GetLevelBounds() => new Vector2(levelWidth, levelHeight);
    public Vector2 GetSafeBounds() => new Vector2(levelWidth - boundaryMargin, levelHeight - boundaryMargin);
    
    // Difficulty scaling methods
    public void ScaleDifficulty(float difficultyFactor)
    {
        difficultyMultiplier = difficultyFactor;
    }
    
    public void ScaleChaos(float chaosFactor)
    {
        chaosMultiplier = chaosFactor;
    }
    
    public void ScaleSpeed(float speedFactor)
    {
        speedMultiplier = speedFactor;
    }
}
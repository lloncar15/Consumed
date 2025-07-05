using UnityEngine;

[CreateAssetMenu(fileName = "BubbleMovementSettings", menuName = "Bubbles/Movement Settings")]
public class BubbleMovementSettings : ScriptableObject
{
    [Header("Base Movement")]
    [Tooltip("Base speed bubbles float upward (units per second)")]
    [Range(0.5f, 10f)] public float baseUpwardSpeed = 2f;
    [Tooltip("Maximum speed for horizontal drift movement")]
    [Range(0f, 5f)] public float horizontalDriftSpeed = 1f;
    [Tooltip("Speed of vertical bobbing motion")]
    [Range(0f, 2f)] public float verticalBobSpeed = 0.5f;
    [Tooltip("Maximum total speed bubble can move (prevents extreme speeds from combined forces)")]
    [Range(1f, 20f)] public float maxSpeed = 8f;
    
    [Header("Perlin Noise Movement")]
    [Tooltip("How quickly the noise pattern changes (higher = more erratic movement)")]
    [Range(0.1f, 5f)] public float perlinFrequency = 1f;
    [Tooltip("Strength of the noise-based drift (higher = more chaotic movement)")]
    [Range(0f, 3f)] public float perlinAmplitude = 1f;
    [Tooltip("Time offset for noise sampling (use different values for variety)")]
    [Range(0f, 10f)] public float perlinTimeOffset = 0f;
    
    [Header("Sine Wave Bobbing")]
    [Tooltip("Frequency of sine wave bobbing (higher = faster bobbing)")]
    [Range(0.1f, 10f)] public float bobFrequency = 2f;
    [Tooltip("Amplitude of sine wave bobbing (higher = more pronounced bobbing)")]
    [Range(0f, 2f)] public float bobAmplitude = 0.3f;
    [Tooltip("Time offset for bobbing pattern (use different values for variety)")]
    [Range(0f, 10f)] public float bobTimeOffset = 0f;
    
    [Header("External Forces")]
    [Tooltip("How much external forces affect bubble movement (0 = immune, 2 = very sensitive)")]
    [Range(0f, 2f)] public float externalForceSensitivity = 1f;
    [Tooltip("How quickly external forces fade away (higher = faster decay)")]
    [Range(0f, 10f)] public float forceDecayRate = 2f;
    
    [Header("Boundary Behavior")]
    [Tooltip("Total level width (bubbles will be contained within this area)")]
    [Range(0f, 20f)] public float levelWidth = 15f;
    [Tooltip("Total level height (bubbles will be contained within this area)")]
    [Range(0f, 20f)] public float levelHeight = 10f;
    [Tooltip("Distance from level edge where boundary push begins")]
    [Range(0f, 5f)] public float boundaryMargin = 2f;
    [Tooltip("Strength of force pushing bubbles back from boundaries")]
    [Range(0f, 10f)] public float boundaryPushStrength = 1f;
    [Tooltip("Distance over which boundary push force fades in")]
    [Range(0f, 5f)] public float boundaryFadeDistance = 1f;
    
    [Header("Lifetime")]
    [Tooltip("Minimum time (seconds) before bubble is automatically destroyed")]
    [Range(1f, 30f)] public float minLifetime = 5f;
    [Tooltip("Maximum time (seconds) before bubble is automatically destroyed")]
    [Range(1f, 30f)] public float maxLifetime = 15f;
    
    [Header("Collision")]
    [Tooltip("Force applied when bubble bursts (for future burst effects)")]
    [Range(0f, 5f)] public float burstForce = 2f;
    
    [Header("Runtime Multipliers")]
    [Tooltip("Global speed multiplier for all movement (1 = normal, 2 = double speed)")]
    [Range(0.1f, 3f)] public float speedMultiplier = 1f;
    [Tooltip("Multiplier for chaotic movement (noise and bobbing intensity)")]
    [Range(0.1f, 3f)] public float chaosMultiplier = 1f;
    [Tooltip("Overall difficulty multiplier applied by the system")]
    [Range(0.1f, 3f)] public float difficultyMultiplier = 1f;
    
    // Calculated properties
    public float CurrentUpwardSpeed => baseUpwardSpeed * speedMultiplier * difficultyMultiplier;
    public float CurrentHorizontalDrift => horizontalDriftSpeed * chaosMultiplier * difficultyMultiplier;
    public float CurrentVerticalBob => verticalBobSpeed * chaosMultiplier;
    public float CurrentPerlinAmplitude => perlinAmplitude * chaosMultiplier * difficultyMultiplier;
    public float CurrentBobAmplitude => bobAmplitude * chaosMultiplier;
    public float CurrentMaxSpeed => maxSpeed * speedMultiplier * difficultyMultiplier;
    
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
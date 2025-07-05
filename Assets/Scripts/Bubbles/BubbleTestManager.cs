using UnityEngine;
using System.Linq;
using UnityEditor;

public class BubbleTestManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BubbleSystem bubbleSystem;
    [SerializeField] private BubblePool bubblePool;
    
    [Header("Manual Spawning")]
    [SerializeField] private BubbleTypeDefinition testBubbleType;
    [SerializeField] private Vector2 spawnPosition = Vector2.zero;
    [SerializeField] private int spawnCount = 1;
    
    [Header("Testing Controls")]
    [SerializeField] private bool enableDebugUI = true;
    [SerializeField] private bool showStats = true;
    [SerializeField] private bool showWeights = true;
    
    [Header("Runtime Parameter Testing")]
    [Range(0.1f, 5f)] public float difficultyOverride = 1f;
    [Range(0.1f, 10f)] public float spawnIntervalOverride = 2f;
    [Range(1, 50)] public int maxBubblesOverride = 15;
    
    [Header("Force Testing")]
    [SerializeField] private Vector2 globalForce = Vector2.zero;
    [SerializeField] private Vector2 localForcePosition = Vector2.zero;
    [SerializeField] private float localForceRadius = 3f;
    [SerializeField] private Vector2 localForceVector = Vector2.zero;
    
    private void Start()
    {
        // Auto-find references if not assigned
        if (bubbleSystem == null)
            bubbleSystem = (BubbleSystem)FindFirstObjectByType(typeof(BubbleSystem));
        if (bubblePool == null)
            bubblePool = (BubblePool)FindFirstObjectByType(typeof(BubblePool));
    }
    
    private void Update() {
        // Apply runtime parameter overrides
        if (!bubbleSystem) return;
        
        bubbleSystem.SetDifficulty(difficultyOverride);
        bubbleSystem.SetSpawnParameters(spawnIntervalOverride, maxBubblesOverride);
    }
    
    // Inspector button methods
    [ContextMenu("Spawn Test Bubble")]
    public void SpawnTestBubble()
    {
        if (testBubbleType != null && bubbleSystem != null)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 pos = spawnPosition + Random.insideUnitCircle * 0.5f;
                bubbleSystem.SpawnBubble(testBubbleType, pos);
            }
            Debug.Log($"Spawned {spawnCount} {testBubbleType.displayName} bubble(s)");
        }
        else
        {
            Debug.LogWarning("Cannot spawn bubble - missing test bubble type or bubble system!");
        }
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    [ContextMenu("Spawn Random Weighted Bubble")]
    public void SpawnRandomWeightedBubble() {
        if (!bubbleSystem) return;
        
        bubbleSystem.SpawnWeightedBubble();
        Debug.Log("Spawned random weighted bubble");
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    [ContextMenu("Spawn Good Bubble")]
    public void SpawnGoodBubble()
    {
        SpawnBubbleByCategory(BubbleCategory.Good);
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    [ContextMenu("Spawn Bad Bubble")]
    public void SpawnBadBubble()
    {
        SpawnBubbleByCategory(BubbleCategory.Bad);
    }
    
    [ContextMenu("Spawn Flying Monster")]
    public void SpawnFlyingMonster()
    {
        SpawnBubbleBySubtype(BubbleSubtype.FlyingMonster);
    }
    
    [ContextMenu("Spawn Ground Monster")]
    public void SpawnGroundMonster()
    {
        SpawnBubbleBySubtype(BubbleSubtype.GroundMonster);
    }
    
    [ContextMenu("Spawn Shooting Monster")]
    public void SpawnShootingMonster()
    {
        SpawnBubbleBySubtype(BubbleSubtype.ShootingMonster);
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    [ContextMenu("Clear All Bubbles")]
    public void ClearAllBubbles() {
        if (!bubbleSystem) return;
        
        bubbleSystem.ReturnAllBubbles();
        Debug.Log("Cleared all bubbles");
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    [ContextMenu("Apply Global Force")]
    public void ApplyGlobalForce() {
        if (!bubbleSystem || globalForce == Vector2.zero) return;
        
        bubbleSystem.AddExternalForceToAllBubbles(globalForce);
        Debug.Log($"Applied global force: {globalForce}");
    }
    
    [ContextMenu("Apply Local Force")]
    public void ApplyLocalForce() {
        if (bubbleSystem == null || localForceVector == Vector2.zero) return;
        
        bubbleSystem.AddExternalForceToNearbyBubbles(localForcePosition, localForceRadius, localForceVector);
        Debug.Log($"Applied local force: {localForceVector} at {localForcePosition} with radius {localForceRadius}");
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    [ContextMenu("Log System Stats")]
    public void LogSystemStats()
    {
        if (bubbleSystem)
            bubbleSystem.LogSystemStats();
        if (bubblePool)
            bubblePool.LogPoolStats();
    }
    
    // Weight manipulation methods
    public void SetBubbleTypeWeight(BubbleSubtype subtype, float weight) {
        if (bubbleSystem == null) return;
        
        BubbleTypeDefinition bubbleType = bubbleSystem.GetBubbleType(subtype);
        
        if (bubbleType == null) return;
        
        bubbleType.SetWeightMultiplier(weight);
        Debug.Log($"Set {subtype} weight multiplier to {weight}");
    }
    
    public void EnableBubbleType(BubbleSubtype subtype, bool enabled) {
        if (bubbleSystem == null) return;
        
        BubbleTypeDefinition bubbleType = bubbleSystem.GetBubbleType(subtype);
        if (bubbleType == null) return;
        
        bubbleType.SetEnabled(enabled);
        Debug.Log($"Set {subtype} enabled to {enabled}");
    }
    
    // Helper methods
    private void SpawnBubbleByCategory(BubbleCategory category)
    {
        if (bubbleSystem == null) return;
        
        var bubbleTypes = bubbleSystem.GetBubbleTypesByCategory(category);
        if (bubbleTypes.Length > 0)
        {
            BubbleTypeDefinition randomType = bubbleTypes[Random.Range(0, bubbleTypes.Length)];
            bubbleSystem.SpawnBubble(randomType, spawnPosition);
            Debug.Log($"Spawned {category} bubble: {randomType.displayName}");
        }
        else
        {
            Debug.LogWarning($"No {category} bubble types available!");
        }
    }
    
    private void SpawnBubbleBySubtype(BubbleSubtype subtype)
    {
        if (bubbleSystem == null) return;
        
        BubbleTypeDefinition bubbleType = bubbleSystem.GetBubbleType(subtype);
        if (bubbleType != null)
        {
            bubbleSystem.SpawnBubble(bubbleType, spawnPosition);
            Debug.Log($"Spawned {subtype} bubble");
        }
        else
        {
            Debug.LogWarning($"No bubble type found for {subtype}!");
        }
    }
    
    // GUI for runtime testing
    private void OnGUI()
    {
        if (!enableDebugUI) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Bubble Test Manager", EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
        
        if (showStats && bubbleSystem)
        {
            GUILayout.Space(10);
            GUILayout.Label("Stats:", EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
            GUILayout.Label($"Active Bubbles: {bubbleSystem.ActiveBubbleCount}");
            GUILayout.Label($"Difficulty: {bubbleSystem.CurrentDifficulty:F2}");
            
            if (bubblePool)
            {
                GUILayout.Label($"Pool Size: {bubblePool.TotalPoolSize}");
            }
        }
        
        if (showWeights && bubbleSystem)
        {
            GUILayout.Space(10);
            GUILayout.Label("Bubble Weights:", EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
            
            foreach (var bubbleType in bubbleSystem.AvailableBubbleTypes)
            {
                if (bubbleType && bubbleType.IsAvailableAtDifficulty(bubbleSystem.CurrentDifficulty))
                {
                    float weight = bubbleType.GetCurrentWeight(bubbleSystem.CurrentDifficulty);
                    GUILayout.Label($"{bubbleType.displayName}: {weight:F1}");
                }
            }
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Spawn Random Bubble"))
            SpawnRandomWeightedBubble();
        
        if (GUILayout.Button("Spawn Good Bubble"))
            SpawnGoodBubble();
        
        if (GUILayout.Button("Spawn Bad Bubble"))
            SpawnBadBubble();
        
        if (GUILayout.Button("Clear All Bubbles"))
            ClearAllBubbles();
        
        if (GUILayout.Button("Apply Global Force"))
            ApplyGlobalForce();
        
        if (GUILayout.Button("Log Stats"))
            LogSystemStats();
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    // Gizmos for visual feedback
    private void OnDrawGizmos()
    {
        // Draw spawn position
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPosition, 0.3f);
        
        // Draw local force area
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(localForcePosition, localForceRadius);
        
        // Draw force vectors
        if (globalForce != Vector2.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, (Vector3)globalForce);
        }
        
        if (localForceVector != Vector2.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine((Vector3)localForcePosition, (Vector3)localForcePosition + (Vector3)localForceVector);
        }
    }
}
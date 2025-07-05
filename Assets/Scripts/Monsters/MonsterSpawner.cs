using System;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Monster Prefabs")]
    [Tooltip("Ground monster prefab to spawn")]
    [SerializeField] private GameObject groundMonsterPrefab;
    [Tooltip("Flying monster prefab to spawn")]
    [SerializeField] private GameObject flyingMonsterPrefab;
    
    [Header("Spawn Effects")]
    [Tooltip("Effect to play when monster spawns")]
    [SerializeField] private GameObject spawnEffect;
    [Tooltip("Duration before destroying spawn effect")]
    [SerializeField] private float spawnEffectDuration = 2f;
    
    [Header("Ground Monster Spawn")]
    [Tooltip("Offset from bubble position for ground monster spawn")]
    [SerializeField] private Vector2 groundSpawnOffset = Vector2.zero;
    [Tooltip("Random range for ground spawn position")]
    [SerializeField] private float groundSpawnRandomRange = 0.5f;
    
    [Header("Flying Monster Spawn")]
    [Tooltip("Offset from bubble position for flying monster spawn")]
    [SerializeField] private Vector2 flyingSpawnOffset = Vector2.up;
    [Tooltip("Random range for flying spawn position")]
    [SerializeField] private float flyingSpawnRandomRange = 1f;
    
    // Events
    public static event Action<MonsterBase, Vector2> OnMonsterSpawned;
    
    // References
    private MonsterPool monsterPool;
    
    private void Awake()
    {
        // Get monster pool reference
        monsterPool = FindObjectOfType<MonsterPool>();
    }
    
    private void OnEnable()
    {
        // Listen for bubble burst events
        BubbleSystem.OnBubbleBurst += HandleBubbleBurst;
    }
    
    private void OnDisable()
    {
        BubbleSystem.OnBubbleBurst -= HandleBubbleBurst;
    }
    
    private void HandleBubbleBurst(BubbleTypeDefinition bubbleType, Vector2 position)
    {
        if (bubbleType == null || bubbleType.category != BubbleCategory.Bad)
            return;
        
        // Spawn monster based on bubble subtype
        switch (bubbleType.subtype)
        {
            case BubbleSubtype.GroundMonster:
                SpawnGroundMonster(position);
                break;
            case BubbleSubtype.FlyingMonster:
                SpawnFlyingMonster(position);
                break;
            case BubbleSubtype.ShootingMonster:
                // TODO: Implement shooting monster when ready
                SpawnGroundMonster(position); // Fallback to ground monster for now
                break;
        }
    }
    
    public MonsterBase SpawnGroundMonster(Vector2 position)
    {
        Vector2 spawnPosition = CalculateGroundSpawnPosition(position);
        return SpawnMonster(MonsterType.Ground, spawnPosition);
    }
    
    public MonsterBase SpawnFlyingMonster(Vector2 position)
    {
        Vector2 spawnPosition = CalculateFlyingSpawnPosition(position);
        return SpawnMonster(MonsterType.Flying, spawnPosition);
    }
    
    private MonsterBase SpawnMonster(MonsterType type, Vector2 position)
    {
        MonsterBase monster = null;
        
        // Try to get from pool first
        if (monsterPool)
        {
            monster = monsterPool.GetMonster(type);
        }
        
        // If no pool or pool empty, create new monster
        if (monster == null)
        {
            GameObject prefab = type == MonsterType.Ground ? groundMonsterPrefab : flyingMonsterPrefab;
            if (prefab)
            {
                GameObject monsterObj = Instantiate(prefab, position, Quaternion.identity);
                monster = monsterObj.GetComponent<MonsterBase>();
            }
        }
        
        if (monster)
        {
            // Position and initialize monster
            monster.transform.position = position;
            monster.Initialize();
            
            // Spawn effect
            if (spawnEffect)
            {
                GameObject effect = Instantiate(spawnEffect, position, Quaternion.identity);
                Destroy(effect, spawnEffectDuration);
            }
            
            OnMonsterSpawned?.Invoke(monster, position);
            Debug.Log($"Spawned {type} monster at {position}");
        }
        else
        {
            Debug.LogWarning($"Failed to spawn {type} monster - no prefab assigned!");
        }
        
        return monster;
    }
    
    private Vector2 CalculateGroundSpawnPosition(Vector2 bubblePosition)
    {
        Vector2 basePosition = bubblePosition + groundSpawnOffset;
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * groundSpawnRandomRange;
        return basePosition + randomOffset;
    }
    
    private Vector2 CalculateFlyingSpawnPosition(Vector2 bubblePosition)
    {
        Vector2 basePosition = bubblePosition + flyingSpawnOffset;
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * flyingSpawnRandomRange;
        return basePosition + randomOffset;
    }
    
    // Manual spawn methods for testing
    [ContextMenu("Spawn Ground Monster At Origin")]
    public void TestSpawnGroundMonster()
    {
        SpawnGroundMonster(Vector2.zero);
    }
    
    [ContextMenu("Spawn Flying Monster At Origin")]
    public void TestSpawnFlyingMonster()
    {
        SpawnFlyingMonster(Vector2.zero);
    }
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Show spawn offsets
        Vector3 center = transform.position;
        
        // Ground monster spawn area
        Gizmos.color = Color.brown;
        Vector3 groundPos = center + (Vector3)groundSpawnOffset;
        Gizmos.DrawWireSphere(groundPos, groundSpawnRandomRange);
        Gizmos.DrawLine(center, groundPos);
        
        // Flying monster spawn area
        Gizmos.color = Color.cyan;
        Vector3 flyingPos = center + (Vector3)flyingSpawnOffset;
        Gizmos.DrawWireSphere(flyingPos, flyingSpawnRandomRange);
        Gizmos.DrawLine(center, flyingPos);
    }
}
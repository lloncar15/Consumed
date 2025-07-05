using System.Collections.Generic;
using UnityEngine;

public class MonsterPool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [Tooltip("Ground monster prefab for pooling")]
    [SerializeField] private GameObject groundMonsterPrefab;
    [Tooltip("Flying monster prefab for pooling")]
    [SerializeField] private GameObject flyingMonsterPrefab;
    [SerializeField] private int initialPoolSizePerType = 3;
    [SerializeField] private int maxPoolSize = 20;
    [SerializeField] private bool autoExpand = true;
    
    // Pools
    private Queue<MonsterBase> _groundMonsterPool = new Queue<MonsterBase>();
    private Queue<MonsterBase> _flyingMonsterPool = new Queue<MonsterBase>();
    private HashSet<MonsterBase> _activeMonsters = new HashSet<MonsterBase>();
    
    // Pool parent transforms
    private Transform _groundPoolParent;
    private Transform _flyingPoolParent;
    private Transform _activeMonsterParent;
    
    public static MonsterPool Instance { get; private set; }
    
    // Properties
    public int ActiveMonsterCount => _activeMonsters.Count;
    public int GroundPoolCount => _groundMonsterPool.Count;
    public int FlyingPoolCount => _flyingMonsterPool.Count;
    public int TotalPoolSize => GroundPoolCount + FlyingPoolCount;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        MonsterBase.OnMonsterDied += ReturnMonsterToPool;
    }
    
    private void OnDisable()
    {
        MonsterBase.OnMonsterDied -= ReturnMonsterToPool;
    }
    
    private void InitializePool()
    {
        // Create parent transforms
        _groundPoolParent = new GameObject("Ground Monster Pool").transform;
        _flyingPoolParent = new GameObject("Flying Monster Pool").transform;
        _activeMonsterParent = new GameObject("Active Monsters").transform;
        
        _groundPoolParent.SetParent(transform);
        _flyingPoolParent.SetParent(transform);
        _activeMonsterParent.SetParent(transform);
        
        // Pre-instantiate monsters
        CreateMonsters(groundMonsterPrefab, _groundMonsterPool, _groundPoolParent, initialPoolSizePerType);
        CreateMonsters(flyingMonsterPrefab, _flyingMonsterPool, _flyingPoolParent, initialPoolSizePerType);
        
        Debug.Log($"MonsterPool initialized with {TotalPoolSize} monsters");
    }
    
    private void CreateMonsters(GameObject prefab, Queue<MonsterBase> pool, Transform parent, int count)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Monster prefab is null! Cannot create monsters.");
            return;
        }
        
        for (int i = 0; i < count; i++)
        {
            GameObject monsterObj = Instantiate(prefab, parent);
            MonsterBase monster = monsterObj.GetComponent<MonsterBase>();
            
            if (monster == null)
            {
                Debug.LogError($"Prefab {prefab.name} doesn't have a MonsterBase component!");
                Destroy(monsterObj);
                continue;
            }
            
            monsterObj.SetActive(false);
            pool.Enqueue(monster);
        }
    }
    
    public MonsterBase GetMonster(MonsterType type)
    {
        Queue<MonsterBase> targetPool = type == MonsterType.Ground ? _groundMonsterPool : _flyingMonsterPool;
        GameObject targetPrefab = type == MonsterType.Ground ? groundMonsterPrefab : flyingMonsterPrefab;
        Transform poolParent = type == MonsterType.Ground ? _groundPoolParent : _flyingPoolParent;
        
        MonsterBase monster = null;
        
        // Try to get from pool
        if (targetPool.Count > 0)
        {
            monster = targetPool.Dequeue();
        }
        // Create new if pool is empty and auto-expand is enabled
        else if (autoExpand && _activeMonsters.Count < maxPoolSize)
        {
            if (targetPrefab != null)
            {
                GameObject monsterObj = Instantiate(targetPrefab, poolParent);
                monster = monsterObj.GetComponent<MonsterBase>();
                
                if (monster == null)
                {
                    Debug.LogError($"Prefab {targetPrefab.name} doesn't have a MonsterBase component!");
                    Destroy(monsterObj);
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"Cannot create new {type} monster - prefab is null!");
                return null;
            }
        }
        
        if (monster != null)
        {
            // Move to active parent and activate
            monster.transform.SetParent(_activeMonsterParent);
            monster.gameObject.SetActive(true);
            _activeMonsters.Add(monster);
        }
        
        return monster;
    }
    
    private void ReturnMonsterToPool(MonsterBase monster)
    {
        if (monster == null || !_activeMonsters.Contains(monster))
            return;
        
        // Remove from active set
        _activeMonsters.Remove(monster);
        
        // Return to appropriate pool
        Queue<MonsterBase> targetPool = monster.Type == MonsterType.Ground ? _groundMonsterPool : _flyingMonsterPool;
        Transform poolParent = monster.Type == MonsterType.Ground ? _groundPoolParent : _flyingPoolParent;
        
        // Reset monster state
        monster.transform.SetParent(poolParent);
        monster.gameObject.SetActive(false);
        
        // Add back to pool
        targetPool.Enqueue(monster);
    }
    
    public void ReturnAllMonsters()
    {
        var monstersToReturn = new List<MonsterBase>(_activeMonsters);
        foreach (var monster in monstersToReturn)
        {
            monster.Die();
        }
    }
    
    public void ClearPools()
    {
        ReturnAllMonsters();
        
        // Clear pools
        while (_groundMonsterPool.Count > 0)
        {
            var monster = _groundMonsterPool.Dequeue();
            if (monster != null)
                DestroyImmediate(monster.gameObject);
        }
        
        while (_flyingMonsterPool.Count > 0)
        {
            var monster = _flyingMonsterPool.Dequeue();
            if (monster != null)
                DestroyImmediate(monster.gameObject);
        }
        
        _activeMonsters.Clear();
    }
    
    // Debug methods
    public void LogPoolStats()
    {
        Debug.Log($"Monster Pool Stats - Ground: {GroundPoolCount}, Flying: {FlyingPoolCount}, Active: {ActiveMonsterCount}");
    }
    
    // Manual testing
    [ContextMenu("Spawn Test Ground Monster")]
    public void TestSpawnGroundMonster()
    {
        var monster = GetMonster(MonsterType.Ground);
        if (monster)
        {
            monster.transform.position = Vector3.zero;
            monster.Initialize();
        }
    }
    
    [ContextMenu("Spawn Test Flying Monster")]
    public void TestSpawnFlyingMonster()
    {
        var monster = GetMonster(MonsterType.Flying);
        if (monster)
        {
            monster.transform.position = Vector3.up * 2f;
            monster.Initialize();
        }
    }
    
    [ContextMenu("Return All Monsters")]
    public void TestReturnAllMonsters()
    {
        ReturnAllMonsters();
    }
}
using System.Collections.Generic;
using UnityEngine;

public class BubblePool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private BubbleTypeDefinition[] bubbleTypes;
    [SerializeField] private int initialPoolSizePerType = 5;
    [SerializeField] private int maxPoolSize = 100;
    [SerializeField] private bool autoExpand = true;
    
    // Pools - Dictionary keyed by BubbleTypeDefinition
    private Dictionary<BubbleTypeDefinition, Queue<Bubble>> bubblePools = new Dictionary<BubbleTypeDefinition, Queue<Bubble>>();
    
    // Pool parent transforms for organization
    private Transform poolParent;
    
    public static BubblePool Instance { get; private set; }
    
    // Properties
    public int TotalPoolSize 
    { 
        get 
        { 
            int total = 0;
            foreach (var pool in bubblePools.Values)
                total += pool.Count;
            return total;
        } 
    }
    
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
    
    private void InitializePool()
    {
        // Create parent transform for organization
        poolParent = new GameObject("Bubble Pool").transform;
        poolParent.SetParent(transform);
        
        // Initialize pools for each bubble type
        foreach (var bubbleType in bubbleTypes)
        {
            if (bubbleType != null && bubbleType.prefab != null)
            {
                bubblePools[bubbleType] = new Queue<Bubble>();
                CreateBubblesForType(bubbleType, initialPoolSizePerType);
            }
        }
        
        Debug.Log($"BubblePool initialized with {TotalPoolSize} bubbles across {bubblePools.Count} types");
    }
    
    private void CreateBubblesForType(BubbleTypeDefinition bubbleType, int count)
    {
        if (bubbleType?.prefab == null)
        {
            Debug.LogWarning($"Bubble type {bubbleType?.name} has null prefab! Cannot create bubbles.");
            return;
        }
        
        var pool = bubblePools[bubbleType];
        
        for (int i = 0; i < count; i++)
        {
            GameObject bubbleObj = Instantiate(bubbleType.prefab, poolParent);
            Bubble bubble = bubbleObj.GetComponent<Bubble>();
            
            if (bubble == null)
            {
                Debug.LogError($"Prefab {bubbleType.prefab.name} doesn't have a Bubble component!");
                Destroy(bubbleObj);
                continue;
            }
            
            bubbleObj.SetActive(false);
            pool.Enqueue(bubble);
        }
    }
    
    public Bubble GetBubble(BubbleTypeDefinition bubbleType)
    {
        if (bubbleType == null || !bubblePools.ContainsKey(bubbleType))
        {
            Debug.LogWarning($"Bubble type {bubbleType?.name} not found in pools!");
            return null;
        }
        
        var targetPool = bubblePools[bubbleType];
        Bubble bubble = null;
        
        // Try to get from pool
        if (targetPool.Count > 0)
        {
            bubble = targetPool.Dequeue();
        }
        // Create new if pool is empty and auto-expand is enabled
        else if (autoExpand && TotalPoolSize < maxPoolSize)
        {
            CreateBubblesForType(bubbleType, 1);
            if (targetPool.Count > 0)
            {
                bubble = targetPool.Dequeue();
            }
        }
        
        return bubble;
    }
    
    public void ReturnBubble(Bubble bubble)
    {
        if (bubble == null || bubble.TypeDefinition == null) return;
        
        var bubbleType = bubble.TypeDefinition;
        if (!bubblePools.ContainsKey(bubbleType))
        {
            Debug.LogWarning($"Cannot return bubble - type {bubbleType.name} not found in pools!");
            return;
        }
        
        // Return to appropriate pool
        var targetPool = bubblePools[bubbleType];
        
        // Reset bubble state
        bubble.transform.SetParent(poolParent);
        bubble.gameObject.SetActive(false);
        
        // Add back to pool
        targetPool.Enqueue(bubble);
    }
    
    public void ClearPools()
    {
        // Clear all pools
        foreach (var pool in bubblePools.Values)
        {
            while (pool.Count > 0)
            {
                var bubble = pool.Dequeue();
                if (bubble != null)
                    DestroyImmediate(bubble.gameObject);
            }
        }
        bubblePools.Clear();
    }
    
    public int GetPoolCount(BubbleTypeDefinition bubbleType)
    {
        return bubblePools.ContainsKey(bubbleType) ? bubblePools[bubbleType].Count : 0;
    }
    
    // Debug methods
    public void LogPoolStats()
    {
        Debug.Log($"Pool Stats - Total: {TotalPoolSize} across {bubblePools.Count} types");
        foreach (var kvp in bubblePools)
        {
            Debug.Log($"  {kvp.Key.name}: {kvp.Value.Count} bubbles");
        }
    }
}
using System;
using UnityEngine;

public enum MonsterState
{
    Spawning,
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Dying,
    Dead
}

public enum MonsterType
{
    Ground,
    Flying
}

public abstract class MonsterBase : MonoBehaviour
{
    [Header("Monster Configuration")]
    [Tooltip("Type of monster - determines movement behavior")]
    [SerializeField] protected MonsterType monsterType;
    [Tooltip("Maximum time monster stays alive before auto-despawn")]
    [SerializeField] protected float maxLifetime = 30f;
    [Tooltip("Range at which monster detects the player")]
    [SerializeField] protected float detectionRange = 5f;
    [Tooltip("Range at which monster can attack the player")]
    [SerializeField] protected float attackRange = 2f;
    [Tooltip("Time monster takes to wind up attack (player reaction window)")]
    [SerializeField] protected float attackWindupTime = 1f;
    [Tooltip("Duration of attack animation")]
    [SerializeField] protected float attackDuration = 0.5f;
    [Tooltip("Cooldown between attacks")]
    [SerializeField] protected float attackCooldown = 2f;
    
    [Header("Movement")]
    [Tooltip("Normal movement speed")]
    [SerializeField] protected float moveSpeed = 3f;
    [Tooltip("Speed when chasing player")]
    [SerializeField] protected float chaseSpeed = 5f;
    
    [Header("State Timing")]
    [Tooltip("Duration of spawn animation")]
    [SerializeField] protected float spawnDuration = 0.5f;
    [Tooltip("Time to wait in idle before starting patrol")]
    [SerializeField] protected float idleWaitTime = 1f;
    [Tooltip("Multiplier for detection range when losing target (hysteresis)")]
    [SerializeField] protected float loseTargetRangeMultiplier = 1.5f;
    [Tooltip("Duration of death animation")]
    [SerializeField] protected float deathAnimationDuration = 1f;
    
    [Header("References")]
    [SerializeField] protected GameObject attackEffect;
    [SerializeField] protected GameObject deathEffect;
    [Tooltip("Duration before destroying death effect")]
    [SerializeField] protected float deathEffectDuration = 2f;
    
    // State Management
    protected MonsterState currentState;
    protected float StateTimer;
    protected float Lifetime;
    protected float LastAttackTime;
    protected bool HasHitPlayerThisAttack;
    
    // Game Manager reference
    protected GameManager GameManager;
    
    // Components
    protected Rigidbody2D MonsterRigidbody;
    protected Collider2D MonsterCollider;
    protected SpriteRenderer SpriteRenderer;
    
    // Events
    public static event Action<MonsterBase> OnMonsterSpawned;
    public static event Action<MonsterBase> OnMonsterDied;
    public static event Action<MonsterBase> OnMonsterAttackStart;
    public static event Action<MonsterBase> OnMonsterAttackHit;
    
    // Properties
    public MonsterState CurrentState => currentState;
    public MonsterType Type => monsterType;
    public bool IsAlive => currentState != MonsterState.Dying && currentState != MonsterState.Dead;
    public bool CanAttack => Time.time >= LastAttackTime + attackCooldown;
    public float DistanceToPlayer => GameManager ? GameManager.GetDistanceToPlayer(transform.position) : float.MaxValue;
    public Vector2 DirectionToPlayer => GameManager ? GameManager.GetDirectionToPlayer(transform.position) : Vector2.zero;
    
    protected virtual void Awake()
    {
        MonsterRigidbody = GetComponent<Rigidbody2D>();
        MonsterCollider = GetComponent<Collider2D>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        
        // Get game manager reference
        GameManager = GameManager.Instance;
        if (GameManager == null)
        {
            Debug.LogError($"MonsterBase: No GameManager instance found! Monster {name} won't function properly.");
        }
    }
    
    protected virtual void Start()
    {
        Initialize();
    }
    
    protected virtual void Update()
    {
        if (!IsAlive || (GameManager && GameManager.IsGameOver)) return;
        
        UpdateLifetime();
        UpdateStateMachine();
        UpdateMonster();
    }
    
    public virtual void Initialize()
    {
        currentState = MonsterState.Spawning;
        StateTimer = 0f;
        Lifetime = 0f;
        LastAttackTime = -attackCooldown;
        
        OnMonsterSpawned?.Invoke(this);
    }
    
    protected virtual void UpdateLifetime()
    {
        Lifetime += Time.deltaTime;
        
        if (Lifetime >= maxLifetime)
        {
            Die();
        }
    }
    
    protected virtual void UpdateStateMachine()
    {
        StateTimer += Time.deltaTime;
        
        switch (currentState)
        {
            case MonsterState.Spawning:
                HandleSpawningState();
                break;
            case MonsterState.Idle:
                HandleIdleState();
                break;
            case MonsterState.Patrolling:
                HandlePatrollingState();
                break;
            case MonsterState.Chasing:
                HandleChasingState();
                break;
            case MonsterState.Attacking:
                HandleAttackingState();
                break;
            case MonsterState.Dying:
                HandleDyingState();
                break;
        }
    }
    
    protected virtual void HandleSpawningState()
    {
        if (StateTimer >= spawnDuration)
        {
            TransitionToState(MonsterState.Idle);
        }
    }
    
    protected virtual void HandleIdleState()
    {
        if (IsPlayerInRange(detectionRange))
        {
            TransitionToState(MonsterState.Chasing);
        }
        else if (StateTimer >= idleWaitTime)
        {
            TransitionToState(MonsterState.Patrolling);
        }
    }
    
    protected virtual void HandlePatrollingState()
    {
        if (IsPlayerInRange(detectionRange))
        {
            TransitionToState(MonsterState.Chasing);
        }
    }
    
    protected virtual void HandleChasingState()
    {
        float loseTargetRange = detectionRange * loseTargetRangeMultiplier;
        
        if (!IsPlayerInRange(loseTargetRange))
        {
            TransitionToState(MonsterState.Patrolling);
        }
        else if (IsPlayerInRange(attackRange) && CanAttack)
        {
            TransitionToState(MonsterState.Attacking);
        }
    }
    
    protected virtual void HandleAttackingState()
    {
        float totalAttackTime = attackWindupTime + attackDuration;
        
        if (StateTimer >= totalAttackTime)
        {
            LastAttackTime = Time.time;
            OnAttackComplete();
            TransitionToState(GetStateAfterAttack());
        }
        else if (StateTimer >= attackWindupTime && StateTimer < totalAttackTime)
        {
            // Attack is active - check for hit (only once per attack)
            if (!HasHitPlayerThisAttack && IsPlayerInRange(attackRange))
            {
                HasHitPlayerThisAttack = true;
                HitPlayer();
            }
        }
    }
    
    protected virtual void HandleDyingState()
    {
        if (StateTimer >= deathAnimationDuration)
        {
            currentState = MonsterState.Dead;
            gameObject.SetActive(false);
        }
    }
    
    protected virtual void TransitionToState(MonsterState newState)
    {
        OnStateExit(currentState);
        currentState = newState;
        StateTimer = 0f;
        OnStateEnter(newState);
    }
    
    protected virtual void OnStateEnter(MonsterState state)
    {
        switch (state)
        {
            case MonsterState.Attacking:
                HasHitPlayerThisAttack = false;
                OnAttackStart();
                OnMonsterAttackStart?.Invoke(this);
                break;
        }
    }
    
    protected virtual void OnStateExit(MonsterState state)
    {
        // Override in derived classes for state-specific cleanup
    }
    
    protected bool IsPlayerInRange(float range)
    {
        return GameManager && GameManager.IsPlayerValid() && DistanceToPlayer <= range;
    }
    
    protected Vector2 GetPlayerPosition()
    {
        return GameManager ? GameManager.GetPlayerPosition() : Vector2.zero;
    }
    
    protected virtual void HitPlayer()
    {
        if (GameManager && GameManager.IsPlayerValid())
        {
            OnMonsterAttackHit?.Invoke(this);
            GameManager.EndGame(); // Trigger game over
            Debug.Log($"{name} hit the player! Game Over!");
        }
    }
    
    public virtual void Die()
    {
        if (currentState == MonsterState.Dying || currentState == MonsterState.Dead)
            return;
            
        TransitionToState(MonsterState.Dying);
        
        // Spawn death effect
        if (deathEffect)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, deathEffectDuration);
        }
        
        OnMonsterDied?.Invoke(this);
    }
    
    // Virtual methods for derived classes to override
    protected abstract void UpdateMonster();
    protected virtual void OnAttackStart() { }
    protected virtual void OnAttackComplete() { }
    protected virtual MonsterState GetStateAfterAttack() 
    { 
        return currentState == MonsterState.Chasing ? MonsterState.Chasing : MonsterState.Patrolling; 
    }
    
    // Debug visualization
    protected virtual void OnDrawGizmos()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Lose target range (when chasing)
        if (Application.isPlaying && currentState == MonsterState.Chasing)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange * loseTargetRangeMultiplier);
        }
        
        // State info
        if (Application.isPlaying)
        {
            Gizmos.color = GetStateColor();
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
        }
        
        // Direction to player
        if (Application.isPlaying && GameManager && GameManager.IsPlayerValid())
        {
            Gizmos.color = Color.cyan;
            Vector2 playerPos = GetPlayerPosition();
            Gizmos.DrawLine(transform.position, playerPos);
        }
    }
    
    private Color GetStateColor()
    {
        return currentState switch
        {
            MonsterState.Spawning => Color.white,
            MonsterState.Idle => Color.blue,
            MonsterState.Patrolling => Color.green,
            MonsterState.Chasing => Color.yellow,
            MonsterState.Attacking => Color.red,
            MonsterState.Dying => Color.gray,
            _ => Color.black
        };
    }
}
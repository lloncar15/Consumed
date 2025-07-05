using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform playerTransform;
    
    [Header("Game State")]
    [SerializeField] private bool gameOver = false;
    [SerializeField] private float gameTime = 0f;
    [SerializeField] private int score = 0;
    
    // Singleton
    public static GameManager Instance { get; private set; }
    
    // Events
    public static event Action OnGameStart;
    public static event Action OnGameOver;
    public static event Action<int> OnScoreChanged;
    public static event Action<float> OnGameTimeChanged;
    
    // Properties
    public PlayerController Player => playerController;
    public Transform PlayerTransform => playerTransform;
    public bool IsGameOver => gameOver;
    public float GameTime => gameTime;
    public int Score => score;
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Auto-find player if not assigned
        if (playerController == null) {
            playerController = FindFirstObjectByType<PlayerController>();
        }
        
        if (playerTransform == null && playerController != null)
        {
            playerTransform = playerController.transform;
        }
        
        if (playerController == null)
        {
            Debug.LogError("GameManager: No PlayerController found in scene!");
        }
        
        StartGame();
    }
    
    private void Update()
    {
        if (!gameOver)
        {
            UpdateGameTime();
        }
    }
    
    private void InitializeGame()
    {
        gameOver = false;
        gameTime = 0f;
        score = 0;
        
        Debug.Log("GameManager initialized");
    }
    
    public void StartGame()
    {
        gameOver = false;
        OnGameStart?.Invoke();
        Debug.Log("Game started");
    }
    
    public void EndGame()
    {
        if (gameOver) return;
        
        gameOver = true;
        OnGameOver?.Invoke();
        Debug.Log($"Game Over! Final Score: {score}, Time: {gameTime:F1}s");
    }
    
    private void UpdateGameTime()
    {
        gameTime += Time.deltaTime;
        OnGameTimeChanged?.Invoke(gameTime);
    }
    
    public void AddScore(int points)
    {
        if (gameOver) return;
        
        score += points;
        OnScoreChanged?.Invoke(score);
    }
    
    public void ResetGame()
    {
        InitializeGame();
        StartGame();
    }
    
    // Player state queries
    public Vector2 GetPlayerPosition()
    {
        return playerTransform ? (Vector2)playerTransform.position : Vector2.zero;
    }
    
    public bool IsPlayerValid()
    {
        return playerController is not null && playerTransform is not null;
    }
    
    public float GetDistanceToPlayer(Vector2 position)
    {
        if (!IsPlayerValid())
            return float.MaxValue;
            
        return Vector2.Distance(position, GetPlayerPosition());
    }
    
    public Vector2 GetDirectionToPlayer(Vector2 fromPosition)
    {
        if (!IsPlayerValid())
            return Vector2.zero;
            
        return (GetPlayerPosition() - fromPosition).normalized;
    }
    
    // Game state management
    public void PauseGame()
    {
        Time.timeScale = 0f;
    }
    
    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }
    
    // Player reference management (for dynamic player spawning)
    public void RegisterPlayer(PlayerController player)
    {
        playerController = player;
        playerTransform = player.transform;
        Debug.Log("Player registered with GameManager");
    }
    
    public void UnregisterPlayer()
    {
        playerController = null;
        playerTransform = null;
        Debug.Log("Player unregistered from GameManager");
    }
}
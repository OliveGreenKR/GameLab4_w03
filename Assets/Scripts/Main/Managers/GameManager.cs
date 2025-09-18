using Sirenix.OdinInspector;
using System;
using UnityEngine;


/// <summary>
/// 게임 상태 정의
/// </summary>
public enum GameState
{
    WaveReady,
    WaveInProgress,
    WaveCompleted,
    GameOver,
    GameCleared
}

/// <summary>
/// 게임 전반을 관리하는 싱글톤 매니저
/// 플레이어 체력, 골드 관리 및 게임오버 판정
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [Header("Initial Stats")]
    [SerializeField] private int _initialHealth = 100;
    [SerializeField] private int _initialGold = 50;
    [SerializeField] private int _enemyKillGoldReward = 10;

    [Header("Wave System")]
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private int _maxWaves = 10;

    [Header("Game State")]
    [SerializeField] private GameState _initialGameState = GameState.WaveReady;
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public GameState CurrentState { get; private set; }

    [ShowInInspector, ReadOnly]
    public int CurrentHealth { get; private set; }

    [ShowInInspector, ReadOnly]
    public int CurrentGold { get; private set; }

    [ShowInInspector, ReadOnly]
    public int CurrentWave => _enemySpawner != null ? _enemySpawner.CurrentCycle : 0;

    [ShowInInspector, ReadOnly]
    public int ActiveEnemyCount { get; private set; }

    // 상태 기반 계산 프로퍼티들
    [ShowInInspector, ReadOnly]
    public bool IsGameOver => CurrentState == GameState.GameOver;

    [ShowInInspector, ReadOnly]
    public bool IsGameCleared => CurrentState == GameState.GameCleared;

    [ShowInInspector, ReadOnly]
    public bool IsSpawning => _enemySpawner != null && _enemySpawner.IsSpawning;

    [ShowInInspector, ReadOnly]
    public bool CanStartWave => CurrentState == GameState.WaveReady || CurrentState == GameState.WaveCompleted;

    [ShowInInspector, ReadOnly]
    public bool IsWaveInProgress => CurrentState == GameState.WaveInProgress;
    #endregion

    #region Events
    /// <summary>게임 상태 변경 이벤트 (oldState, newState)</summary>
    public event Action<GameState, GameState> OnGameStateChanged;

    /// <summary>골드 변화 이벤트 (oldGold, newGold)</summary>
    public event Action<int, int> OnGoldChanged;

    /// <summary>체력 변화 이벤트 (oldHealth, newHealth)</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>게임오버 이벤트</summary>
    public event Action OnGameOver;

    /// <summary>웨이브 시작 이벤트 (waveNumber)</summary>
    public event Action<int> OnWaveStarted;

    /// <summary>웨이브 완료 이벤트 (waveNumber)</summary>
    public event Action<int> OnWaveCompleted;

    /// <summary>게임 클리어 이벤트</summary>
    public event Action OnGameCleared;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        InitializeStats();
    }

    private void OnDestroy()
    {
        // Scene 이벤트 구독 해제
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        BattleInteractionSystem.OnEntityKilled -= OnEnemyKilled;

        if (_enemySpawner != null)
        {
            _enemySpawner.OnSpawnCompleted -= OnSpawnCompleted;
        }
    }
    #endregion

    #region Public Methods - Game Control
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    #endregion

    #region Public Methods - Stats
    public bool SpendGold(int cost)
    {
        if (cost <= 0 || CurrentGold < cost || IsGameOver)
            return false;

        int oldGold = CurrentGold;
        CurrentGold -= cost;
        OnGoldChanged?.Invoke(oldGold, CurrentGold);
        return true;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsGameOver)
            return;

        int oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        OnHealthChanged?.Invoke(oldHealth, CurrentHealth);

        CheckGameOverCondition();
    }
    #endregion

    #region Public Methods - Wave Management
    public void StartWave()
    {
        if (!CanStartWave || _enemySpawner == null)
            return;

        _enemySpawner.StartSpawning();
        ChangeGameState(GameState.WaveInProgress);
    }

    public void StopWave()
    {
        if (_enemySpawner == null || !IsWaveInProgress)
            return;

        _enemySpawner.StopSpawning();
        ChangeGameState(GameState.WaveCompleted);
    }
    #endregion

    #region Public Methods - Game Events
    public void OnEnemyKilled(IBattleEntity killer, IBattleEntity victim)
    {
        if (victim == null || IsGameOver)
            return;
        //increase Gold
        int oldGold = CurrentGold;
        CurrentGold += _enemyKillGoldReward;
        OnGoldChanged?.Invoke(oldGold, CurrentGold);

        //decrease ActiveEnemyCount
        ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);

        CheckAndUpdateWaveState();
    }
    #endregion

    #region Private Methods - State Management
    private void ChangeGameState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        GameState oldState = CurrentState;
        CurrentState = newState;

        Debug.Log($"[GameManager] State changed: {oldState} → {newState}");

        OnGameStateChanged?.Invoke(oldState, newState);
        HandleStateTransition(newState);
    }

    private void HandleStateTransition(GameState newState)
    {
        switch (newState)
        {
            case GameState.WaveInProgress:
                OnWaveStarted?.Invoke(CurrentWave);
                break;

            case GameState.WaveCompleted:
                OnWaveCompleted?.Invoke(CurrentWave);
                break;

            case GameState.GameOver:
                OnGameOver?.Invoke();
                break;

            case GameState.GameCleared:
                OnGameCleared?.Invoke();
                break;
        }
    }

    private void CheckAndUpdateWaveState()
    {
        if (CurrentState != GameState.WaveInProgress)
            return;

        if (!IsSpawning && ActiveEnemyCount == 0)
        {
            if (CurrentWave >= _maxWaves)
            {
                ChangeGameState(GameState.GameCleared);
            }
            else
            {
                ChangeGameState(GameState.WaveCompleted);
            }
        }
    }

    private void CheckGameOverCondition()
    {
        if (CurrentHealth <= 0 && CurrentState != GameState.GameOver)
        {
            ChangeGameState(GameState.GameOver);
        }
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("[GameManager] Multiple instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
        }
    }

    private void InitializeStats()
    {
        CurrentHealth = _initialHealth;
        CurrentGold = _initialGold;
        ActiveEnemyCount = 0;

        ChangeGameState(_initialGameState);

        BattleInteractionSystem.OnEntityKilled -= OnEnemyKilled;
        BattleInteractionSystem.OnEntityKilled += OnEnemyKilled;

        if (_enemySpawner != null)
        {
            _enemySpawner.OnSpawnCompleted -= OnSpawnCompleted;
            _enemySpawner.OnSpawnCompleted += OnSpawnCompleted;
        }
    }

    private void ReconnectEnemySpawner()
    {
        if (_enemySpawner != null)
        {
            _enemySpawner.OnSpawnCompleted -= OnSpawnCompleted;
        }

        _enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (_enemySpawner != null)
        {
            _enemySpawner.OnSpawnCompleted -= OnSpawnCompleted;
            _enemySpawner.OnSpawnCompleted += OnSpawnCompleted;
        }
        else
        {
            Debug.LogError("[GameManager] No EnemySpawner found in the scene.");
        }
    }
    #endregion

    #region Private Methods - Event CallBacks
    private void OnSpawnCompleted(int spawnedCount, int currentCycle)
    {
        ActiveEnemyCount += spawnedCount;
        CheckAndUpdateWaveState();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ReconnectEnemySpawner();
        InitializeStats();
    }
    #endregion
}
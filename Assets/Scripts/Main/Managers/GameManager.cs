using Sirenix.OdinInspector;
using System;
using UnityEngine;

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
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public int CurrentHealth { get; private set; }

    [ShowInInspector, ReadOnly]
    public int CurrentGold { get; private set; }

    [ShowInInspector, ReadOnly]
    public bool IsGameOver { get; private set; }

    [ShowInInspector, ReadOnly]
    public int CurrentWave => _enemySpawner != null ? _enemySpawner.CurrentCycle : 0;

    [ShowInInspector, ReadOnly]
    public bool IsSpawning => _enemySpawner != null && _enemySpawner.IsSpawning;

    [ShowInInspector, ReadOnly]
    public int ActiveEnemyCount { get; private set; }

    [ShowInInspector, ReadOnly]
    public bool IsWaveComplete => !IsSpawning && ActiveEnemyCount == 0;
    #endregion

    #region Events
    /// <summary>골드 변화 이벤트 (oldGold, newGold) </summary>
    public event Action<int, int> OnGoldChanged;  

    /// <summary>체력 변화 이벤트 (oldHealth, newHealth) </summary>
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
    /// <summary>게임 재시작</summary>
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    #endregion

    #region Public Methods - Stats
    /// <summary>골드 소모</summary>
    public bool SpendGold(int cost)
    {
        if (cost <= 0 || CurrentGold < cost)
            return false;

        int oldGold = CurrentGold;
        CurrentGold -= cost;
        OnGoldChanged?.Invoke(oldGold, CurrentGold);
        return true;
    }

    /// <summary>체력 감소</summary>
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsGameOver)
            return;

        int oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        OnHealthChanged?.Invoke(oldHealth, CurrentHealth);
        CheckGameOver();
    }
    #endregion

    #region Public Methods - Wave Management
    /// <summary>웨이브 시작</summary>
    public void StartWave()
    {
        if (_enemySpawner == null || IsGameOver)
            return;

        _enemySpawner.StartSpawning();
        OnWaveStarted?.Invoke(CurrentWave);
    }

    /// <summary>웨이브 중지</summary>
    public void StopWave()
    {
        if (_enemySpawner == null)
            return;


        _enemySpawner.StopSpawning();
    }

    /// <summary>웨이브 완료 체크</summary>
    public void CheckWaveCompletion()
    {
        if (IsWaveComplete && !IsGameOver)
        {
            OnWaveCompleted?.Invoke(CurrentWave);

            // 게임 클리어 체크
            if (CurrentWave >= _maxWaves)
            {
                OnGameCleared?.Invoke();
            }
        }
    }
    #endregion

    #region Public Methods - Game Events
    /// <summary>적 처치시 호출</summary>
    public void OnEnemyKilled(IBattleEntity killer, IBattleEntity victim)
    {
        if (victim == null || IsGameOver)
            return;
        //TODO : 희생자의 타입별 차등 보상 (일반/엘리트)
        //골드 획득
        int oldGold = CurrentGold;
        CurrentGold += _enemyKillGoldReward;
        OnGoldChanged?.Invoke(oldGold, CurrentGold);

        // 적 카운트 감소
        ActiveEnemyCount = Mathf.Max(0, ActiveEnemyCount - 1);
        CheckWaveCompletion();
    }
    #endregion

    #region Private Methods
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
        IsGameOver = false;

        // 적 처치 이벤트 구독
        BattleInteractionSystem.OnEntityKilled -= OnEnemyKilled;
        BattleInteractionSystem.OnEntityKilled += OnEnemyKilled;

        if (_enemySpawner != null)
        {
            _enemySpawner.OnSpawnCompleted -= OnSpawnCompleted;
            _enemySpawner.OnSpawnCompleted += OnSpawnCompleted;
        }
    }

    private void CheckGameOver()
    {
        if (CurrentHealth <= 0 && !IsGameOver)
        {
            IsGameOver = true;
            OnGameOver?.Invoke();
        }
    }
    private void ReconnectEnemySpawner()
    {
        // 기존 구독 해제
        if (_enemySpawner != null)
        {
            _enemySpawner.OnSpawnCompleted -= OnSpawnCompleted;
        }

        // 재참조
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

    #region Private Methods -  Event CallBacks
    private void OnSpawnCompleted(int spawnedCount, int currentCycle)
    {
        ActiveEnemyCount += spawnedCount;
        CheckWaveCompletion();
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ReconnectEnemySpawner();
        InitializeStats();
    }
    #endregion
}
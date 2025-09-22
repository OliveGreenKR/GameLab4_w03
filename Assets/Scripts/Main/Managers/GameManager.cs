using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using TMPro;
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
    [Header("Player Integration")]
    [SerializeField] private PlayerBattleEntity _playerBattleEntity;

    [Header("Initial Stats")]
    [SerializeField] private int _initialGold = 50;

    [Header("Enemy Kill Rewards")]
    [InfoBox("적 타입별 골드 보상 설정")]
    [SerializeField] private int _normalEnemyGoldReward = 1;
    [SerializeField] private int _eliteEnemyGoldReward = 25;

    [Header("Wave System")]
    [SerializeField] private EnemySpawner _enemySpawner;
    [SerializeField] private int _maxWaves = 10;
    [SerializeField] private int _currentWave = 1;

    [Header("Game State")]
    [SerializeField] private GameState _initialGameState = GameState.WaveReady;

    [Header("Play Session Tracking")]
    [InfoBox("플레이 세션 기록 시스템")]
    [SerializeField] private float _gameStartTime = 0f;
    [SerializeField] private int _totalEarnedGold = 0;

    [Header("UI References")]
    [SerializeField] Canvas _hudUI = null;
    [SerializeField] Canvas _gameOverUI = null;
    [SerializeField] TMP_Text _gameOverTxt = null;
    #endregion

    #region Private Fields
    private InputSystem_Actions _inputActions;
    private int _activeNormalEnemyCount = 0;
    private int _activeEliteEnemyCount = 0;
    private int _totalNormalEnemySpawned = 0;
    private int _totalEliteEnemySpawned = 0;
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public GameState CurrentState { get; private set; }

    [ShowInInspector, ReadOnly]
    public int CurrentGold { get; private set; }

    [ShowInInspector, ReadOnly]
    public int CurrentWave => _currentWave;

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

    // 몬스터 추적
    [ShowInInspector, ReadOnly]
    public int ActiveEnemyCount => _activeNormalEnemyCount + _activeEliteEnemyCount;

    [ShowInInspector, ReadOnly]
    public int ActiveNormalEnemyCount => _activeNormalEnemyCount;

    [ShowInInspector, ReadOnly]
    public int ActiveEliteEnemyCount => _activeEliteEnemyCount;

    [ShowInInspector, ReadOnly]
    public int TotalNormalEnemyCount => _totalNormalEnemySpawned;

    [ShowInInspector, ReadOnly]
    public int TotalEliteEnemyCount => _totalEliteEnemySpawned;

    [ShowInInspector, ReadOnly]
    public string NormalEnemyProgress => $"{ActiveNormalEnemyCount}/{TotalNormalEnemyCount}";

    [ShowInInspector, ReadOnly]
    public string EliteEnemyProgress => $"{ActiveEliteEnemyCount}/{TotalEliteEnemyCount}";

    // 플레이 추적
    [ShowInInspector, ReadOnly]
    public float SurvivalTimeSeconds => _gameStartTime > 0f ? Time.time - _gameStartTime : 0f;

    [ShowInInspector, ReadOnly]
    public int TotalEarnedGold => _totalEarnedGold;

    // 전역 인풋 시스템
    public InputSystem_Actions InputActions => _inputActions;
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
        StartGame();
    }

    private void OnEnable()
    {
        _inputActions?.Enable();
    }

    private void OnDisable()
    {
        _inputActions?.Disable();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        _inputActions?.Dispose();
    }
    #endregion

    #region Public Methods - Cursor Control
    public void SetCursorState(bool visible, CursorLockMode lockMode)
    {
        Cursor.visible = visible;
        Cursor.lockState = lockMode;
    }
    /// <summary>
    /// 중앙 고정 커서 설정
    /// </summary>
    /// <param name="visible"></param>
    /// <param name="lockMode"></param>
    public void CursorLock()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// 커서 자유모드
    /// </summary>
    /// <param name="visible"></param>
    /// <param name="lockMode"></param>
    public void CursorFree()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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

        // Proxy에서 적 타입 조회
        PrefabType enemyType = GetEnemyTypeFromVictim(victim);
        int goldReward = GetGoldRewardForEnemyType(enemyType);

        Debug.Log($"[GameManager] Enemy killed: {victim.GameObject.name} (Type: {enemyType}), Reward: {goldReward} Gold", victim.GameObject);

        // 골드 지급 및 누적
        int oldGold = CurrentGold;
        CurrentGold += goldReward;
        _totalEarnedGold += goldReward;
        OnGoldChanged?.Invoke(oldGold, CurrentGold);

        // 적 수 감소
        switch (enemyType)
        {
            case PrefabType.EnemyNormal:
                _activeNormalEnemyCount = Mathf.Max(0, _activeNormalEnemyCount - 1);
                break;
            case PrefabType.EnemyElite:
                _activeEliteEnemyCount = Mathf.Max(0, _activeEliteEnemyCount - 1);
                break;
            default:
                Debug.LogWarning($"[GameManager] Unknown enemy type on kill: {enemyType}. No active count decremented.", victim.GameObject);
                break;
        }
        CheckAndUpdateWaveState();

    }
    #endregion

    #region Private Methods - Reward Calculation
    /// <summary>
    /// 처치된 적으로부터 적 타입 조회
    /// </summary>
    /// <param name="victim">처치된 적 엔티티</param>
    /// <returns>적 타입 (찾지 못하면 EnemyNormal)</returns>
    private PrefabType GetEnemyTypeFromVictim(IBattleEntity victim)
    {
        if (victim?.GameObject == null)
        {
            Debug.LogWarning("[GameManager] Victim or GameObject is null. Using normal enemy reward.");
            return PrefabType.EnemyNormal;
        }

        EnemyGameManagerProxy proxy = victim.GameObject.GetComponent<EnemyGameManagerProxy>();
        if (proxy == null)
        {
            Debug.LogWarning($"[GameManager] No EnemyGameManagerProxy found on {victim.GameObject.name}. Using normal enemy reward.", victim.GameObject);
            return PrefabType.EnemyNormal;
        }

        return proxy.EnemyType;
    }

    /// <summary>
    /// 적 타입에 따른 골드 보상 계산
    /// </summary>
    /// <param name="enemyType">적 타입</param>
    /// <returns>골드 보상량</returns>
    private int GetGoldRewardForEnemyType(PrefabType enemyType)
    {
        switch (enemyType)
        {
            case PrefabType.EnemyNormal:
                return _normalEnemyGoldReward;
            case PrefabType.EnemyElite:
                return _eliteEnemyGoldReward;
            default:
                Debug.LogWarning($"[GameManager] Unknown enemy type: {enemyType}. Using normal reward.");
                return _normalEnemyGoldReward;
        }
    }
    #endregion

    #region Public Methods - Game Control
    /// <summary>
    /// 게임 재시작 
    /// </summary>
    public void RestartGame()
    {
        // 내부 상태 완전 리셋
        ResetInternalState();


        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        Debug.Log("[GameManager] Game restarted via scene reload");
    }

    /// <summary>
    /// 인게임 재시작 - 상태 리셋 방식
    /// </summary>
    public void RestartGameInPlace()
    {
        // 내부 상태 완전 리셋
        ResetInternalState();

        //HUD, GameOver UI 초기 상태 설정
        if (_hudUI != null) _hudUI.enabled = true;
        if (_gameOverUI != null) _gameOverUI.enabled = false;

        // 시스템별 초기화
        InitializeWaveSystem();
        ResetDifficultySystem();
        InitializePlayerState();

        // 게임 시작
        StartWave();

        Debug.Log("[GameManager] Game restarted in-place");
    }

    /// <summary>
    /// 게임 시작 - 웨이브 및 난이도 통합 초기화
    /// </summary>
    public void StartGame()
    {
        if (_enemySpawner == null)
        {
            Debug.LogWarning("[GameManager] Cannot start game: EnemySpawner not found");
            return;
        }

        SubscribeToEvents();
        _inputActions?.Enable();
        CursorLock();

        // 내부 상태 완전 리셋
        ResetInternalState();

        //HUD, GameOver UI 초기 상태 설정
        if (_hudUI != null) _hudUI.enabled = true;
        if (_gameOverUI != null) _gameOverUI.enabled = false;

        // 웨이브 시스템 초기화
        InitializeWaveSystem();

        // 난이도 시스템 리셋
        ResetDifficultySystem();

        // 플레이어 상태 초기화
        InitializePlayerState();

        // 웨이브 시작
        StartWave();

        Debug.Log("[GameManager] Game started successfully");
    }
    #endregion

    #region Private Methods - State Management
    private void OnGameOvered()
    {
        //HUD, GameOver UI 상태 설정
        if (_hudUI != null) _hudUI.enabled = false;
        if (_gameOverUI != null && _gameOverTxt != null)
        {
            _gameOverTxt.text = $"Game Over\n Gold Earned: {_totalEarnedGold}";
            _gameOverUI.enabled = true;

        }

        // 커서 해제
        CursorFree();
    }
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
            case GameState.WaveReady:
                // 자동 웨이브 시작
                StartWave();
                break;

            case GameState.WaveInProgress:
                OnWaveStarted?.Invoke(CurrentWave);
                break;

            case GameState.WaveCompleted:
                OnWaveCompleted?.Invoke(CurrentWave);

                // 자동 웨이브 진행 처리
                if (_currentWave >= _maxWaves)
                {
                    ChangeGameState(GameState.GameCleared);
                }
                else
                {
                    // NextWave를 직접 호출하지 않고 WaveReady로 전환
                    _currentWave++;

                    // 스폰너 난이도 업그레이드
                    if (_enemySpawner != null)
                    {
                        _enemySpawner.UpgradeDifficulty();
                    }

                    ChangeGameState(GameState.WaveReady);
                    Debug.Log($"[GameManager] Auto-advanced to wave {_currentWave}");
                }
                break;

            case GameState.GameOver:
                OnGameOvered();
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
            ChangeGameState(GameState.WaveCompleted);
        }
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeWaveSystem()
    {
        if (_enemySpawner == null) return;

        // 스폰너가 실행 중이면 정지
        if (_enemySpawner.IsSpawning)
        {
            _enemySpawner.StopSpawning();
        }

        _enemySpawner.ResetAllStates();

        ChangeGameState(GameState.WaveReady);
        Debug.Log("[GameManager] Wave system initialized");
    }

    private void ResetDifficultySystem()
    {
        if (_enemySpawner == null) return;

        // 난이도 시스템 리셋
        _enemySpawner.ResetDifficulty();
        Debug.Log("[GameManager] Difficulty system reset");
    }

    private void InitializePlayerState()
    {
        if (_playerBattleEntity == null) return;

        // 플레이어 체력 완전 회복
        _playerBattleEntity.InitializePlayer();
        Debug.Log("[GameManager] Player state initialized");
    }

    /// <summary>
    /// 내부 상태 완전 리셋
    /// </summary>
    private void ResetInternalState()
    {
        // 웨이브 리셋
        _currentWave = 1;

        // 적 카운터 리셋
        _activeNormalEnemyCount = 0;
        _activeEliteEnemyCount = 0;
        _totalNormalEnemySpawned = 0;
        _totalEliteEnemySpawned = 0;

        // 세션 데이터 리셋
        _gameStartTime = Time.time;
        _totalEarnedGold = 0;

        // 골드 초기화
        int oldGold = CurrentGold;
        CurrentGold = _initialGold;
        OnGoldChanged?.Invoke(oldGold, CurrentGold);

        Debug.Log("[GameManager] Internal state reset completed");
    }

    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // InputSystem_Actions 초기화
            _inputActions = new InputSystem_Actions();
        }
        else
        {
            Debug.LogWarning("[GameManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    #endregion

    #region Private Methods - Player Integration
    private void SubscribeToPlayerEvents()
    {
        if (_playerBattleEntity == null)
        {
            _playerBattleEntity = FindFirstObjectByType<PlayerBattleEntity>();
        }

        if (_playerBattleEntity != null && _playerBattleEntity.BattleStat != null)
        {
            _playerBattleEntity.BattleStat.OnStatChanged -= OnPlayerHealthChanged;
            _playerBattleEntity.BattleStat.OnStatChanged += OnPlayerHealthChanged;
            _playerBattleEntity.BattleStat.OnDeath -= OnPlayerDeath;
            _playerBattleEntity.BattleStat.OnDeath += OnPlayerDeath;
        }
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (_playerBattleEntity != null && _playerBattleEntity.BattleStat != null)
        {
            _playerBattleEntity.BattleStat.OnStatChanged -= OnPlayerHealthChanged;
            _playerBattleEntity.BattleStat.OnDeath -= OnPlayerDeath;
        }
    }

    private void OnPlayerHealthChanged(BattleStatType statType, float oldValue, float newValue)
    {
        //DoSomething : Player GameOVer?
    }

    private void OnPlayerDeath(IBattleEntity killer)
    {
        if (CurrentState != GameState.GameOver)
        {
            ChangeGameState(GameState.GameOver);
        }
    }
    #endregion

    #region Private Methods - Event CallBacks
    /// <summary>
    /// 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        BattleInteractionSystem.OnEntityKilled -= OnEnemyKilled;
        BattleInteractionSystem.OnEntityKilled += OnEnemyKilled;

        if (_enemySpawner != null)
        {
            _enemySpawner.OnSpawnCompleted += OnSpawnCompleted;
            _enemySpawner.OnDifficultyUpgraded += OnDifficultyUpgraded;
        }

        SubscribeToPlayerEvents();
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        BattleInteractionSystem.OnEntityKilled -= OnEnemyKilled;

        if (_enemySpawner != null)
        {
            _enemySpawner.OnSpawnCompleted -= OnSpawnCompleted;
            _enemySpawner.OnDifficultyUpgraded -= OnDifficultyUpgraded;
        }

        UnsubscribeFromPlayerEvents();
    }

    /// <summary>
    /// 난이도 업그레이드 시 스폰 정지
    /// </summary>
    private void OnDifficultyUpgraded(int newCycle)
    {
        if (CurrentState == GameState.WaveInProgress)
        {
            StopWave();
            Debug.Log($"[GameManager] Wave stopped due to difficulty upgrade (Cycle: {newCycle})");
        }
    }

    private void OnSpawnCompleted(Dictionary<PrefabType, int> spawnedByType, int currentCycle)
    {
        // 타입별 스폰 카운터 업데이트
        foreach (var kvp in spawnedByType)
        {
            int spawnedCount = kvp.Value;

            switch (kvp.Key)
            {
                case PrefabType.EnemyNormal:
                    _activeNormalEnemyCount += spawnedCount;
                    _totalNormalEnemySpawned += spawnedCount;
                    break;

                case PrefabType.EnemyElite:
                    _activeEliteEnemyCount += spawnedCount;
                    _totalEliteEnemySpawned += spawnedCount;
                    break;
            }
        }

        CheckAndUpdateWaveState();
    }
    #endregion
}
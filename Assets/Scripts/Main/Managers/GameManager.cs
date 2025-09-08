using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [TabGroup("References")]
    [Header("Game Systems")]
    [Required]
    [SerializeField] private EnemySpawner _enemySpawner;

    [TabGroup("Settings")]
    [Header("Team Settings")]
    [InfoBox("플레이어 팀 ID")]
    [SerializeField] private int _playerTeamId = 0;

    [TabGroup("Settings")]
    [InfoBox("적 팀 ID")]
    [SerializeField] private int _enemyTeamId = 1;

    [TabGroup("Settings")]
    [Header("Level Up Settings")]
    [InfoBox("첫 번째 레벨업에 필요한 기본 킬 수")]
    [PropertyRange(1, 100)]
    [SuffixLabel("kills")]
    [SerializeField] private int _baseLevelUpKills = 10;

    [TabGroup("Settings")]
    [InfoBox("레벨업 요구 킬 수 증가 배율 (1.2 = 20% 증가)")]
    [PropertyRange(1.1f, 2.0f)]
    [SuffixLabel("multiplier")]
    [SerializeField] private float _levelUpMultiplier = 1.2f;
    #endregion

    #region Events
    /// <summary>
    /// 게임이 시작될 때 발생하는 이벤트
    /// </summary>
    public event Action OnGameStarted;

    /// <summary>
    /// 적을 처치했을 때 발생하는 이벤트
    /// </summary>
    public event Action<int> OnEnemyKilled; // (totalKillCount)

    /// <summary>
    /// 레벨업했을 때 발생하는 이벤트
    /// </summary>
    public event Action<int> OnPlayerLevelUp; // (newLevel)
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int EnemyKillCount { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentLevel { get; private set; } = 1;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int KillsForNextLevel { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    [ProgressBar(0, "KillsForNextLevel")]
    public int KillsTowardsNextLevel => EnemyKillCount;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsGameActive { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        SubscribeToBattleEvents();
        InitializeLevelSystem();
        StartGame();
    }

    private void OnDestroy()
    {
        UnsubscribeFromBattleEvents();
    }
    #endregion

    #region Public Methods - Game Control
    /// <summary>
    /// 게임 시작 (난이도 초기화 및 스폰 시작)
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[GameManager] Starting game...", this);  

        if (_enemySpawner == null)
        {
            Debug.LogError("[GameManager] EnemySpawner is not assigned!", this);
            return;
        }

        // 킬 카운트 및 레벨 리셋
        ResetGameProgress();

        // EnemySpawner 난이도 초기화 및 스폰 시작
        _enemySpawner.ResetDifficulty();
        _enemySpawner.StartSpawning();

        // 게임 상태 업데이트
        IsGameActive = true;

        // 이벤트 발생
        OnGameStarted?.Invoke();

        Debug.Log("[GameManager] Game started - difficulty reset and spawning initiated", this);
    }

    /// <summary>
    /// 게임 종료
    /// </summary>
    public void EndGame()
    {
        if (_enemySpawner != null)
        {
            _enemySpawner.StopSpawning();
        }

        // 게임 상태 업데이트
        IsGameActive = false;

        Debug.Log($"[GameManager] Game ended - Level: {CurrentLevel}, Total enemies killed: {EnemyKillCount}", this);
    }
    #endregion

    #region Public Methods - Query
    /// <summary>
    /// 현재 적 처치 수 조회
    /// </summary>
    /// <returns>적 처치 수</returns>
    public int GetEnemyKillCount()
    {
        return EnemyKillCount;
    }

    /// <summary>
    /// 현재 레벨 조회
    /// </summary>
    /// <returns>현재 레벨</returns>
    public int GetCurrentLevel()
    {
        return CurrentLevel;
    }

    /// <summary>
    /// 다음 레벨업까지 필요한 킬 수 조회
    /// </summary>
    /// <returns>다음 레벨업까지 필요한 킬 수</returns>
    public int GetKillsForNextLevel()
    {
        return KillsForNextLevel;
    }
    #endregion

    #region Private Methods - Event Handling
    private void SubscribeToBattleEvents()
    {
        BattleInteractionSystem.OnEntityKilled -= OnEntityKilled;
        BattleInteractionSystem.OnEntityKilled += OnEntityKilled;
    }

    private void UnsubscribeFromBattleEvents()
    {
        BattleInteractionSystem.OnEntityKilled -= OnEntityKilled;
    }

    private void OnEntityKilled(IBattleEntity killer, IBattleEntity victim)
    {
        if (killer == null || victim == null) return;

        // 플레이어가 적을 죽인 경우만 카운트
        bool isPlayerKill = killer.TeamId == _playerTeamId;
        bool isEnemyVictim = victim.TeamId == _enemyTeamId;

        if (isPlayerKill && isEnemyVictim)
        {
            EnemyKillCount++;
            OnEnemyKilled?.Invoke(EnemyKillCount);

            // 레벨업 체크
            CheckForLevelUp();

            Debug.Log($"[GameManager] Enemy killed! Total: {EnemyKillCount}", this);
        }
    }
    #endregion

    #region Private Methods - Core Logic
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[GameManager] Multiple GameManager instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
        }
    }

    private void InitializeLevelSystem()
    {
        CurrentLevel = 1;
        KillsForNextLevel = _baseLevelUpKills;
        Debug.Log($"[GameManager] Level system initialized - Level {CurrentLevel}, Next level at {KillsForNextLevel} kills", this);
    }

    private void ResetGameProgress()
    {
        EnemyKillCount = 0;
        CurrentLevel = 1;
        KillsForNextLevel = _baseLevelUpKills;
    }

    private void CheckForLevelUp()
    {
        if (EnemyKillCount >= KillsForNextLevel)
        {
            CurrentLevel++;
            CalculateNextLevelRequirement();

            OnPlayerLevelUp?.Invoke(CurrentLevel);
            Debug.Log($"[GameManager] LEVEL UP! Level {CurrentLevel} reached! Next level at {KillsForNextLevel} kills", this);
        }
    }

    private void CalculateNextLevelRequirement()
    {
        // 이전 요구 킬 수에 배율 적용
        float nextRequirement = KillsForNextLevel * _levelUpMultiplier;
        KillsForNextLevel = Mathf.RoundToInt(nextRequirement);
    }
    #endregion
}
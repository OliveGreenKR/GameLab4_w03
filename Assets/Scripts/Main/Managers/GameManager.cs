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
    [SerializeField] private int _enemyTeamId = 99;
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
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int EnemyKillCount { get; private set; }

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
        if (_enemySpawner == null)
        {
            Debug.LogError("[GameManager] EnemySpawner is not assigned!", this);
            return;
        }

        // 킬 카운트 리셋
        ResetKillCount();

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

        Debug.Log($"[GameManager] Game ended - Total enemies killed: {EnemyKillCount}", this);
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

    private void ResetKillCount()
    {
        EnemyKillCount = 0;
    }
    #endregion
}
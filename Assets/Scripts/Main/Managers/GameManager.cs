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
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public int CurrentHealth { get; private set; }

    [ShowInInspector, ReadOnly]
    public int CurrentGold { get; private set; }

    [ShowInInspector, ReadOnly]
    public bool IsGameOver { get; private set; }
    #endregion

    #region Events
    /// <summary>골드 변화 이벤트 (oldGold, newGold) </summary>
    public event Action<int, int> OnGoldChanged; 

    /// <summary>체력 변화 이벤트 (oldHealth, newHealth) </summary>
    public event Action<int, int> OnHealthChanged; 

    /// <summary>게임오버 이벤트</summary>
    public event Action OnGameOver;
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
        BattleInteractionSystem.OnEntityKilled -= OnEnemyKilled;
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

    #region Public Methods - Game Events
    /// <summary>적 처치시 호출</summary>
    public void OnEnemyKilled(IBattleEntity killer, IBattleEntity victim)
    {
        if (victim == null || IsGameOver)
            return;
        //TODO : 희생자의 타입별 차등 보상 (일반/엘리트)
        int oldGold = CurrentGold;
        CurrentGold += _enemyKillGoldReward;
        OnGoldChanged?.Invoke(oldGold, CurrentGold);
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
        BattleInteractionSystem.OnEntityKilled += OnEnemyKilled;
    }

    private void CheckGameOver()
    {
        if (CurrentHealth <= 0 && !IsGameOver)
        {
            IsGameOver = true;
            OnGameOver?.Invoke();
        }
    }
    #endregion
}
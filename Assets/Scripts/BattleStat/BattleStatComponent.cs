using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class BattleStatComponent : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Initial Stats")]
    [Header("Base Stats")]
    [SuffixLabel("units")]
    [PropertyRange(1f, 100000f)]
    [SerializeField] private float _baseHealth = 100f;

    [TabGroup("Initial Stats")]
    [SuffixLabel("damage")]
    [PropertyRange(1f, 100000f)]
    [SerializeField] private float _baseAttack = 10f;

    [TabGroup("Initial Stats")]
    [SuffixLabel("attacks/sec")]
    [PropertyRange(0.1f, 100f)]
    [SerializeField] private float _baseAttackSpeed = 1f;

    [TabGroup("Initial Stats")]
    [SuffixLabel("multiplier")]
    [PropertyRange(0.1f, 100f)]
    [SerializeField] private float _baseEffectRange = 1f;

    [TabGroup("Settings")]
    [Header("Team Settings")]
    [SerializeField] private int _teamId = 0;

    [TabGroup("Settings")]
    [Header("Optional Data Asset")]
    [SerializeField] private BattleStatData _statData;
    #endregion

    #region Properties
    [TabGroup("Current Stats")]
    [ShowInInspector, ReadOnly]
    [ProgressBar(0, "MaxHealth")]
    public float CurrentHealth { get; private set; }

    [TabGroup("Current Stats")]
    [ShowInInspector, ReadOnly]
    public float MaxHealth { get; private set; }

    [TabGroup("Current Stats")]
    [ShowInInspector, ReadOnly]
    public float CurrentAttack { get; private set; }

    [TabGroup("Current Stats")]
    [ShowInInspector, ReadOnly]
    public float CurrentAttackSpeed { get; private set; }

    [TabGroup("Current Stats")]
    [ShowInInspector, ReadOnly]
    public float CurrentEffectRange { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsAlive => CurrentHealth > 0f;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int TeamId => _teamId;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
    #endregion

    #region Events
    /// <summary>
    /// 스탯이 변경될 때 발생하는 이벤트
    /// </summary>
    public event Action<BattleStatType, float, float> OnStatChanged;

    /// <summary>
    /// 데미지를 받을 때 발생하는 이벤트
    /// </summary>
    public event Action<float, IBattleEntity> OnDamageTaken;

    /// <summary>
    /// 죽을 때 발생하는 이벤트
    /// </summary>
    public event Action<IBattleEntity> OnDeath;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_statData == null)
        {
            Debug.LogError("[BattleStatComponent] BattleStatData is required!", this);
        }
    }

    private void Start()
    {
        InitializeStats();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 스탯 초기화
    /// </summary>
    public void InitializeStats()
    {
        if (_statData == null)
        {
            Debug.LogError("[BattleStatComponent] Cannot initialize stats without BattleStatData!", this);
            return;
        }

        CurrentHealth = _statData.BaseHealth;
        MaxHealth = _statData.MaxHealth;
        CurrentAttack = _statData.BaseAttack;
        CurrentAttackSpeed = _statData.BaseAttackSpeed;
        CurrentEffectRange = _statData.BaseEffectRange;
    }

    /// <summary>
    /// 특정 스탯 값 조회
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>현재 스탯 값</returns>
    public float GetStat(BattleStatType statType)
    {
        switch (statType)
        {
            case BattleStatType.Health:
                return CurrentHealth;
            case BattleStatType.MaxHealth:
                return MaxHealth;
            case BattleStatType.Attack:
                return CurrentAttack;
            case BattleStatType.AttackSpeed:
                return CurrentAttackSpeed;
            case BattleStatType.EffectRange:
                return CurrentEffectRange;
            default:
                Debug.LogWarning($"[BattleStatComponent] Unknown stat type: {statType}", this);
                return 0f;
        }
    }

    /// <summary>
    /// 특정 스탯 값 설정
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="value">설정할 값</param>
    public void SetStat(BattleStatType statType, float value)
    {
        float oldValue = GetStat(statType);

        switch (statType)
        {
            case BattleStatType.Health:
                CurrentHealth = value;
                ClampHealth();
                break;
            case BattleStatType.MaxHealth:
                MaxHealth = value;
                ClampHealth();
                break;
            case BattleStatType.Attack:
                CurrentAttack = Mathf.Max(0f, value);
                break;
            case BattleStatType.AttackSpeed:
                CurrentAttackSpeed = Mathf.Max(0.1f, value);
                break;
            case BattleStatType.EffectRange:
                CurrentEffectRange = Mathf.Max(0.1f, value);
                break;
            default:
                Debug.LogWarning($"[BattleStatComponent] Cannot set unknown stat type: {statType}", this);
                return;
        }

        TriggerStatChanged(statType, oldValue, value);
    }

    /// <summary>
    /// 특정 스탯에 값 추가/감소
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="delta">변화량</param>
    public void ModifyStat(BattleStatType statType, float delta)
    {
        float currentValue = GetStat(statType);
        SetStat(statType, currentValue + delta);
    }

    /// <summary>
    /// 데미지 적용
    /// </summary>
    /// <param name="damage">데미지 양</param>
    /// <param name="attacker">공격자</param>
    /// <returns>실제 적용된 데미지</returns>
    public float ApplyDamage(float damage, IBattleEntity attacker = null)
    {
        if (!IsAlive || damage <= 0f)
            return 0f;

        float actualDamage = Mathf.Min(damage, CurrentHealth);
        CurrentHealth -= actualDamage;

        OnDamageTaken?.Invoke(actualDamage, attacker);

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            TriggerDeath(attacker);
        }

        return actualDamage;
    }

    /// <summary>
    /// 체력 회복
    /// </summary>
    /// <param name="amount">회복량</param>
    /// <returns>실제 회복된 양</returns>
    public float Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return 0f;

        float oldHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        float actualHealing = CurrentHealth - oldHealth;

        if (actualHealing > 0f)
        {
            TriggerStatChanged(BattleStatType.Health, oldHealth, CurrentHealth);
        }

        return actualHealing;
    }

    /// <summary>
    /// 팀 ID 설정
    /// </summary>
    /// <param name="teamId">팀 식별자</param>
    public void SetTeamId(int teamId)
    {
        _teamId = teamId;
    }
    #endregion

    #region Private Methods
    private void ClampHealth()
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth);
    }

    private void TriggerStatChanged(BattleStatType statType, float oldValue, float newValue)
    {
        if (Mathf.Approximately(oldValue, newValue))
            return;

        OnStatChanged?.Invoke(statType, oldValue, newValue);
    }

    private void TriggerDeath(IBattleEntity killer)
    {
        OnDeath?.Invoke(killer);
    }
    #endregion
}
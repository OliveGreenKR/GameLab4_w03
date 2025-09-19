using Sirenix.OdinInspector;
using System;
using UnityEngine;

public abstract class BaseBattleEntity : MonoBehaviour, IBattleEntity
{
    #region Serialized Fields
    [TabGroup("Battle")]
    [Header("Battle System")]
    [Required]
    [SerializeField] protected BattleStatComponent _battleStat;

    [TabGroup("Combat")]
    [SuffixLabel("seconds")]
    [SerializeField] protected float _invulnerabilityDuration = 1f;
    #endregion

    #region IBattleEntity Implementation
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;
    public bool IsAlive => _battleStat != null && _battleStat.IsAlive;
    public int TeamId => (int)_battleStat.GetCurrentStat(BattleStatType.TeamId);

    public virtual float TakeDamage(IBattleEntity attacker, float damage)
    {
        if (_battleStat == null || _isInvulnerable) return 0f;

        float actualDamage = _battleStat.ApplyDamage(damage, attacker);

        if (actualDamage > 0f)
        {
            TriggerInvulnerability();
        }

        return actualDamage;
    }

    public virtual float DealDamage(IBattleEntity target, float baseDamage)
    {
        return ProcessDamageToTarget(target);
    }

    public float GetCurrentStat(BattleStatType statType)
    {
        return _battleStat != null ? _battleStat.GetCurrentStat(statType) : 0f;
    }

    public void SetCurrentStat(BattleStatType statType, float value)
    {
        _battleStat?.SetCurrentStat(statType, value);
    }

    public void ModifyStat(BattleStatType statType, float delta)
    {
        _battleStat?.ModifyStat(statType, delta);
    }
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInvulnerable => _isInvulnerable;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float InvulnerabilityTimeRemaining => _invulnerabilityTimeRemaining;
    #endregion

    #region Private Fields
    private bool _isInvulnerable = false;
    private float _invulnerabilityTimeRemaining = 0f;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        if (_battleStat == null)
        {
            _battleStat = GetComponent<BattleStatComponent>();
        }

        if (_battleStat == null)
        {
            Debug.LogError("[BaseBattleEntity] BattleStatComponent required!", this);
        }
    }

    protected virtual void Start()
    {
        SubscribeToBattleStatEvents();
    }

    protected virtual void Update()
    {
        UpdateInvulnerability();
    }

    protected virtual void OnDestroy()
    {
        UnsubscribeFromBattleStatEvents();
    }
    #endregion

    #region Public Methods - Battle
    /// <summary>
    /// 사망 판정 후 호출
    /// </summary>
    /// <param name="killer"></param>
    public virtual void OnDeath(IBattleEntity killer = null)
    {
        Debug.Log($"{gameObject.name} has died.", this);
    }

    /// <summary>
    /// 무적 상태 수동 활성화
    /// </summary>
    /// <param name="duration">무적 지속 시간</param>
    public void SetInvulnerable(float duration)
    {
        _isInvulnerable = true;
        _invulnerabilityTimeRemaining = Mathf.Max(0f, duration);
    }

    /// <summary>
    /// 다른 엔티티에게 공격
    /// </summary>
    /// <param name="target">공격 대상</param>
    /// <returns>실제 가해진 데미지</returns>
    public virtual float AttackTarget(IBattleEntity target)
    {
        if (target == null || !IsAlive)
            return 0f;

        float attackStat = GetCurrentStat(BattleStatType.Attack);
        return DealDamage(target, attackStat);
    }
    #endregion

    #region Protected Methods - Virtual
    /// <summary>
    /// 나가는 데미지 계산
    /// </summary>
    /// <param name="target">대상 엔티티</param>
    /// <returns>계산된 데미지</returns>
    protected virtual float CalculateFinalDamage(IBattleEntity target)
    {
        return GetCurrentStat(BattleStatType.Attack);
    }

    /// <summary>
    /// 데미지 받았을 때 호출되는 함수
    /// 하위 클래스에서 오버라이드하여 추가 처리
    /// </summary>
    /// <param name="damage"></param>
    /// <param name="attacker"></param>
    protected virtual void OnDamageTakenFromBattleStat(float damage, IBattleEntity attacker)
    {
        // 하위 클래스에서 오버라이드하여 추가 처리
    }
    #endregion

    #region Protected Methods - Non-Virtual
    protected float ProcessDamageToTarget(IBattleEntity target)
    {
        if (target == null || !IsAlive)
            return 0f;

        if (BattleInteractionSystem.IsSameTeam(this, target))
            return 0f;

        float outgoingDamage = CalculateFinalDamage(target);
        if (outgoingDamage <= 0f)
            return 0f;

        return BattleInteractionSystem.ProcessDamageInteraction(this, target, outgoingDamage);
    }
    #endregion

    #region Private Methods - Invulnerability
    private void TriggerInvulnerability()
    {
        _isInvulnerable = true;
        _invulnerabilityTimeRemaining = _invulnerabilityDuration;
    }

    private void UpdateInvulnerability()
    {
        if (!_isInvulnerable) return;

        _invulnerabilityTimeRemaining -= Time.deltaTime;

        if (_invulnerabilityTimeRemaining <= 0f)
        {
            _isInvulnerable = false;
            _invulnerabilityTimeRemaining = 0f;
        }
    }
    #endregion

    #region Private Methods - Battle Events
    private void SubscribeToBattleStatEvents()
    {
        if (_battleStat == null) return;

        _battleStat.OnDamageTaken -= OnDamageTakenFromBattleStat;
        _battleStat.OnDamageTaken += OnDamageTakenFromBattleStat;
        _battleStat.OnDeath -= OnBattleStatDeath;
        _battleStat.OnDeath += OnBattleStatDeath;
    }

    private void UnsubscribeFromBattleStatEvents()
    {
        if (_battleStat == null) return;

        _battleStat.OnDamageTaken -= OnDamageTakenFromBattleStat;
        _battleStat.OnDeath -= OnBattleStatDeath;
    }

    private void OnBattleStatDeath(IBattleEntity killer)
    {
        OnDeath(killer);
    }
    #endregion
}
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBattleEntity : MonoBehaviour, IBattleEntity
{
    #region Serialized Fields
    [TabGroup("Battle")]
    [Header("Battle System")]
    [Required]
    [SerializeField] private BattleStatComponent _battleStat;

    [TabGroup("Combat")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _invulnerabilityDuration = 1f;

    [TabGroup("Combat")]
    [SerializeField] private bool _hasContactDamage = true;
    #endregion

    #region IBattleEntity Implementation
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;
    public bool IsAlive => _battleStat != null && _battleStat.IsAlive;
    public int TeamId => (int)_battleStat.GetCurrentStat(BattleStatType.TeamId);

    public float TakeDamage(IBattleEntity attacker, float damage)
    {
        if (_battleStat == null || _isInvulnerable) return 0f;

        float actualDamage = _battleStat.ApplyDamage(damage, attacker);

        if (actualDamage > 0f)
        {
            TriggerInvulnerability();
        }

        return actualDamage;
    }

    public float DealDamage(IBattleEntity target, float baseDamage)
    {
        return BattleInteractionSystem.ProcessDamageInteraction(this, target, baseDamage);
    }

    public void OnDeath(IBattleEntity killer = null)
    {
        Debug.Log($"{gameObject.name} has died.", this);
        OnCharacterDeath?.Invoke(killer);
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

    #region Events
    /// <summary>
    /// 캐릭터가 죽었을 때 발생하는 이벤트
    /// </summary>
    public event Action<IBattleEntity> OnCharacterDeath;

    /// <summary>
    /// 캐릭터가 데미지를 받았을 때 발생하는 이벤트
    /// </summary>
    public event Action<float, IBattleEntity> OnCharacterDamaged;
    #endregion

    #region Private Fields
    private bool _isInvulnerable = false;
    private float _invulnerabilityTimeRemaining = 0f;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_battleStat == null)
        {
            _battleStat = GetComponent<BattleStatComponent>();
        }

        if (_battleStat == null)
        {
            Debug.LogError("[PlayerBattleEntity] BattleStatComponent required!", this);
        }
    }

    private void Start()
    {
        SubscribeToBattleStatEvents();
    }

    private void Update()
    {
        UpdateInvulnerability();
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessContactDamage(other);
    }

    private void OnDestroy()
    {
        UnsubscribeFromBattleStatEvents();
    }

    #endregion

    #region Public Methods
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
    public float AttackTarget(IBattleEntity target)
    {
        if (target == null || !IsAlive)
            return 0f;
        float attackStat = GetCurrentStat(BattleStatType.Attack);
        return DealDamage(target, attackStat);
    }
    #endregion

    #region Private Methods
    private void SubscribeToBattleStatEvents()
    {
        if (_battleStat == null) return;

        _battleStat.OnDamageTaken += OnBattleStatDamageTaken;
        _battleStat.OnDeath += OnBattleStatDeath;
    }

    private void UnsubscribeFromBattleStatEvents()
    {
        if (_battleStat == null) return;

        _battleStat.OnDamageTaken -= OnBattleStatDamageTaken;
        _battleStat.OnDeath -= OnBattleStatDeath;
    }

    private void OnBattleStatDamageTaken(float damage, IBattleEntity attacker)
    {
        OnCharacterDamaged?.Invoke(damage, attacker);
    }

    private void OnBattleStatDeath(IBattleEntity killer)
    {
        OnDeath(killer);
    }

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

    private void ProcessContactDamage(Collider other)
    {
        if(!_hasContactDamage || _isInvulnerable)
        {
            return;
        }

        IBattleEntity otherEntity = other.GetComponent<IBattleEntity>();
        if (otherEntity == null) return;

        if (BattleInteractionSystem.IsSameTeam(this, otherEntity)) return;

        if (otherEntity is ProjectileBase projectile)
        {
            // 투사체는 자체적으로 데미지 처리함
            return;
        }

        
        if(_hasContactDamage == false)
            return;
        // 접촉 데미지 처리 (적 엔티티와 직접 접촉)
        float contactDamage = otherEntity.GetCurrentStat(BattleStatType.Attack);
        if (contactDamage > 0f)
        {
            otherEntity.TakeDamage(this,contactDamage);
        }
    }
    #endregion
}
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
///  Projectile Base, 이펙트 부착이 가능하며
/// 생명 주기는 스스로 관리. 매니저를 통해 생명주기의 완료 프로세스를 위탁
/// </summary>
public class ProjectileBase : MonoBehaviour, IBattleEntity
{
    #region Serialized Fields
    [TabGroup("Movement")]
    [Header("Movement Settings")]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _forwardSpeedUnitsPerSecond = 10f;

    [TabGroup("Lifetime")]
    [Header("Lifetime Settings")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _lifetimeSeconds = 5f;

    [TabGroup("Components")]
    [Header("Attack Trigger")]
    [Required]
    [SerializeField] protected Collider _attackTrigger;

    [TabGroup("Components")]
    [Header("RigidBody")]
    [Required]
    [SerializeField] protected Rigidbody _rigid;

    [TabGroup("Battle")]
    [Header("Battle System")]
    [Required]
    [SerializeField] private BattleStatComponent _battleStat;

    [TabGroup("Battle")]
    [SerializeField] private int _teamId = 0;

    [TabGroup("Battle")]
    [SuffixLabel("damage")]
    [SerializeField] private float _baseDamage = 10f;
    #endregion

    #region IBattleEntity Implementation
    public Transform Transform => transform;
    public GameObject GameObject => gameObject;
    public bool IsAlive => gameObject.activeInHierarchy && _battleStat != null && _battleStat.IsAlive;
    public int TeamId => _teamId;

    public float TakeDamage(IBattleEntity attacker, float damage)
    {
        //투사체는 외부로부터 데미지를 받지 않음.
        return 0.0f;
    }
     
    public float DealDamage(IBattleEntity target, float baseDamage)
    {
        float actualDamage = BattleInteractionSystem.ProcessDamageInteraction(this, target, baseDamage);    
        _battleStat.ApplyDamage(1.0f);//투사체는 공격시도마다 체력 1 감소 (관통 방지)

        return actualDamage;
    }

    public void OnDeath(IBattleEntity killer = null)
    {
        Debug.Log("[ProjectileBase] Projectile has been destroyed.", this); 
        DestroyProjectile();
    }

    public float GetStat(BattleStatType statType)
    {
        return _battleStat != null ? _battleStat.GetStat(statType) : 0f;
    }

    public void SetStat(BattleStatType statType, float value)
    {
        _battleStat?.SetStat(statType, value);
    }

    public void ModifyStat(BattleStatType statType, float delta)
    {
        _battleStat?.ModifyStat(statType, delta);
    }
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Collider AttackTrigger => _attackTrigger;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float ForwardSpeed => _forwardSpeedUnitsPerSecond;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float RemainingLifetime => _remainingLifetime;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsActive => gameObject.activeInHierarchy;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float BaseDamage => _baseDamage;
    #endregion

    #region Private Fields
    private float _remainingLifetime;

    [SerializeField]
    private List<IProjectileEffect> _effects = new List<IProjectileEffect>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeBattleStatEvents();
    }
    private void Update()
    {
        UpdateLifetime();
        GoForward();
        ProcessEffectsOnUpdate();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{gameObject.name} hit {other.gameObject.name}");

        ProcessEffectsOnHit(other);
        ProcessBattleInteraction(other);
    }

    private void OnEnable()
    {
        _remainingLifetime = _lifetimeSeconds;
        OnProjectileActivated();
        InitializeBattleStatEvents();
    }

    private void OnDisable()
    {
        UnsubscribeBattleStatEvents();
        OnProjectileDeactivated();
    }

    private void OnDestroy()
    {
        UnsubscribeBattleStatEvents();
    }

    #endregion

    #region Public Methods
    public void Initialize(float lifetimeSeconds = -1f, float forwardSpeed = -1f)
    {
        if (lifetimeSeconds > 0f)
        {
            _lifetimeSeconds = lifetimeSeconds;
        }

        if (forwardSpeed > 0f)
        {
            _forwardSpeedUnitsPerSecond = forwardSpeed;
        }

        _remainingLifetime = _lifetimeSeconds;
    }

    public void SetTeamId(int teamId)
    {
        _teamId = teamId;
    }

    public void SetBaseDamage(float damage)
    {
        _baseDamage = Mathf.Max(0f, damage);
    }

    public void AddEffect(IProjectileEffect effect)
    {
        if (effect == null) return;

        bool shouldAdd = effect.OnAttachedProjectile(this, _effects);

        if (shouldAdd)
        {
            _effects.Add(effect);
            SortEffectsByPriority();
        }
    }

    public void RemoveEffect(IProjectileEffect effect)
    {
        if (effect != null)
        {
            _effects.Remove(effect);
        }
    }

    public void DestroyProjectile()
    {
        ProcessEffectsOnDestroy();

        if (ProjectileManager.Instance != null)
        {
            ProjectileManager.Instance.ReturnProjectile(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region Protected Virtual Methods
    protected virtual void GoForward()
    {
        transform.Translate(Vector3.forward * _forwardSpeedUnitsPerSecond * Time.deltaTime);
    }

    protected virtual void OnProjectileActivated()
    {
        // 자식 클래스에서 오버라이드
    }

    protected virtual void OnProjectileDeactivated()
    {
        // 자식 클래스에서 오버라이드
    }
    #endregion

    #region Private Methods
    private void UpdateLifetime()
    {
        _remainingLifetime -= Time.deltaTime;

        if (_remainingLifetime <= 0f)
        {
            DestroyProjectile();
        }
    }

    private void ProcessEffectsOnHit(Collider target)
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            _effects[i].OnHit(this, target);
        }
    }

    private void ProcessEffectsOnDestroy()
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            _effects[i].OnDestroy(this);
        }

        _effects.Clear();
    }

    private void ProcessEffectsOnUpdate()
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            _effects[i].OnUpdate(this);
        }
    }

    private void SortEffectsByPriority()
    {
        _effects = _effects.OrderBy(effect => effect.Priority).ToList();
    }

    private void ProcessBattleInteraction(Collider other)
    {
        IBattleEntity targetEntity = other.GetComponent<IBattleEntity>();
        if (targetEntity != null)
        {
            Debug.Log($"{gameObject.name} ({TeamId}) attacks {other.gameObject.name} ({targetEntity.TeamId}) for {_baseDamage} damage.");
            DealDamage(targetEntity, _baseDamage);
        }
    }
    #endregion

    #region Privae Binding BattleCompoenet Events
    private void InitializeBattleStatEvents()
    {
        if (_battleStat == null) return;

        _battleStat.OnDeath -= OnDeath;
        _battleStat.OnDeath += OnDeath;
    }

    private void UnsubscribeBattleStatEvents()
    {
        if (_battleStat == null) return;

        _battleStat.OnDeath -= OnDeath;
    }
    #endregion 
}
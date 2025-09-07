using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 투사체 소멸 모드
/// </summary>
public enum ProjectileDestroyMode
{
    Deactivate,  // 비활성화 (풀링용)
    Destroy      // 실제 파괴
}

/// <summary>
///  Projectile Base, 이펙트 부착이 가능하며
/// 생명 주기는 스스로 관리. 매니저를 통해 생명주기의 완료 프로세스를 위탁
/// </summary>
public class ProjectileBase : MonoBehaviour, IBattleEntity, IProjectile
{
    #region Serialized Fields
    [TabGroup("Settings")]
    [Header("Destroy Settings")]
    [SerializeField] private ProjectileDestroyMode _destroyMode = ProjectileDestroyMode.Deactivate;

    [TabGroup("Projectile")]
    [Header("Projectile Type")]
    [SerializeField] private ProjectileType _projectileType = ProjectileType.BasicProjectile;

    [TabGroup("Movement")]
    [Header("Movement Settings")]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _forwardSpeedUnitsPerSecond = 10f;

    [TabGroup("Lifetime")]
    [Header("Lifetime Settings")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _lifetimeSeconds = 5f;

    [TabGroup("Projectile")]
    [Header("Projectile Stats")]
    [SerializeField] private int _basePierceCount = 0;
    [TabGroup("Projectile")]
    [SerializeField] private float _baseDamageMultiplier = 1f;

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

    #region Events - IProjectile
    /// <summary>
    /// 투사체가 활성화될 때 발생하는 이벤트
    /// </summary>
    public event System.Action<IProjectile> OnProjectileActivated;

    /// <summary>
    /// 투사체가 충돌했을 때 발생하는 이벤트
    /// </summary>
    public event System.Action<IProjectile, Collider> OnProjectileHit;

    /// <summary>
    /// 투사체가 소멸될 때 발생하는 이벤트
    /// </summary>
    public event System.Action<IProjectile> OnProjectileDestroyed;

    /// <summary>
    /// 투사체 업데이트 시 발생하는 이벤트 (매 프레임)
    /// </summary>
    public event System.Action<IProjectile> OnProjectileUpdate;
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
        // 투사체 데미지 배율 적용
        float finalDamage = baseDamage * _currentDamageMultiplier;
        float actualDamage = BattleInteractionSystem.ProcessDamageInteraction(this, target, finalDamage);
        return actualDamage;
    }

    public void OnDeath(IBattleEntity killer = null)
    {
        Debug.Log("[ProjectileBase] Projectile has been destroyed.", this);
        HandleProjectileDeath(); // 통합 함수 호출
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

    #region IProjectile Implementation
    public ProjectileType ProjectileType => _projectileType;
    public bool IsActive => gameObject.activeInHierarchy;
    public float RemainingLifetime => _remainingLifetime;
    public float ForwardSpeed => _forwardSpeedUnitsPerSecond;
    public int PierceCount => _currentPierceCount;
    public float DamageMultiplier => _currentDamageMultiplier;

    public void SetPierceCount(int pierceCount)
    {
        _currentPierceCount = Mathf.Max(0, pierceCount);
        SyncPierceCountToBattleStat();
    }

    public void ModifyPierceCount(int delta)
    {
        SetPierceCount(_currentPierceCount + delta);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        _currentDamageMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ModifyDamageMultiplier(float multiplier)
    {
        SetDamageMultiplier(_currentDamageMultiplier * multiplier);
    }
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public ProjectileDestroyMode CurrentDestroyMode => _destroyMode;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Collider AttackTrigger => _attackTrigger;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentPierceCount => _currentPierceCount;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentDamageMultiplier => _currentDamageMultiplier;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentAttackStat => GetCurrentStat(BattleStatType.Attack);
    #endregion

    #region Private Fields
    private float _remainingLifetime;
    private int _currentPierceCount;
    private float _currentDamageMultiplier;
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
        OnProjectileUpdate?.Invoke(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"{gameObject.name} hit {other.gameObject.name}");
        OnProjectileHit?.Invoke(this, other);
        ProcessBattleInteraction(other);
    }

    private void OnEnable()
    {
        _remainingLifetime = _lifetimeSeconds;
        InitializeProjectileStats();
        OnProjectileActivated?.Invoke(this);
        InitializeBattleStatEvents();
    }

    private void OnDisable()
    {
        UnsubscribeBattleStatEvents();
        // OnProjectileDestroyed 이벤트 호출 제거 (HandleProjectileDeath에서 처리)
    }

    private void OnDestroy()
    {
        ClearAllEvents();
        UnsubscribeBattleStatEvents();
        // OnProjectileDestroyed 이벤트 호출 제거 (HandleProjectileDeath에서 처리)
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
        InitializeProjectileStats();
    }

    public void SetTeamId(int teamId)
    {
        _teamId = teamId;
    }

    public void SetBaseDamage(float damage)
    {
        _baseDamage = Mathf.Max(0f, damage);
    }

    public void DestroyProjectile()
    {
        switch (_destroyMode)
        {
            case ProjectileDestroyMode.Deactivate:
                // 비활성화 (풀링용) - 외부에서 이벤트 구독으로 풀 반환 처리
                gameObject.SetActive(false);
                break;

            case ProjectileDestroyMode.Destroy:
                // 실제 파괴
                Destroy(gameObject);
                break;
        }
    }

    /// <summary>
    /// 투사체 소멸 모드 설정
    /// </summary>
    /// <param name="destroyMode">소멸 모드</param>
    public void SetDestroyMode(ProjectileDestroyMode destroyMode)
    {
        _destroyMode = destroyMode;
    }
    #endregion

    #region Protected Virtual Methods
    protected virtual void GoForward()
    {
        transform.Translate(Vector3.forward * _forwardSpeedUnitsPerSecond * Time.deltaTime);
    }
    #endregion

    #region Private Methods
    private void InitializeProjectileStats()
    {
        _currentPierceCount = _basePierceCount;
        _currentDamageMultiplier = _baseDamageMultiplier;
        SyncPierceCountToBattleStat();
    }

    private void SyncPierceCountToBattleStat()
    {
        // 관통 횟수를 BattleStat의 Health에 직접 연동 (관통력 = 체력)
        if (_battleStat != null)
        {
            _battleStat.SetCurrentStat(BattleStatType.Health, _currentPierceCount + 1); // +1: 최소 1번 충돌 가능
        }
    }

    private void UpdateLifetime()
    {
        _remainingLifetime -= Time.deltaTime;

        if (_remainingLifetime <= 0f)
        {
            HandleProjectileDeath(); // 통합 소멸 함수 호출
        }
    }
    private void ProcessBattleInteraction(Collider other)
    {
        IBattleEntity targetEntity = other.GetComponent<IBattleEntity>();
        if (targetEntity != null)
        {
            float damage = DealDamage(targetEntity, _baseDamage);
            _battleStat.ApplyDamage(1.0f);//투사체는 공격시도마다 체력 1 감소 (관통 방지)
            //Debug.Log($"{gameObject.name} ({TeamId}) attacks {other.gameObject.name} ({targetEntity.TeamId}) for {_baseDamage} base damage.");
        }
    }
    private void ClearAllEvents()
    {
        OnProjectileActivated = null;
        OnProjectileHit = null;
        OnProjectileDestroyed = null;
        OnProjectileUpdate = null;
    }

    /// <summary>
    /// 투사체 소멸 처리 (모든 소멸 경로의 통합점)
    /// </summary>
    private void HandleProjectileDeath()
    {
        if (!gameObject.activeInHierarchy) return; // 중복 호출 방지

        // 1. IProjectile 소멸 이벤트 호출
        OnProjectileDestroyed?.Invoke(this);

        // 2. DestroyProjectile 실행
        DestroyProjectile();
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
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Rendering.CameraUI;

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
public class ProjectileBase : BaseBattleEntity, IProjectile
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

    [TabGroup("Projectile")]
    [SerializeField] private float _baseSpeedMultiplier = 1f;

    [TabGroup("Projectile")]
    [SerializeField] private int _baseSplitAvailableCount = 0;

    [TabGroup("Projectile")]
    [SerializeField] private int _baseSplitProjectileCount = 3;

    [TabGroup("Projectile")]
    [SerializeField] private float _baseSplitAngleRange = 120f;

    [TabGroup("Components")]
    [Header("Attack Trigger")]
    [Required]
    [SerializeField] protected Collider _attackTrigger;

    [TabGroup("Components")]
    [Header("RigidBody")]
    [Required]
    [SerializeField] protected Rigidbody _rigid;
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
    /// 투사체가 충돌했을 때 발생하는 이벤트
    /// </summary>
    public event System.Action<IProjectile, Collider> AfterProjectileHit;

    /// <summary>
    /// 투사체가 소멸되기 전에 발생하는 이벤트
    /// </summary>
    public event System.Action<IProjectile> BeforeProjectileDestroyed;

    /// <summary>
    /// 투사체가 소멸될 때 발생하는 이벤트
    /// </summary>
    public event System.Action<IProjectile> OnProjectileDestroyed;

    /// <summary>
    /// 투사체 업데이트 시 발생하는 이벤트 (매 프레임)
    /// </summary>
    public event System.Action<IProjectile> OnProjectileUpdate;
    #endregion

    #region BaseBattleEntity Overrides
    public override float TakeDamage(IBattleEntity attacker, float damage)
    {
        //투사체는 외부로부터 데미지를 받지 않음.
        return 0.0f;
    }

    protected override void OnValidTriggered(IBattleEntity target, float actualDamage)
    {
        if (actualDamage > 0f)
        {
            _battleStat.ApplyDamage(1.0f);
        }
    }

    protected override float CalculateFinalDamage(IBattleEntity target)
    {
        return GetCurrentStat(BattleStatType.Attack) * _currentDamageMultiplier;
    }

    public override void OnDeath(IBattleEntity killer = null)
    {
        //Debug.Log("[ProjectileBase] Projectile has been destroyed.", this);
        HandleProjectileDeath(); // 통합 함수 호출
    }
    #endregion

    #region IProjectile Implementation
    public ProjectileType ProjectileType => _projectileType;
    public ProjectileLauncher Owner => _owner;

    public bool IsActive => gameObject.activeInHierarchy;
    public float RemainingLifetime => _remainingLifetime;
    public float ForwardSpeed => _forwardSpeedUnitsPerSecond;
    public int PierceCount => _currentPierceCount;
    public float DamageMultiplier => _currentDamageMultiplier;

    public int SplitCount => _currentSplitCount;
    public int SplitAvailableCount => _currentSplitAvailableCount;
    public int SplitProjectileCount => _currentSplitProjectileCount;
    public float SplitAngleRange => _currentSplitAngleRange;
    public void SetOwner(ProjectileLauncher owner)
    {
        _owner = owner;
    }
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
    public void SetSplitCount(int splitCount)
    {
        _currentSplitAvailableCount = Mathf.Max(0, splitCount);
    }

    public void ModifySplitCount(int delta)
    {
        SetSplitCount(_currentSplitAvailableCount + delta);
    }

    public void SetSplitAvailableCount(int availableCount)
    {
        _currentSplitAvailableCount = Mathf.Max(0, availableCount);
    }

    public void ModifySplitAvailableCount(int delta)
    {
        SetSplitAvailableCount(_currentSplitAvailableCount + delta);
    }

    public void SetSplitProjectileCount(int projectileCount)
    {
        _currentSplitProjectileCount = Mathf.Max(0, projectileCount);
    }

    public void ModifySplitProjectileCount(int delta)
    {
        SetSplitProjectileCount(_currentSplitProjectileCount + delta);
    }

    public void SetSplitAngleRange(float angleRange)
    {
        _currentSplitAngleRange = Mathf.Clamp(angleRange, 0f, 360f);
    }

    public void ModifySplitAngleRange(float delta)
    {
        SetSplitAngleRange(_currentSplitAngleRange + delta);
    }

    public IProjectile CreateClone(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (_owner == null) return null;

        IProjectile clone = _owner.CreateProjectile(_projectileType, worldPosition, worldRotation, true);
        if (clone != null)
        {
            clone.Initialize(_remainingLifetime, _forwardSpeedUnitsPerSecond);
            clone.SetDamageMultiplier(_currentDamageMultiplier);
            clone.SetPierceCount(_currentPierceCount);
            clone.SetSplitAvailableCount(_currentSplitAvailableCount);
            clone.SetSplitProjectileCount(_currentSplitProjectileCount);
            clone.SetSplitAngleRange(_currentSplitAngleRange);
        }
        return clone;
    }
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public ProjectileDestroyMode CurrentDestroyMode => _destroyMode;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float RemainingLifeTime => _remainingLifetime;

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
    public int CurrentSplitAvailableCount => _currentSplitAvailableCount;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentSplitProjectileCount => _currentSplitProjectileCount;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentSplitAngleRange => _currentSplitAngleRange;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CurrentAttackStat => GetCurrentStat(BattleStatType.Attack);

    #endregion

    #region Private Fields
    private float _remainingLifetime;
    private int _currentPierceCount;
    private float _currentDamageMultiplier;
    private int _currentSplitCount;
    private int _currentSplitAvailableCount;
    private int _currentSplitProjectileCount;
    private float _currentSplitAngleRange;
    private ProjectileLauncher _owner;
    #endregion

    #region Unity Lifecycle
    protected override void Start()
    {
        base.Start();
    }

    private void Update()
    {
        base.Update();

        UpdateLifetime();
        GoForward();
        OnProjectileUpdate?.Invoke(this);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        OnProjectileHit?.Invoke(this, other);
        base.OnTriggerEnter(other);
        AfterProjectileHit?.Invoke(this, other);
    }

    private void OnEnable()
    {
        _remainingLifetime = _lifetimeSeconds;
        InitializeProjectileStats();
        OnProjectileActivated?.Invoke(this);
        Debug.Log($"[ProjectileBase] Projectile Activated. Lifetime: {_lifetimeSeconds}s, Speed: {_forwardSpeedUnitsPerSecond} units/sec", this);
    }

    private void OnDisable()
    {
        // OnProjectileDestroyed 이벤트 호출 제거 (HandleProjectileDeath에서 처리)
    }

    protected override void OnDestroy()
    {
        ClearAllEvents();
        base.OnDestroy();
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
    public void DestroyProjectile()
    {
        ProcessSplit();

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

        // IProjectile 소멸 이벤트 호출
        OnProjectileDestroyed?.Invoke(this);
    }

    public void ProcessSplit()
    {
        Debug.Log($"[ProjectileBase] Attempting to split. Available Splits: {_currentSplitAvailableCount}, Projectiles per Split: {_currentSplitProjectileCount}", this);

        if (_currentSplitAvailableCount <= 0 || _owner == null || _currentSplitProjectileCount <= 0) return;

        _currentSplitAvailableCount--;

        Vector3 originalPosition = transform.position;
        Vector3 originalDirection = transform.forward;

        float angleStep = _currentSplitAngleRange / (_currentSplitProjectileCount - 1);
        float startAngle = -_currentSplitAngleRange * 0.5f;

        for (int i = 0; i < _currentSplitProjectileCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Quaternion yawRotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
            Vector3 splitDirection = yawRotation * originalDirection;
            Quaternion splitRotation = Quaternion.LookRotation(splitDirection);

            CreateClone(originalPosition, splitRotation);
        }

        Debug.Log($"[ProjectileBase] Split into {_currentSplitProjectileCount} cloned projectiles", this);
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
        float actualSpeed = _forwardSpeedUnitsPerSecond;
        transform.Translate(Vector3.forward * actualSpeed * Time.deltaTime);
    }
    #endregion

    #region Private Methods
    private void InitializeProjectileStats()
    {
        _currentPierceCount = _basePierceCount;
        _currentDamageMultiplier = _baseDamageMultiplier;
        _currentSplitAvailableCount = _baseSplitAvailableCount;
        _currentSplitProjectileCount = _baseSplitProjectileCount;
        _currentSplitAngleRange = _baseSplitAngleRange;
        // BattleStat 완전 초기화
        if (_battleStat != null)
        {
            _battleStat.InitializeStats(); // BattleStatData로부터 완전 리셋
            //Debug.Log($"[ProjectileBase] Initialized BattleStat for Projectile.", this);
            ////현재 스탯 출력
            //Debug.Log($"[ProjectileBase] Current Attack Stat: {GetCurrentStat(BattleStatType.Attack)}", this);
            //Debug.Log($"[ProjectileBase] Current Health Stat: {GetCurrentStat(BattleStatType.Health)}", this);

        }
        SyncPierceCountToBattleStat();
    }

    private void SyncPierceCountToBattleStat()
    {
        // 관통 횟수를 BattleStat의 Health에 직접 연동 (관통력 = 체력)
        if (_battleStat != null)
        {
            _battleStat.SetCurrentStat(BattleStatType.Health, _currentPierceCount + 1); // +1: 최소 1번 충돌 가능
            Debug.Log($"[ProjectileBase] Synced PierceCount {_currentPierceCount} to BattleStat Health {_battleStat.GetCurrentStat(BattleStatType.Health)}", this);
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

        // 1. 소멸 전 이벤트 호출 (분열 처리용)
        BeforeProjectileDestroyed?.Invoke(this);

        // 2. 실제 소멸 처리
        DestroyProjectile();
    }
    #endregion
}
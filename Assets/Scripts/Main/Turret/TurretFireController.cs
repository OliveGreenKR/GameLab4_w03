using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 터렛 발사 제어 및 ProjectileLauncher 스탯 프록시 컴포넌트
/// </summary>
public class TurretFireController : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Launcher Reference")]
    [Required]
    [SerializeField] private ProjectileLauncher _projectileLauncher;

    [TabGroup("Fire Rate Settings")]
    [Header("Fire Rate Constraints")]
    [SuffixLabel("shots/sec")]
    [SerializeField] private float _minFireRate = 0.1f;
    [SerializeField] private float _maxFireRate = 50f;
    [SerializeField] private float _defaultFireRate = 2f;

    [TabGroup("Damage Settings")]
    [Header("Damage Constraints")]
    [SuffixLabel("damage")]
    [SerializeField] private float _minBaseDamage = 1f;
    [SerializeField] private float _maxBaseDamage = 1000f;
    [SerializeField] private float _defaultBaseDamage = 25f;

    [TabGroup("Speed Settings")]
    [Header("Projectile Speed Constraints")]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _minProjectileSpeed = 1f;
    [SerializeField] private float _maxProjectileSpeed = 200f;
    [SerializeField] private float _defaultProjectileSpeed = 30f;

    [TabGroup("Lifetime Settings")]
    [Header("Lifetime Constraints")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _minLifetime = 0.1f;
    [SerializeField] private float _maxLifetime = 30f;
    [SerializeField] private float _defaultLifetime =3f;

    [TabGroup("Fire Control")]
    [Header("Fire Execution Settings")]
    [SuffixLabel("degrees")]
    [SerializeField] private float _aimingToleranceAngle = 2f;
    [InfoBox("Require Aiming ToleranceAngle")]
    [SerializeField] private bool _requireAimingAccuracy = true;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CachedFireRate { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CachedBaseDamage { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CachedProjectileSpeed { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CachedLifetime { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool CanFire => _projectileLauncher != null && _projectileLauncher.CanFire;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }
    #endregion

    #region Private Fields
    private bool _needsSync = false;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeComponent();
        ValidateReferences();
        ValidateConstraints();
        LoadInitialValues();

        IsInitialized = true;
        Debug.Log("[TurretFireController] Component initialized", this);
    }

    private void Update()
    {
        if (IsInitialized)
        {
            ProcessSyncIfNeeded();
        }
    }

    private void OnValidate()
    {
        // Fire Rate 제약 검증
        _minFireRate = Mathf.Max(0.01f, _minFireRate);
        _maxFireRate = Mathf.Max(_minFireRate, _maxFireRate);
        _defaultFireRate = Mathf.Clamp(_defaultFireRate, _minFireRate, _maxFireRate);

        // Damage 제약 검증
        _minBaseDamage = Mathf.Max(0.1f, _minBaseDamage);
        _maxBaseDamage = Mathf.Max(_minBaseDamage, _maxBaseDamage);
        _defaultBaseDamage = Mathf.Clamp(_defaultBaseDamage, _minBaseDamage, _maxBaseDamage);

        // Speed 제약 검증
        _minProjectileSpeed = Mathf.Max(0.1f, _minProjectileSpeed);
        _maxProjectileSpeed = Mathf.Max(_minProjectileSpeed, _maxProjectileSpeed);
        _defaultProjectileSpeed = Mathf.Clamp(_defaultProjectileSpeed, _minProjectileSpeed, _maxProjectileSpeed);

        // Lifetime 제약 검증
        _minLifetime = Mathf.Max(0.01f, _minLifetime);
        _maxLifetime = Mathf.Max(_minLifetime, _maxLifetime);
        _defaultLifetime = Mathf.Clamp(_defaultLifetime, _minLifetime, _maxLifetime);

        // Aiming tolerance 검증
        _aimingToleranceAngle = Mathf.Clamp(_aimingToleranceAngle, 0.1f, 45f);

        // 런타임 중이면 동기화 마크
        if (Application.isPlaying && IsInitialized)
        {
            MarkNeedsSync();
        }
    }

    private void OnDestroy()
    {
        IsInitialized = false;
    }
    #endregion

    #region Public Methods - Fire Control
    /// <summary>지정된 방향으로 발사 실행</summary>
    /// <param name="worldDirection">발사 방향</param>
    /// <returns>발사 성공 여부</returns>
    public bool ExecuteFire(Vector3 worldDirection)
    {
        if (!CanFireInDirection(worldDirection))
            return false;

        if (_projectileLauncher == null)
            return false;

        return _projectileLauncher.Fire(worldDirection);
    }

    /// <summary>특정 위치로 발사 실행</summary>
    /// <param name="worldTargetPosition">타겟 위치</param>
    /// <returns>발사 성공 여부</returns>
    public bool ExecuteFireAt(Vector3 worldTargetPosition)
    {
        if (_projectileLauncher == null)
            return false;

        Vector3 direction = (worldTargetPosition - transform.position).normalized;
        return ExecuteFire(direction);
    }

    /// <summary>발사 가능 여부 확인</summary>
    /// <param name="worldDirection">발사 방향</param>
    /// <returns>발사 가능 여부</returns>
    public bool CanFireInDirection(Vector3 worldDirection)
    {
        if (!IsInitialized || _projectileLauncher == null)
            return false;

        if (!_projectileLauncher.CanFire)
            return false;

        if (_requireAimingAccuracy)
        {
            Vector3 currentForward = transform.forward;
            float angle = Vector3.Angle(currentForward, worldDirection);

            if (angle > _aimingToleranceAngle)
                return false;
        }

        return true;
    }
    #endregion

    #region Public Methods - Fire Rate Control
    /// <summary>발사속도 설정</summary>
    /// <param name="fireRate">발사속도 (초당 발사 횟수)</param>
    public void SetFireRate(float fireRate)
    {
        float clampedRate = ClampFireRate(fireRate);
        CachedFireRate = clampedRate;
        MarkNeedsSync();

        Debug.Log($"[TurretFireController] Fire rate set to {CachedFireRate:F2} shots/sec", this);
    }

    /// <summary>발사속도 수정</summary>
    /// <param name="delta">변화량</param>
    public void ModifyFireRate(float delta)
    {
        SetFireRate(CachedFireRate + delta);
    }
    #endregion

    #region Public Methods - Damage Control
    /// <summary>기본 데미지 설정</summary>
    /// <param name="damage">기본 데미지</param>
    public void SetBaseDamage(float damage)
    {
        float clampedDamage = ClampBaseDamage(damage);
        CachedBaseDamage = clampedDamage;
        MarkNeedsSync();

        Debug.Log($"[TurretFireController] Base damage set to {CachedBaseDamage:F1}", this);
    }

    /// <summary>기본 데미지 수정</summary>
    /// <param name="delta">변화량</param>
    public void ModifyBaseDamage(float delta)
    {
        SetBaseDamage(CachedBaseDamage + delta);
    }
    #endregion

    #region Public Methods - Projectile Speed Control
    /// <summary>투사체 속도 설정</summary>
    /// <param name="speed">투사체 속도</param>
    public void SetProjectileSpeed(float speed)
    {
        float clampedSpeed = ClampProjectileSpeed(speed);
        CachedProjectileSpeed = clampedSpeed;
        MarkNeedsSync();

        Debug.Log($"[TurretFireController] Projectile speed set to {CachedProjectileSpeed:F1} units/sec", this);
    }

    /// <summary>투사체 속도 수정</summary>
    /// <param name="delta">변화량</param>
    public void ModifyProjectileSpeed(float delta)
    {
        SetProjectileSpeed(CachedProjectileSpeed + delta);
    }
    #endregion

    #region Public Methods - Lifetime Control
    /// <summary>투사체 생명시간 설정</summary>
    /// <param name="lifetime">생명시간 (초)</param>
    public void SetLifetime(float lifetime)
    {
        float clampedLifetime = ClampLifetime(lifetime);
        CachedLifetime = clampedLifetime;
        MarkNeedsSync();

        Debug.Log($"[TurretFireController] Projectile lifetime set to {CachedLifetime:F1} seconds", this);
    }

    /// <summary>투사체 생명시간 수정</summary>
    /// <param name="delta">변화량 (초)</param>
    public void ModifyLifetime(float delta)
    {
        SetLifetime(CachedLifetime + delta);
    }
    #endregion

    #region Private Methods - Launcher Synchronization
    private void SyncToLauncher()
    {
        if (_projectileLauncher == null)
            return;

        _projectileLauncher.SetFireRate(CachedFireRate);
        _projectileLauncher.SetProjectileSpeed(CachedProjectileSpeed);
        _projectileLauncher.SetProjectileLifetime(CachedLifetime);

        Debug.Log($"[TurretFireController] Synced to launcher - " +
                 $"FireRate: {CachedFireRate:F2}, Speed: {CachedProjectileSpeed:F1}, Lifetime: {CachedLifetime:F1}", this);
    }

    private void SyncFromLauncher()
    {
        if (_projectileLauncher == null)
            return;

        CachedFireRate = _projectileLauncher.GetFireRate();
        CachedProjectileSpeed = _projectileLauncher.GetProjectileSpeed();
        CachedLifetime = _projectileLauncher.GetProjectileLifetime();

        Debug.Log($"[TurretFireController] Synced from launcher - " +
                 $"FireRate: {CachedFireRate:F2}, Speed: {CachedProjectileSpeed:F1}, Lifetime: {CachedLifetime:F1}", this);
    }

    private void MarkNeedsSync()
    {
        _needsSync = true;
    }

    private void ProcessSyncIfNeeded()
    {
        if (_needsSync)
        {
            SyncToLauncher();
            _needsSync = false;
        }
    }
    #endregion

    #region Private Methods - Validation
    private float ClampFireRate(float fireRate)
    {
        return Mathf.Clamp(fireRate, _minFireRate, _maxFireRate);
    }

    private float ClampBaseDamage(float damage)
    {
        return Mathf.Clamp(damage, _minBaseDamage, _maxBaseDamage);
    }

    private float ClampProjectileSpeed(float speed)
    {
        return Mathf.Clamp(speed, _minProjectileSpeed, _maxProjectileSpeed);
    }

    private float ClampLifetime(float lifetime)
    {
        return Mathf.Clamp(lifetime, _minLifetime, _maxLifetime);
    }
    #endregion

    #region Private Methods - Initialization
    private void InitializeComponent()
    {
        IsInitialized = false;
        _needsSync = false;

        CachedFireRate = _defaultFireRate;
        CachedBaseDamage = _defaultBaseDamage;
        CachedProjectileSpeed = _defaultProjectileSpeed;
        CachedLifetime = _defaultLifetime;
    }

    private void ValidateReferences()
    {
        if (_projectileLauncher == null)
        {
            _projectileLauncher = GetComponent<ProjectileLauncher>();
            if (_projectileLauncher == null)
            {
                Debug.LogError("[TurretFireController] ProjectileLauncher component not found!", this);
            }
            else
            {
                Debug.LogWarning("[TurretFireController] ProjectileLauncher auto-assigned from same GameObject", this);
            }
        }
    }

    private void LoadInitialValues()
    {
        if (_projectileLauncher == null)
            return;

        // 캐시된 값들을 런처에 적용
        SyncToLauncher();

        Debug.Log($"[TurretFireController] Initial values loaded - " +
                 $"FireRate: {CachedFireRate:F2}, Damage: {CachedBaseDamage:F1}, " +
                 $"Speed: {CachedProjectileSpeed:F1}, Lifetime: {CachedLifetime:F1}", this);
    }

    private void ValidateConstraints()
    {
        bool hasInvalidConstraints = false;

        if (_minFireRate >= _maxFireRate)
        {
            Debug.LogError("[TurretFireController] Invalid fire rate constraints", this);
            hasInvalidConstraints = true;
        }

        if (_minBaseDamage >= _maxBaseDamage)
        {
            Debug.LogError("[TurretFireController] Invalid damage constraints", this);
            hasInvalidConstraints = true;
        }

        if (_minProjectileSpeed >= _maxProjectileSpeed)
        {
            Debug.LogError("[TurretFireController] Invalid speed constraints", this);
            hasInvalidConstraints = true;
        }

        if (_minLifetime >= _maxLifetime)
        {
            Debug.LogError("[TurretFireController] Invalid lifetime constraints", this);
            hasInvalidConstraints = true;
        }

        if (hasInvalidConstraints)
        {
            Debug.LogWarning("[TurretFireController] Some constraint validations failed", this);
        }
    }
    #endregion
}
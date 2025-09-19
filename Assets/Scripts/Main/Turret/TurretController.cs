using Sirenix.OdinInspector;
using System;
using UnityEngine;

/// <summary>
/// 터렛 상태 정의
/// </summary>
public enum TurretState
{
    Idle,           // 정지 상태
    Scanning,       // 주변 탐색 중
    Targeting,      // 타겟 추적 중  
    Firing,         // 발사 중
    Reloading       // 재장전 중
}

/// <summary>
/// 터렛 전체 상태와 하부 컴포넌트들을 조율하는 마스터 컨트롤러
/// </summary>
public class TurretController : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Component References")]
    [Required]
    [SerializeField] private TurretSectorSettings _sectorSettings;

    [TabGroup("Component References")]
    [Header("Core Components")]
    [Required]
    [SerializeField] private TurretRotationController _rotationController;

    [TabGroup("Component References")]
    [Required]
    [SerializeField] private ProjectileLauncher _projectileLauncher;

    [TabGroup("Component References")]
    [Header("Detection & Targeting")]
    [Required]
    [SerializeField] private TurretSectorDetection _sectorDetection;

    [TabGroup("Component References")]
    [Required]
    [SerializeField] private TurretTargeting _targeting;

    [TabGroup("Component References")]
    [Required]
    [SerializeField] private TurretFireController _fireController;

    [TabGroup("Settings")]
    [Header("Turret Behavior")]
    [SerializeField] private bool _autoStartScanning = true;

    [TabGroup("Settings")]
    [Header("Target Management")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _targetLostTimeout = 2f;

    [TabGroup("Settings")]
    [Header("Fire Control")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _reloadDuration = 1f;

    [TabGroup("Settings")]
    [SuffixLabel("degrees")]
    [SerializeField] private float _aimingCompleteTolerance = 2f;

    [TabGroup("Settings")]
    [Header("State Timing")]
    [SuffixLabel("seconds")]
    [SerializeField] private float _stateTransitionDelay = 0.1f;

    [TabGroup("Debug")]
    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogs = true;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public TurretState CurrentState { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public IBattleEntity CurrentTarget { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool HasValidTarget => CurrentTarget != null && CurrentTarget.IsAlive;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsOperational => _rotationController != null && _projectileLauncher != null;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float TimeInCurrentState { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float TargetLostTimer { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float ReloadTimer { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsTurretActive { get; private set; }
    #endregion

    #region Events
    /// <summary>터렛 상태 변경 이벤트</summary>
    public event Action<TurretState, TurretState> OnStateChanged;

    /// <summary>타겟 획득 이벤트</summary>
    public event Action<IBattleEntity> OnTargetAcquired;

    /// <summary>타겟 상실 이벤트</summary>
    public event Action<IBattleEntity> OnTargetLost;

    /// <summary>발사 이벤트</summary>
    public event Action<IBattleEntity> OnFired;

    /// <summary>터렛 활성화 상태 변경 이벤트</summary>
    public event Action<bool> OnTurretActiveChanged;
    #endregion

    #region Private Fields
    private float _stateTimer;
    private IBattleEntity _previousTarget;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
        CurrentState = TurretState.Idle;
        TimeInCurrentState = 0f;
        TargetLostTimer = 0f;
        ReloadTimer = 0f;
        IsTurretActive = false;
    }

    private void Start()
    {
        if (!ValidateComponents())
        {
            Debug.LogError("[TurretController] Component validation failed!", this);
            return;
        }

        SubscribeToComponentEvents();

        if (_autoStartScanning)
        {
            SetTurretActive(true);
        }

        LogDebug("Initialized successfully");
    }

    private void Update()
    {
        if (!IsTurretActive)
            return;

        UpdateTimers();
        UpdateCurrentState();
    }

    private void OnValidate()
    {
    }

    private void OnDestroy()
    {
        UnsubscribeFromComponentEvents();
    }
    #endregion

    #region Public Methods - State Control
    /// <summary>터렛 활성화/비활성화</summary>
    /// <param name="active">활성화 여부</param>
    public void SetTurretActive(bool active)
    {
        if (IsTurretActive == active)
            return;

        IsTurretActive = active;

        if (active)
        {
            StartScanning();
        }
        else
        {
            EmergencyStop();
        }

        OnTurretActiveChanged?.Invoke(active);
        LogDebug($"Turret {(active ? "activated" : "deactivated")}");
    }

    /// <summary>수동 타겟 지정</summary>
    /// <param name="target">지정할 타겟</param>
    public void SetManualTarget(IBattleEntity target)
    {
        if (!IsTurretActive)
        {
            LogDebug("Cannot set manual target: turret is inactive");
            return;
        }

        if (target == null)
        {
            LogDebug("Cannot set null manual target");
            return;
        }

        _targeting?.SetManualTarget(target);
        LogDebug($"Manual target set: {target.GameObject.name}");
    }

    /// <summary>수동 타겟 해제 및 스캔 모드 복귀</summary>
    public void ClearManualTarget()
    {
        if (!IsTurretActive)
            return;

        _targeting?.ClearManualTargeting();
        StartScanning();
        LogDebug("Manual target cleared, returning to scan mode");
    }

    /// <summary>즉시 발사 명령 (디버그용)</summary>
    public void FireImmediate()
    {
        if (!IsTurretActive || !HasValidTarget)
            return;

        if (CanFire())
        {
            ExecuteFiring();
            LogDebug("Immediate fire executed");
        }
    }

    /// <summary>터렛 완전 정지</summary>
    public void EmergencyStop()
    {
        _rotationController?.StopRotation();
        _targeting?.ClearTarget();
        ChangeState(TurretState.Idle);

        CurrentTarget = null;
        TargetLostTimer = 0f;
        ReloadTimer = 0f;

        LogDebug("Emergency stop executed");
    }
    #endregion

    #region Public Methods - Upgrade Interface
    /// <summary>발사 속도 업그레이드 적용</summary>
    /// <param name="newFireRate">새로운 발사 속도</param>
    public void UpgradeFireRate(float newFireRate)
    {
        if (_fireController == null)
            return;

        _fireController.SetFireRate(newFireRate);
        LogDebug($"Fire rate upgraded to {newFireRate:F2} shots/sec");
    }

    /// <summary>회전 속도 업그레이드 적용</summary>
    /// <param name="newRotationSpeed">새로운 회전 속도</param>
    public void UpgradeRotationSpeed(float newRotationSpeed)
    {
        if (_rotationController == null)
            return;

        _rotationController.SetRotationSpeed(newRotationSpeed);
        LogDebug($"Rotation speed upgraded to {newRotationSpeed:F1}°/sec");
    }

    /// <summary>탐지 범위 업그레이드 적용</summary>
    /// <param name="newDetectionRange">새로운 탐지 범위</param>
    public void UpgradeDetectionRange(float newDetectionRange)
    {
        if (_sectorDetection == null)
            return;

        // TurretSectorDetection은 TurretSectorSettings를 통해 업그레이드
        var settings = _sectorDetection.GetComponent<TurretSectorSettings>();
        if (settings != null)
        {
            float rangeIncrease = newDetectionRange - settings.DetectionRadius;
            if (rangeIncrease > 0f)
            {
                settings.UpgradeDetectionRange(rangeIncrease);
            }
        }

        LogDebug($"Detection range upgraded to {newDetectionRange:F1} units");
    }

    /// <summary>투사체 효과 추가</summary>
    /// <param name="effectAsset">추가할 효과</param>
    public void AddProjectileEffect(ProjectileEffectSO effectAsset)
    {
        if (_projectileLauncher == null || effectAsset == null)
            return;

        _projectileLauncher.AddEffect(effectAsset);
        LogDebug($"Added projectile effect: {effectAsset.name}");
    }

    /// <summary>투사체 효과 제거</summary>
    /// <param name="effectAsset">제거할 효과</param>
    public void RemoveProjectileEffect(ProjectileEffectSO effectAsset)
    {
        if (_projectileLauncher == null || effectAsset == null)
            return;

        _projectileLauncher.RemoveEffect(effectAsset);
        LogDebug($"Removed projectile effect: {effectAsset.name}");
    }
    #endregion

    #region Private Methods - State Management
    private void ChangeState(TurretState newState)
    {
        if (CurrentState == newState)
            return;

        TurretState oldState = CurrentState;
        CurrentState = newState;
        TimeInCurrentState = 0f;
        _stateTimer = 0f;

        OnStateChanged?.Invoke(oldState, newState);
        LogStateChange(oldState, newState);
    }

    private void UpdateCurrentState()
    {
        switch (CurrentState)
        {
            case TurretState.Idle:
                HandleIdleState();
                break;
            case TurretState.Scanning:
                HandleScanningState();
                break;
            case TurretState.Targeting:
                HandleTargetingState();
                break;
            case TurretState.Firing:
                HandleFiringState();
                break;
            case TurretState.Reloading:
                HandleReloadingState();
                break;
        }
    }

    private void HandleIdleState()
    {
        // Idle 상태에서는 아무것도 하지 않음
        // SetTurretActive(true) 호출시 스캔 모드로 전환됨
    }

    private void HandleScanningState()
    {
        // 타겟이 발견되면 타겟팅 모드로 전환
        if (HasValidTarget)
        {
            StartTargeting(CurrentTarget);
            return;
        }

        // 회전 컨트롤러가 스캔 중이 아니면 스캔 시작
        if (_rotationController != null && !_rotationController.IsRotating)
        {
            StartScanning();
        }
    }

    private void HandleTargetingState()
    {
        // 타겟이 유효하지 않으면 스캔 모드로 복귀
        if (!HasValidTarget)
        {
            TargetLostTimer += Time.deltaTime;
            if (TargetLostTimer >= _targetLostTimeout)
            {
                StartScanning();
                TargetLostTimer = 0f;
            }
            return;
        }

        TargetLostTimer = 0f;

        // 조준이 완료되면 발사 상태로 전환
        if (IsAimingComplete() && CanFire())
        {
            ChangeState(TurretState.Firing);
        }
    }

    private void HandleFiringState()
    {
        // 타겟이 유효하지 않으면 스캔 모드로 복귀
        if (!HasValidTarget)
        {
            StartScanning();
            return;
        }

        // 발사 실행
        if (CanFire())
        {
            ExecuteFiring();
            ChangeState(TurretState.Reloading);
        }
        else
        {
            // 발사할 수 없으면 타겟팅 상태로 복귀
            ChangeState(TurretState.Targeting);
        }
    }

    private void HandleReloadingState()
    {
        ReloadTimer += Time.deltaTime;

        if (ReloadTimer >= _reloadDuration)
        {
            ReloadTimer = 0f;

            // 타겟이 여전히 유효하면 타겟팅 상태로, 아니면 스캔 상태로
            if (HasValidTarget)
            {
                ChangeState(TurretState.Targeting);
            }
            else
            {
                StartScanning();
            }
        }
    }

    private void UpdateTimers()
    {
        TimeInCurrentState += Time.deltaTime;
        _stateTimer += Time.deltaTime;
    }
    #endregion

    #region Private Methods - Component Coordination
    private void InitializeComponents()
    {
        CurrentTarget = null;
        _previousTarget = null;
        TimeInCurrentState = 0f;
        _stateTimer = 0f;
        TargetLostTimer = 0f;
        ReloadTimer = 0f;
    }

    private void SubscribeToComponentEvents()
    {
        if (_targeting != null)
        {
            _targeting.OnTargetChanged -= OnTargetChanged;
            _targeting.OnTargetChanged += OnTargetChanged;
        }

        if (_rotationController != null)
        {
            _rotationController.OnRotationComplete -= OnRotationComplete;
            _rotationController.OnRotationComplete += OnRotationComplete;
        }

        if (_projectileLauncher != null)
        {
            _projectileLauncher.OnProjectileCreated -= OnProjectileFired;
            _projectileLauncher.OnProjectileCreated += OnProjectileFired;
        }
    }

    private void UnsubscribeFromComponentEvents()
    {
        if (_targeting != null)
        {
            _targeting.OnTargetChanged -= OnTargetChanged;
        }

        if (_rotationController != null)
        {
            _rotationController.OnRotationComplete -= OnRotationComplete;
        }

        if (_projectileLauncher != null)
        {
            _projectileLauncher.OnProjectileCreated -= OnProjectileFired;
        }
    }

    private void StartScanning()
    {
        if (_rotationController == null || _targeting == null || _sectorSettings == null)
            return;

        _targeting.StartAutoTargeting();
        _rotationController.StartContinuousRotation(_sectorSettings.EffectiveScanMin, _sectorSettings.EffectiveScanMax);
        ChangeState(TurretState.Scanning);
    }

    private void StartTargeting(IBattleEntity target)
    {
        if (_rotationController == null || target == null)
            return;

        Vector3 targetDirection = _targeting.GetDirectionToTarget();
        _rotationController.StopRotation();

        // 타겟 방향으로 회전 시작
        float targetAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
        _rotationController.RotateTo(targetAngle);

        ChangeState(TurretState.Targeting);
    }

    private void ExecuteFiring()
    {
        if (_fireController == null || !HasValidTarget)
            return;

        Vector3 fireDirection = _targeting.GetDirectionToTarget();

        if (_fireController.ExecuteFire(fireDirection))
        {
            OnFired?.Invoke(CurrentTarget);
            LogDebug($"Fired at target: {CurrentTarget.GameObject.name}");
        }
    }

    private bool CanFire()
    {
        if (_fireController == null || !HasValidTarget)
            return false;

        Vector3 fireDirection = _targeting.GetDirectionToTarget();
        return _fireController.CanFireInDirection(fireDirection);
    }

    private bool IsAimingComplete()
    {
        if (_rotationController == null || !HasValidTarget)
            return false;

        Vector3 targetDirection = _targeting.GetDirectionToTarget();
        Vector3 currentDirection = _rotationController.GetForwardDirection();

        float angleDifference = Vector3.Angle(currentDirection, targetDirection);
        return angleDifference <= _aimingCompleteTolerance;
    }
    #endregion

    #region Private Methods - Event Handlers
    private void OnTargetChanged(IBattleEntity previousTarget, IBattleEntity newTarget)
    {
        _previousTarget = CurrentTarget;
        CurrentTarget = newTarget;

        if (newTarget != null && previousTarget == null)
        {
            // 새 타겟 획득
            OnTargetAcquired?.Invoke(newTarget);

            if (CurrentState == TurretState.Scanning)
            {
                StartTargeting(newTarget);
            }

            LogDebug($"Target acquired: {newTarget.GameObject.name}");
        }
        else if (newTarget == null && previousTarget != null)
        {
            // 타겟 상실
            OnTargetLost?.Invoke(previousTarget);
            TargetLostTimer = 0f;

            LogDebug($"Target lost: {previousTarget.GameObject.name}");
        }
        else if (newTarget != null && previousTarget != null && newTarget != previousTarget)
        {
            // 타겟 교체
            LogDebug($"Target changed: {previousTarget.GameObject.name} → {newTarget.GameObject.name}");

            if (CurrentState == TurretState.Targeting || CurrentState == TurretState.Firing)
            {
                StartTargeting(newTarget);
            }
        }
    }

    private void OnRotationComplete()
    {
        if (CurrentState == TurretState.Targeting && HasValidTarget)
        {
            // 타겟팅 중 회전 완료 - 발사 시도
            if (CanFire())
            {
                ChangeState(TurretState.Firing);
            }
        }

        LogDebug("Rotation completed");
    }

    private void OnProjectileFired(IProjectile projectile)
    {
        if (projectile == null)
            return;

        LogDebug($"Projectile fired: {projectile.ProjectileType}");
    }
    #endregion

    #region Private Methods - Validation
    private bool ValidateComponents()
    {
        bool isValid = true;

        if (_rotationController == null)
        {
            Debug.LogError("[TurretController] TurretRotationController reference missing!", this);
            isValid = false;
        }

        if (_projectileLauncher == null)
        {
            Debug.LogError("[TurretController] ProjectileLauncher reference missing!", this);
            isValid = false;
        }

        if (_sectorDetection == null)
        {
            Debug.LogError("[TurretController] TurretSectorDetection reference missing!", this);
            isValid = false;
        }

        if (_targeting == null)
        {
            Debug.LogError("[TurretController] TurretTargeting reference missing!", this);
            isValid = false;
        }

        if (_fireController == null)
        {
            Debug.LogError("[TurretController] TurretFireController reference missing!", this);
            isValid = false;
        }

        if (_sectorSettings == null)
        {
            Debug.LogError("[TurretController] TurretSectorSettings reference missing!", this);
            isValid = false;
        }

        return isValid;
    }

    private void LogStateChange(TurretState oldState, TurretState newState)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[TurretController] State changed: {oldState} → {newState}", this);
        }
    }

    private void LogDebug(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[TurretController] {message}", this);
        }
    }
    #endregion
}
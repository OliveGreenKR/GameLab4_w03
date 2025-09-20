using Sirenix.OdinInspector;
using System;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    enum DampingMode
    {
        IndependentAxis,
        ExponentialDecay,
        UnifiedDirectionPreserving,
        CriticalDamping,
    }

    #region Serialized Fields
    [TabGroup("Target")]
    [Header("Follow Target")]
    [Required]
    [SerializeField] private Transform _targetTransform;

    [TabGroup("Target")]
    [Header("Tracking Options")]
    [SerializeField] private bool _enablePositionTracking = true;
    [TabGroup("Target")]
    [SerializeField] private bool _enableRotationTracking = true;

    [TabGroup("Offset", "Rotation")]
    [Header("Rotation Offset")]
    [SuffixLabel("degrees")]
    [SerializeField] private Vector3 _offsetRotationDegrees = Vector3.zero;

    [TabGroup("Offset", "Distance")]
    [Header("Distance Offset")]
    [InfoBox("Forward(Z), Right(X), Up(Y) relative to target rotation + offset rotation")]
    [SuffixLabel("units")]
    [SerializeField] private Vector3 _offsetDistance = new Vector3(0f, 2f, -5f);

    [TabGroup("Damping", "Common")]
    [Header("Critical Daming Options")]
    [SerializeField] private float VIBRATION_FILTER_THRESHOLD_UNITS = 0.001f;
    [TabGroup("Damping", "Common")]
    [SerializeField] private float VIBRATION_FILTER_THRESHOLD_DEGREES = 0.001f;
    [TabGroup("Damping", "Common")]
    [SerializeField] private float CRITICAL_DAMPING_COEFFICIENT = 2.0f;
    [TabGroup("Damping", "Common")]
    [SerializeField] private float HIGH_FREQUENCY_CUTOFF_RATE = 20.0f;

    [TabGroup("Damping", "Position")]
    [Header("Position Damping")]
    [SerializeField] private bool _enablePositionDamping = true;
    [TabGroup("Damping", "Position")]
    [SuffixLabel("units per second")]
    [SerializeField] private Vector3 _positionDampingSpeed = new Vector3(10f, 10f, 10f);

    [TabGroup("Damping", "Rotation")]
    [Header("Rotation Damping")]
    [SerializeField] private bool _enableRotationDamping = true;
    [TabGroup("Damping", "Rotation")]
    [SuffixLabel("degrees per second")]
    [SerializeField] private Vector3 _rotationDampingSpeed = new Vector3(90f, 90f, 90f);

    [TabGroup("Damping", "Mode")]
    [Header("Damping Mode")]
    [SerializeField] private DampingMode _positionDampMode = DampingMode.ExponentialDecay;
    [TabGroup("Damping", "Mode")]
    [SerializeField] private DampingMode _rotationDampMode = DampingMode.ExponentialDecay;

    [TabGroup("Threshold")]
    [Header("Tracking Thresholds")]
    [SuffixLabel("units")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float _positionThreshold = 0.001f;

    [TabGroup("Threshold")]
    [SuffixLabel("degrees")]
    [Range(0.0f, 1.0f)]
    [SerializeField] private float _rotationThreshold = 0.001f;

    [TabGroup("Settings")]
    [Header("Default Camera Settings")]
    [SerializeField] private CameraSettings _defaultSettings;

    [TabGroup("Settings")]
    [Header("Aim Camera Settings")]
    [SerializeField] private CameraSettings _aimCameraSettings;

    [TabGroup("Settings")]
    [Header("Current Camera Settings")]
    [SerializeField, ReadOnly] private CameraSettings _currentSettings;

    [TabGroup("Settings")]
    [Header("Target Camera Settings")]
    [SerializeField] private CameraSettings _targetSettings;

    [TabGroup("Settings")]
    [Header("Settings Transition Speed")]
    [SerializeField, Range(0.1f, 50f)] private float _settingsTransitionSpeed = 5f;

    [GUIColor("green")]
    [Button(ButtonSizes.Large)]
    [ButtonGroup("Apply Settings")]
    private void ApplyAimSettings() => ApplyCameraSettings(_aimCameraSettings, true, _settingsTransitionSpeed);

    [GUIColor("cyan")]
    [ButtonGroup("Apply Settings")]
    private void ApplyDefaultSettings() => ApplyCameraSettings(_defaultSettings, true, _settingsTransitionSpeed);

    [Button(ButtonSizes.Large)]
    [GUIColor(1, 0.5f, 0)]
    private void SaveCurrentSetting()
    {
        _currentSettings.PositionDampingSpeed = _positionDampingSpeed;
        _currentSettings.RotationDampingSpeed = _rotationDampingSpeed;
        _currentSettings.OffsetDistance = _offsetDistance;
        _currentSettings.OffsetRotationDegrees = _offsetRotationDegrees;
        _currentSettings.IsEnablePositionDamping = _enablePositionDamping;
        _currentSettings.IsEnableRotationDamping = _enableRotationDamping;
        if (_camera != null)
        {
            _currentSettings.FieldOfView = _camera.fieldOfView;
        }
        Debug.Log("[ThirdPersonCameraController] Current settings saved to _currentSettings.");
    }
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsPositionDampingEnabled => _enablePositionDamping;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsRotationDampingEnabled => _enableRotationDamping;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Transform TargetTransform => _targetTransform;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 TargetWorldPosition => _targetWorldPosition;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 TargetWorldRotationDegrees => _targetWorldRotationDegrees;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentWorldPosition => transform.position;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentWorldRotationDegrees => transform.rotation.eulerAngles;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsTransitioningSettings => _isTransitioningSettings;
    #endregion

    #region Private Fields
    private Vector3 _targetWorldPosition;
    private Vector3 _targetWorldRotationDegrees;

    // Settings Transition
    private Camera _camera;
    private bool _isTransitioningSettings = false;

    // Velocity Tracking for Critical Damping
    private Vector3 _currentPositionVelocity = Vector3.zero;
    private Vector3 _currentRotationVelocity = Vector3.zero;

    // Vibration Filtering
    private Vector3 _previousTargetPosition = Vector3.zero;
    private Quaternion _previousTargetRotation = Quaternion.identity;
    private Vector3 _filteredTargetPosition = Vector3.zero;
    private Quaternion _filteredTargetRotation = Quaternion.identity;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("[ThirdPersonCameraController] Camera component not found!");
        }

        if (_targetTransform != null)
        {
            CalculateTargetTransform();
        }
        else
        {
            Debug.LogError("[ThirdPersonCameraController] Target Transform not assigned!");
        }

        if (_defaultSettings != null)
        {
            ApplySettingsImmediately(_defaultSettings);
        }
    }

    private void Update()
    {

    }

    private void LateUpdate()
    {
        if (_targetTransform == null) return;

        CalculateTargetTransform();

        if (_isTransitioningSettings)
        {
            UpdateSettingsTransition();

        }
        if (_targetTransform == null) return;
        TrackToTarget();
    }

    private void OnDestroy()
    {
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 위치 댐핑 활성화/비활성화 설정
    /// </summary>
    /// <param name="enabled">위치 댐핑 활성화 여부</param>
    public void SetPositionDampingEnabled(bool enabled)
    {
        _enablePositionDamping = enabled;
    }

    /// <summary>
    /// 회전 댐핑 활성화/비활성화 설정
    /// </summary>
    /// <param name="enabled">회전 댐핑 활성화 여부</param>
    public void SetRotationDampingEnabled(bool enabled)
    {
        _enableRotationDamping = enabled;
    }

    /// <summary>
    /// 전체 댐핑 활성화/비활성화 설정
    /// </summary>
    /// <param name="enabled">댐핑 활성화 여부</param>
    public void SetDampingEnabled(bool enabled)
    {
        _enablePositionDamping = enabled;
        _enableRotationDamping = enabled;
    }

    /// <summary>
    /// 추적할 타겟 설정
    /// </summary>
    /// <param name="target">추적할 Transform</param>
    public void SetTarget(Transform target)
    {
        _targetTransform = target;
    }

    /// <summary>
    /// 회전 오프셋 설정
    /// </summary>
    /// <param name="rotationDegrees">회전 오프셋 (degrees)</param>
    public void SetOffsetRotation(Vector3 rotationDegrees)
    {
        _offsetRotationDegrees = rotationDegrees;
    }

    /// <summary>
    /// 거리 오프셋 설정
    /// </summary>
    /// <param name="distance">거리 오프셋 (Forward, Right, Up)</param>
    public void SetOffsetDistance(Vector3 distance)
    {
        _offsetDistance = distance;
    }

    /// <summary>
    /// 댐핑 속도 설정
    /// </summary>
    /// <param name="positionDamping">위치 댐핑 속도</param>
    /// <param name="rotationDamping">회전 댐핑 속도</param>
    public void SetDampingSpeed(Vector3 positionDamping, Vector3 rotationDamping)
    {
        _positionDampingSpeed = positionDamping;
        _rotationDampingSpeed = rotationDamping;
    }

    /// <summary>
    /// Camera Settings 적용
    /// </summary>
    /// <param name="settings"> 적용할 카메라 세팅 에셋</param>
    /// <param name="isLerp"> lerp transition 적용 여부</param>
    /// <param name="dampSpeed">lerp transition 속도</param>
    public void ApplyCameraSettings(CameraSettings settings, bool isLerp = false, float dampSpeed = 5f)
    {
        if (settings == null) return;

        if (isLerp)
        {
            _targetSettings = settings;
            _settingsTransitionSpeed = dampSpeed;
            _isTransitioningSettings = true;

            if (_currentSettings == null)
            {
                _currentSettings = _defaultSettings;
            }
        }
        else
        {
            ApplySettingsImmediately(settings);
        }
    }

    public void AimModeStart()
    {
        ApplyAimSettings();
    }

    public void AImModeEnd()
    {
        ApplyDefaultSettings();
    }

    #endregion

    #region Private Methods
    private void CalculateTargetTransform()
    {
        if (_targetTransform == null) return;

        // 타겟의 회전에 오프셋 회전 적용
        Quaternion targetRotation = _targetTransform.rotation;
        Quaternion offsetRotation = Quaternion.Euler(_offsetRotationDegrees);
        Quaternion finalRotation = targetRotation * offsetRotation;

        // 최종 회전에서 오프셋 거리 적용하여 위치 계산
        Vector3 offsetDirection = finalRotation * _offsetDistance;
        Vector3 finalPosition = _targetTransform.position + offsetDirection;


        //Debug.Log($"Target moved: {_targetWorldPosition:F6}");

        // 타겟 값 설정
        _targetWorldPosition = finalPosition;
        _targetWorldRotationDegrees = finalRotation.eulerAngles;
    }
    #endregion

    #region Damping and Tracking
    private void TrackToTarget()
    {
        if (_enablePositionTracking)
        {
            TrackPosition();
        }

        if (_enableRotationTracking)
        {
            TrackRotation();
        }
    }

    private void TrackPosition()
    {
        Vector3 currentPosition = transform.position;
        float positionDistance = Vector3.Distance(currentPosition, _targetWorldPosition);

        if (positionDistance > _positionThreshold)
        {
            if (!_enablePositionDamping)
            {
                transform.position = _targetWorldPosition;
                return;
            }

            Vector3 nextPosition = DampVector3(currentPosition, _targetWorldPosition, _positionDampMode);
            transform.position = nextPosition;
        }
        else
        {
            transform.position = _targetWorldPosition;
        }
    }

    private void TrackRotation()
    {
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(_targetWorldRotationDegrees);

        float angularDistance = Quaternion.Angle(currentRotation, targetRotation);

        if (angularDistance > _rotationThreshold)
        {
            if (!_enableRotationDamping)
            {
                transform.rotation = targetRotation;
                return;
            }

            Quaternion nextRotation = DampQuaternion(currentRotation, targetRotation, _rotationDampMode);
            transform.rotation = nextRotation;
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }

    private Vector3 DampVector3(Vector3 current, Vector3 target, DampingMode mode)
    {
        // Apply vibration filtering first
        Vector3 filteredTarget = ApplyVibrationFilter(target, _previousTargetPosition, _filteredTargetPosition);
        _previousTargetPosition = target;
        _filteredTargetPosition = filteredTarget;

        float distance = Vector3.Distance(current, filteredTarget);

        switch (mode)
        {
            case DampingMode.IndependentAxis:
                {
                    Vector3 dampingSpeed = new Vector3(
                        _positionDampingSpeed.x * Time.deltaTime,
                        _positionDampingSpeed.y * Time.deltaTime,
                        _positionDampingSpeed.z * Time.deltaTime
                    );
                    return new Vector3(
                        Mathf.Lerp(current.x, filteredTarget.x, dampingSpeed.x),
                        Mathf.Lerp(current.y, filteredTarget.y, dampingSpeed.y),
                        Mathf.Lerp(current.z, filteredTarget.z, dampingSpeed.z)
                    );
                }

            case DampingMode.UnifiedDirectionPreserving:
                {
                    Vector3 direction = (filteredTarget - current).normalized;
                    float dampingRate = _positionDampingSpeed.x;
                    float dampedDistance = distance * Mathf.Exp(-dampingRate * Time.deltaTime);
                    return current + direction * (distance - dampedDistance);
                }

            case DampingMode.ExponentialDecay:
                {
                    float dampingRate = _positionDampingSpeed.x;
                    return filteredTarget + (current - filteredTarget) * Mathf.Exp(-dampingRate * Time.deltaTime);
                }

            case DampingMode.CriticalDamping:
                {
                    return CriticalDampVector3(current, filteredTarget, ref _currentPositionVelocity, _positionDampingSpeed.x);
                }

            default:
                return current;
        }
    }
    private Quaternion DampQuaternion(Quaternion current, Quaternion target, DampingMode mode)
    {
        // Apply vibration filtering for rotation
        Quaternion filteredTarget = ApplyQuaternionVibrationFilter(target, _previousTargetRotation, _filteredTargetRotation);
        _previousTargetRotation = target;
        _filteredTargetRotation = filteredTarget;

        float angularDistance = Quaternion.Angle(current, filteredTarget);

        switch (mode)
        {
            case DampingMode.IndependentAxis:
                {
                    Vector3 dampingSpeed = new Vector3(
                        _rotationDampingSpeed.x * Time.deltaTime,
                        _rotationDampingSpeed.y * Time.deltaTime,
                        _rotationDampingSpeed.z * Time.deltaTime
                    );

                    Vector3 currentEuler = current.eulerAngles;
                    Vector3 targetEulerFiltered = filteredTarget.eulerAngles;

                    float deltaX = Mathf.DeltaAngle(currentEuler.x, targetEulerFiltered.x);
                    float deltaY = Mathf.DeltaAngle(currentEuler.y, targetEulerFiltered.y);
                    float deltaZ = Mathf.DeltaAngle(currentEuler.z, targetEulerFiltered.z);

                    Vector3 nextEuler = new Vector3(
                        currentEuler.x + deltaX * dampingSpeed.x,
                        currentEuler.y + deltaY * dampingSpeed.y,
                        currentEuler.z + deltaZ * dampingSpeed.z
                    );

                    return Quaternion.Euler(nextEuler);
                }

            case DampingMode.UnifiedDirectionPreserving:
                {
                    float dampingRate = _rotationDampingSpeed.x;
                    float dampedAngle = angularDistance * Mathf.Exp(-dampingRate * Time.deltaTime);
                    float progress = (angularDistance - dampedAngle) / angularDistance;
                    return Quaternion.Slerp(current, filteredTarget, progress);
                }

            case DampingMode.ExponentialDecay:
                {
                    float dampingRate = _rotationDampingSpeed.x;
                    float t = 1.0f - Mathf.Exp(-dampingRate * Time.deltaTime);
                    return Quaternion.Slerp(current, filteredTarget, t);
                }

            case DampingMode.CriticalDamping:
                {
                    return CriticalDampQuaternion(current, filteredTarget, ref _currentRotationVelocity, _rotationDampingSpeed.x);
                }

            default:
                return current;
        }
    }

    /// <summary>
    /// 진동 필터링을 통해 고주파 노이즈 제거
    /// </summary>
    /// <param name="newTarget">새로운 타겟 위치</param>
    /// <param name="previousTarget">이전 타겟 위치</param>
    /// <param name="filteredTarget">필터링된 타겟 위치</param>
    /// <returns>필터링된 타겟 위치</returns>
    private Vector3 ApplyVibrationFilter(Vector3 newTarget, Vector3 previousTarget, Vector3 filteredTarget)
    {
        float deltaDistance = Vector3.Distance(newTarget, previousTarget);

        // 임계값 이하의 작은 변화는 필터링
        if (deltaDistance < VIBRATION_FILTER_THRESHOLD_UNITS)
        {
            return filteredTarget;
        }

        // 고주파 컷오프 필터 적용
        float filterRate = HIGH_FREQUENCY_CUTOFF_RATE * Time.deltaTime;
        float t = 1.0f - Mathf.Exp(-filterRate);

        return Vector3.Lerp(filteredTarget, newTarget, t);
    }

    /// <summary>
    /// Spring-Mass-Damper 기반 Critical Damping 구현
    /// </summary>
    /// <param name="current">현재 위치</param>
    /// <param name="target">목표 위치</param>
    /// <param name="velocity">현재 속도 (ref)</param>
    /// <param name="dampingSpeed">댐핑 속도</param>
    /// <returns>댐핑된 위치</returns>
    private Vector3 CriticalDampVector3(Vector3 current, Vector3 target, ref Vector3 velocity, float dampingSpeed)
    {
        float deltaTime = Time.deltaTime;
        float omega = dampingSpeed; // Natural frequency
        float zeta = 1.0f; // Critical damping ratio

        Vector3 displacement = target - current;

        // Critical damping: x(t) = (A + Bt)e^(-ωt)
        float dampingFactor = Mathf.Exp(-omega * deltaTime);
        float impulseFactor = omega * deltaTime * dampingFactor;

        Vector3 newPosition = current + (displacement + velocity * deltaTime) * dampingFactor;
        velocity = (velocity - displacement * omega) * dampingFactor;

        return newPosition;
    }

    /// <summary>
    /// Quaternion용 진동 필터링 (순수 Quaternion space)
    /// </summary>
    /// <param name="newTarget">새로운 타겟 회전</param>
    /// <param name="previousTarget">이전 타겟 회전</param>
    /// <param name="filteredTarget">필터링된 타겟 회전</param>
    /// <returns>필터링된 타겟 회전</returns>
    private Quaternion ApplyQuaternionVibrationFilter(Quaternion newTarget, Quaternion previousTarget, Quaternion filteredTarget)
    {
        float deltaAngle = Quaternion.Angle(newTarget, previousTarget);

        if (deltaAngle < VIBRATION_FILTER_THRESHOLD_DEGREES)
        {
            return filteredTarget;
        }

        float filterRate = HIGH_FREQUENCY_CUTOFF_RATE * Time.deltaTime;
        float t = 1.0f - Mathf.Exp(-filterRate);

        return Quaternion.Slerp(filteredTarget, newTarget, t);
    }

    /// <summary>
    /// Quaternion용 Critical Damping 구현 (순수 Quaternion space)
    /// </summary>
    /// <param name="current">현재 회전</param>
    /// <param name="target">목표 회전</param>
    /// <param name="angularVelocity">현재 각속도 (ref)</param>
    /// <param name="dampingSpeed">댐핑 속도</param>
    /// <returns>댐핑된 회전</returns>
    private Quaternion CriticalDampQuaternion(Quaternion current, Quaternion target, ref Vector3 angularVelocity, float dampingSpeed)
    {
        float deltaTime = Time.deltaTime;
        float omega = dampingSpeed;

        // 최단 경로 회전 계산
        Quaternion deltaRotation = target * Quaternion.Inverse(current);

        // Quaternion을 축-각도로 변환
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

        // 각도를 -180~180 범위로 정규화
        if (angle > 180f) angle -= 360f;
        angle *= Mathf.Deg2Rad;

        Vector3 displacement = axis * angle;

        // Critical damping 계산
        float dampingFactor = Mathf.Exp(-omega * deltaTime);
        Vector3 dampedDisplacement = (displacement + angularVelocity * deltaTime) * dampingFactor;
        angularVelocity = (angularVelocity - displacement * omega) * dampingFactor;

        // 결과를 Quaternion으로 변환
        float dampedAngle = dampedDisplacement.magnitude;
        if (dampedAngle > Mathf.Epsilon)
        {
            Vector3 dampedAxis = dampedDisplacement.normalized;
            Quaternion dampedRotation = Quaternion.AngleAxis(dampedAngle * Mathf.Rad2Deg, dampedAxis);
            return dampedRotation * current;
        }

        return current;
    }
    #endregion

    private void UpdateSettingsTransition()
    {
        if (_targetSettings == null || _currentSettings == null) return;

        float deltaTime = Time.deltaTime;
        float speed = _settingsTransitionSpeed * deltaTime;

        // 분리된 댐핑 옵션 바로 적용
        _enablePositionDamping = _targetSettings.IsEnablePositionDamping;
        _enableRotationDamping = _targetSettings.IsEnableRotationDamping;

        // Lerp 설정값들
        _offsetDistance = Vector3.Lerp(_offsetDistance, _targetSettings.OffsetDistance, speed);
        _offsetRotationDegrees = Vector3.Lerp(_offsetRotationDegrees, _targetSettings.OffsetRotationDegrees, speed);
        _positionDampingSpeed = Vector3.Lerp(_positionDampingSpeed, _targetSettings.PositionDampingSpeed, speed);
        _rotationDampingSpeed = Vector3.Lerp(_rotationDampingSpeed, _targetSettings.RotationDampingSpeed, speed);

        if (_camera != null)
        {
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetSettings.FieldOfView, speed);
        }

        // 전환 완료 체크
        if (Vector3.Distance(_offsetDistance, _targetSettings.OffsetDistance) < _positionThreshold &&
            Vector3.Distance(_offsetRotationDegrees, _targetSettings.OffsetRotationDegrees) < _rotationThreshold &&
            (_camera == null || Mathf.Abs(_camera.fieldOfView - _targetSettings.FieldOfView) < 0.1f))
        {
            ApplySettingsImmediately(_targetSettings);
            _isTransitioningSettings = false;
        }
    }

    private void ApplySettingsImmediately(CameraSettings settings)
    {
        if (settings == null) return;

        _offsetDistance = settings.OffsetDistance;
        _offsetRotationDegrees = settings.OffsetRotationDegrees;
        _positionDampingSpeed = settings.PositionDampingSpeed;
        _rotationDampingSpeed = settings.RotationDampingSpeed;
        _enablePositionDamping = settings.IsEnablePositionDamping;
        _enableRotationDamping = settings.IsEnableRotationDamping;

        if (_camera != null)
        {
            _camera.fieldOfView = settings.FieldOfView;
        }

        _currentSettings = settings;
        _isTransitioningSettings = false;
    }

}
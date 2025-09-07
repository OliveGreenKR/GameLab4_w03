using Sirenix.OdinInspector;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;

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
    [Header("Input Event Provider GameObject")]
    [SerializeField] private GameObject _inputEventProviderGameObject;

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

    [TabGroup("Damping", "Position")]
    [Header("Position Damping")]
    [SuffixLabel("units per second")]
    [SerializeField] private Vector3 _positionDampingSpeed = new Vector3(10f, 10f, 10f);

    [TabGroup("Damping", "Rotation")]
    [Header("Rotation Damping")]
    [SuffixLabel("degrees per second")]
    [SerializeField] private Vector3 _rotationDampingSpeed = new Vector3(90f, 90f, 90f);

    [TabGroup("Damping", "Mode")]
    [Header("Damping Mode")]
    [SerializeField] private DampingMode _positionDampMode = DampingMode.ExponentialDecay;
    [TabGroup("Damping", "Mode")]
    [SerializeField] private DampingMode _rotationDampMode = DampingMode.ExponentialDecay;

    //[TabGroup("Damping", "Settings")]
    //[Header("Damping Settings")]
    //[InfoBox("Prevents overshooting when close to target")]
    //[SerializeField] private float _dampingThresholdRatio = 0.05f;

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
    [Header("Damping Control")]
    [SerializeField] private bool _enableDamping = true;

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
    private void ApplyAimSettings() => ApplyCameraSettings(_aimCameraSettings,true, _settingsTransitionSpeed);

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
    public bool IsDampingEnabled => _enableDamping;

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

    // Input Event Provider
    private IInputEventProvider _inputEventProvider = null;

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

        if (InitializeInputEventProvider())
        {
            SubscribeToInputEvents();
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
        UnsubscribeFromInputEvents();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 댐핑 활성화/비활성화 설정
    /// </summary>
    /// <param name="enabled">댐핑 활성화 여부</param>
    public void SetDampingEnabled(bool enabled)
    {
        _enableDamping = enabled;
        Debug.Log($"ThirdPersonCamera : Damping : {enabled} ");
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


        Debug.Log($"Target moved: {_targetWorldPosition:F6}");

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
            if (!_enableDamping)
            {
                transform.position = _targetWorldPosition;
                return;
            }

            Vector3 nextPosition = DampVector3(currentPosition, _targetWorldPosition, _positionDampMode);
            transform.position = nextPosition;
        }
    }

    private void TrackRotation()
    {
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(_targetWorldRotationDegrees);

        float angularDistance = Quaternion.Angle(currentRotation, targetRotation);

        if (angularDistance > _rotationThreshold)
        {
            if (!_enableDamping)
            {
                transform.rotation = targetRotation;
                return;
            }

            Quaternion nextRotation = DampQuaternion(currentRotation, targetRotation, _rotationDampMode);
            transform.rotation = nextRotation;
        }
    }

    private Vector3 DampVector3(Vector3 current, Vector3 target, DampingMode mode)
    {
        float distance = Vector3.Distance(current, target);

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
                        Mathf.Lerp(current.x, target.x, dampingSpeed.x),
                        Mathf.Lerp(current.y, target.y, dampingSpeed.y),
                        Mathf.Lerp(current.z, target.z, dampingSpeed.z)
                    );
                }
            case DampingMode.UnifiedDirectionPreserving:
                {
                    Vector3 direction = (target - current).normalized;
                    float dampingRate = _positionDampingSpeed.x;
                    float dampedDistance = distance * Mathf.Exp(-dampingRate * Time.deltaTime);
                    return current + direction * (distance - dampedDistance);
                }
            case DampingMode.ExponentialDecay:
                {
                    float dampingRate = _positionDampingSpeed.x;
                    return target + (current - target) * Mathf.Exp(-dampingRate * Time.deltaTime);
                }
            case DampingMode.CriticalDamping:
                {
                    float dampingRate = _positionDampingSpeed.x;
                    float t = 1.0f - Mathf.Exp(-dampingRate * Time.deltaTime);
                    return Vector3.Lerp(current, target, t);
                }
            default:
                return current;
        }
    }

    private Quaternion DampQuaternion(Quaternion current, Quaternion target, DampingMode mode)
    {
        float angularDistance = Quaternion.Angle(current, target);

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
                    Vector3 targetEuler = target.eulerAngles;

                    float deltaX = Mathf.DeltaAngle(currentEuler.x, targetEuler.x);
                    float deltaY = Mathf.DeltaAngle(currentEuler.y, targetEuler.y);
                    float deltaZ = Mathf.DeltaAngle(currentEuler.z, targetEuler.z);

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
                    return Quaternion.Slerp(current, target, progress);
                }
            case DampingMode.ExponentialDecay:
                {
                    float dampingRate = _rotationDampingSpeed.x;
                    float t = 1.0f - Mathf.Exp(-dampingRate * Time.deltaTime);
                    return Quaternion.Slerp(current, target, t);
                }
            case DampingMode.CriticalDamping:
                {
                    float dampingRate = _rotationDampingSpeed.x;
                    float t = 1.0f - Mathf.Exp(-dampingRate * Time.deltaTime);
                    return Quaternion.Slerp(current, target, t);
                }
            default:
                return current;
        }
    }
    #endregion

    #region Private Methods - Settings Transition
    private void UpdateSettingsTransition()
    {
        if (_targetSettings == null || _currentSettings == null) return;

        float deltaTime = Time.deltaTime;
        float speed = _settingsTransitionSpeed * deltaTime;

        //damping 옵션은 바로 적용
        _enableDamping = _targetSettings.IsEnableDamping;

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
        _enableDamping = settings.IsEnableDamping;

        if (_camera != null)
        {
            _camera.fieldOfView = settings.FieldOfView;
        }

        _currentSettings = settings;
        _isTransitioningSettings = false;
    }
    #endregion

    #region Private Methods - InputProvider Events  
    private bool InitializeInputEventProvider()
    {
        if (_inputEventProviderGameObject == null)
        {
            Debug.LogWarning("[ThirdPersonCameraController] Input Event Provider GameObject not assigned.");
            return false;
        }
        _inputEventProvider = _inputEventProviderGameObject.GetComponent<IInputEventProvider>();
        if (_inputEventProvider == null)
        {
            Debug.LogWarning("[ThirdPersonCameraController] IInputEventProvider component not found on the assigned GameObject.");
            return false;
        }
        return true;
    }
    private void SubscribeToInputEvents()
    {
        _inputEventProvider.OnAimModeEnded -= OnAimModeEnded;
        _inputEventProvider.OnAimModeEnded += OnAimModeEnded;
        _inputEventProvider.OnAimModeStarted -= OnAimModeStarted;
        _inputEventProvider.OnAimModeStarted += OnAimModeStarted;
    }

    private void UnsubscribeFromInputEvents()
    {
        _inputEventProvider.OnAimModeEnded -= OnAimModeEnded;
        _inputEventProvider.OnAimModeStarted -= OnAimModeStarted;
    }

    private void OnAimModeStarted()
    {
        ApplyAimSettings();
    }

    private void OnAimModeEnded()
    {
        ApplyDefaultSettings();
    }
    #endregion
}
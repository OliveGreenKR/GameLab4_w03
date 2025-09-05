using Sirenix.OdinInspector;
using UnityEngine;

public class TPSCameraController : MonoBehaviour, IAngleController
{
    #region Serialized Fields
    [TabGroup("Basic", "Target")]
    [Header("Target")]
    [Required]
    [SerializeField] private GameObject _targetGameObject;

    [TabGroup("Basic", "Target")]
    [Header("Target Screen Position")]
    [PropertyRange(0f, 1f)]
    [SuffixLabel("screen ratio")]
    [SerializeField] private float _targetScreenPositionX = 0.5f;

    [TabGroup("Basic", "Target")]
    [PropertyRange(0f, 1f)]
    [SuffixLabel("screen ratio")]
    [SerializeField] private float _targetScreenPositionY = 0.5f;

    [TabGroup("Basic", "Angles")]
    [Header("Initial Camera Angles")]
    [PropertyRange(-180f, 180f)]
    [SuffixLabel("degrees")]
    [SerializeField] private float _initialYawDegrees = 0f;

    [TabGroup("Basic", "Angles")]
    [PropertyRange(-80f, 80f)]
    [SuffixLabel("degrees")]
    [SerializeField] private float _initialPitchDegrees = 15f;

    [TabGroup("Distance", "Base")]
    [Header("Distance Settings")]
    [PropertyRange(2f, 50f)]
    [SuffixLabel("units")]
    [SerializeField] private float _baseDistanceUnits = 8f;

    [TabGroup("Distance", "Base")]
    [PropertyRange(1f, 50f)]
    [SuffixLabel("units")]
    [SerializeField] private float _minDistanceUnits = 3f;

    [TabGroup("Distance", "Base")]
    [PropertyRange(5f, 60f)]
    [SuffixLabel("units")]
    [SerializeField] private float _maxDistanceUnits = 15f;

    [TabGroup("Distance", "Base")]
    [Header("Height Offset")]
    [PropertyRange(0f, 5f)]
    [SuffixLabel("units")]
    [SerializeField] private float _targetHeightOffsetUnits = 1.5f;

    [TabGroup("Distance", "Speed")]
    [Header("Speed Based Distance")]
    [PropertyRange(0f, 2f)]
    [SuffixLabel("multiplier")]
    [SerializeField] private float _speedDistanceMultiplier = 0.5f;

    [TabGroup("Distance", "Speed")]
    [PropertyRange(0f, 100f)]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _maxSpeedForDistanceUnitsPerSecond = 10f;

    [TabGroup("Movement", "Damping")]
    [Header("Damping")]
    [PropertyRange(0.1f, 20f)]
    [SuffixLabel("speed")]
    [SerializeField] private float _positionDampingSpeed = 2f;

    [TabGroup("Movement", "Damping")]
    [PropertyRange(0.1f, 360f)]
    [SuffixLabel("speed")]
    [SerializeField] private float _rotationDampingSpeed = 3f;

    [TabGroup("Movement", "DeadZone")]
    [Header("Dead Zone")]
    [PropertyRange(0.001f, 1f)]
    [SuffixLabel("units")]
    [SerializeField] private float _positionDeadZoneUnits = 0.01f;

    [TabGroup("Movement", "DeadZone")]
    [PropertyRange(0.001f, 5f)]
    [SuffixLabel("degrees")]
    [SerializeField] private float _rotationDeadZoneDegrees = 0.1f;

    [TabGroup("Movement", "DeadZone")]
    [PropertyRange(0.01f, 1f)]
    [SuffixLabel("ratio")]
    [SerializeField] private float _positionLerpthresholdVelocitypRatio = 0.05f;

    [TabGroup("Advanced", "Visibility")]
    [Header("Visibility")]
    [SerializeField] private LayerMask _obstacleLayerMask = -1;

    [TabGroup("Advanced", "Visibility")]
    [PropertyRange(0.1f, 2f)]
    [SuffixLabel("seconds")]
    [SerializeField] private float _visibilityCheckInterval = 0.2f;

    [TabGroup("Aiming", "Settings")]
    [Header("Aiming FOV")]
    [PropertyRange(0.3f, 0.8f)]
    [SuffixLabel("ratio")]
    [SerializeField] private float _aimingFOVRatio = 0.6f;

    [TabGroup("Aiming", "Settings")]
    [Header("Aiming Distance")]
    [PropertyRange(-5f, 5f)]
    [SuffixLabel("units")]
    [SerializeField] private float _aimingDistanceOffset = -2f;

    [TabGroup("Aiming", "Settings")]
    [Header("Aiming Screen Position")]
    [PropertyRange(0f, 1f)]
    [SuffixLabel("screen ratio")]
    [SerializeField] private float _aimingScreenPositionX = 0.3f;

    [TabGroup("Aiming", "Settings")]
    [PropertyRange(0f, 1f)]
    [SuffixLabel("screen ratio")]
    [SerializeField] private float _aimingScreenPositionY = 0.5f;

    [TabGroup("Aiming", "Settings")]
    [Header("Aiming Height Offset")]
    [PropertyRange(-2f, 2f)]
    [SuffixLabel("units")]
    [SerializeField] private float _aimingHeightOffset = 0.3f;

    [TabGroup("Aiming", "Settings")]
    [Header("Aiming Transition")]
    [PropertyRange(1f, 10f)]
    [SuffixLabel("speed")]
    [SerializeField] private float _aimingTransitionSpeed = 5f;

    [TabGroup("Aiming", "Debug")]
    [Button("Toggle Aiming Mode")]
    private void DebugToggleAiming()
    {
        ToggleAimingMode();
    }

    #endregion

    #region Properties
    public float CurrentDistanceUnits { get; private set; }
    public bool IsTargetVisible { get; private set; }
    public Vector2 TargetScreenPosition => new Vector2(_targetScreenPositionX, _targetScreenPositionY);

    [TabGroup("Aiming", "Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsAimingMode { get; private set; }

    [TabGroup("Aiming", "Debug")]
    [ShowInInspector, ReadOnly]
    [ProgressBar(0, 1)]
    public float AimingProgress { get; private set; }
    #endregion

    #region Private Fields
    // 카메라 컴포넌트
    private Camera _camera;

    // 타겟 컴포넌트 캐싱
    private Transform _targetTransform;
    private CharacterController _targetCharacterController;

    // 구면 좌표 상태 (외부 접근 가능)
    [SerializeField] private float _currentYawDegrees;
    [SerializeField] private float _currentPitchDegrees;

    // 상태 관리
    private float _visibilityCheckTimer;
    private Vector3 _targetWorldPosition;
    private Vector3 _targetPositionWithOffset;
    private float _currentTargetDistanceUnits;
    private float _baseFOV;
    [SerializeField] private float _targetFOV;

    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeReferences();

        // 초기 카메라 각도 설정
        _currentYawDegrees = _initialYawDegrees;
        _currentPitchDegrees = _initialPitchDegrees;

        // 초기 거리 설정
        CurrentDistanceUnits = _baseDistanceUnits;
        _currentTargetDistanceUnits = _baseDistanceUnits;

        // 초기 상태
        IsTargetVisible = true;
        _visibilityCheckTimer = 0f;

        // 초기 카메라 상태 캐싱
        _baseFOV = _camera.fieldOfView;
        _targetFOV = _baseFOV;

        // 초기 카메라 위치 즉시 설정
        if (_targetTransform != null)
        {
            UpdateTargetPosition();
            CalculateTargetPosition();
            transform.position = _targetWorldPosition;

            // 타겟을 바라보도록 초기 회전 설정
            Vector3 lookDirection = (_targetPositionWithOffset - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    private void LateUpdate()
    {
        if (_targetTransform == null) return;

        UpdateTargetPosition();
        UpdateVisibilityCheck();
        CalculateTargetDistance();
        CalculateTargetPosition();
        ApplySmoothMovement();
        UpdateAimingTransition();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 추적할 타겟 설정
    /// </summary>
    /// <param name="targetGameObject">타겟 GameObject</param>
    public void SetTarget(GameObject targetGameObject)
    {
        _targetGameObject = targetGameObject;

        if (_targetGameObject != null)
        {
            // 컴포넌트 다시 캐싱
            _targetTransform = _targetGameObject.transform;
            _targetCharacterController = _targetGameObject.GetComponent<CharacterController>();

            // 즉시 위치 업데이트
            UpdateTargetPosition();
            CalculateTargetPosition();
        }
    }

    /// <summary>
    /// 타겟의 화면 내 위치 설정
    /// </summary>
    /// <param name="screenPositionX">화면 X 비율 (0~1)</param>
    /// <param name="screenPositionY">화면 Y 비율 (0~1)</param>
    public void SetTargetScreenPosition(float screenPositionX, float screenPositionY)
    {
        _targetScreenPositionX = Mathf.Clamp01(screenPositionX);
        _targetScreenPositionY = Mathf.Clamp01(screenPositionY);
    }

    /// <summary>
    /// 타겟의 화면 내 위치 설정
    /// </summary>
    /// <param name="screenPosition">화면 위치 비율 (0~1)</param>
    public void SetTargetScreenPosition(Vector2 screenPosition)
    {
        SetTargetScreenPosition(screenPosition.x, screenPosition.y);
    }

    /// <summary>
    /// 카메라 각도 설정 (마우스 입력용)
    /// </summary>
    /// <param name="yawDegrees">수평 회전 각도 (-180 ~ 180)</param>
    /// <param name="pitchDegrees">수직 회전 각도 (-80 ~ 80)</param>
    public void SetAngles(float yawDegrees, float pitchDegrees)
    {
        _currentYawDegrees = Mathf.Clamp(yawDegrees, -180f, 180f);
        _currentPitchDegrees = Mathf.Clamp(pitchDegrees, -80f, 80f);
    }

    /// <summary>
    /// 카메라 각도 조정 (마우스 델타 입력용)
    /// </summary>
    /// <param name="deltaYawDegrees">수평 회전 변화량</param>
    /// <param name="deltaPitchDegrees">수직 회전 변화량</param>
    public void AdjustAngles(float deltaYawDegrees, float deltaPitchDegrees)
    {
        if (Mathf.Abs(deltaYawDegrees) > _rotationDeadZoneDegrees)
        {
            _currentYawDegrees += deltaYawDegrees;
            _currentYawDegrees = Mathf.Repeat(_currentYawDegrees + 180f, 360f) - 180f; // -180 ~ 180 순환
        }
        if (Mathf.Abs(deltaPitchDegrees) > _rotationDeadZoneDegrees)
        {
            _currentPitchDegrees += deltaPitchDegrees;
            _currentPitchDegrees = Mathf.Clamp(_currentPitchDegrees, -80f, 80f);       // -80 ~ 80 제한
        }
        Debug.Log($"[TPSCameraController] AdjustAngles: Yaw={_currentYawDegrees}, Pitch={_currentPitchDegrees}");
    }

    /// <summary>
    /// 현재 카메라 각도 가져오기
    /// </summary>
    /// <returns>Vector2(yaw, pitch)</returns>
    public Vector2 GetCurrentAngles()
    {
        return new Vector2(_currentYawDegrees, _currentPitchDegrees);
    }

    /// <summary>
    /// 에이밍 모드 설정
    /// </summary>
    /// <param name="isAiming">에이밍 모드 활성화 여부</param>
    public void SetAimingMode(bool isAiming)
    {
        IsAimingMode = isAiming;
    }

    /// <summary>
    /// 에이밍 모드 토글
    /// </summary>
    public void ToggleAimingMode()
    {
        SetAimingMode(!IsAimingMode);
    }
    #endregion

    #region Private Methods
    private void UpdateVisibilityCheck()
    {
        _visibilityCheckTimer += Time.deltaTime;

        if (_visibilityCheckTimer >= _visibilityCheckInterval)
        {
            PerformVisibilityRaycast();
            _visibilityCheckTimer = 0f;
        }
    }

    private void AdjustDistanceForVisibility()
    {
        if (!IsTargetVisible)
        {
            // 타겟이 안 보이면 거리를 줄여서 가까이 이동
            float adjustedDistance = _currentTargetDistanceUnits * 0.7f;
            _currentTargetDistanceUnits = Mathf.Max(adjustedDistance, _minDistanceUnits);
        }
    }

    private void UpdateTargetPosition()
    {
        if (_targetTransform == null) return;

        // 에이밍 모드에 따른 높이 오프셋 사용
        Vector3 baseTargetPosition = _targetTransform.position + Vector3.up * GetCurrentTargetHeightOffset();

        Vector3 screenOffset = CalculateScreenOffset();
        _targetPositionWithOffset = baseTargetPosition + screenOffset;
    }

    private Vector3 CalculateScreenOffset()
    {
        if (_camera == null) return Vector3.zero;

        // 에이밍 모드에 따른 스크린 포지션 사용
        Vector2 offsetFromCenter = new Vector2(GetCurrentTargetScreenPositionX() - 0.5f, GetCurrentTargetScreenPositionY() - 0.5f);

        float halfFOVRadians = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float verticalSize = _currentTargetDistanceUnits * Mathf.Tan(halfFOVRadians);
        float horizontalSize = verticalSize * _camera.aspect;

        Vector3 cameraRight = transform.right;
        Vector3 cameraUp = transform.up;

        return cameraRight * (offsetFromCenter.x * horizontalSize * 2f) +
               cameraUp * (offsetFromCenter.y * verticalSize * 2f);
    }

    private void CalculateTargetPosition()
    {
        if (_targetTransform == null) return;

        // 구면 좌표를 카르테시안 좌표로 변환
        float yawRadians = _currentYawDegrees * Mathf.Deg2Rad;
        float pitchRadians = _currentPitchDegrees * Mathf.Deg2Rad;

        // 구면 좌표 계산 (Y-up 좌표계)
        float horizontalDistance = _currentTargetDistanceUnits * Mathf.Cos(pitchRadians);
        float verticalOffset = _currentTargetDistanceUnits * Mathf.Sin(pitchRadians);

        Vector3 horizontalOffset = new Vector3(
            horizontalDistance * Mathf.Sin(yawRadians),    // X축 (좌우)
            0f,                                            // Y축 (높이는 별도 계산)
            horizontalDistance * Mathf.Cos(yawRadians)     // Z축 (앞뒤)
        );

        // 최종 카메라 위치 = 오프셋 적용된 타겟 위치 + 수평 오프셋 + 수직 오프셋
        _targetWorldPosition = _targetPositionWithOffset + horizontalOffset + Vector3.up * verticalOffset;
    }

    private void ApplySmoothMovement()
    {
        bool updatePosition = false;

        // 위치변화량이 한계값 이상
        Vector3 currentPosition = transform.position;
        float positionDistance = Vector3.Distance(currentPosition, _targetWorldPosition);
        float positionDistMag = Mathf.Abs(positionDistance);
        updatePosition = positionDistMag > _positionDeadZoneUnits;

        float targetSpeed = 0f;
        if (_targetCharacterController != null)
        {
            targetSpeed = _targetCharacterController.velocity.magnitude;
        }

        if (updatePosition)
        {
            // 객체 속도에 근거한 Lerp 적용
            if (positionDistMag < _positionLerpthresholdVelocitypRatio * targetSpeed)
            {
                transform.position = _targetWorldPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(currentPosition, _targetWorldPosition, _positionDampingSpeed * Time.deltaTime);
            }
        }

        bool updateRotation = false;
        // 회전 변화량이 한계값 이상
        if (_targetTransform != null)
        {
            Vector3 lookDirection = (_targetPositionWithOffset - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            float rotationAngle = Quaternion.Angle(transform.rotation, targetRotation);

            updateRotation = Mathf.Abs(rotationAngle) > _rotationDeadZoneDegrees;
            if (updateRotation)
            {
                Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationDampingSpeed * Time.deltaTime);
                transform.rotation = newRotation;
            }
        }

        // 현재 실제 거리 업데이트
        if (_targetTransform != null)
        {
            CurrentDistanceUnits = Vector3.Distance(transform.position, _targetPositionWithOffset);
        }
    }

    private void PerformVisibilityRaycast()
    {
        if (_targetTransform == null) return;

        Vector3 directionToTarget = (_targetPositionWithOffset - transform.position).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, _targetPositionWithOffset);

        Ray visibilityRay = new Ray(transform.position, directionToTarget);

        if (Physics.Raycast(visibilityRay, out RaycastHit hit, distanceToTarget, _obstacleLayerMask))
        {
            // 장애물이 타겟을 가리고 있음
            IsTargetVisible = false;
        }
        else
        {
            // 타겟이 보임
            IsTargetVisible = true;
        }
    }

    private void InitializeReferences()
    {
        // 카메라 컴포넌트 캐싱
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        if (_camera == null)
        {
            Debug.LogError("[TPSCameraController] Camera component required!");
        }

        // 타겟 GameObject에서 필요한 컴포넌트들 캐싱
        if (_targetGameObject != null)
        {
            _targetTransform = _targetGameObject.transform;
            _targetCharacterController = _targetGameObject.GetComponent<CharacterController>();

            // 필수 컴포넌트 검증
            if (_targetTransform == null)
            {
                Debug.LogError("[TPSCameraController] Target Transform not found!");
            }

            if (_targetCharacterController == null)
            {
                Debug.LogWarning("[TPSCameraController] Target CharacterController not found! Speed-based distance will not work.");
            }
        }
        else
        {
            Debug.LogError("[TPSCameraController] Target GameObject not assigned!");
        }
    }

    private void CalculateTargetDistance()
    {
        // 에이밍 모드에 따른 기본 거리 사용
        float baseDistance = GetCurrentBaseDistance();

        // 타겟 속도에 따른 거리 조절 (CharacterController 사용)
        if (_targetCharacterController != null)
        {
            float targetSpeed = _targetCharacterController.velocity.magnitude;
            float speedRatio = Mathf.Clamp01(targetSpeed / _maxSpeedForDistanceUnitsPerSecond);
            float speedDistanceOffset = speedRatio * _speedDistanceMultiplier * _baseDistanceUnits;
            baseDistance += speedDistanceOffset;
        }

        // 최소/최대 거리 제한
        _currentTargetDistanceUnits = Mathf.Clamp(baseDistance, _minDistanceUnits, _maxDistanceUnits);

        // 가시성에 따른 거리 조절
        AdjustDistanceForVisibility();
    }

    private void UpdateAimingTransition()
    {
        float targetProgress = IsAimingMode ? 1f : 0f;
        AimingProgress = Mathf.Lerp(AimingProgress, targetProgress, _aimingTransitionSpeed * Time.deltaTime);

        if (Mathf.Abs(AimingProgress - targetProgress) < 0.01f)
        {
            AimingProgress = targetProgress;
        }

        UpdateAimingFOV();
    }

    private void UpdateAimingFOV()
    {
        if (_camera == null) return;

        float targetFOV = Mathf.Lerp(_baseFOV, _baseFOV * _aimingFOVRatio, AimingProgress);
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, _rotationDampingSpeed * Time.deltaTime);
    }

    private float GetCurrentTargetScreenPositionX()
    {
        return Mathf.Lerp(_targetScreenPositionX, _aimingScreenPositionX, AimingProgress);
    }

    private float GetCurrentTargetScreenPositionY()
    {
        return Mathf.Lerp(_targetScreenPositionY, _aimingScreenPositionY, AimingProgress);
    }

    private float GetCurrentTargetHeightOffset()
    {
        return Mathf.Lerp(_targetHeightOffsetUnits, _targetHeightOffsetUnits + _aimingHeightOffset, AimingProgress);
    }

    private float GetCurrentBaseDistance()
    {
        return _baseDistanceUnits + (_aimingDistanceOffset * AimingProgress);
    }
    #endregion
}
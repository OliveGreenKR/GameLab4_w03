using UnityEngine;

public class CameraController : MonoBehaviour , IAngleController
{
    #region Serialized Fields
    [Header("Target")]
    [SerializeField] private GameObject _playerGameObject;
  
    [Header("Camera Angles")]
    [SerializeField][Range(-180f, 180f)] private float _initialYawDegrees = 0f;
    [SerializeField][Range(-80f, 80f)] private float _initialPitchDegrees = 15f;

    [Header("Distance Settings")]
    [SerializeField][Range(2f, 50f)] private float _baseDistanceUnits = 8f;
    [SerializeField][Range(1f, 50f)] private float _minDistanceUnits = 3f;
    [SerializeField][Range(5f, 60f)] private float _maxDistanceUnits = 15f;

    [Header("Height Offset")]
    [SerializeField][Range(0f, 5f)] private float _playerHeightOffsetUnits = 1.5f;

    [Header("Speed Based Distance")]
    [SerializeField][Range(0f, 2f)] private float _speedDistanceMultiplier = 0.5f;
    [SerializeField][Range(0f, 100f)] private float _maxSpeedForDistanceUnitsPerSecond = 10f;

    [Header("Damping")]
    [SerializeField] private float _positionDampingSpeed = 2f;
    [SerializeField] private float _rotationDampingSpeed = 3f;

    [Header("Dead Zone")]
    [SerializeField] private float _positionDeadZoneUnits = 0.01f;
    [SerializeField] private float _rotationDeadZoneDegrees = 0.1f;
    [SerializeField] private float _positionLerpthresholdVelocitypRatio = 0.05f;
    //[SerializeField] private float _rotationLerpthreshold = 0.01f;

    [Header("Visibility")]
    [SerializeField] private LayerMask _obstacleLayerMask = -1;
    [SerializeField][Range(0.1f, 1f)] private float _visibilityCheckInterval = 0.2f;

    [Header("Input")]
    [SerializeField] private InputSystem_Actions _actions;
    #endregion

    #region Properties
    public float CurrentDistanceUnits { get; private set; }
    public bool IsPlayerVisible { get; private set; }
    public float CurrentYawDegrees { get; private set; }
    public float CurrentPitchDegrees { get; private set; }
    #endregion

    #region Private Fields
    // 카메라 컴포넌트
    private Camera _camera;

    // 플레이어 컴포넌트 캐싱
    private Transform _playerTransform;
    private Rigidbody _playerRigidbody;
    private PlayerController _playerController;

    // 구면 좌표 상태
    private float _currentYawDegrees;
    private float _currentPitchDegrees;

    // 상태 관리
    private float _visibilityCheckTimer;
    private Vector3 _targetWorldPosition;
    [SerializeField] private Vector3 _playerTargetPosition;
    private float _currentTargetDistanceUnits;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeReferences();

        // 초기 카메라 각도 설정
        _currentYawDegrees = _initialYawDegrees;
        _currentPitchDegrees = _initialPitchDegrees;
        CurrentYawDegrees = _currentYawDegrees;
        CurrentPitchDegrees = _currentPitchDegrees;

        // 초기 거리 설정
        CurrentDistanceUnits = _baseDistanceUnits;
        _currentTargetDistanceUnits = _baseDistanceUnits;

        // 초기 상태
        IsPlayerVisible = true;
        _visibilityCheckTimer = 0f;

        // 초기 카메라 위치 즉시 설정
        if (_playerTransform != null)
        {
            UpdatePlayerTargetPosition();
            CalculateTargetPosition();
            transform.position = _targetWorldPosition;

            // 플레이어를 바라보도록 초기 회전 설정
            Vector3 lookDirection = (_playerTargetPosition - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    private void LateUpdate()
    {
        if (_playerTransform == null) return;

        UpdatePlayerTargetPosition();
        UpdateVisibilityCheck();
        CalculateTargetDistance();
        CalculateTargetPosition();
        ApplySmoothMovement();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 추적할 플레이어 설정
    /// </summary>
    /// <param name="playerGameObject">플레이어 GameObject</param>
    public void SetTargetPlayer(GameObject playerGameObject)
    {
        _playerGameObject = playerGameObject;

        if (_playerGameObject != null)
        {
            // 컴포넌트 다시 캐싱
            _playerTransform = _playerGameObject.transform;
            _playerRigidbody = _playerGameObject.GetComponent<Rigidbody>();
            _playerController = _playerGameObject.GetComponent<PlayerController>();

            // 즉시 위치 업데이트
            UpdatePlayerTargetPosition();
            CalculateTargetPosition();
        }
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
        if(Mathf.Abs(deltaYawDegrees) > _rotationDeadZoneDegrees)
        {
            _currentYawDegrees += deltaYawDegrees;
            _currentYawDegrees = Mathf.Repeat(_currentYawDegrees + 180f, 360f) - 180f; // -180 ~ 180 순환
        }
        if(Mathf.Abs(deltaPitchDegrees) > _rotationDeadZoneDegrees)
        {
            _currentPitchDegrees += deltaPitchDegrees;
            _currentPitchDegrees = Mathf.Clamp(_currentPitchDegrees, -80f, 80f);       // -80 ~ 80 제한
        }
    }

    /// <summary>
    /// 현재 카메라 각도 가져오기
    /// </summary>
    /// <returns>Vector2(yaw, pitch)</returns>
    public Vector2 GetCurrentAngles()
    {
        return new Vector2(_currentYawDegrees, _currentPitchDegrees);
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
        if (!IsPlayerVisible)
        {
            // 플레이어가 안 보이면 거리를 줄여서 가까이 이동
            float adjustedDistance = _currentTargetDistanceUnits * 0.7f;
            _currentTargetDistanceUnits = Mathf.Max(adjustedDistance, _minDistanceUnits);
        }
    }

    private void UpdatePlayerTargetPosition()
    {
        if (_playerTransform == null) return;

        // 플레이어 위치에 높이 오프셋 적용 (허리 기준점)
        _playerTargetPosition = _playerTransform.position + Vector3.up * _playerHeightOffsetUnits;
    }

    private void CalculateTargetPosition()
    {
        if (_playerTransform == null) return;

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

        // 최종 카메라 위치 = 플레이어 타겟 위치 + 수평 오프셋 + 수직 오프셋
        _targetWorldPosition = _playerTargetPosition + horizontalOffset + Vector3.up * verticalOffset;
    }

    private void ApplySmoothMovement()
    {
        
        bool updatePosition = false;   
        

        //위치변화량이 한계값 이상
        Vector3 currentPosition = transform.position;
        float positionDistance = Vector3.Distance(currentPosition, _targetWorldPosition);
        float positionDistMag = Mathf.Abs(positionDistance);
        updatePosition = positionDistMag > _positionDeadZoneUnits;
        //Debug.Log($"Distance to Target : {positionDistance}");

        float playerSpeed = _playerRigidbody.linearVelocity.magnitude;

        if (updatePosition)
        {
            // 객체 속도에 근거한 Lerp 적용
            if (positionDistMag < _positionLerpthresholdVelocitypRatio * playerSpeed)
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
        if (_playerTransform != null)
        {
            Vector3 lookDirection = (_playerTargetPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            float rotationAngle = Quaternion.Angle(transform.rotation, targetRotation);

            updateRotation = Mathf.Abs(rotationAngle) > _rotationDeadZoneDegrees;
            if (updateRotation)
            {
                Quaternion NewRotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationDampingSpeed * Time.deltaTime);
                transform.rotation = NewRotation;
            }
        }

        // 현재 실제 거리 업데이트
        if (_playerTransform != null)
        {
            CurrentDistanceUnits = Vector3.Distance(transform.position, _playerTargetPosition);
        }

        

        // Properties 업데이트
        CurrentYawDegrees = _currentYawDegrees;
        CurrentPitchDegrees = _currentPitchDegrees;
    }

    private void PerformVisibilityRaycast()
    {
        if (_playerTransform == null) return;

        Vector3 directionToPlayer = (_playerTargetPosition - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, _playerTargetPosition);

        Ray visibilityRay = new Ray(transform.position, directionToPlayer);

        if (Physics.Raycast(visibilityRay, out RaycastHit hit, distanceToPlayer, _obstacleLayerMask))
        {
            // 장애물이 플레이어를 가리고 있음
            IsPlayerVisible = false;
        }
        else
        {
            // 플레이어가 보임
            IsPlayerVisible = true;
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
            Debug.LogError("[CameraController] Camera component required!");
        }

        // 플레이어 GameObject에서 필요한 컴포넌트들 캐싱
        if (_playerGameObject != null)
        {
            _playerTransform = _playerGameObject.transform;
            _playerRigidbody = _playerGameObject.GetComponent<Rigidbody>();
            _playerController = _playerGameObject.GetComponent<PlayerController>();

            // 필수 컴포넌트 검증
            if (_playerTransform == null)
            {
                Debug.LogError("[CameraController] Player Transform not found!");
            }

            if (_playerRigidbody == null)
            {
                Debug.LogWarning("[CameraController] Player Rigidbody not found! Speed-based distance will not work.");
            }

            if (_playerController == null)
            {
                Debug.LogWarning("[CameraController] PlayerController not found! Movement state detection will not work.");
            }
        }
        else
        {
            Debug.LogError("[CameraController] Player GameObject not assigned!");
        }
    }

    private void CalculateTargetDistance()
    {
        float baseDistance = _baseDistanceUnits;

        // 플레이어 속도에 따른 거리 조절 (캐싱된 컴포넌트 사용)
        if (_playerController != null && _playerRigidbody != null)
        {
            float playerSpeed = _playerRigidbody.linearVelocity.magnitude;
            float speedRatio = Mathf.Clamp01(playerSpeed / _maxSpeedForDistanceUnitsPerSecond);
            float speedDistanceOffset = speedRatio * _speedDistanceMultiplier * _baseDistanceUnits;
            baseDistance += speedDistanceOffset;
        }

        // 최소/최대 거리 제한
        _currentTargetDistanceUnits = Mathf.Clamp(baseDistance, _minDistanceUnits, _maxDistanceUnits);

        // 가시성에 따른 거리 조절
        AdjustDistanceForVisibility();
    }
    #endregion
}
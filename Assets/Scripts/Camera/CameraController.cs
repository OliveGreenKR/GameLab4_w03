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
    // ī�޶� ������Ʈ
    private Camera _camera;

    // �÷��̾� ������Ʈ ĳ��
    private Transform _playerTransform;
    private Rigidbody _playerRigidbody;
    private PlayerController _playerController;

    // ���� ��ǥ ����
    private float _currentYawDegrees;
    private float _currentPitchDegrees;

    // ���� ����
    private float _visibilityCheckTimer;
    private Vector3 _targetWorldPosition;
    [SerializeField] private Vector3 _playerTargetPosition;
    private float _currentTargetDistanceUnits;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeReferences();

        // �ʱ� ī�޶� ���� ����
        _currentYawDegrees = _initialYawDegrees;
        _currentPitchDegrees = _initialPitchDegrees;
        CurrentYawDegrees = _currentYawDegrees;
        CurrentPitchDegrees = _currentPitchDegrees;

        // �ʱ� �Ÿ� ����
        CurrentDistanceUnits = _baseDistanceUnits;
        _currentTargetDistanceUnits = _baseDistanceUnits;

        // �ʱ� ����
        IsPlayerVisible = true;
        _visibilityCheckTimer = 0f;

        // �ʱ� ī�޶� ��ġ ��� ����
        if (_playerTransform != null)
        {
            UpdatePlayerTargetPosition();
            CalculateTargetPosition();
            transform.position = _targetWorldPosition;

            // �÷��̾ �ٶ󺸵��� �ʱ� ȸ�� ����
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
    /// ������ �÷��̾� ����
    /// </summary>
    /// <param name="playerGameObject">�÷��̾� GameObject</param>
    public void SetTargetPlayer(GameObject playerGameObject)
    {
        _playerGameObject = playerGameObject;

        if (_playerGameObject != null)
        {
            // ������Ʈ �ٽ� ĳ��
            _playerTransform = _playerGameObject.transform;
            _playerRigidbody = _playerGameObject.GetComponent<Rigidbody>();
            _playerController = _playerGameObject.GetComponent<PlayerController>();

            // ��� ��ġ ������Ʈ
            UpdatePlayerTargetPosition();
            CalculateTargetPosition();
        }
    }

    /// <summary>
    /// ī�޶� ���� ���� (���콺 �Է¿�)
    /// </summary>
    /// <param name="yawDegrees">���� ȸ�� ���� (-180 ~ 180)</param>
    /// <param name="pitchDegrees">���� ȸ�� ���� (-80 ~ 80)</param>
    public void SetAngles(float yawDegrees, float pitchDegrees)
    {
        _currentYawDegrees = Mathf.Clamp(yawDegrees, -180f, 180f);
        _currentPitchDegrees = Mathf.Clamp(pitchDegrees, -80f, 80f);
    }

    /// <summary>
    /// ī�޶� ���� ���� (���콺 ��Ÿ �Է¿�)
    /// </summary>
    /// <param name="deltaYawDegrees">���� ȸ�� ��ȭ��</param>
    /// <param name="deltaPitchDegrees">���� ȸ�� ��ȭ��</param>
    public void AdjustAngles(float deltaYawDegrees, float deltaPitchDegrees)
    {
        if(Mathf.Abs(deltaYawDegrees) > _rotationDeadZoneDegrees)
        {
            _currentYawDegrees += deltaYawDegrees;
            _currentYawDegrees = Mathf.Repeat(_currentYawDegrees + 180f, 360f) - 180f; // -180 ~ 180 ��ȯ
        }
        if(Mathf.Abs(deltaPitchDegrees) > _rotationDeadZoneDegrees)
        {
            _currentPitchDegrees += deltaPitchDegrees;
            _currentPitchDegrees = Mathf.Clamp(_currentPitchDegrees, -80f, 80f);       // -80 ~ 80 ����
        }
    }

    /// <summary>
    /// ���� ī�޶� ���� ��������
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
            // �÷��̾ �� ���̸� �Ÿ��� �ٿ��� ������ �̵�
            float adjustedDistance = _currentTargetDistanceUnits * 0.7f;
            _currentTargetDistanceUnits = Mathf.Max(adjustedDistance, _minDistanceUnits);
        }
    }

    private void UpdatePlayerTargetPosition()
    {
        if (_playerTransform == null) return;

        // �÷��̾� ��ġ�� ���� ������ ���� (�㸮 ������)
        _playerTargetPosition = _playerTransform.position + Vector3.up * _playerHeightOffsetUnits;
    }

    private void CalculateTargetPosition()
    {
        if (_playerTransform == null) return;

        // ���� ��ǥ�� ī���׽þ� ��ǥ�� ��ȯ
        float yawRadians = _currentYawDegrees * Mathf.Deg2Rad;
        float pitchRadians = _currentPitchDegrees * Mathf.Deg2Rad;

        // ���� ��ǥ ��� (Y-up ��ǥ��)
        float horizontalDistance = _currentTargetDistanceUnits * Mathf.Cos(pitchRadians);
        float verticalOffset = _currentTargetDistanceUnits * Mathf.Sin(pitchRadians);

        Vector3 horizontalOffset = new Vector3(
            horizontalDistance * Mathf.Sin(yawRadians),    // X�� (�¿�)
            0f,                                            // Y�� (���̴� ���� ���)
            horizontalDistance * Mathf.Cos(yawRadians)     // Z�� (�յ�)
        );

        // ���� ī�޶� ��ġ = �÷��̾� Ÿ�� ��ġ + ���� ������ + ���� ������
        _targetWorldPosition = _playerTargetPosition + horizontalOffset + Vector3.up * verticalOffset;
    }

    private void ApplySmoothMovement()
    {
        
        bool updatePosition = false;   
        

        //��ġ��ȭ���� �Ѱ谪 �̻�
        Vector3 currentPosition = transform.position;
        float positionDistance = Vector3.Distance(currentPosition, _targetWorldPosition);
        float positionDistMag = Mathf.Abs(positionDistance);
        updatePosition = positionDistMag > _positionDeadZoneUnits;
        //Debug.Log($"Distance to Target : {positionDistance}");

        float playerSpeed = _playerRigidbody.linearVelocity.magnitude;

        if (updatePosition)
        {
            // ��ü �ӵ��� �ٰ��� Lerp ����
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
        // ȸ�� ��ȭ���� �Ѱ谪 �̻�
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

        // ���� ���� �Ÿ� ������Ʈ
        if (_playerTransform != null)
        {
            CurrentDistanceUnits = Vector3.Distance(transform.position, _playerTargetPosition);
        }

        

        // Properties ������Ʈ
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
            // ��ֹ��� �÷��̾ ������ ����
            IsPlayerVisible = false;
        }
        else
        {
            // �÷��̾ ����
            IsPlayerVisible = true;
        }
    }

    private void InitializeReferences()
    {
        // ī�޶� ������Ʈ ĳ��
        if (_camera == null)
        {
            _camera = GetComponent<Camera>();
        }

        if (_camera == null)
        {
            Debug.LogError("[CameraController] Camera component required!");
        }

        // �÷��̾� GameObject���� �ʿ��� ������Ʈ�� ĳ��
        if (_playerGameObject != null)
        {
            _playerTransform = _playerGameObject.transform;
            _playerRigidbody = _playerGameObject.GetComponent<Rigidbody>();
            _playerController = _playerGameObject.GetComponent<PlayerController>();

            // �ʼ� ������Ʈ ����
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

        // �÷��̾� �ӵ��� ���� �Ÿ� ���� (ĳ�̵� ������Ʈ ���)
        if (_playerController != null && _playerRigidbody != null)
        {
            float playerSpeed = _playerRigidbody.linearVelocity.magnitude;
            float speedRatio = Mathf.Clamp01(playerSpeed / _maxSpeedForDistanceUnitsPerSecond);
            float speedDistanceOffset = speedRatio * _speedDistanceMultiplier * _baseDistanceUnits;
            baseDistance += speedDistanceOffset;
        }

        // �ּ�/�ִ� �Ÿ� ����
        _currentTargetDistanceUnits = Mathf.Clamp(baseDistance, _minDistanceUnits, _maxDistanceUnits);

        // ���ü��� ���� �Ÿ� ����
        AdjustDistanceForVisibility();
    }
    #endregion
}
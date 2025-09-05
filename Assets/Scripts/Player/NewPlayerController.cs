using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewPlayerController : MonoBehaviour, IReSpawnable
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Core References")]
    [Required]
    [SerializeField] private InputSystem_Actions _inputs;

    [TabGroup("References")]
    [Required]
    [SerializeField] private CharacterController _characterController;

    [TabGroup("References")]
    [SerializeField] private Camera _camera;

    [TabGroup("Movement", "Basic")]
    [Header("Movement Settings")]
    [PropertyRange(0.1f, 20f)]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _moveSpeedUnitsPerSecond = 5f;

    [TabGroup("Movement", "Basic")]
    [PropertyRange(0.1f, 50f)]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _maxSpeedUnitsPerSecond = 10f;

    [TabGroup("Movement", "Air")]
    [Header("Air Movement")]
    [PropertyRange(0f, 1f)]
    [SuffixLabel("multiplier")]
    [SerializeField] private float _airControlMultiplier = 0.5f;

    [TabGroup("Jump")]
    [Header("Jump Settings")]
    [PropertyRange(1f, 20f)]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _jumpSpeedUnitsPerSecond = 10f;

    [TabGroup("Jump")]
    [PropertyRange(0.1f, 1f)]
    [SuffixLabel("seconds")]
    [SerializeField] private float _coyoteTimeDurationSeconds = 0.2f;

    [TabGroup("Movement", "Damping")]
    [Header("Movement Damping")]
    [PropertyRange(1f, 20f)]
    [SuffixLabel("speed")]
    [SerializeField] private float _groundDampingSpeed = 10f;

    [TabGroup("Movement", "Damping")]
    [PropertyRange(1f, 20f)]
    [SuffixLabel("speed")]
    [SerializeField] private float _airDampingSpeed = 5f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsMoving => _currentVelocity.magnitude > 0.1f;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsGrounded => _characterController != null && _characterController.isGrounded;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentVelocityUnitsPerSecond => _currentVelocity;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector2 CurrentMoveInput => _currentMoveInput;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 LastSpawnPosition => _lastSpawnPosition;
    #endregion

    #region Private Fields
    private Vector3 _currentVelocity = Vector3.zero;
    private Vector2 _currentMoveInput = Vector2.zero;
    private Vector3 _lastSpawnPosition = Vector3.zero;
    private float _coyoteTimeRemaining = 0f;
    private bool _wasGroundedLastFrame = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_inputs == null)
        {
            _inputs = new InputSystem_Actions();
        }
    }

    private void Start()
    {
        InitializeReferences();
        _lastSpawnPosition = transform.position;
    }

    private void Update()
    {
        UpdateGroundedState();
        UpdateCoyoteTime();
        HandleMovement();
        HandleJump();
        ApplyMovement();
    }

    private void OnEnable()
    {
        EnableInput();
    }

    private void OnDisable()
    {
        DisableInput();
    }
    #endregion

    #region IReSpawnable Implementation
    public bool ReSpawn(Vector3 worldPosition, Quaternion worldRotation)
    {
        // 위치와 회전 설정
        transform.position = worldPosition;
        transform.rotation = worldRotation;

        // CharacterController 상태 초기화
        if (_characterController != null)
        {
            _characterController.enabled = false;
            _characterController.enabled = true;
        }

        // 상태 초기화
        _currentVelocity = Vector3.zero;
        _currentMoveInput = Vector2.zero;
        _coyoteTimeRemaining = 0f;
        _wasGroundedLastFrame = false;

        // 스폰 위치 기록
        _lastSpawnPosition = worldPosition;

        Debug.Log($"[NewPlayerController] Respawned at position: {worldPosition}");

        return true;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 입력 활성화
    /// </summary>
    public void EnableInput()
    {
        if (_inputs == null) return;

        _inputs.Enable();
        _inputs.Player.Move.performed += OnMovePerformed;
        _inputs.Player.Move.canceled += OnMoveCanceled;
        _inputs.Player.Jump.performed += OnJumpPerformed;
        _inputs.Player.ColorChange.performed += OnMouseClicked;
        _inputs.Player.Zoom.performed += OnMouseRightPressed;
        _inputs.Player.Zoom.canceled += OnMouseRightCancled;
    }

    /// <summary>
    /// 입력 비활성화
    /// </summary>
    public void DisableInput()
    {
        if (_inputs == null) return;

        _inputs.Disable();
        _inputs.Player.Move.performed -= OnMovePerformed;
        _inputs.Player.Move.canceled -= OnMoveCanceled;
        _inputs.Player.Jump.performed -= OnJumpPerformed;
        _inputs.Player.ColorChange.performed -= OnMouseClicked;
        _inputs.Player.Zoom.performed -= OnMouseRightPressed;
        _inputs.Player.Zoom.canceled -= OnMouseRightCancled;

        _currentMoveInput = Vector2.zero;
    }

    /// <summary>
    /// 이동 속도 설정
    /// </summary>
    /// <param name="speedUnitsPerSecond">이동 속도</param>
    public void SetMoveSpeed(float speedUnitsPerSecond)
    {
        _moveSpeedUnitsPerSecond = Mathf.Clamp(speedUnitsPerSecond, 0.1f, 20f);
    }

    /// <summary>
    /// 점프 속도 설정
    /// </summary>
    /// <param name="jumpSpeedUnitsPerSecond">점프 속도</param>
    public void SetJumpSpeed(float jumpSpeedUnitsPerSecond)
    {
        _jumpSpeedUnitsPerSecond = Mathf.Clamp(jumpSpeedUnitsPerSecond, 1f, 20f);
    }
    #endregion

    #region Private Methods
    private void InitializeReferences()
    {
        if (_characterController == null)
        {
            _characterController = GetComponent<CharacterController>();
        }

        if (_camera == null)
        {
            _camera = Camera.main;
            Debug.Log("[NewPlayerController] Main Camera assigned.");
        }

        CheckReferencesAndLog();
    }

    private void CheckReferencesAndLog()
    {
        if (_characterController == null)
        {
            Debug.LogError("[NewPlayerController] CharacterController required!");
        }

        if (_inputs == null)
        {
            Debug.LogError("[NewPlayerController] InputSystem_Actions required!");
        }

        if (_camera == null)
        {
            Debug.LogError("[NewPlayerController] Camera required!");
        }
    }

    private void HandleMovement()
    {
        if (_currentMoveInput.magnitude < 0.1f)
        {
            // 입력이 없으면 감속
            float dampingSpeed = IsGrounded ? _groundDampingSpeed : _airDampingSpeed;
            _currentVelocity.x = Mathf.Lerp(_currentVelocity.x, 0f, dampingSpeed * Time.deltaTime);
            _currentVelocity.z = Mathf.Lerp(_currentVelocity.z, 0f, dampingSpeed * Time.deltaTime);
            return;
        }

        // 입력 방향 계산
        Vector3 inputDirection = new Vector3(_currentMoveInput.x, 0f, _currentMoveInput.y);

        // 카메라 기준 방향 변환
        if (_camera != null)
        {
            Quaternion cameraRotation = Quaternion.Euler(0f, _camera.transform.eulerAngles.y, 0f);
            inputDirection = cameraRotation * inputDirection;
        }

        inputDirection = inputDirection.normalized;

        // 이동 속도 계산
        float currentMoveSpeed = _moveSpeedUnitsPerSecond;
        if (!IsGrounded)
        {
            currentMoveSpeed *= _airControlMultiplier;
        }

        // 수평 속도 설정
        Vector3 targetVelocity = inputDirection * currentMoveSpeed;
        _currentVelocity.x = targetVelocity.x;
        _currentVelocity.z = targetVelocity.z;

        // 최대 속도 제한
        Vector3 horizontalVelocity = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z);
        if (horizontalVelocity.magnitude > _maxSpeedUnitsPerSecond)
        {
            horizontalVelocity = horizontalVelocity.normalized * _maxSpeedUnitsPerSecond;
            _currentVelocity.x = horizontalVelocity.x;
            _currentVelocity.z = horizontalVelocity.z;
        }
    }

    private void HandleJump()
    {
        // 점프 입력은 OnJumpPerformed에서 처리
        // 여기서는 추가적인 점프 로직이 필요할 경우 구현
    }

    private void UpdateGroundedState()
    {
        _wasGroundedLastFrame = IsGrounded;

        // 지면에서 떨어진 순간 코요테 타임 시작
        if (_wasGroundedLastFrame && !IsGrounded)
        {
            _coyoteTimeRemaining = _coyoteTimeDurationSeconds;
        }
    }

    private void UpdateCoyoteTime()
    {
        if (_coyoteTimeRemaining > 0f)
        {
            _coyoteTimeRemaining -= Time.deltaTime;
        }
    }

    private void ApplyMovement()
    {
        if (_characterController == null) return;

        Vector3 movement = _currentVelocity * Time.deltaTime;
        _characterController.Move(movement);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        _currentMoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _currentMoveInput = Vector2.zero;
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        // 지면에 있거나 코요테 타임 내에서만 점프 가능
        if (IsGrounded || _coyoteTimeRemaining > 0f)
        {
            _currentVelocity.y = _jumpSpeedUnitsPerSecond;
            _coyoteTimeRemaining = 0f; // 코요테 타임 소모
        }
    }

    private void OnMouseClicked(InputAction.CallbackContext context)
    {
        Debug.Log("[NewPlayerController] Mouse Clicked");
    }

    private void OnMouseRightPressed(InputAction.CallbackContext context)
    {
        Debug.Log("[NewPlayerController] Mouse Right Pressed");
    }

    private void OnMouseRightCancled(InputAction.CallbackContext context)
    {
        Debug.Log("[NewPlayerController] Mouse Right Cancled");
    }
    #endregion
}
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class NewPlayerController : MonoBehaviour, IReSpawnable, IPlayerInputProvider
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
    [SuffixLabel("units/sec")]
    [SerializeField] private float _moveSpeedUnitsPerSecond = 5f;

    [TabGroup("Movement", "Basic")]
    [SuffixLabel("units/sec")]
    [SerializeField] private float _maxSpeedUnitsPerSecond = 10f;

    //[TabGroup("Movement", "Basic")]
    //[PropertyRange(0f, 720f)]
    //[SuffixLabel("degrees/sec")]
    //[SerializeField] private float _characterRotateSpeed = 180f;

    [TabGroup("Gravity")]
    [SerializeField] private bool _usePhysicsScale = true;

    [TabGroup("Gravity")]
    [ShowIf("@_usePhysicsScale == false")]
    [SerializeField] private float _gravityScale = 9.81f;

    [TabGroup("Movement", "Air")]
    [Header("Air Movement")]
    [PropertyRange(0f, 1f)]
    [SuffixLabel("multiplier")]
    [SerializeField] private float _airControlMultiplier = 0.5f;

    [TabGroup("Jump")]
    [Header("Jump Settings")]
    [PropertyRange(1f, 100f)]
    [SuffixLabel("units")]
    [SerializeField] private float _jumpHeight = 10.0f;

    [TabGroup("Jump")]
    [PropertyRange(0.1f, 1f)]
    [SuffixLabel("seconds")]
    [SerializeField] private float _coyoteTimeDurationSeconds = 0.2f;

    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsMoving => _currentVelocity.magnitude > 0.1f;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsGrounded => _isGrounded;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentVelocityUnitsPerSecond => _currentVelocity;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector2 CurrentMoveInput => _currentMoveInput;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 LastSpawnPosition => _lastSpawnPosition;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 LastGroudnNormal => _lastGroundNormal;

    #endregion

    #region Private Fields
    private Vector3 _currentVelocity = Vector3.zero;
    private Vector2 _currentMoveInput = Vector2.zero;
    private Vector3 _lastSpawnPosition = Vector3.zero;
    private Vector3 _lastGroundNormal = Vector3.up;
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private float _coyoteTimeRemaining = 0f;

    private bool _isGrounded = false;
    private bool _isFiring = false;
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
        _isFiring = false;
    }

    private void Update()
    {
        if(_isFiring)
        {
            Fire();
        }

        UpdateGroundedState();
        UpdateCoyoteTime();
        HandleMovement();
        ApplyMovement();
    }

    private void OnEnable()
    {
        EnableInput();
        _isFiring = false;
    }

    private void OnDisable()
    {
        DisableInput();
        _isFiring = false;
    }

    //private void OnControllerColliderHit(ControllerColliderHit hit)
    //{
    //    if(_characterController.isGrounded)
    //    {
    //        _lastGroundNormal = hit.normal;
    //    }
    //}
    #endregion

    #region IInputEventProvider Implementation
    /// <summary>
    /// 조준 모드 시작 이벤트
    /// </summary>
    public event Action OnAimModeStarted;

    /// <summary>
    /// 조준 모드 종료 이벤트
    /// </summary>
    public event Action OnAimModeEnded;

    /// <summary>
    /// 발사 이벤트
    /// </summary>
    public event Action OnFire;
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

        _inputs.Player.Enable();
        _inputs.Player.Move.performed += OnMovePerformed;
        _inputs.Player.Move.canceled += OnMoveCanceled;
        _inputs.Player.Jump.performed += OnJumpPerformed;
        _inputs.Player.Zoom.performed += OnMouseRightPressed;
        _inputs.Player.Zoom.canceled += OnMouseRightCancled;
        _inputs.Player.Fire.performed += OnMouseClicked;
        _inputs.Player.Fire.canceled += OnMouseClickCanceled;
    }

    /// <summary>
    /// 입력 비활성화
    /// </summary>
    public void DisableInput()
    {
        if (_inputs == null) return;

        _inputs.Player.Disable();
        _inputs.Player.Move.performed -= OnMovePerformed;
        _inputs.Player.Move.canceled -= OnMoveCanceled;
        _inputs.Player.Jump.performed -= OnJumpPerformed;
        _inputs.Player.Zoom.performed -= OnMouseRightPressed;
        _inputs.Player.Zoom.canceled -= OnMouseRightCancled;
        _inputs.Player.Fire.performed -= OnMouseClicked;
        _inputs.Player.Fire.canceled -= OnMouseClickCanceled;


        _currentMoveInput = Vector2.zero;
    }



    /// <summary>
    /// 이동 속도 설정
    /// </summary>
    /// <param name="speedUnitsPerSecond">이동 속도</param>
    public void SetMoveSpeed(float speedUnitsPerSecond)
    {
        _moveSpeedUnitsPerSecond = Mathf.Clamp(speedUnitsPerSecond, 0.1f, _maxSpeedUnitsPerSecond);
    }

    /// <summary>
    /// 점프 속도 설정
    /// </summary>
    /// <param name="jumpSpeedUnitsPerSecond">점프 속도</param>
    public void SetJumpHeight(float jumpHeight)
    {
        _jumpHeight = jumpHeight;
    }
    #endregion

    #region Public Methods - Movement Input Control
    /// <summary>
    /// 움직임 입력 활성화 (이동, 점프)
    /// </summary>
    public void EnableMovementInput()
    {
        if (_inputs == null) return;

        _inputs.Player.Move.performed -= OnMovePerformed;
        _inputs.Player.Move.performed += OnMovePerformed;
        _inputs.Player.Move.canceled -= OnMoveCanceled;
        _inputs.Player.Move.canceled += OnMoveCanceled;
        _inputs.Player.Jump.performed -= OnJumpPerformed;
        _inputs.Player.Jump.performed += OnJumpPerformed;
    }

    /// <summary>
    /// 움직임 입력 비활성화 (이동, 점프)
    /// </summary>
    public void DisableMovementInput()
    {
        if (_inputs == null) return;

        _inputs.Player.Move.performed -= OnMovePerformed;
        _inputs.Player.Move.canceled -= OnMoveCanceled;
        _inputs.Player.Jump.performed -= OnJumpPerformed;

        // 현재 입력 상태 초기화
        _currentMoveInput = Vector2.zero;
    }
    #endregion

    #region Public Methods - Battle
    public void Fire()
    {
        OnFire?.Invoke();
        //Debug.Log("[NewPlayerController] Firing...");
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

        // 중력 적용 (지면이 아닐 때만)
        if (!_characterController.isGrounded)
        {
            if (_usePhysicsScale)
            {
                _currentVelocity.y += Physics.gravity.y * Time.deltaTime;
            }
            else
            {
                _currentVelocity.y -= _gravityScale * Time.deltaTime;
            }
        }
        else if (_currentVelocity.y < 0)
        {
            _currentVelocity.y = 0f;
        }

        if (_currentMoveInput.magnitude < 0.1f)
        {
            // 입력이 없으면 감속
            _currentVelocity = new Vector3(0, _currentVelocity.y, 0);
            //float dampingSpeed = IsGrounded ? _groundDampingSpeed : _airDampingSpeed;
            //_currentVelocity.x = Mathf.Lerp(_currentVelocity.x, 0f, dampingSpeed * Time.deltaTime);
            //_currentVelocity.z = Mathf.Lerp(_currentVelocity.z, 0f, dampingSpeed * Time.deltaTime);
            return;
        }

        // 카메라의 Forward 벡터에서 Y축을 제외하여 평면상의 전방 벡터를 얻음
        Vector3 cameraForward = Vector3.Scale(_camera.transform.forward, new Vector3(1, 0, 1)).normalized;
        // 카메라의 Right 벡터는 이미 평면상에 있으므로 그대로 사용
        Vector3 cameraRight = _camera.transform.right;

        // 입력 방향을 카메라의 전방 및 우측 벡터를 기준으로 변환
        Vector3 moveDirection = (cameraForward * _currentMoveInput.y + cameraRight * _currentMoveInput.x).normalized;

        //// 캐릭터 회전
        //if (moveDirection.magnitude > 0.1f) // 입력이 있을 때만 회전
        //{
        //    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        //    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _characterRotateSpeed * Time.deltaTime);
        //}

        // 이동 속도 계산
        float currentMoveSpeed = _moveSpeedUnitsPerSecond;
        if (!IsGrounded)
        {
            currentMoveSpeed *= _airControlMultiplier;
        }

        // 입력 방향으로 이동
        Vector3 inputMoveVelocity = moveDirection * currentMoveSpeed;
        _currentVelocity.x = inputMoveVelocity.x;
        _currentVelocity.z = inputMoveVelocity.z;

        // 최대 속도 제한
        Vector3 horizontalVelocity = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z);
        if (horizontalVelocity.magnitude > _maxSpeedUnitsPerSecond)
        {
            horizontalVelocity = horizontalVelocity.normalized * _maxSpeedUnitsPerSecond;
            _currentVelocity.x = horizontalVelocity.x;
            _currentVelocity.z = horizontalVelocity.z;
        }

    }

    private void UpdateGroundedState()
    {
        bool previousGrounded = _isGrounded;
        _isGrounded = _characterController.isGrounded;

        // 지면에서 떨어진 순간 코요테 타임 시작
        if (previousGrounded && !_isGrounded)
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
        if (_characterController == null) 
            return;

        Vector3 movement = _currentVelocity * Time.deltaTime;
        _characterController.Move(movement);
    }
    #endregion

    #region Input Callbacks
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
            if (_usePhysicsScale)
                _currentVelocity.y = Mathf.Sqrt(_jumpHeight * 2.0f * Physics.gravity.magnitude);
            else
                _currentVelocity.y = Mathf.Sqrt(_jumpHeight * 2.0f * _gravityScale);

            _coyoteTimeRemaining = 0f; // 코요테 타임 소모
        }

        Debug.Log($"Jump Performed : {_currentVelocity.y}");
    }

    private void OnMouseClicked(InputAction.CallbackContext context)
    {
        _isFiring = true;
        Debug.Log("[NewPlayerController] Mouse Clicked");
        //Fire();
    }

    private void OnMouseClickCanceled(InputAction.CallbackContext context)
    {
        _isFiring = false;
    }

    private void OnMouseRightPressed(InputAction.CallbackContext context)
    {
        OnAimModeStarted?.Invoke();
        Debug.Log("[NewPlayerController] Mouse Right Pressed");
    }

    private void OnMouseRightCancled(InputAction.CallbackContext context)
    {
        OnAimModeEnded?.Invoke();
        Debug.Log("[NewPlayerController] Mouse Right Cancled");
    }
    #endregion

}
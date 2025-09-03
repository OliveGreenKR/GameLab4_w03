using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IReSpawnable
{
    #region References
    [SerializeField] private InputSystem_Actions _inputs;
    [SerializeField] private Rigidbody _rigid;
    [SerializeField] Camera _camera = null;
    #endregion
    public bool IsMoving => _isMoving;

    [Header("Player State")]
    [SerializeField] public bool IsGrounded = false;
    [SerializeField] private bool _isMoving = false;
    [SerializeField] private Vector3 _currentVelocity;

    [Header("Rigid Settings")]
    [SerializeField] public float GravityMultiplier = 3.0f;
    [SerializeField] public float FallingGravityMultiplier = 5.0f;

    [Header("Movement Settings")]
    [SerializeField] public float MovementSpeed = 5.0f;
    [SerializeField] public float MovementAccelInAir = 10.0f;
    [SerializeField] public float JumpImpulseAccel = 10.0f;
    [SerializeField] public float FallingMovementSpeedMultiplier = 0.5f;
    [SerializeField] public float MaxSpeed = 10.0f;
    [SerializeField] public float MaxFallSpeed = 30.0f;

    [Header("Input State")]
    [SerializeField] Vector2 _currentMoveInput = Vector2.zero;
    

    [Header("Respawn State")]
    public Vector3 LastSpawnPosition { get; private set; }

    #region Unity Life-Cycle
    private void Awake()
    {
        _inputs =  new InputSystem_Actions();
    }

    private void Start()
    {
        InitializeReferences();
    }
    private void Update()
    {
        
    }
    private void LateUpdate()
    {
        if (_rigid.linearVelocity.magnitude < 0.1f)
        {
            _rigid.linearVelocity = Vector3.zero;
            _isMoving = false;
        }
        else
        {
            _isMoving = true;
        }
    }

    private void FixedUpdate()
    {
        HandleContinuousMovement();
        ApplyMoreGravity();
        LimitMaxSpeed();  
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

        // 물리 상태 초기화
        if (_rigid != null)
        {
            _rigid.linearVelocity = Vector3.zero;
            _rigid.angularVelocity = Vector3.zero;
        }

        // 플레이어 상태 초기화
        IsGrounded = false;
        _isMoving = false;
        _currentMoveInput = Vector2.zero;

        // 스폰 위치 기록
        LastSpawnPosition = worldPosition;

        Debug.Log($"[Player] Respawned at position: {worldPosition}");

        return true;
    }
    #endregion

    #region Privates - Initialize
    private void InitializeReferences()
    {
        if(_rigid == null)
        {
            _rigid = GetComponent<Rigidbody>();
        }
        if(_camera == null)
        {
            _camera = Camera.main;
            Debug.Log("Main Camera Setted for player.");
        }
        CheckReferencesAndLog();
    }

    private void CheckReferencesAndLog()
    {
        if (_rigid == null)
        {
            Debug.LogError("[player] rigid required!");
        }
        if ( _inputs == null )
        {
            Debug.LogError("[player] input required!");
        }
        if (_camera == null)
        {
            Debug.LogError("Camera required for player!");
        }
    }

    public void EnableInput()
    {
        if (_inputs == null)
        {
            return;
        }
        _inputs.Enable();
        _inputs.Player.Move.performed += OnMovePerformed;
        _inputs.Player.Move.canceled += OnMoveCanceled;
        _inputs.Player.Jump.performed += OnJumpPerformed;
        _inputs.Player.ColorChange.performed += OnMouseClicked;


    }

    public void DisableInput()
    {
        if (_inputs == null)
        {
            return;
        }
        _inputs.Disable();
        _inputs.Player.Move.performed -= OnMovePerformed;
        _inputs.Player.Move.canceled -= OnMoveCanceled;
        _inputs.Player.Jump.performed -= OnJumpPerformed;
        _inputs.Player.ColorChange.performed -= OnMouseClicked;
        _currentMoveInput = Vector2.zero;
    }
    #endregion

    #region Input Event Handler
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        _currentMoveInput = context.ReadValue<Vector2>();
        //_isMoving = _currentMoveInput.magnitude > 0;
    }
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _currentMoveInput = Vector2.zero;
        _rigid.linearVelocity = new Vector3(0, _rigid.linearVelocity.y, 0);
        //_isMoving = false;
    }

    private void OnMouseClicked(InputAction.CallbackContext context)
    {
        GameManager.Instance.OnPlayerClicked();
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if(IsGrounded)
        {
            _rigid.linearVelocity = new Vector3(_rigid.linearVelocity.x, 0, _rigid.linearVelocity.z);
            _rigid.AddForce(Vector3.up * JumpImpulseAccel, ForceMode.VelocityChange);  
            IsGrounded = false;
        }
        
    }
    #endregion

    #region Private Methods
    private void HandleContinuousMovement()
    {

        //if (IsGrounded && _currentMoveInput == Vector2.zero)
        //{
        //    _rigid.linearVelocity = Vector3.zero;
        //    return;
        //}


        Vector3 direction = new Vector3(_currentMoveInput.x, 0.0f, _currentMoveInput.y);
        if(_camera)
        {
            // 3. 카메라의 Transform 컴포넌트에 접근하여 eulerAngles.y 값을 가져옵니다.
            Quaternion cameraRotation = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0);

            // 4. 방향 벡터를 카메라의 Y축 회전만큼 회전시킵니다.
            direction = cameraRotation * direction;
        }

        // 대각선 이동 시 속도 정규화 (방향이 있을 때만)
        if (direction.magnitude > 0.1f)
        {
            direction = direction.normalized;
        }

        Vector3 targetVelocity;

        if (IsGrounded)
        {
            targetVelocity = direction * MovementSpeed;
            _rigid.AddForce(targetVelocity, ForceMode.VelocityChange);
        }
        else
        {
            targetVelocity = direction * MovementAccelInAir * FallingMovementSpeedMultiplier;
            if (targetVelocity.magnitude < MaxSpeed)
            {
                _rigid.AddForce(targetVelocity, ForceMode.VelocityChange);
            }
            
        }
        
    }

    private void LimitMaxSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(_rigid.linearVelocity.x, 0, _rigid.linearVelocity.z);

        if (horizontalVelocity.magnitude > MaxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * MaxSpeed;
            _rigid.linearVelocity = new Vector3(horizontalVelocity.x, _rigid.linearVelocity.y, horizontalVelocity.z);
        }

        // 최대 낙하 속도 제한
        if (_rigid.linearVelocity.y < -MaxFallSpeed)
        {
            _rigid.linearVelocity = new Vector3(_rigid.linearVelocity.x, -MaxFallSpeed, _rigid.linearVelocity.z);
        }

        _currentVelocity = _rigid.linearVelocity;
    }

    private void ApplyMoreGravity()
    {
        float gravityMultiplier;

        // 하강 중일 때 더 강한 중력 적용
        if (_rigid.linearVelocity.y < 0)
        {
            gravityMultiplier = FallingGravityMultiplier;
        }
        else
        {
            gravityMultiplier = GravityMultiplier;
        }

        Vector3 extraGravity = Physics.gravity * (gravityMultiplier - 1.0f);
        _rigid.AddForce(extraGravity,ForceMode.Acceleration);
    }
    #endregion
}


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
        // ��ġ�� ȸ�� ����
        transform.position = worldPosition;
        transform.rotation = worldRotation;

        // ���� ���� �ʱ�ȭ
        if (_rigid != null)
        {
            _rigid.linearVelocity = Vector3.zero;
            _rigid.angularVelocity = Vector3.zero;
        }

        // �÷��̾� ���� �ʱ�ȭ
        IsGrounded = true;
        _isMoving = false;
        _currentMoveInput = Vector2.zero;

        // ���� ��ġ ���
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
        _isMoving = _currentMoveInput.magnitude > 0;
    }
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _currentMoveInput = Vector2.zero;
        _isMoving = false;
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
        if (!_isMoving)
        {
            if(IsGrounded)
            {
                _rigid.linearVelocity = Vector3.zero;
            }
            return;
        }

        Vector3 direction = new Vector3(_currentMoveInput.x, 0.0f, _currentMoveInput.y);
        if(_camera)
        {
            // 3. ī�޶��� Transform ������Ʈ�� �����Ͽ� eulerAngles.y ���� �����ɴϴ�.
            Quaternion cameraRotation = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0);

            // 4. ���� ���͸� ī�޶��� Y�� ȸ����ŭ ȸ����ŵ�ϴ�.
            direction = cameraRotation * direction;
        }

        // �밢�� �̵� �� �ӵ� ����ȭ (������ ���� ����)
        if (direction.magnitude > 0.1f)
        {
            direction = direction.normalized;
        }

        Vector3 targetVelocity;

        if (IsGrounded)
        {
            targetVelocity = direction * MovementSpeed;
            // Y��(����) �ӵ��� �����ϰ� ���� �ӵ��� ���� ����
            _rigid.linearVelocity = new Vector3(targetVelocity.x, _rigid.linearVelocity.y, targetVelocity.z);
        }
        else
        {
            _rigid.AddForce(direction * MovementAccelInAir * FallingMovementSpeedMultiplier, ForceMode.VelocityChange);
            //if (_rigid.linearVelocity.magnitude < MaxSpeed)
            //{
                
            //}
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

        // �ִ� ���� �ӵ� ����
        if (_rigid.linearVelocity.y < -MaxFallSpeed)
        {
            _rigid.linearVelocity = new Vector3(_rigid.linearVelocity.x, -MaxFallSpeed, _rigid.linearVelocity.z);
        }

        _currentVelocity = _rigid.linearVelocity;
    }

    private void ApplyMoreGravity()
    {
        float gravityMultiplier;

        // �ϰ� ���� �� �� ���� �߷� ����
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


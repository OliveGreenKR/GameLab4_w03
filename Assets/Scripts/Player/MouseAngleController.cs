using UnityEngine;

public class MouseAngleController : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private InputSystem_Actions _inputs;
    [SerializeField] private GameObject _angleControllerGameObject;

    [Header("Mouse Settings")]
    [SerializeField][Range(0.01f, 2.0f)] private float _deltaMultiplier = 0.05f;
    [SerializeField][Range(0.1f, 10f)] private float _mouseSensitivityX = 2f;
    [SerializeField][Range(0.1f, 10f)] private float _mouseSensitivityY = 2f;
    [SerializeField] private bool _invertYAxis = false;
    [SerializeField] private bool _invertXAxis = false;

    [Header("Reset Settings")]
    [SerializeField][Range(-180f, 180f)] private float _defaultYawDegrees = 180f;
    [SerializeField][Range(-80f, 80f)] private float _defaultPitchDegrees = 0f;

    [Header("Input Settings")]
    [SerializeField] private bool _enableMouseInput = true;
    #endregion

    #region Properties
    public bool IsMouseInputEnabled { get; private set; }
    #endregion

    #region Private Fields
    private IAngleController _angleController;
    private Vector2 _currentLookInput = Vector2.zero;
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
        IsMouseInputEnabled = _enableMouseInput;
    }

    private void Update()
    {
        if (!IsMouseInputEnabled || _angleController == null) return;

        HandleMouseInput();
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

    #region Public Methods
    /// <summary>
    /// ���콺 �Է� Ȱ��ȭ/��Ȱ��ȭ
    /// </summary>
    /// <param name="enabled">Ȱ��ȭ ����</param>
    public void SetMouseInputEnabled(bool enabled)
    {
        IsMouseInputEnabled = enabled;
    }

    /// <summary>
    /// ��Ÿ ���� ����
    /// </summary>
    /// <param name="multiplier">��Ÿ �Է� ����</param>
    public void SetDeltaMultiplier(float multiplier)
    {
        _deltaMultiplier = Mathf.Clamp(multiplier, 0.01f, 5f);
    }

    /// <summary>
    /// ���콺 �ΰ��� ����
    /// </summary>
    /// <param name="sensitivityX">X�� �ΰ���</param>
    /// <param name="sensitivityY">Y�� �ΰ���</param>
    public void SetMouseSensitivity(float sensitivityX, float sensitivityY)
    {
        _mouseSensitivityX = Mathf.Clamp(sensitivityX, 0.1f, 10f);
        _mouseSensitivityY = Mathf.Clamp(sensitivityY, 0.1f, 10f);
    }

    /// <summary>
    /// �⺻ ���� ����
    /// </summary>
    /// <param name="yawDegrees">�⺻ Yaw ����</param>
    /// <param name="pitchDegrees">�⺻ Pitch ����</param>
    public void SetDefaultAngles(float yawDegrees, float pitchDegrees)
    {
        _defaultYawDegrees = Mathf.Clamp(yawDegrees, -180f, 180f);
        _defaultPitchDegrees = Mathf.Clamp(pitchDegrees, -80f, 80f);
    }
    #endregion

    #region Private Methods
    private void InitializeReferences()
    {
        if (_angleControllerGameObject != null)
        {
            _angleController = _angleControllerGameObject.GetComponent<IAngleController>();

            if (_angleController == null)
            {
                Debug.LogError("[MouseAngleController] IAngleController not found on assigned GameObject!");
            }
        }
        else
        {
            Debug.LogError("[MouseAngleController] Angle Controller GameObject not assigned!");
        }
    }

    private void EnableInput()
    {
        if (_inputs == null) return;

        _inputs.Enable();
        _inputs.Player.Look.performed += OnLookPerformed;
        _inputs.Player.Look.canceled += OnLookCanceled;
        _inputs.Player.ResetLook.performed += OnResetLookPerformed;
    }

    private void DisableInput()
    {
        if (_inputs == null) return;

        _inputs.Disable();
        _inputs.Player.Look.performed -= OnLookPerformed;
        _inputs.Player.Look.canceled -= OnLookCanceled;
        _inputs.Player.ResetLook.performed -= OnResetLookPerformed;
    }

    private void OnLookPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        _currentLookInput = context.ReadValue<Vector2>();
    }

    private void OnLookCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        _currentLookInput = Vector2.zero;
    }

    private void OnResetLookPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (_angleController != null)
        {
            _angleController.SetAngles(_defaultYawDegrees, _defaultPitchDegrees);
        }
    }

    private void HandleMouseInput()
    {
        // ���� Look �Է°� ���
        Vector2 lookDelta = _currentLookInput;

        // ��Ÿ ���� ���� (��ü �Է°� ����)
        lookDelta *= _deltaMultiplier;

        // �ΰ��� ����
        lookDelta.x *= _mouseSensitivityX;
        lookDelta.y *= _mouseSensitivityY;

        // �� ���� ����
        if (_invertXAxis) lookDelta.x = -lookDelta.x;
        if (_invertYAxis) lookDelta.y = -lookDelta.y;

        // ���� ���� (Y���� Pitch�̹Ƿ� ����)
        _angleController.AdjustAngles(lookDelta.x, -lookDelta.y);
    }
    #endregion
}
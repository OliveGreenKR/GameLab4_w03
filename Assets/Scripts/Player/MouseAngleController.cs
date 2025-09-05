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
    /// 마우스 입력 활성화/비활성화
    /// </summary>
    /// <param name="enabled">활성화 여부</param>
    public void SetMouseInputEnabled(bool enabled)
    {
        IsMouseInputEnabled = enabled;
    }

    /// <summary>
    /// 델타 배율 설정
    /// </summary>
    /// <param name="multiplier">델타 입력 배율</param>
    public void SetDeltaMultiplier(float multiplier)
    {
        _deltaMultiplier = Mathf.Clamp(multiplier, 0.01f, 5f);
    }

    /// <summary>
    /// 마우스 민감도 설정
    /// </summary>
    /// <param name="sensitivityX">X축 민감도</param>
    /// <param name="sensitivityY">Y축 민감도</param>
    public void SetMouseSensitivity(float sensitivityX, float sensitivityY)
    {
        _mouseSensitivityX = Mathf.Clamp(sensitivityX, 0.1f, 10f);
        _mouseSensitivityY = Mathf.Clamp(sensitivityY, 0.1f, 10f);
    }

    /// <summary>
    /// 기본 각도 설정
    /// </summary>
    /// <param name="yawDegrees">기본 Yaw 각도</param>
    /// <param name="pitchDegrees">기본 Pitch 각도</param>
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
        // 현재 Look 입력값 사용
        Vector2 lookDelta = _currentLookInput;

        // 델타 배율 적용 (전체 입력값 조절)
        lookDelta *= _deltaMultiplier;

        // 민감도 적용
        lookDelta.x *= _mouseSensitivityX;
        lookDelta.y *= _mouseSensitivityY;

        // 축 반전 적용
        if (_invertXAxis) lookDelta.x = -lookDelta.x;
        if (_invertYAxis) lookDelta.y = -lookDelta.y;

        // 각도 조정 (Y축은 Pitch이므로 반전)
        _angleController.AdjustAngles(lookDelta.x, -lookDelta.y);
        Debug.Log($"MouseInput Adjust to : {lookDelta.x}, {lookDelta.y}");
    }
    #endregion
}
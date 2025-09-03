using UnityEngine;

public class PlayerGroundChecker : MonoBehaviour
{
    #region Serialized Fields
    [Header("References")]
    [SerializeField] private PlayerController _playerController = null;
    [SerializeField] private Rigidbody _playerRigidbody = null;
    [SerializeField] private Transform _footTransform = null;

    [Header("Ground Check Settings")]
    [SerializeField] private float _velocityThresholdUnitsPerSecond = 0.5f;
    [SerializeField] private float _groundCheckDistanceUnits = 0.1f;
    [SerializeField] private LayerMask _groundLayerMask = -1;

    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogging = false;
    [SerializeField] private bool _enableDebugVisualization = false;
    [SerializeField]private bool _cachedIsGrounded = false; // Inspector 표시용 캐시

    #endregion

    #region Properties
    public bool IsGrounded { get; private set; }
    public float CurrentVerticalVelocity { get; private set; }
    public bool IsVelocityConditionMet { get; private set; }
    public bool IsRaycastHitGround { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeReferences();
        IsGrounded = false;
    }

    private void Update()
    {
        CheckGroundedStatus();
    }

    private void OnDrawGizmos()
    {
        if (!_enableDebugVisualization || _footTransform == null)
            return;

        // Raycast 시각화
        Vector3 rayStart = _footTransform.position;
        Vector3 rayEnd = rayStart + Vector3.down * _groundCheckDistanceUnits;

        // 현재 상태에 따른 색상 설정
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(rayStart, rayEnd);

        // 발 위치 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rayStart, 0.05f);
    }
    #endregion

    #region Public Methods
    // 설정은 SerializeField로만 관리
    #endregion

    #region Private Methods
    private void InitializeReferences()
    {
        if (_playerController == null)
        {
            _playerController = GetComponent<PlayerController>();
            if (_playerController == null)
            {
                Debug.LogError("[PlayerGroundChecker] PlayerController not found!");
            }
        }

        if (_playerRigidbody == null)
        {
            _playerRigidbody = GetComponent<Rigidbody>();
            if (_playerRigidbody == null)
            {
                Debug.LogError("[PlayerGroundChecker] Rigidbody not found!");
            }
        }

        if (_footTransform == null)
        {
            Debug.LogError("[PlayerGroundChecker] Foot Transform is required!");
        }
    }

    private void CheckGroundedStatus()
    {
        if (_playerRigidbody == null || _footTransform == null || _playerController == null)
            return;

        // 현재 수직 속도 업데이트
        CurrentVerticalVelocity = _playerRigidbody.linearVelocity.y;

        // 속도 조건 체크
        IsVelocityConditionMet = IsVerticalVelocityNearZero();

        bool newGroundedState;

        if (IsVelocityConditionMet)
        {
            // 속도가 작을 때만 Raycast 실행
            IsRaycastHitGround = PerformGroundRaycast();
            newGroundedState = IsRaycastHitGround;
        }
        else
        {
            // 속도가 클 때는 확실히 공중 상태 (Raycast 생략)
            IsRaycastHitGround = false;
            newGroundedState = false;
        }

        // 상태가 변경되었을 때만 업데이트
        if (newGroundedState != _playerController.IsGrounded)
        {
            _playerController.IsGrounded = newGroundedState;

            if (_enableDebugLogging)
            {
                Debug.Log($"[PlayerGroundChecker] Ground state changed to: {newGroundedState}");
            }
        }

        // 캐시된 상태 업데이트 (Inspector 표시용)
        _cachedIsGrounded = _playerController.IsGrounded;

        // 디버그 로깅
        if (_enableDebugLogging)
        {
            LogDebugInfo(IsVelocityConditionMet, IsRaycastHitGround, _cachedIsGrounded);
        }
    }

    private bool IsVerticalVelocityNearZero()
    {
        return Mathf.Abs(CurrentVerticalVelocity) <= _velocityThresholdUnitsPerSecond;
    }

    private bool PerformGroundRaycast()
    {
        Vector3 rayOrigin = _footTransform.position;
        Vector3 rayDirection = Vector3.down;

        bool hitGround = Physics.Raycast(
            rayOrigin,
            rayDirection,
            _groundCheckDistanceUnits,
            _groundLayerMask
        );

        return hitGround;
    }

    private void LogDebugInfo(bool velocityCondition, bool raycastHit, bool finalResult)
    {
        Debug.Log($"[PlayerGroundChecker] Velocity: {CurrentVerticalVelocity:F2} " +
                 $"(Condition: {velocityCondition}), " +
                 $"Raycast: {raycastHit}, " +
                 $"Final: {finalResult}");
    }
    #endregion
}
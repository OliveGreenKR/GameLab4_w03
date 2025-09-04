using UnityEngine;
using System.Collections.Generic;

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

    [Header("Moving Platform Settings")]
    [SerializeField] private List<string> _movingPlatformTagNames = new List<string>();

    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogging = false;
    [SerializeField] private bool _enableDebugVisualization = false;
    [SerializeField] private bool _cachedIsGrounded = false;

    #endregion

    #region Properties
    public bool IsGrounded { get; private set; }
    public float CurrentVerticalVelocity { get; private set; }
    public bool IsVelocityConditionMet { get; private set; }
    public bool IsRaycastHitGround { get; private set; }
    public bool IsAttachedToMovingPlatform { get; private set; }
    public GameObject CurrentGroundObject { get; private set; }
    #endregion

    #region Private Fields
    private RaycastHit _cachedGroundHit;
    private bool _hasValidGroundHit = false;
    private Transform _originalParentTransform = null;
    private HashSet<string> _movingPlatformTagSet = new HashSet<string>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeReferences();
        InitializeMovingPlatformTags();
        IsGrounded = false;
        IsAttachedToMovingPlatform = false;
        _originalParentTransform = gameObject.transform.parent;
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
        Color gizmoColor;
        if (IsAttachedToMovingPlatform)
        {
            gizmoColor = Color.magenta; // 이동 플랫폼에 부착된 상태
        }
        else if (IsGrounded)
        {
            gizmoColor = Color.green; // 일반 지면 상태
        }
        else
        {
            gizmoColor = Color.red; // 공중 상태
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(rayStart, rayEnd);

        // 발 위치 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(rayStart, 0.05f);

        // 현재 지면 객체가 있다면 연결선 표시
        if (_hasValidGroundHit && CurrentGroundObject != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rayStart, _cachedGroundHit.point);
        }
    }
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

    private void InitializeMovingPlatformTags()
    {
        _movingPlatformTagSet.Clear();

        foreach (string tagName in _movingPlatformTagNames)
        {
            if (!string.IsNullOrEmpty(tagName))
            {
                _movingPlatformTagSet.Add(tagName);
            }
        }

        if (_enableDebugLogging)
        {
            Debug.Log($"[PlayerGroundChecker] Initialized {_movingPlatformTagSet.Count} moving platform tags");
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
            HandleGroundStateChange(newGroundedState);
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
            out _cachedGroundHit,
            _groundCheckDistanceUnits,
            _groundLayerMask
        );

        if (hitGround)
        {
            _hasValidGroundHit = true;
            CurrentGroundObject = _cachedGroundHit.collider.gameObject;
        }
        else
        {
            _hasValidGroundHit = false;
            CurrentGroundObject = null;
        }

        return hitGround;
    }

    private void HandleGroundStateChange(bool newGroundedState)
    {
        _playerController.IsGrounded = newGroundedState;

        if (newGroundedState)
        {
            // 지면에 닿았을 때
            if (_hasValidGroundHit && CurrentGroundObject != null)
            {
                if (IsMovingPlatform(CurrentGroundObject))
                {
                    AttachToMovingPlatform(CurrentGroundObject);
                }
            }
        }
        else
        {
            // 지면에서 벗어났을 때
            if (IsAttachedToMovingPlatform)
            {
                DetachFromMovingPlatform();
            }
        }

        if (_enableDebugLogging)
        {
            Debug.Log($"[PlayerGroundChecker] Ground state changed to: {newGroundedState}");
        }
    }

    private void AttachToMovingPlatform(GameObject groundObject)
    {
        if (groundObject == null || IsAttachedToMovingPlatform)
            return;

        // 플레이어를 지면 객체의 자식으로 설정
        gameObject.transform.SetParent(groundObject.transform);
        //gameObject.transform.SetParent(groundObject.transform.parent); // 원래 부모 유지
        IsAttachedToMovingPlatform = true;


        if (_enableDebugLogging)
        {
            Debug.Log($"[PlayerGroundChecker] Attached to moving platform: {groundObject.name}");
        }
    }

    private void DetachFromMovingPlatform()
    {
        if (!IsAttachedToMovingPlatform)
            return;

        // 플레이어를 원래 부모로 복원
        gameObject.transform.SetParent(_originalParentTransform);
        IsAttachedToMovingPlatform = false;
        CurrentGroundObject = null;

        if (_enableDebugLogging)
        {
            Debug.Log("[PlayerGroundChecker] Detached from moving platform");
        }
    }

    private bool IsMovingPlatform(GameObject targetObject)
    {
        if (targetObject == null || _movingPlatformTagSet.Count == 0)
            return false;

        return _movingPlatformTagSet.Contains(targetObject.tag);
    }

    private void LogDebugInfo(bool velocityCondition, bool raycastHit, bool finalResult)
    {
        string platformInfo = IsAttachedToMovingPlatform ?
            $" [Platform: {CurrentGroundObject?.name}]" : "";

        Debug.Log($"[PlayerGroundChecker] Velocity: {CurrentVerticalVelocity:F2} " +
                 $"(Condition: {velocityCondition}), " +
                 $"Raycast: {raycastHit}, " +
                 $"Final: {finalResult}" +
                 platformInfo);
    }
    #endregion
}
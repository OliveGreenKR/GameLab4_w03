using Sirenix.OdinInspector;
using UnityEngine;

public class ThirdPersonCameraController : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("Target")]
    [Header("Follow Target")]
    [Required]
    [SerializeField] private Transform _targetTransform;

    [TabGroup("Target")]
    [Header("Tracking Options")]
    [SerializeField] private bool _enablePositionTracking = true;
    [TabGroup("Target")]
    [SerializeField] private bool _enableRotationTracking = true;

    [TabGroup("Offset", "Rotation")]
    [Header("Rotation Offset")]
    [SuffixLabel("degrees")]
    [SerializeField] private Vector3 _offsetRotationDegrees = Vector3.zero;

    [TabGroup("Offset", "Distance")]
    [Header("Distance Offset")]
    [InfoBox("Forward(Z), Right(X), Up(Y) relative to target rotation + offset rotation")]
    [SuffixLabel("units")]
    [SerializeField] private Vector3 _offsetDistance = new Vector3(0f, 2f, -5f);

    [TabGroup("Damping", "Position")]
    [Header("Position Damping")]
    [SuffixLabel("units per second")]
    [SerializeField] private Vector3 _positionDampingSpeed = new Vector3(10f, 10f, 10f);

    [TabGroup("Damping", "Rotation")]
    [Header("Rotation Damping")]
    [SuffixLabel("degrees per second")]
    [SerializeField] private Vector3 _rotationDampingSpeed = new Vector3(90f, 90f, 90f);

    [TabGroup("Damping", "Settings")]
    [Header("Damping Settings")]
    [InfoBox("Prevents overshooting when close to target")]
    [SerializeField] private float _dampingThresholdRatio = 0.05f;

    [TabGroup("Threshold")]
    [Header("Tracking Thresholds")]
    [SuffixLabel("units")]
    [Range(0.0001f, 1.0f)]
    [SerializeField] private float _positionThreshold = 0.001f;

    [TabGroup("Threshold")]
    [SuffixLabel("degrees")]
    [Range(0.0001f, 1.0f)]
    [SerializeField] private float _rotationThreshold = 0.001f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Transform TargetTransform => _targetTransform;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 TargetWorldPosition => _targetWorldPosition;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 TargetWorldRotationDegrees => _targetWorldRotationDegrees;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentWorldPosition => transform.position;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Vector3 CurrentWorldRotationDegrees => transform.rotation.eulerAngles;
    #endregion

    #region Private Fields
    private Vector3 _targetWorldPosition;
    private Vector3 _targetWorldRotationDegrees;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (_targetTransform != null)
        {
            CalculateTargetTransform();
        }
        else
        {
            Debug.LogError("[ThirdPersonCameraController] Target Transform not assigned!");
        }
    }

    private void Update()
    {
        if (_targetTransform == null) return;

        CalculateTargetTransform();
    }

    private void LateUpdate()
    {
        if (_targetTransform == null) return;
        TrackToTarget();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 추적할 타겟 설정
    /// </summary>
    /// <param name="target">추적할 Transform</param>
    public void SetTarget(Transform target)
    {
        _targetTransform = target;
    }

    /// <summary>
    /// 회전 오프셋 설정
    /// </summary>
    /// <param name="rotationDegrees">회전 오프셋 (degrees)</param>
    public void SetOffsetRotation(Vector3 rotationDegrees)
    {
        _offsetRotationDegrees = rotationDegrees;
    }

    /// <summary>
    /// 거리 오프셋 설정
    /// </summary>
    /// <param name="distance">거리 오프셋 (Forward, Right, Up)</param>
    public void SetOffsetDistance(Vector3 distance)
    {
        _offsetDistance = distance;
    }

    /// <summary>
    /// 댐핑 속도 설정
    /// </summary>
    /// <param name="positionDamping">위치 댐핑 속도</param>
    /// <param name="rotationDamping">회전 댐핑 속도</param>
    public void SetDampingSpeed(Vector3 positionDamping, Vector3 rotationDamping)
    {
        _positionDampingSpeed = positionDamping;
        _rotationDampingSpeed = rotationDamping;
    }
    #endregion

    #region Private Methods
    private void CalculateTargetTransform()
    {
        if (_targetTransform == null) return;

        // 타겟의 회전에 오프셋 회전 적용
        Quaternion targetRotation = _targetTransform.rotation;
        Quaternion offsetRotation = Quaternion.Euler(_offsetRotationDegrees);
        Quaternion finalRotation = targetRotation * offsetRotation;

        // 최종 회전에서 오프셋 거리 적용하여 위치 계산
        Vector3 offsetDirection = finalRotation * _offsetDistance;
        Vector3 finalPosition = _targetTransform.position + offsetDirection;

        // 타겟 값 설정
        _targetWorldPosition = finalPosition;
        _targetWorldRotationDegrees = finalRotation.eulerAngles;
    }

    private void TrackToTarget()
    {
        if (_enablePositionTracking)
        {
            TrackPosition();
        }

        if (_enableRotationTracking)
        {
            TrackRotation();
        }
    }

    private void TrackPosition()
    {
        Vector3 currentPosition = transform.position;
        float positionDistance = Vector3.Distance(currentPosition, _targetWorldPosition);

        if (positionDistance > _positionThreshold)
        {
            // 각 축별로 개별 댐핑 적용
            Vector3 dampingSpeed = new Vector3(
                _positionDampingSpeed.x * Time.deltaTime,
                _positionDampingSpeed.y * Time.deltaTime,
                _positionDampingSpeed.z * Time.deltaTime
            );

            Vector3 nextPosition = new Vector3(
                Mathf.Lerp(currentPosition.x, _targetWorldPosition.x, dampingSpeed.x),
                Mathf.Lerp(currentPosition.y, _targetWorldPosition.y, dampingSpeed.y),
                Mathf.Lerp(currentPosition.z, _targetWorldPosition.z, dampingSpeed.z)
            );

            float stepDistance = Vector3.Distance(currentPosition, nextPosition);
            float avgDampingSpeed = (_positionDampingSpeed.x + _positionDampingSpeed.y + _positionDampingSpeed.z) / 3f;

            // 진동 방지: 스텝 거리가 댐핑 속도 기반 임계값보다 작으면 즉시 이동
            if (stepDistance < avgDampingSpeed * _dampingThresholdRatio * Time.deltaTime)
            {
                transform.position = _targetWorldPosition;
            }
            else
            {
                transform.position = nextPosition;
            }
        }
    }

    private void TrackRotation()
    {
        Quaternion targetRotation = Quaternion.Euler(_targetWorldRotationDegrees);
        Quaternion currentRotation = transform.rotation;

        float rotationAngle = Quaternion.Angle(currentRotation, targetRotation);

        if (rotationAngle > _rotationThreshold)
        {
            // 각 축별로 개별 댐핑 적용
            Vector3 dampingSpeed = new Vector3(
                _rotationDampingSpeed.x * Time.deltaTime,
                _rotationDampingSpeed.y * Time.deltaTime,
                _rotationDampingSpeed.z * Time.deltaTime
            );

            Vector3 currentEuler = currentRotation.eulerAngles;
            Vector3 targetEuler = _targetWorldRotationDegrees;

            // 각 축별 각도 차이 계산 (최단 경로)
            float deltaX = Mathf.DeltaAngle(currentEuler.x, targetEuler.x);
            float deltaY = Mathf.DeltaAngle(currentEuler.y, targetEuler.y);
            float deltaZ = Mathf.DeltaAngle(currentEuler.z, targetEuler.z);

            Vector3 nextEuler = new Vector3(
                currentEuler.x + deltaX * dampingSpeed.x,
                currentEuler.y + deltaY * dampingSpeed.y,
                currentEuler.z + deltaZ * dampingSpeed.z
            );

            Quaternion nextRotation = Quaternion.Euler(nextEuler);
            float stepAngle = Quaternion.Angle(currentRotation, nextRotation);
            float avgDampingSpeed = (_rotationDampingSpeed.x + _rotationDampingSpeed.y + _rotationDampingSpeed.z) / 3f;

            // 진동 방지: 스텝 각도가 댐핑 속도 기반 임계값보다 작으면 즉시 회전
            if (stepAngle < avgDampingSpeed * _dampingThresholdRatio * Time.deltaTime)
            {
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = nextRotation;
            }
        }
    }
    #endregion
}
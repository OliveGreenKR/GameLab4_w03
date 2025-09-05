using Sirenix.OdinInspector;
using UnityEngine;

public class DampingController : MonoBehaviour, IAngleController
{
    [TabGroup("Target", "Transform")]
    [SerializeField] private Vector3 _targetWorldPosition = Vector3.zero;
    [TabGroup("Target", "Transform")]
    [SerializeField] private Vector3 _targetWorldRotationDegrees = Vector3.zero;
    [TabGroup("Target", "Transform")]
    [SerializeField] private Vector3 _targetWorldScale = Vector3.zero;

    [TabGroup("Damping", "Settings")]
    [SuffixLabel("units per seconds")]
    [SerializeField, Range(0f, 50.0f)] private float _positionDampingSpeed = 10.0f;
    [TabGroup("Damping", "Settings")]
    [SuffixLabel("degrees per seconds")]
    [SerializeField, Range(0f, 360.0f)] private float _rotationDampingSpeed = 10.0f;
    [TabGroup("Damping", "Settings")]
    [SuffixLabel("units per seconds")]
    [SerializeField, Range(0f, 50.0f)] private float _scaleDampingSpeed = 10.0f;

    [TabGroup("Damping", "Settings")]
    [InfoBox("When the distance to the target is less than this ratio of the damping speed, the damping will be disabled to prevent overshooting.")]
    [SerializeField] private float _DampingThresholdRatio = 0.05f;

    [TabGroup("Threshold", "Settings")]
    [SuffixLabel("units")]
    [Range(0.0001f, 1.0f)]
    [SerializeField] private float _positionThreshold = 0.001f;

    [TabGroup("Threshold", "Settings")]
    [SuffixLabel("degrees")]
    [Range(0.0001f, 1.0f)]
    [SerializeField] private float _rotationDegreesThreshold = 0.001f;


    void Start()
    {
        InitilalizeTarget();
    }

    void Update()
    {
        TrackToTarget();
    }

    #region Initialize  
    private void InitilalizeTarget()
    {
        _targetWorldPosition = transform.position;
        _targetWorldRotationDegrees = transform.rotation.eulerAngles;
        _targetWorldScale = transform.lossyScale;
    }
    #endregion

    #region IAngleController Implementation
    public void SetAngles(float yawDegrees, float pitchDegrees)
    {
        _targetWorldRotationDegrees.y = yawDegrees;
        _targetWorldRotationDegrees.x = pitchDegrees;
    }

    public void AdjustAngles(float deltaYawDegrees, float deltaPitchDegrees)
    {
        _targetWorldRotationDegrees.y += deltaYawDegrees;
        _targetWorldRotationDegrees.x += deltaPitchDegrees;
        // Pitch 제한 (-80 ~ 80)
        _targetWorldRotationDegrees.x = Mathf.Clamp(_targetWorldRotationDegrees.x, -89f, 89f);
        // Yaw를 -180 ~ 180 범위로 유지
        if (_targetWorldRotationDegrees.y > 180f)
        {
            _targetWorldRotationDegrees.y -= 360f;
        }
        else if (_targetWorldRotationDegrees.y < -180f)
        {
            _targetWorldRotationDegrees.y += 360f;
        }
    }
    public Vector2 GetCurrentAngles()
    {
        return new Vector2(_targetWorldRotationDegrees.y, _targetWorldRotationDegrees.x);
    }
    #endregion

    #region Privates - Damping Tracking
    private void TrackToTarget()
    {
        TrackPosition();
        TrackRotation();
        TrackScale();
    }

    private void TrackPosition()
    {
        Vector3 currentPosition = transform.position;
        float positionDistance = Vector3.Distance(currentPosition, _targetWorldPosition);

        if (positionDistance > _positionThreshold)
        {
            Vector3 nextPosition = Vector3.Lerp(currentPosition, _targetWorldPosition, _positionDampingSpeed * Time.deltaTime);
            float stepDistance = Vector3.Distance(currentPosition, nextPosition);

            // 진동 방지: 스텝 거리가 댐핑 속도 기반 임계값보다 작으면 즉시 이동
            if (stepDistance < _positionDampingSpeed * _DampingThresholdRatio * Time.deltaTime)
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
        Vector3 currentRotationDegrees = transform.rotation.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(_targetWorldRotationDegrees);
        Quaternion currentRotation = transform.rotation;

        float rotationAngle = Quaternion.Angle(currentRotation, targetRotation);

        if (rotationAngle > _rotationDegreesThreshold)
        {
            Quaternion nextRotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationDampingSpeed * Time.deltaTime);
            float stepAngle = Quaternion.Angle(currentRotation, nextRotation);

            // 진동 방지: 스텝 각도가 댐핑 속도 기반 임계값보다 작으면 즉시 회전
            if (stepAngle < _rotationDampingSpeed * _DampingThresholdRatio * Time.deltaTime)
            {
                transform.rotation = targetRotation;
            }
            else
            {
                transform.rotation = nextRotation;
            }
        }
    }

    private void TrackScale()
    {
        Vector3 currentScale = transform.localScale;
        float scaleDistance = Vector3.Distance(currentScale, _targetWorldScale);

        if (scaleDistance > _positionThreshold) // 스케일도 position threshold 사용
        {
            Vector3 nextScale = Vector3.Lerp(currentScale, _targetWorldScale, _scaleDampingSpeed * Time.deltaTime);
            float stepDistance = Vector3.Distance(currentScale, nextScale);

            // 진동 방지: 스텝 거리가 댐핑 속도 기반 임계값보다 작으면 즉시 스케일 변경
            if (stepDistance < _scaleDampingSpeed * _DampingThresholdRatio * Time.deltaTime)
            {
                transform.localScale = _targetWorldScale;
            }
            else
            {
                transform.localScale = nextScale;
            }
        }
    }
    #endregion
}
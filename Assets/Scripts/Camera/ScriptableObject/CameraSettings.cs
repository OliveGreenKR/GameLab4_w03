using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "Scriptable Objects/CameraSettings")]
public class CameraSettings : ScriptableObject
{
    #region Serialized Fields
    [TabGroup("Position")]
    [Header("Camera Offset")]
    [InfoBox("Forward(Z), Right(X), Up(Y) relative to target")]
    [SuffixLabel("units")]
    [SerializeField] private Vector3 _offsetDistance = new Vector3(0f, 2f, -5f);

    [TabGroup("Position")]
    [SuffixLabel("degrees")]
    [SerializeField] private Vector3 _offsetRotationDegrees = Vector3.zero;

    [TabGroup("Camera")]
    [Header("Camera Properties")]
    [PropertyRange(10f, 120f)]
    [SuffixLabel("degrees")]
    [SerializeField] private float _fieldOfView = 60f;

    [TabGroup("Damping", "Position")]
    [Header("Position Damping")]
    [SerializeField] private bool _isEnablePositionDamping = true;

    [TabGroup("Damping", "Rotation")]
    [Header("Rotation Damping")]
    [SerializeField] private bool _isEnableRotationDamping = true;

    [TabGroup("Damping")]
    [Header("Position Damping")]
    [SuffixLabel("units per second")]
    [SerializeField] private Vector3 _positionDampingSpeed = new Vector3(10f, 10f, 10f);

    [TabGroup("Damping")]
    [Header("Rotation Damping")]
    [SuffixLabel("degrees per second")]
    [SerializeField] private Vector3 _rotationDampingSpeed = new Vector3(90f, 90f, 90f);
    #endregion

    #region Properties
    public bool IsEnablePositionDamping
    {
        get => _isEnablePositionDamping;
        set => _isEnablePositionDamping = value;
    }

    public bool IsEnableRotationDamping
    {
        get => _isEnableRotationDamping;
        set => _isEnableRotationDamping = value;
    }

    public Vector3 OffsetDistance
    {
        get => _offsetDistance;
        set => _offsetDistance = value;
    }

    public Vector3 OffsetRotationDegrees
    {
        get => _offsetRotationDegrees;
        set => _offsetRotationDegrees = value;
    }

    public float FieldOfView
    {
        get => _fieldOfView;
        set => _fieldOfView = value;
    }

    public Vector3 PositionDampingSpeed
    {
        get => _positionDampingSpeed;
        set => _positionDampingSpeed = value;
    }

    public Vector3 RotationDampingSpeed
    {
        get => _rotationDampingSpeed;
        set => _rotationDampingSpeed = value;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 다른 CameraSettings의 값을 복사
    /// </summary>
    /// <param name="other">복사할 CameraSettings</param>
    public void CopyFrom(CameraSettings other)
    {
        if (other == null) return;

        _offsetDistance = other._offsetDistance;
        _offsetRotationDegrees = other._offsetRotationDegrees;
        _fieldOfView = other._fieldOfView;
        _isEnablePositionDamping = other._isEnablePositionDamping;
        _isEnableRotationDamping = other._isEnableRotationDamping;
        _positionDampingSpeed = other._positionDampingSpeed;
        _rotationDampingSpeed = other._rotationDampingSpeed;
    }

    /// <summary>
    /// 새로운 CameraSettings 인스턴스 생성
    /// </summary>
    /// <returns>복사된 새 CameraSettings</returns>
    public CameraSettings Clone()
    {
        CameraSettings clone = CreateInstance<CameraSettings>();
        clone.CopyFrom(this);
        return clone;
    }

    /// <summary>
    /// 설정 검증 및 수정
    /// </summary>
    public void ValidateSettings()
    {
        _fieldOfView = Mathf.Clamp(_fieldOfView, 10f, 120f);

        _positionDampingSpeed = new Vector3(
            Mathf.Max(0f, _positionDampingSpeed.x),
            Mathf.Max(0f, _positionDampingSpeed.y),
            Mathf.Max(0f, _positionDampingSpeed.z)
        );

        _rotationDampingSpeed = new Vector3(
            Mathf.Max(0f, _rotationDampingSpeed.x),
            Mathf.Max(0f, _rotationDampingSpeed.y),
            Mathf.Max(0f, _rotationDampingSpeed.z)
        );
    }
    #endregion

    #region Private Methods
    private void OnValidate()
    {
        ValidateSettings();
    }
    #endregion
}
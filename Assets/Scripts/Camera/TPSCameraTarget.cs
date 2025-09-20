using UnityEngine;

public enum RotationSpace
{
    Local,
    World
}

public class TPSCameraTarget : MonoBehaviour, IAngleController
{
    #region Serialized Fields
    [Header("Rotation Settings")]
    [SerializeField] private RotationSpace _rotationSpace = RotationSpace.Local;

    [Header("Angle Limits")]
    [SerializeField][Range(-89f, 89f)] private float _maxPitchDegrees = 89f;
    [SerializeField][Range(-89f, 89f)] private float _minPitchDegrees = -89f;
    #endregion

    #region Properties
    public RotationSpace CurrentRotationSpace => _rotationSpace;
    public float MaxPitchDegrees => _maxPitchDegrees;
    public float MinPitchDegrees => _minPitchDegrees;
    #endregion

    #region Public Methods
    /// <summary>
    /// 현재 각도에 델타 값을 더해서 회전 적용
    /// </summary>
    /// <param name="deltaYawDegrees">Yaw 각도 증가량</param>
    /// <param name="deltaPitchDegrees">Pitch 각도 증가량</param>
    public void AdjustAngles(float deltaYawDegrees, float deltaPitchDegrees)
    {
        Vector2 currentAngles = GetCurrentAngles();
        float newYaw = currentAngles.x + deltaYawDegrees;
        float newPitch = currentAngles.y + deltaPitchDegrees;

        SetAngles(newYaw, newPitch);
    }

    /// <summary>
    /// 지정된 각도로 회전 설정
    /// </summary>
    /// <param name="yawDegrees">Yaw 각도</param>
    /// <param name="pitchDegrees">Pitch 각도</param>
    public void SetAngles(float yawDegrees, float pitchDegrees)
    {
        float clampedPitch = ClampPitch(pitchDegrees);
        float normalizedYaw = NormalizeAngle(yawDegrees);

        if (_rotationSpace == RotationSpace.Local)
        {
            ApplyLocalRotation(normalizedYaw, clampedPitch);
        }
        else
        {
            ApplyWorldRotation(normalizedYaw, clampedPitch);
        }
    }

    /// <summary>
    /// 현재 Yaw, Pitch 각도 반환
    /// </summary>
    /// <returns>x: Yaw, y: Pitch</returns>
    public Vector2 GetCurrentAngles()
    {
        return GetAnglesFromRotation(transform.rotation);
    }

    /// <summary>
    /// 회전 공간 설정
    /// </summary>
    /// <param name="space">로컬 또는 월드 공간</param>
    public void SetRotationSpace(RotationSpace space)
    {
        _rotationSpace = space;
    }

    /// <summary>
    /// Pitch 제한 설정
    /// </summary>
    /// <param name="minPitch">최소 Pitch 각도</param>
    /// <param name="maxPitch">최대 Pitch 각도</param>
    public void SetPitchLimits(float minPitch, float maxPitch)
    {
        _minPitchDegrees = Mathf.Clamp(minPitch, -89f, 89f);
        _maxPitchDegrees = Mathf.Clamp(maxPitch, -89f, 89f);

        if (_minPitchDegrees > _maxPitchDegrees)
        {
            float temp = _minPitchDegrees;
            _minPitchDegrees = _maxPitchDegrees;
            _maxPitchDegrees = temp;
        }
    }
    #endregion

    #region Private Methods
    private void ApplyLocalRotation(float yawDegrees, float pitchDegrees)
    {
        transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
    }

    private void ApplyWorldRotation(float yawDegrees, float pitchDegrees)
    {
        // 월드 축 기준 절대 회전 (로컬과 동일한 결과)
        transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
    }

    private Vector2 GetAnglesFromRotation(Quaternion rotation)
    {
        Vector3 eulerAngles = rotation.eulerAngles;

        float yaw = eulerAngles.y;
        if (yaw > 180f) yaw -= 360f;

        float pitch = eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        return new Vector2(yaw, pitch);
    }

    private float ClampPitch(float pitchDegrees)
    {
        return Mathf.Clamp(pitchDegrees, _minPitchDegrees, _maxPitchDegrees);
    }

    private float NormalizeAngle(float angleDegrees)
    {
        while (angleDegrees > 180f) angleDegrees -= 360f;
        while (angleDegrees < -180f) angleDegrees += 360f;
        return angleDegrees;
    }
    #endregion
}
using UnityEngine;

public class AngleController : MonoBehaviour, IAngleController
{
    #region Public Methods
    /// <summary>
    /// 현재 각도에 델타 값을 더해서 즉시 회전 적용
    /// </summary>
    /// <param name="deltaYawDegrees">Yaw 각도 증가량</param>
    /// <param name="deltaPitchDegrees">Pitch 각도 증가량</param>
    public void AdjustAngles(float deltaYawDegrees, float deltaPitchDegrees)
    {
        Vector3 currentEulerAngles = transform.eulerAngles;

        // 현재 각도를 -180~180 범위로 변환
        float currentYaw = currentEulerAngles.y;
        if (currentYaw > 180f) currentYaw -= 360f;

        float currentPitch = currentEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;

        // 델타 값 적용
        float newYaw = currentYaw + deltaYawDegrees;
        float newPitch = currentPitch + deltaPitchDegrees;

        // Pitch 제한 (-89 ~ 89)
        newPitch = Mathf.Clamp(newPitch, -89f, 89f);

        // Yaw를 -180 ~ 180 범위로 유지
        while (newYaw > 180f) newYaw -= 360f;
        while (newYaw < -180f) newYaw += 360f;

        // 즉시 회전 적용
        transform.rotation = Quaternion.Euler(newPitch, newYaw, 0f);
    }

    /// <summary>
    /// 지정된 각도로 즉시 회전 설정
    /// </summary>
    /// <param name="yawDegrees">Yaw 각도</param>
    /// <param name="pitchDegrees">Pitch 각도</param>
    public void SetAngles(float yawDegrees, float pitchDegrees)
    {
        // Pitch 제한
        pitchDegrees = Mathf.Clamp(pitchDegrees, -89f, 89f);

        // Yaw 범위 정규화
        while (yawDegrees > 180f) yawDegrees -= 360f;
        while (yawDegrees < -180f) yawDegrees += 360f;

        // 즉시 회전 적용
        transform.rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
    }

    /// <summary>
    /// 현재 Yaw, Pitch 각도 반환
    /// </summary>
    /// <returns>x: Yaw, y: Pitch</returns>
    public Vector2 GetCurrentAngles()
    {
        Vector3 eulerAngles = transform.eulerAngles;

        // -180~180 범위로 변환
        float yaw = eulerAngles.y;
        if (yaw > 180f) yaw -= 360f;

        float pitch = eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        return new Vector2(yaw, pitch);
    }
    #endregion
}
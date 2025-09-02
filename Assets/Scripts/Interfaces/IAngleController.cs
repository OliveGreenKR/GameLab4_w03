using UnityEngine;

/// <summary>
/// 각도 제어를 위한 인터페이스
/// </summary>
public interface IAngleController
{
    /// <summary>
    /// 각도 절대값 설정
    /// </summary>
    /// <param name="yawDegrees">수평 회전 각도 (-180 ~ 180)</param>
    /// <param name="pitchDegrees">수직 회전 각도 (-80 ~ 80)</param>
    void SetAngles(float yawDegrees, float pitchDegrees);

    /// <summary>
    /// 각도 상대값 조정 (델타)
    /// </summary>
    /// <param name="deltaYawDegrees">수평 회전 변화량</param>
    /// <param name="deltaPitchDegrees">수직 회전 변화량</param>
    void AdjustAngles(float deltaYawDegrees, float deltaPitchDegrees);

    /// <summary>
    /// 현재 각도 조회
    /// </summary>
    /// <returns>Vector2(yaw, pitch)</returns>
    Vector2 GetCurrentAngles();
}
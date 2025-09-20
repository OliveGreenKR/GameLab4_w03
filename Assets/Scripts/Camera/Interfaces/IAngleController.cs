using UnityEngine;

/// <summary>
/// ���� ��� ���� �������̽�
/// </summary>
public interface IAngleController
{
    /// <summary>
    /// ���� ���밪 ����
    /// </summary>
    /// <param name="yawDegrees">���� ȸ�� ���� (-180 ~ 180)</param>
    /// <param name="pitchDegrees">���� ȸ�� ���� (-80 ~ 80)</param>
    void SetAngles(float yawDegrees, float pitchDegrees);

    /// <summary>
    /// ���� ��밪 ���� (��Ÿ)
    /// </summary>
    /// <param name="deltaYawDegrees">���� ȸ�� ��ȭ��</param>
    /// <param name="deltaPitchDegrees">���� ȸ�� ��ȭ��</param>
    void AdjustAngles(float deltaYawDegrees, float deltaPitchDegrees);

    /// <summary>
    /// ���� ���� ��ȸ
    /// </summary>
    /// <returns>Vector2(yaw, pitch)</returns>
    Vector2 GetCurrentAngles();
}
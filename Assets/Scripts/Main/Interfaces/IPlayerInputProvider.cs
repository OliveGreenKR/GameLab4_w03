using System;

/// <summary>
/// 플레이어 입력 이벤트를 제공하는 인터페이스
/// </summary>
public interface IPlayerInputProvider
{
    /// <summary>
    /// 조준 모드 시작 이벤트
    /// </summary>
    event System.Action OnAimModeStarted;

    /// <summary>
    /// 조준 모드 종료 이벤트
    /// </summary>
    event System.Action OnAimModeEnded;

    /// <summary>
    /// 발사 이벤트
    /// </summary>
    event System.Action OnFire;
}

using UnityEngine;

using System;

/// <summary>
/// 입력 이벤트를 제공하는 인터페이스
/// </summary>
public interface IInputEventProvider
{
    /// <summary>
    /// 조준 모드 시작 이벤트
    /// </summary>
    event Action OnAimModeStarted;

    /// <summary>
    /// 조준 모드 종료 이벤트
    /// </summary>
    event Action OnAimModeEnded;
}

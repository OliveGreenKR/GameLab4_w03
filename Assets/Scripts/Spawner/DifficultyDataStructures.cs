using System;
using UnityEngine;

/// <summary>
/// 난이도 진행 설정 구조체
/// 주기마다 반드시 증가하는 최소값들을 정의
/// </summary>
[Serializable]
public struct DifficultyProgression
{
    [Header("Pack Size Minimum Increase")]
    [Tooltip("주기마다 팩 사이즈 최소값 증가량")]
    public float packSizeMinIncrease;

    [Header("Enemy Stats Minimum Increase")]
    [Tooltip("주기마다 적 체력 최소값 증가량")]
    public float healthMinIncrease;

    [Tooltip("주기마다 적 이동속도 최소값 증가량")]
    public float moveSpeedMinIncrease;

    [Tooltip("주기마다 적 공격력 최소값 증가량")]
    public float attackMinIncrease;

    /// <summary>
    /// 지정된 스탯 타입의 최소값 증가량 반환
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>증가량</returns>
    public float GetMinIncrease(SpawnStatType statType)
    {
        switch (statType)
        {
            case SpawnStatType.Health:
                return healthMinIncrease;
            case SpawnStatType.MoveSpeed:
                return moveSpeedMinIncrease;
            case SpawnStatType.Attack:
                return attackMinIncrease;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 팩 사이즈 최소값 증가량 반환
    /// </summary>
    /// <returns>팩 사이즈 증가량</returns>
    public float GetPackSizeMinIncrease()
    {
        return packSizeMinIncrease;
    }
}

/// <summary>
/// 가중치 기반 최대값 업그레이드 설정
/// </summary>
[Serializable]
public struct WeightedMaxUpgrade
{
    [Header("Upgrade Target")]
    [Tooltip("업그레이드 대상 (PackSize는 특별 처리)")]
    public MaxUpgradeTarget target;

    [Header("Upgrade Settings")]
    [Tooltip("업그레이드 적용 시 증가량")]
    public float upgradeAmount;

    [Tooltip("초기 가중치")]
    public float initialWeight;

    [Header("Weight Settings")]
    [Tooltip("가중치 증가량 (주기마다)")]
    public float weightIncrease;

    [Tooltip("최대 가중치 한계")]
    public float maxWeight;
}

/// <summary>
/// 최대값 업그레이드 대상 타입
/// </summary>
public enum MaxUpgradeTarget
{
    PackSizeMax,    // 팩 사이즈 최대값
    HealthMax,      // 적 체력 최대값
    MoveSpeedMax,   // 적 이동속도 최대값
    AttackMax       // 적 공격력 최대값
}

/// <summary>
/// MaxUpgradeTarget 확장 메소드
/// </summary>
public static class MaxUpgradeTargetExtensions
{
    /// <summary>
    /// MaxUpgradeTarget을 SpawnStatType으로 변환
    /// </summary>
    /// <param name="target">업그레이드 대상</param>
    /// <returns>해당하는 SpawnStatType (PackSizeMax는 Health 반환)</returns>
    public static SpawnStatType ToSpawnStatType(this MaxUpgradeTarget target)
    {
        switch (target)
        {
            case MaxUpgradeTarget.PackSizeMax:
                return SpawnStatType.Health; // 팩 사이즈는 특별 처리
            case MaxUpgradeTarget.HealthMax:
                return SpawnStatType.Health;
            case MaxUpgradeTarget.MoveSpeedMax:
                return SpawnStatType.MoveSpeed;
            case MaxUpgradeTarget.AttackMax:
                return SpawnStatType.Attack;
            default:
                return SpawnStatType.Health;
        }
    }

    /// <summary>
    /// 팩 사이즈 관련 업그레이드인지 확인
    /// </summary>
    /// <param name="target">업그레이드 대상</param>
    /// <returns>팩 사이즈 관련이면 true</returns>
    public static bool IsPackSizeUpgrade(this MaxUpgradeTarget target)
    {
        return target == MaxUpgradeTarget.PackSizeMax;
    }
}
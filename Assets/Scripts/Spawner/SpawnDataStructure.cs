using System;
using UnityEngine;

/// <summary>
/// 스폰 스탯의 범위를 정의하는 구조체
/// </summary>
[Serializable]
public struct SpawnStatRange
{
    [Header("Health Range")]
    public float minHealth;
    public float maxHealth;

    [Header("Move Speed Range")]
    public float minMoveSpeed;
    public float maxMoveSpeed;

    [Header("Attack Range")]
    public float minAttack;
    public float maxAttack;

    /// <summary>
    /// 지정된 스탯 타입의 최소값 반환
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>최소값</returns>
    public float GetMinValue(SpawnStatType statType)
    {
        switch (statType)
        {
            case SpawnStatType.Health:
                return minHealth;
            case SpawnStatType.MoveSpeed:
                return minMoveSpeed;
            case SpawnStatType.Attack:
                return minAttack;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 지정된 스탯 타입의 최대값 반환
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>최대값</returns>
    public float GetMaxValue(SpawnStatType statType)
    {
        switch (statType)
        {
            case SpawnStatType.Health:
                return maxHealth;
            case SpawnStatType.MoveSpeed:
                return maxMoveSpeed;
            case SpawnStatType.Attack:
                return maxAttack;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 지정된 스탯 타입의 범위 내에서 랜덤 값 생성
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>랜덤 값</returns>
    public float GetRandomValue(SpawnStatType statType)
    {
        float min = GetMinValue(statType);
        float max = GetMaxValue(statType);
        return UnityEngine.Random.Range(min, max);
    }

    /// <summary>
    /// 지정된 스탯 타입의 최소값 설정
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="value">설정할 최소값</param>
    public void SetMinValue(SpawnStatType statType, float value)
    {
        switch (statType)
        {
            case SpawnStatType.Health:
                minHealth = value;
                break;
            case SpawnStatType.MoveSpeed:
                minMoveSpeed = value;
                break;
            case SpawnStatType.Attack:
                minAttack = value;
                break;
        }
    }

    /// <summary>
    /// 지정된 스탯 타입의 최대값 설정
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <param name="value">설정할 최대값</param>
    public void SetMaxValue(SpawnStatType statType, float value)
    {
        switch (statType)
        {
            case SpawnStatType.Health:
                maxHealth = value;
                break;
            case SpawnStatType.MoveSpeed:
                maxMoveSpeed = value;
                break;
            case SpawnStatType.Attack:
                maxAttack = value;
                break;
        }
    }
}
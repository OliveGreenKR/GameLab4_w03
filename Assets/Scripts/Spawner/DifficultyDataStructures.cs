using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;


/// <summary>
/// 스탯별 % 기반 증가율 설정
/// </summary>
[Serializable]
public struct StatPercentageModifier
{
    [BoxGroup("Health Modifiers")]
    [InfoBox("체력 관련 % 증가율 설정")]
    [SuffixLabel("%")]
    public float healthMinIncreasePercent;

    [BoxGroup("Health Modifiers")]
    [SuffixLabel("%")]
    public float healthMaxIncreasePercent;

    [BoxGroup("Move Speed Modifiers")]
    [InfoBox("이동속도 관련 % 증가율 설정")]
    [SuffixLabel("%")]
    public float moveSpeedMinIncreasePercent;

    [BoxGroup("Move Speed Modifiers")]
    [SuffixLabel("%")]
    public float moveSpeedMaxIncreasePercent;

    [BoxGroup("Attack Modifiers")]
    [InfoBox("공격력 관련 % 증가율 설정")]
    [SuffixLabel("%")]
    public float attackMinIncreasePercent;

    [BoxGroup("Attack Modifiers")]
    [SuffixLabel("%")]
    public float attackMaxIncreasePercent;

    /// <summary>
    /// 지정된 스탯 타입의 최소값 증가율 반환
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>% 증가율 (0.0 ~ 1.0)</returns>
    public float GetMinIncreasePercent(SpawnStatType statType)
    {
        switch (statType)
        {
            case SpawnStatType.Health:
                return healthMinIncreasePercent * 0.01f;
            case SpawnStatType.MoveSpeed:
                return moveSpeedMinIncreasePercent * 0.01f;
            case SpawnStatType.Attack:
                return attackMinIncreasePercent * 0.01f;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 지정된 스탯 타입의 최대값 증가율 반환
    /// </summary>
    /// <param name="statType">스탯 타입</param>
    /// <returns>% 증가율 (0.0 ~ 1.0)</returns>
    public float GetMaxIncreasePercent(SpawnStatType statType)
    {
        switch (statType)
        {
            case SpawnStatType.Health:
                return healthMaxIncreasePercent * 0.01f;
            case SpawnStatType.MoveSpeed:
                return moveSpeedMaxIncreasePercent * 0.01f;
            case SpawnStatType.Attack:
                return attackMaxIncreasePercent * 0.01f;
            default:
                return 0f;
        }
    }
}

/// <summary>
/// 적 타입별 개별 난이도 설정
/// </summary>
[Serializable]
public struct EnemyTypeDifficultyConfig
{
    [BoxGroup("Enemy Type")]
    [InfoBox("이 설정을 적용할 적 타입")]
    public PrefabType enemyType;

    [BoxGroup("Pack Size Modifiers")]
    [InfoBox("팩 사이즈 증가율 설정 (이 타입의 스폰 비중 조정)")]
    [SuffixLabel("%")]
    [PropertyRange(0f, 100f)]
    public float packSizeInfluencePercent;

    [BoxGroup("Stat Percentage Modifiers")]
    [InfoBox("스탯별 % 기반 증가율")]
    public StatPercentageModifier statModifiers;

    /// <summary>
    /// 팩 사이즈 영향도 반환 (0.0 ~ 1.0)
    /// </summary>
    /// <returns>팩 사이즈 영향도</returns>
    public float GetPackSizeInfluence()
    {
        return packSizeInfluencePercent * 0.01f;
    }

    /// <summary>
    /// 해당 타입의 기본 유효성 검사
    /// </summary>
    /// <returns>유효하면 true</returns>
    public bool IsValid()
    {
        return enemyType != default(PrefabType);
    }
}

/// <summary>
/// % 기반 난이도 진행 시스템
/// 기존 고정값 시스템과 병행 사용 가능
/// </summary>
[Serializable]
public struct PercentageBasedDifficultyProgression
{
    [BoxGroup("System Settings")]
    [InfoBox("% 기반 시스템 사용 여부")]
    [SerializeField] public bool usePercentageSystem;

    [BoxGroup("Fallback Settings")]
    [InfoBox("% 시스템 비활성화 시 사용할 기존 고정값 시스템")]
    [SerializeField] public DifficultyProgression fallbackProgression;

    [BoxGroup("Enemy Type Configurations")]
    [InfoBox("적 타입별 개별 난이도 설정 목록")]
    [SerializeField] public EnemyTypeDifficultyConfig[] enemyTypeConfigs;

    /// <summary>
    /// 특정 적 타입의 설정 조회
    /// </summary>
    /// <param name="enemyType">조회할 적 타입</param>
    /// <returns>해당 타입 설정, 없으면 null</returns>
    public EnemyTypeDifficultyConfig? GetConfigForEnemyType(PrefabType enemyType)
    {
        if (enemyTypeConfigs == null) return null;

        foreach (var config in enemyTypeConfigs)
        {
            if (config.enemyType == enemyType && config.IsValid())
            {
                return config;
            }
        }

        return null;
    }

    /// <summary>
    /// 모든 설정된 적 타입 목록 반환
    /// </summary>
    /// <returns>설정된 적 타입 배열</returns>
    public PrefabType[] GetConfiguredEnemyTypes()
    {
        if (enemyTypeConfigs == null) return new PrefabType[0];

        var types = new System.Collections.Generic.List<PrefabType>();
        foreach (var config in enemyTypeConfigs)
        {
            if (config.IsValid())
            {
                types.Add(config.enemyType);
            }
        }

        return types.ToArray();
    }

    /// <summary>
    /// 설정 검증
    /// </summary>
    /// <returns>검증 결과 메시지</returns>
    public string ValidateConfiguration()
    {
        if (!usePercentageSystem)
            return "% 시스템이 비활성화되어 있습니다.";

        if (enemyTypeConfigs == null || enemyTypeConfigs.Length == 0)
            return "적 타입 설정이 없습니다.";

        // 중복 타입 검사
        var seenTypes = new System.Collections.Generic.HashSet<PrefabType>();
        foreach (var config in enemyTypeConfigs)
        {
            if (!config.IsValid())
                continue;

            if (seenTypes.Contains(config.enemyType))
                return $"중복된 적 타입 설정: {config.enemyType}";

            seenTypes.Add(config.enemyType);
        }

        return "설정이 올바릅니다.";
    }
}

/// <summary>
/// 난이도 진행 설정 구조체
/// 주기마다 반드시 증가하는 최소값들을 정의
/// </summary>
[Serializable]
public struct DifficultyProgression
{
    [BoxGroup("Pack Size Mandatory Increase")]
    [InfoBox("매 주기마다 팩 사이즈 최소값이 반드시 이만큼 증가합니다.")]
    [SuffixLabel("enemies")]
    [PropertyRange(0f, 100f)]
    public float packSizeMinIncrease;

    [BoxGroup("Enemy Stats Mandatory Increase")]
    [InfoBox("매 주기마다 적 스탯 최소값들이 반드시 증가하는 양입니다. 난이도 하한선을 보장합니다.")]
    [SuffixLabel("HP")]
    [PropertyRange(0f, 100f)]
    public float healthMinIncrease;

    [BoxGroup("Enemy Stats Mandatory Increase")]
    [SuffixLabel("units/sec")]
    [PropertyRange(0f, 100f)]
    public float moveSpeedMinIncrease;

    [BoxGroup("Enemy Stats Mandatory Increase")]
    [SuffixLabel("damage")]
    [PropertyRange(0f, 100f)]
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
/// 매 주기마다 가중치에 따라 하나의 업그레이드만 선택됩니다
/// </summary>
[Serializable]
public struct WeightBasedUpgrade
{
    [BoxGroup("Upgrade Target")]
    [InfoBox("업그레이드할 대상을 선택하세요. PackSizeMax는 스폰되는 적의 수를 늘립니다.")]
    public MaxUpgradeTarget target;

    [BoxGroup("Upgrade Settings")]
    [InfoBox("업그레이드 적용 시 증가할 수치입니다.")]
    [SuffixLabel("amount")]
    public float upgradeAmount;

    [BoxGroup("Weight Settings")]
    [InfoBox("가중치가 높을수록 선택될 확률이 높습니다. 절대값이 아닌 상대적 비율입니다.")]
    [SuffixLabel("weight")]
    public float initialWeight;

    [BoxGroup("Weight Settings")]
    [InfoBox("매 주기마다 가중치가 이만큼 증가합니다. 시간이 지날수록 중요해지는 업그레이드에 높게 설정하세요.")]
    [SuffixLabel("per cycle")]
    public float weightIncrease;

    [BoxGroup("Weight Settings")]
    [InfoBox("가중치의 최대 한계값입니다. 무한정 증가를 방지합니다.")]
    [SuffixLabel("max")]
    public float maxWeight;

    /// <summary>
    /// 현재 주기의 가중치 계산
    /// </summary>
    /// <param name="currentCycle">현재 주기 (1부터 시작)</param>
    /// <returns>계산된 가중치</returns>
    public float GetCurrentWeight(int currentCycle)
    {
        float cycleWeight = initialWeight + (weightIncrease * (currentCycle - 1));
        return Mathf.Min(cycleWeight, maxWeight);
    }

    /// <summary>
    /// 업그레이드 대상이 팩 사이즈인지 확인
    /// </summary>
    public bool IsPackSizeUpgrade => target == MaxUpgradeTarget.PackSizeMax;

    /// <summary>
    /// 업그레이드 대상의 스탯 타입 반환
    /// </summary>
    public SpawnStatType GetStatType()
    {
        return target.ToSpawnStatType();
    }
}

/// <summary>
/// 최대값 업그레이드 대상 타입
/// </summary>
public enum MaxUpgradeTarget
{
    [InspectorName("Pack Size (적 수량)")]
    PackSizeMax,

    [InspectorName("Health (적 체력)")]
    HealthMax,

    [InspectorName("Move Speed (이동속도)")]
    MoveSpeedMax,

    [InspectorName("Attack (공격력)")]
    AttackMax
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

    /// <summary>
    /// 업그레이드 대상의 표시 이름 반환
    /// </summary>
    /// <param name="target">업그레이드 대상</param>
    /// <returns>표시 이름</returns>
    public static string GetDisplayName(this MaxUpgradeTarget target)
    {
        switch (target)
        {
            case MaxUpgradeTarget.PackSizeMax:
                return "팩 사이즈";
            case MaxUpgradeTarget.HealthMax:
                return "체력";
            case MaxUpgradeTarget.MoveSpeedMax:
                return "이동속도";
            case MaxUpgradeTarget.AttackMax:
                return "공격력";
            default:
                return "알 수 없음";
        }
    }
}

/// <summary>
/// 가중치 기반 업그레이드 컬렉션 검증 및 유틸리티
/// </summary>
[Serializable]
public class WeightBasedUpgradeCollection
{
    [InfoBox("가중치 시스템: 매 주기마다 가중치 비율에 따라 하나의 업그레이드만 선택됩니다.", InfoMessageType.Info)]
    [SerializeField]
    private WeightBasedUpgrade[] _upgrades;

    public WeightBasedUpgrade[] Upgrades => _upgrades;

    /// <summary>
    /// 현재 주기의 총 가중치 계산
    /// </summary>
    /// <param name="currentCycle">현재 주기</param>
    /// <returns>총 가중치 합계</returns>
    public float GetTotalWeight(int currentCycle)
    {
        if (_upgrades == null || _upgrades.Length == 0) return 0f;

        float total = 0f;
        foreach (var upgrade in _upgrades)
        {
            total += upgrade.GetCurrentWeight(currentCycle);
        }
        return total;
    }

    /// <summary>
    /// 가중치에 따라 업그레이드 선택
    /// </summary>
    /// <param name="currentCycle">현재 주기</param>
    /// <returns>선택된 업그레이드, 없으면 null</returns>
    public WeightBasedUpgrade? SelectUpgrade(int currentCycle)
    {
        if (_upgrades == null || _upgrades.Length == 0) return null;

        float totalWeight = GetTotalWeight(currentCycle);
        if (totalWeight <= 0f) return null;

        float randomPoint = UnityEngine.Random.Range(0f, totalWeight);
        float currentSum = 0f;

        foreach (var upgrade in _upgrades)
        {
            currentSum += upgrade.GetCurrentWeight(currentCycle);
            if (randomPoint <= currentSum)
            {
                return upgrade;
            }
        }

        // 안전장치: 마지막 업그레이드 반환
        return _upgrades[_upgrades.Length - 1];
    }

    /// <summary>
    /// 현재 주기의 각 업그레이드별 선택 확률 계산
    /// </summary>
    /// <param name="currentCycle">현재 주기</param>
    /// <returns>업그레이드별 확률 딕셔너리</returns>
    public Dictionary<MaxUpgradeTarget, float> GetSelectionProbabilities(int currentCycle)
    {
        var probabilities = new Dictionary<MaxUpgradeTarget, float>();

        if (_upgrades == null || _upgrades.Length == 0) return probabilities;

        float totalWeight = GetTotalWeight(currentCycle);
        if (totalWeight <= 0f) return probabilities;

        foreach (var upgrade in _upgrades)
        {
            float probability = (upgrade.GetCurrentWeight(currentCycle) / totalWeight) * 100f;
            probabilities[upgrade.target] = probability;
        }

        return probabilities;
    }

    /// <summary>
    /// 설정 검증
    /// </summary>
    /// <returns>검증 결과 메시지</returns>
    public string ValidateConfiguration()
    {
        if (_upgrades == null || _upgrades.Length == 0)
            return "업그레이드가 설정되지 않았습니다.";

        // 중복 대상 검사
        var targetCounts = new Dictionary<MaxUpgradeTarget, int>();
        foreach (var upgrade in _upgrades)
        {
            if (targetCounts.ContainsKey(upgrade.target))
                targetCounts[upgrade.target]++;
            else
                targetCounts[upgrade.target] = 1;
        }

        var duplicates = targetCounts.Where(kvp => kvp.Value > 1).ToList();
        if (duplicates.Count > 0)
        {
            string duplicateTargets = string.Join(", ", duplicates.Select(kvp => kvp.Key.GetDisplayName()));
            return $"중복된 업그레이드 대상이 있습니다: {duplicateTargets}";
        }

        return "설정이 올바릅니다.";
    }
}
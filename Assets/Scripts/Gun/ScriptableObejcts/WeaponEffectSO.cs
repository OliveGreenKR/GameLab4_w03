using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 무기 효과 ScriptableObject
/// 배율 기반으로 무기 스탯을 수정
/// </summary>
[CreateAssetMenu(fileName = "New Weapon Effect", menuName = "Weapon System/Weapon Effect")]
public class WeaponEffectSO : ScriptableObject, IWeaponEffect
{
    #region Serialized Fields
    [Header("Effect Settings")]
    [InfoBox("낮은 우선순위가 먼저 적용됩니다")]
    [SerializeField] protected int _priority = 50;

    [Header("Effect Info")]
    [SerializeField, TextArea(2, 4)]
    protected string _description = "효과 설명을 입력하세요";

    [Header("Stat Multipliers")]
    [InfoBox("발사속도 배율")]
    [SuffixLabel("x")]
    [SerializeField] private float _fireRateMultiplier = 1.0f;

    [InfoBox("정확도 배율")]
    [SuffixLabel("x")]
    [SerializeField] private float _accuracyMultiplier = 1.0f;

    [InfoBox("데미지 배율")]
    [SuffixLabel("x")]
    [SerializeField] private float _damageMultiplier = 1.0f;

    [InfoBox("반동 배율")]
    [SuffixLabel("x")]
    [SerializeField] private float _recoilMultiplier = 1.0f;
    #endregion

    #region IWeaponEffect Implementation
    public int Priority => _priority;
    public string EffectName => name;

    public WeaponStatData ApplyToWeapon(WeaponStatData baseStats)
    {
        if (!ValidateWeaponStats(baseStats))
        {
            LogEffect("Invalid weapon stats provided");
            return baseStats;
        }

        WeaponStatData modifiedStats = baseStats.ApplyMultipliers(
            _fireRateMultiplier,
            _damageMultiplier,
            _accuracyMultiplier,
            _recoilMultiplier
        );

        LogEffect($"Applied {EffectName}: FireRate {_fireRateMultiplier:F2}x, Accuracy {_accuracyMultiplier:F2}x");
        return modifiedStats;
    }

    public bool CanApplyToWeapon(WeaponStatData weaponStats)
    {
        return ValidateWeaponStats(weaponStats);
    }
    #endregion

    #region Properties
    [ShowInInspector, ReadOnly]
    public string EffectDescription => _description;

    [ShowInInspector, ReadOnly]
    public float FireRateMultiplier => _fireRateMultiplier;

    [ShowInInspector, ReadOnly]
    public float AccuracyMultiplier => _accuracyMultiplier;

    [ShowInInspector, ReadOnly]
    public float DamageMultiplier => _damageMultiplier;

    [ShowInInspector, ReadOnly]
    public float RecoilMultiplier => _recoilMultiplier;
    #endregion

    #region Unity Lifecycle
    protected virtual void OnValidate()
    {
        _priority = Mathf.Clamp(_priority, 0, 100);
        UpdateDescription();
    }
    #endregion

    #region Private Methods
    private void UpdateDescription()
    {
        _description = $"발사속도 {_fireRateMultiplier:F1}x, 정확도 {_accuracyMultiplier:F1}x, 데미지 {_damageMultiplier:F1}x, 반동 {_recoilMultiplier:F1}x";
    }

    private void LogEffect(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[{GetType().Name}] {message}", this);
#endif
    }

    private bool ValidateWeaponStats(WeaponStatData stats)
    {
        return stats.FireRate > 0f && stats.Accuracy >= 0f;
    }
    #endregion

    #region Public Methods - Description Generation
    /// <summary>무기 효과 속성을 기반으로 설명 자동 생성</summary>
    [Button("Generate Description", ButtonSizes.Medium)]
    [GUIColor(0.7f, 1f, 0.7f)]
    public void GenerateDescription()
    {
        _description = CreateAutoDescription();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>무기 효과 맞춤 설명 생성</summary>
    /// <returns>생성된 설명 텍스트</returns>
    protected virtual string CreateAutoDescription()
    {
        var description = new StringBuilder();

        // 이펙트 기본 정보
        description.AppendLine($"[무기 효과]");

        // 스탯 변화 정보 수집
        var statChanges = new List<string>();

        if (!Mathf.Approximately(_fireRateMultiplier, 1.0f))
        {
            string fireRateChange = FormatMultiplierChange(_fireRateMultiplier);
            statChanges.Add($"발사속도 {fireRateChange}");
        }

        if (!Mathf.Approximately(_damageMultiplier, 1.0f))
        {
            string damageChange = FormatMultiplierChange(_damageMultiplier);
            statChanges.Add($"데미지 {damageChange}");
        }

        if (!Mathf.Approximately(_accuracyMultiplier, 1.0f))
        {
            string accuracyChange = FormatMultiplierChange(_accuracyMultiplier);
            statChanges.Add($"정확도 {accuracyChange}");
        }

        if (!Mathf.Approximately(_recoilMultiplier, 1.0f))
        {
            string recoilChange = FormatMultiplierChange(_recoilMultiplier);
            statChanges.Add($"반동 {recoilChange}");
        }

        // 효과 표시
        if (statChanges.Count > 0)
        {
            description.AppendLine($"효과: {string.Join(", ", statChanges)}");
        }
        else
        {
            description.AppendLine("효과: 스탯 변화 없음");
        }

        // 우선순위 정보
        string priorityLevel = GetPriorityLevelDescription(_priority);
        description.Append($"우선순위: {_priority} ({priorityLevel})");

        return description.ToString().Trim();
    }

    /// <summary>배율 변화를 사용자 친화적 텍스트로 변환</summary>
    /// <param name="multiplier">배율 값</param>
    /// <returns>변화 설명 텍스트</returns>
    protected string FormatMultiplierChange(float multiplier)
    {
        if (multiplier > 1.0f)
        {
            return $"+{(multiplier - 1.0f) * 100:F0}%";
        }
        else if (multiplier < 1.0f)
        {
            return $"{(multiplier - 1.0f) * 100:F0}%";
        }
        else
        {
            return "변화없음";
        }
    }

    /// <summary>우선순위 레벨 설명 반환</summary>
    /// <param name="priority">우선순위 값</param>
    /// <returns>우선순위 설명</returns>
    protected string GetPriorityLevelDescription(int priority)
    {
        if (priority <= 20) return "최우선";
        if (priority <= 40) return "높음";
        if (priority <= 60) return "보통";
        if (priority <= 80) return "낮음";
        return "최후순";
    }
    #endregion
}
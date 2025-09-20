using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 집중 사격 무기 효과 ScriptableObject
/// 정확도와 데미지 향상, 발사속도 감소
/// </summary>
[CreateAssetMenu(fileName = "New Focus Weapon Effect", menuName = "Weapon System/Focus Effect")]
public class FocusWeaponEffectSO : WeaponEffectSO
{
    #region Serialized Fields
    [TabGroup("Focus Settings")]
    [Header("Fire Rate Reduction")]
    [InfoBox("발사속도 배율 (1.0 = 변화없음, 0.6 = 40% 감소)")]
    [SuffixLabel("x")]
    [SerializeField] private float _fireRateMultiplier = 0.6f;

    [TabGroup("Focus Settings")]
    [Header("Accuracy Improvement")]
    [InfoBox("정확도 배율 (1.0 = 변화없음, 1.2 = 20% 증가)")]
    [SuffixLabel("x")]
    [SerializeField] private float _accuracyMultiplier = 1.2f;

    [TabGroup("Focus Settings")]
    [Header("Damage Boost")]
    [InfoBox("데미지 배율 (1.0 = 변화없음, 1.1 = 10% 증가)")]
    [SuffixLabel("x")]
    [SerializeField] private float _damageMultiplier = 1.1f;

    [TabGroup("Focus Settings")]
    [Header("Recoil Reduction")]
    [InfoBox("반동 배율 (1.0 = 변화없음, 0.5 = 50% 감소)")]
    [PropertyRange(0.1f, 1.0f)]
    [SuffixLabel("x")]
    [SerializeField] private float _recoilMultiplier = 0.5f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float FireRateReduction => (1f - _fireRateMultiplier) * 100f;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float AccuracyImprovement => (_accuracyMultiplier - 1f) * 100f;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float DamageIncrease => (_damageMultiplier - 1f) * 100f;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float RecoilReduction => (1f - _recoilMultiplier) * 100f;
    #endregion

    #region WeaponEffectSO Implementation
    public override WeaponStatData ApplyToWeapon(WeaponStatData baseStats)
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

        LogEffect($"Applied Focus effect: FireRate {_fireRateMultiplier:F2}x, Accuracy {_accuracyMultiplier:F2}x");
        return modifiedStats;
    }

    public override bool CanApplyToWeapon(WeaponStatData weaponStats)
    {
        return ValidateWeaponStats(weaponStats) &&
               weaponStats.CurrentFireRate > 0.1f &&
               weaponStats.CurrentAccuracy < 100f;
    }
    #endregion

    #region Unity Lifecycle
    protected override void OnValidate()
    {
        base.OnValidate();

        UpdateDescription();
    }
    #endregion

    #region Private Methods
    private void UpdateDescription()
    {
        _description = $"집중 사격 모드: 정확도 +{AccuracyImprovement:F0}%, 발사속도 -{FireRateReduction:F0}%, 데미지 +{DamageIncrease:F0}%, 반동 -{RecoilReduction:F0}%";
    }
    #endregion
}
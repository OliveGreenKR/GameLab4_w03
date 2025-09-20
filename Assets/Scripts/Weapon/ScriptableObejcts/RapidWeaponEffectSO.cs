using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 연사 모드 무기 효과 ScriptableObject
/// 발사속도 증가, 정확도 및 반동 증가 (불안정화)
/// </summary>
[CreateAssetMenu(fileName = "New Rapid Weapon Effect", menuName = "Weapon System/Rapid Effect")]
public class RapidWeaponEffectSO : WeaponEffectSO
{
    #region Serialized Fields
    [TabGroup("Rapid Settings")]
    [Header("Fire Rate Multiplier")]
    [InfoBox("발사속도 배율")]
    [SuffixLabel("x")]
    [SerializeField] private float _fireRateMultiplier = 1.8f;

    [TabGroup("Rapid Settings")]
    [Header("Accuracy Multiplier")]
    [InfoBox("정확도 배율")]
    [SuffixLabel("x")]
    [SerializeField] private float _accuracyMultiplier = 0.7f;

    [TabGroup("Rapid Settings")]
    [Header("Damage Multiplier")]
    [InfoBox("데미지 배율")]
    [SuffixLabel("x")]
    [SerializeField] private float _damageMultiplier = 0.9f;

    [TabGroup("Rapid Settings")]
    [Header("Recoil Multiplier")]
    [InfoBox("반동 배율")]
    [SuffixLabel("x")]
    [SerializeField] private float _recoilMultiplier = 1.5f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float FireRateMultiplier => _fireRateMultiplier;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float AccuracyMultiplier => _accuracyMultiplier;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float DamageMultiplier => _damageMultiplier;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float RecoilMultiplier => _recoilMultiplier;
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

        LogEffect($"Applied Rapid effect: FireRate {_fireRateMultiplier:F2}x, Accuracy {_accuracyMultiplier:F2}x");
        return modifiedStats;
    }

    public override bool CanApplyToWeapon(WeaponStatData weaponStats)
    {
        return ValidateWeaponStats(weaponStats) &&
               weaponStats.CurrentFireRate > 0.1f &&
               weaponStats.CurrentAccuracy > 10f;
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
        _description = $"연사 모드: 발사속도 {_fireRateMultiplier:F1}x, 정확도 {_accuracyMultiplier:F1}x, 데미지 {_damageMultiplier:F1}x, 반동 {_recoilMultiplier:F1}x";
    }
    #endregion
}
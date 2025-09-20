using Sirenix.OdinInspector;
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
        return stats.CurrentFireRate > 0f && stats.CurrentAccuracy >= 0f;
    }
    #endregion
}
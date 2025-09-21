using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>무기 효과 업그레이드 전략</summary>
[CreateAssetMenu(fileName = "New Weapon Effect Upgrade", menuName = "UpgradeStrategy/Upgrade Strategy/Weapon Effect")]
public class WeaponEffectUpgradeStrategySO : BaseUpgradeStrategySO
{
    #region Serialized Fields
    [BoxGroup("Weapon Effect Settings")]
    [Header("Weapon Effect Asset")]
    [Required]
    [SerializeField] private WeaponEffectSO _weaponEffectAsset;

    [BoxGroup("Weapon Effect Settings")]
    [Header("Upgrade Target Type")]
    [SerializeField] private UpgradeType _targetUpgradeType = UpgradeType.WeaponPiercing;
    #endregion

    #region Properties
    public override UpgradeCategory Category => UpgradeCategory.WeaponEffect;
    public override UpgradeType TargetUpgradeType => _targetUpgradeType;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public WeaponEffectSO WeaponEffectAsset => _weaponEffectAsset;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public UpgradeType TargetUpgrade => _targetUpgradeType;
    #endregion

    #region BaseUpgradeStrategySO Implementation
    public override void ApplyUpgrade(IUpgradable target)
    {
        if (!CanApplyTo(target))
        {
            Debug.LogWarning($"[WeaponEffectUpgradeStrategySO] Cannot apply {_targetUpgradeType} weapon effect to target", this);
            return;
        }

        target.ApplyUpgrade(_targetUpgradeType, 1f, _applicationType, _temporaryDurationSeconds);

        Debug.Log($"[WeaponEffectUpgradeStrategySO] Applied {_weaponEffectAsset.name} weapon effect ({_applicationType})", this);
    }

    public override void RemoveUpgrade(IUpgradable target)
    {
        if (!CanApplyTo(target))
        {
            Debug.LogWarning($"[WeaponEffectUpgradeStrategySO] Cannot remove {_targetUpgradeType} weapon effect from target", this);
            return;
        }

        target.RemoveUpgrade(_targetUpgradeType, 1f);

        Debug.Log($"[WeaponEffectUpgradeStrategySO] Removed {_weaponEffectAsset.name} weapon effect", this);
    }

    public override bool CanApplyTo(IUpgradable target)
    {
        if (!base.CanApplyTo(target))
            return false;

        return target.CanReceiveUpgrade(_targetUpgradeType);
    }

    public override bool IsValid()
    {
        return base.IsValid() && _weaponEffectAsset != null;
    }
    #endregion

    #region Public Methods
    /// <summary>설정된 무기 효과 에셋 반환</summary>
    /// <returns>WeaponEffectSO 에셋</returns>
    public WeaponEffectSO GetWeaponEffectAsset()
    {
        return _weaponEffectAsset;
    }
    #endregion

    #region Unity Lifecycle
    protected override void OnValidate()
    {
        base.OnValidate();

        if (_weaponEffectAsset != null && string.IsNullOrEmpty(_displayName))
        {
            _displayName = _weaponEffectAsset.EffectName + " Effect";
        }
    }
    #endregion
}
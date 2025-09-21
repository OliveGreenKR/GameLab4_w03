using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>무기 스탯 타입</summary>
public enum WeaponStatType
{
    Damage,
    FireRate,
    Accuracy,
    Recoil,
    ProjectileSpeed,
    ProjectileLifetime
}

/// <summary>업그레이드 적용 방식</summary>
public enum UpgradeApplicationMode
{
    Add,        // 절대값 증가
    Multiply    // 배수 적용
}

/// <summary>무기 스탯 업그레이드 전략</summary>
[CreateAssetMenu(fileName = "New Weapon Stat Upgrade", menuName = "Shop/Upgrade Strategy/Weapon Stat")]
public class WeaponStatUpgradeStrategySO : BaseUpgradeStrategySO
{
    #region Serialized Fields
    [BoxGroup("Weapon Stat Settings")]
    [Header("Upgrade Target")]
    [SerializeField] private UpgradeType _targetUpgradeType = UpgradeType.WeaponDamage;

    [BoxGroup("Weapon Stat Settings")]
    [Header("Upgrade Value")]
    [SerializeField] private float _upgradeValue = 1f;

    [BoxGroup("Weapon Stat Settings")]
    [Header("Application Mode")]
    [SerializeField] private UpgradeApplicationMode _applicationMode = UpgradeApplicationMode.Add;
    #endregion

    #region Properties
    public override UpgradeType TargetUpgradeType => _targetUpgradeType;
    public override UpgradeCategory Category => UpgradeCategory.WeaponStat;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public UpgradeType TargetUpgrade => _targetUpgradeType;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float UpgradeValue => _upgradeValue;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public UpgradeApplicationMode ApplicationMode => _applicationMode;
    #endregion

    #region BaseUpgradeStrategySO Implementation
    public override void ApplyUpgrade(IUpgradable target)
    {
        if (!CanApplyTo(target))
        {
            Debug.LogWarning($"[WeaponStatUpgradeStrategySO] Cannot apply {_targetUpgradeType} upgrade to target", this);
            return;
        }

        float finalValue = CalculateFinalValue(_upgradeValue);

        target.ApplyUpgrade(_targetUpgradeType, finalValue, _applicationType, _temporaryDurationSeconds);

        Debug.Log($"[WeaponStatUpgradeStrategySO] Applied {_targetUpgradeType} upgrade: {finalValue:F2} ({_applicationMode}, {_applicationType})", this);
    }

    public override void RemoveUpgrade(IUpgradable target)
    {
        if (!CanApplyTo(target))
        {
            Debug.LogWarning($"[WeaponStatUpgradeStrategySO] Cannot remove {_targetUpgradeType} upgrade from target", this);
            return;
        }

        float finalValue = CalculateFinalValue(_upgradeValue);

        target.RemoveUpgrade(_targetUpgradeType, finalValue);

        Debug.Log($"[WeaponStatUpgradeStrategySO] Removed {_targetUpgradeType} upgrade: {finalValue:F2}", this);
    }

    public override bool CanApplyTo(IUpgradable target)
    {
        if (!base.CanApplyTo(target))
            return false;

        return target.CanReceiveUpgrade(_targetUpgradeType);
    }

    public override bool IsValid()
    {
        return base.IsValid() && _upgradeValue != 0f;
    }
    #endregion

    #region Private Methods
    private float CalculateFinalValue(float baseValue)
    {
        return baseValue;
    }
    #endregion

    #region Unity Lifecycle
    protected override void OnValidate()
    {
        base.OnValidate();

        if (_applicationMode == UpgradeApplicationMode.Multiply)
        {
            _upgradeValue = Mathf.Max(0.0f, _upgradeValue);
        }
    }
    #endregion
}
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>투사체 효과 업그레이드 전략</summary>
[CreateAssetMenu(fileName = "New Projectile Effect Upgrade", menuName = "Shop/Upgrade Strategy/Projectile Effect")]
public class ProjectileEffectUpgradeStrategySO : BaseUpgradeStrategySO
{
    #region Serialized Fields
    [BoxGroup("Projectile Effect Settings")]
    [Header("Projectile Effect Asset")]
    [Required]
    [SerializeField] private ProjectileEffectSO _projectileEffectAsset;

    [BoxGroup("Projectile Effect Settings")]
    [Header("Upgrade Target Type")]
    [SerializeField] private UpgradeType _targetUpgradeType = UpgradeType.WeaponPiercing;
    #endregion

    #region Properties
    public override UpgradeCategory Category => UpgradeCategory.ProjectileEffect;
    public override UpgradeType TargetUpgradeType => _targetUpgradeType;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public ProjectileEffectSO ProjectileEffectAsset => _projectileEffectAsset;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public UpgradeType TargetUpgrade => _targetUpgradeType;
    #endregion

    #region BaseUpgradeStrategySO Implementation
    public override void ApplyUpgrade(IUpgradable target)
    {
        if (!CanApplyTo(target))
        {
            Debug.LogWarning($"[ProjectileEffectUpgradeStrategySO] Cannot apply {_targetUpgradeType} projectile effect to target", this);
            return;
        }

        target.ApplyUpgrade(_targetUpgradeType, 1f, _applicationType, _temporaryDurationSeconds);

        Debug.Log($"[ProjectileEffectUpgradeStrategySO] Applied {_projectileEffectAsset.name} projectile effect ({_applicationType})", this);
    }

    public override void RemoveUpgrade(IUpgradable target)
    {
        if (!CanApplyTo(target))
        {
            Debug.LogWarning($"[ProjectileEffectUpgradeStrategySO] Cannot remove {_targetUpgradeType} projectile effect from target", this);
            return;
        }

        target.RemoveUpgrade(_targetUpgradeType, 1f);

        Debug.Log($"[ProjectileEffectUpgradeStrategySO] Removed {_projectileEffectAsset.name} projectile effect", this);
    }

    public override bool CanApplyTo(IUpgradable target)
    {
        if (!base.CanApplyTo(target))
            return false;

        return target.CanReceiveUpgrade(_targetUpgradeType);
    }

    public override bool IsValid()
    {
        return base.IsValid() && _projectileEffectAsset != null;
    }
    #endregion

    #region Public Methods
    /// <summary>설정된 투사체 효과 에셋 반환</summary>
    /// <returns>ProjectileEffectSO 에셋</returns>
    public ProjectileEffectSO GetProjectileEffectAsset()
    {
        return _projectileEffectAsset;
    }
    #endregion

    #region Unity Lifecycle
    protected override void OnValidate()
    {
        base.OnValidate();

        if (_projectileEffectAsset != null && string.IsNullOrEmpty(_displayName))
        {
            _displayName = _projectileEffectAsset.name + " Effect";
        }
    }
    #endregion
}
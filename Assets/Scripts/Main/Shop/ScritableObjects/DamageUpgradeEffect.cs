using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>데미지 증가 업그레이드 효과</summary>
[CreateAssetMenu(fileName = "Damage Upgrade", menuName = "Shop/Effects/Weapon Basic/Damage")]
public class DamageUpgradeEffect : UpgradeEffectSO
{
    #region Serialized Fields
    [TabGroup("Damage Settings")]
    [Header("Damage Configuration")]
    [SerializeField] private float _damageIncrease = 10f;

    [TabGroup("Damage Settings")]
    [SerializeField] private bool _isPercentageIncrease = false;

    [TabGroup("Damage Settings")]
    [ShowIf("_isPercentageIncrease")]
    [SerializeField] private float _percentageAmount = 20f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float DamageIncrease => _damageIncrease;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsPercentageIncrease => _isPercentageIncrease;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float PercentageAmount => _percentageAmount;
    #endregion

    #region Unity Lifecycle
    protected override void OnValidate()
    {
        base.OnValidate();

        if (_damageIncrease <= 0f)
        {
            _damageIncrease = 1f;
        }

        if (_isPercentageIncrease && _percentageAmount <= 0f)
        {
            _percentageAmount = 1f;
        }
    }
    #endregion

    #region Public Methods - Abstract Implementation
    /// <summary>데미지 업그레이드 효과를 적용합니다</summary>
    /// <param name="weapon">플레이어 무기 컨트롤러</param>
    /// <param name="player">플레이어 배틀 엔티티</param>
    public override void ApplyUpgrade(PlayerWeaponController weapon, PlayerBattleEntity player)
    {
        if (!ValidateTargets(weapon,player))
        {
            return;
        }

        if (_isPercentageIncrease)
        {
            float multiplier = 1f + (_percentageAmount / 100f);
            weapon.ModifyDamageMultiplier(multiplier);

            if (Application.isPlaying)
            {
                Debug.Log($"[DamageUpgradeEffect] Applied {_percentageAmount}% damage increase (multiplier: {multiplier:F2})", this);
            }
        }
        else
        {
            weapon.ModifyBaseDamage(_damageIncrease);

            if (Application.isPlaying)
            {
                Debug.Log($"[DamageUpgradeEffect] Applied +{_damageIncrease} base damage increase", this);
            }
        }
    }

    /// <summary>데미지 업그레이드 적용 가능 여부를 확인합니다</summary>
    /// <param name="weapon">플레이어 무기 컨트롤러</param>
    /// <param name="player">플레이어 배틀 엔티티</param>
    /// <returns>적용 가능하면 true</returns>
    public override bool CanApply(PlayerWeaponController weapon, PlayerBattleEntity player)
    {
        return ValidateTargets(weapon, player);
    }
    #endregion
}
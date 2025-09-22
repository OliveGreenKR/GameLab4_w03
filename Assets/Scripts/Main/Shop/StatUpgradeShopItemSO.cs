using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 수치 기반 업그레이드 상점 아이템
/// WeaponDamage, PlayerMaxHealth, MoveSpeed 등에 사용
/// </summary>
[CreateAssetMenu(fileName = "StatUpgradeItem_", menuName = "Shop System/Stat Upgrade Item", order = 1)]
public class StatUpgradeShopItemSO : ShopItemSO
{
    #region Serialized Fields
    [TabGroup("Upgrade")]
    [Header("Upgrade Value")]
    [SerializeField] private float _upgradeValue;
    #endregion

    #region Properties - Upgrade Data
    public float UpgradeValue => _upgradeValue;
    #endregion
}
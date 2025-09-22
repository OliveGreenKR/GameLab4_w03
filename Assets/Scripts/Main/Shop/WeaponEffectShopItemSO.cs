using Sirenix.OdinInspector;
using UnityEngine;



/// <summary>
/// 무기 효과 상점 아이템
/// WeaponEffectSO를 추가하는 아이템에 사용
/// </summary>
[CreateAssetMenu(fileName = "WeaponEffectItem_", menuName = "Shop System/Weapon Effect Item", order = 2)]
public class WeaponEffectShopItemSO : ShopItemSO
{
    #region Serialized Fields
    [TabGroup("Effect")]
    [Header("Weapon Effect")]
    [Required]
    [SerializeField] private WeaponEffectSO _weaponEffect;
    #endregion

    #region Properties - Effect Data
    public WeaponEffectSO WeaponEffect => _weaponEffect;
    #endregion
}
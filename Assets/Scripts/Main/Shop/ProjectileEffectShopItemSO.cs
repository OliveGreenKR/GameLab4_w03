using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 투사체 효과 상점 아이템
/// ProjectileEffectSO를 추가하는 아이템에 사용
/// </summary>
[CreateAssetMenu(fileName = "ProjectileEffectItem_", menuName = "Shop System/Projectile Effect Item", order = 3)]
public class ProjectileEffectShopItemSO : ShopItemSO
{
    #region Serialized Fields
    [TabGroup("Effect")]
    [Header("Projectile Effect")]
    [Required]
    [SerializeField] private ProjectileEffectSO _projectileEffect;
    #endregion

    #region Properties - Effect Data
    public ProjectileEffectSO ProjectileEffect => _projectileEffect;
    #endregion
}
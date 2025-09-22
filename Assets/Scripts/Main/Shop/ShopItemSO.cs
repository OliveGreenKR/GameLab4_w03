using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>상점 아이템 타입</summary>
public enum ShopItemType
{
    WeaponDamage,
    WeaponFireRate,
    ProjectileSpeed,
    ProjectileLifetime,
    PlayerHeal,
    PlayerMaxHealth,
    PlayerMoveSpeed,
    WeaponEffect,
    ProjectileEffect,
    TemporaryWeaponDamage,
    TemporaryMoveSpeed
}
/// <summary>
/// 상점 아이템 데이터 추상 베이스 클래스
/// 모든 상점 아이템의 공통 데이터를 정의 (순수 데이터 컨테이너)
/// </summary>
public abstract class ShopItemSO : ScriptableObject
{
    #region Serialized Fields
    [TabGroup("Basic Info")]
    [Header("Item Information")]
    [SerializeField] private string _itemName;

    [TabGroup("Basic Info")]
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    [TabGroup("Basic Info")]
    [PreviewField(100)]
    [SerializeField] private Sprite _itemIcon;

    [TabGroup("Basic Info")]
    [SerializeField] private ShopItemType _itemType;

    [TabGroup("Pricing")]
    [Header("Price Settings")]
    [Min(1)]
    [SerializeField] private int _basePrice;

    [TabGroup("Pricing")]
    [Range(1.0f, 3.0f)]
    [SerializeField] private float _priceInflationMultiplier;
    #endregion

    #region Properties - Basic Info
    public string ItemName => _itemName;
    public string Description => _description;
    public Sprite ItemIcon => _itemIcon;
    public ShopItemType ItemType => _itemType;
    #endregion

    #region Properties - Pricing Data
    public int BasePrice => _basePrice;
    public float PriceInflationMultiplier => _priceInflationMultiplier;
    #endregion
}
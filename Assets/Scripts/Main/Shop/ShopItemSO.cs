using Sirenix.OdinInspector;
using System.Text;
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
    [TextArea(2, 14)]
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

    #region Public Methods - Description Generation
    /// <summary>아이템 속성을 기반으로 설명 자동 생성</summary>
    [Button("Generate Description", ButtonSizes.Medium)]
    [GUIColor(0.7f, 1f, 0.7f)]
    public void GenerateDescription()
    {
        _description = CreateAutoDescription();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>아이템 타입별 맞춤 설명 생성</summary>
    /// <returns>생성된 설명 텍스트</returns>
    protected virtual string CreateAutoDescription()
    {
        var description = new StringBuilder();

        // 기본 아이템 정보
        description.AppendLine($"[{GetItemTypeDisplayName(_itemType)}]");

        // 가격 정보
        description.AppendLine($"기본 가격: {_basePrice}G");

        if (_priceInflationMultiplier > 1.0f)
        {
            description.AppendLine($" (구매시 {_priceInflationMultiplier:F1}배씩 증가)");
        }

        return description.ToString().Trim();
    }

    /// <summary>아이템 타입별 표시 이름 반환</summary>
    /// <param name="itemType">아이템 타입</param>
    /// <returns>표시용 이름</returns>
    protected string GetItemTypeDisplayName(ShopItemType itemType)
    {
        switch (itemType)
        {
            case ShopItemType.WeaponDamage: return "무기 공격력 강화";
            case ShopItemType.WeaponFireRate: return "무기 연사속도 강화";
            case ShopItemType.ProjectileSpeed: return "투사체 속도 강화";
            case ShopItemType.ProjectileLifetime: return "투사체 생존시간 강화";
            case ShopItemType.PlayerHeal: return "플레이어 회복";
            case ShopItemType.PlayerMaxHealth: return "최대 체력 강화";
            case ShopItemType.PlayerMoveSpeed: return "이동속도 강화";
            case ShopItemType.WeaponEffect: return "무기 효과";
            case ShopItemType.ProjectileEffect: return "투사체 효과";
            case ShopItemType.TemporaryWeaponDamage: return "일시적인 공격력 증가";
            case ShopItemType.TemporaryMoveSpeed: return "일시적인 이동속도 증가";
            default: return itemType.ToString();
        }
    }
    #endregion
}
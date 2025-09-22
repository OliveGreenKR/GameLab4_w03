using Sirenix.OdinInspector;
using System.Text;
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

    #region Protected Methods - Description Generation Override
    /// <summary>스탯 업그레이드 상점 아이템 특화 설명 생성</summary>
    /// <returns>스탯 업그레이드 아이템 맞춤 설명</returns>
    protected override string CreateAutoDescription()
    {
        var description = new StringBuilder();

        // 기본 아이템 정보
        description.AppendLine($"[{GetItemTypeDisplayName(ItemType)}]");

        // 스탯 증가량 정보
        string statText = GetStatUpgradeText(ItemType, UpgradeValue);
        description.AppendLine(statText);

        // 가격 정보
        description.AppendLine($"기본 가격: {BasePrice}G");

        if (PriceInflationMultiplier > 1.0f)
        {
            description.Append($"(구매시 {PriceInflationMultiplier:F1}배씩 증가)");
        }

        return description.ToString().Trim();
    }

    /// <summary>아이템 타입별 스탯 증가량 텍스트 생성</summary>
    /// <param name="itemType">아이템 타입</param>
    /// <param name="upgradeValue">증가량</param>
    /// <returns>스탯 증가량 설명</returns>
    private string GetStatUpgradeText(ShopItemType itemType, float upgradeValue)
    {
        switch (itemType)
        {
            case ShopItemType.WeaponDamage:
                return $"데미지 +{upgradeValue:F1}";

            case ShopItemType.WeaponFireRate:
                return $"발사속도 +{upgradeValue:F1}/초";

            case ShopItemType.ProjectileSpeed:
                return $"투사체 속도 +{upgradeValue:F1} units/sec";

            case ShopItemType.ProjectileLifetime:
                return $"투사체 생존시간 +{upgradeValue:F1}초";

            case ShopItemType.PlayerHeal:
                return $"즉시 체력 회복 {upgradeValue:F1}HP";

            case ShopItemType.PlayerMaxHealth:
                return $"최대 체력 +{upgradeValue:F1}HP";

            case ShopItemType.PlayerMoveSpeed:
                return $"이동속도 +{upgradeValue:F1} units/sec";

            case ShopItemType.TemporaryWeaponDamage:
                return $"임시 공격력 증가 +{upgradeValue:F1}";

            case ShopItemType.TemporaryMoveSpeed:
                return $"임시 이동속도 증가 +{upgradeValue:F1}";

            default:
                return $"스탯 증가: +{upgradeValue:F1}";
        }
    }
    #endregion
}
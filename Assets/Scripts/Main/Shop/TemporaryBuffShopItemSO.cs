using Sirenix.OdinInspector;
using System.Text;
using UnityEngine;

/// <summary>
/// 임시 버프 상점 아이템
/// 기존 ShopItemSO를 임시 효과로 변환하는 래퍼 클래스
/// </summary>
[CreateAssetMenu(fileName = "TempBuffItem_", menuName = "Shop System/Temporary Buff Item", order = 4)]
public class TemporaryBuffShopItemSO : ShopItemSO
{
    #region Serialized Fields
    [TabGroup("Base Item")]
    [Header("Base Item")]
    [Required]
    [SerializeField] private ShopItemSO _baseItem;

    [TabGroup("Buff Settings")]
    [Header("Temporary Buff Settings")]
    [Min(1f)]
    [SerializeField] private float _buffDurationSeconds;

    [TabGroup("Buff Settings")]
    [InfoBox("기존 효과 대비 버프 효과 배율")]
    [SerializeField] private float _buffIntensityMultiplier;
    #endregion

    #region Properties - Base Item Access
    public ShopItemSO BaseItem => _baseItem;
    #endregion

    #region Properties - Buff Data
    public float BuffDurationSeconds => _buffDurationSeconds;
    public float BuffIntensityMultiplier => _buffIntensityMultiplier;
    #endregion

    #region Properties - Computed Values
    public bool HasValidBaseItem => _baseItem != null;
    #endregion

    #region Protected Methods - Description Generation Override
    /// <summary>임시 버프 상점 아이템 특화 설명 생성</summary>
    /// <returns>임시 버프 아이템 맞춤 설명</returns>
    protected override string CreateAutoDescription()
    {
        var description = new StringBuilder();

        // 기본 아이템 정보
        description.AppendLine($"[{GetItemTypeDisplayName(ItemType)}]");

        // 임시 버프 정보
        if (HasValidBaseItem && BaseItem is StatUpgradeShopItemSO statItem)
        {
            // 버프된 스탯량 계산
            float buffedValue = statItem.UpgradeValue * BuffIntensityMultiplier;

            // 스탯 증가량과 지속시간 표시
            string statText = GetTemporaryBuffText(ItemType, buffedValue, BuffDurationSeconds);
            description.AppendLine(statText);

        }
        else
        {
            description.AppendLine("기본 아이템: 미할당 또는 잘못된 타입");
            description.AppendLine($"지속시간: {BuffDurationSeconds:F1}초");
        }

        // 가격 정보
        description.AppendLine($"기본 가격: {BasePrice}G");

        if (PriceInflationMultiplier > 1.0f)
        {
            description.Append($"(구매시 {PriceInflationMultiplier:F1}배씩 증가)");
        }

        return description.ToString().Trim();
    }

    /// <summary>임시 버프 타입별 설명 텍스트 생성</summary>
    /// <param name="itemType">아이템 타입</param>
    /// <param name="buffedValue">버프된 수치</param>
    /// <param name="durationSeconds">지속시간</param>
    /// <returns>임시 버프 설명</returns>
    private string GetTemporaryBuffText(ShopItemType itemType, float buffedValue, float durationSeconds)
    {
        string statText;

        switch (itemType)
        {
            case ShopItemType.TemporaryWeaponDamage:
                statText = $"임시 공격력 +{buffedValue:F1}";
                break;

            case ShopItemType.TemporaryMoveSpeed:
                statText = $"임시 이동속도 +{buffedValue:F1} units/sec";
                break;

            default:
                statText = $"임시 버프 +{buffedValue:F1}";
                break;
        }

        return $"{statText} \n({durationSeconds:F1}초간)";
    }
    #endregion
}
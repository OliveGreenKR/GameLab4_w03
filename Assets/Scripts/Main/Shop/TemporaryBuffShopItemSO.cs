using Sirenix.OdinInspector;
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
    [Range(0.1f, 5.0f)]
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
}
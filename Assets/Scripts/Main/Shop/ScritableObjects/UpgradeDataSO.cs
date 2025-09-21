using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 업그레이드 카테고리 정의
/// </summary>
public enum UpgradeCategory
{
    WeaponBasic,     // 무기 기본 스탯
    WeaponSpecial,   // 무기 특수 효과
    PlayerStats,     // 플레이어 영구 스탯
    TemporaryBuffs,  // 임시 버프
    InstantEffects   // 즉시 효과
}

/// <summary>
/// 업그레이드 정보와 효과를 연결하는 데이터
/// </summary>
[CreateAssetMenu(fileName = "New Upgrade", menuName = "Shop/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    #region Serialized Fields
    [TabGroup("Basic Info")]
    [Header("Display Information")]
    [SerializeField] private string _displayName = "New Upgrade";

    [TabGroup("Basic Info")]
    [SerializeField, TextArea(2, 4)] private string _description = "업그레이드 설명을 입력하세요";

    [TabGroup("Basic Info")]
    [SerializeField] private Sprite _icon;

    [TabGroup("Basic Info")]
    [SerializeField] private UpgradeCategory _category = UpgradeCategory.WeaponBasic;

    [TabGroup("Basic Info")]
    [Header("Cost Settings")]
    [SerializeField, PropertyRange(1, 10000)] private int _baseCost = 50;

    [TabGroup("Effect")]
    [Header("Upgrade Effect")]
    [Required]
    [SerializeField] private UpgradeEffectSO _effect;

    [TabGroup("Purchase Rules")]
    [Header("Purchase Limitations")]
    [SerializeField] private bool _canPurchaseMultiple = false;

    [TabGroup("Purchase Rules")]
    [ShowIf("_canPurchaseMultiple")]
    [SerializeField, PropertyRange(1, 10)] private int _maxPurchases = 1;

    [TabGroup("Purchase Rules")]
    [ShowIf("_canPurchaseMultiple")]
    [SerializeField, PropertyRange(1f, 5f)] private float _costIncreaseMultiplier = 1.5f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public string DisplayName => _displayName;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public string Description => _description;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public Sprite Icon => _icon;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public UpgradeCategory Category => _category;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int BaseCost => _baseCost;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public UpgradeEffectSO Effect => _effect;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool CanPurchaseMultiple => _canPurchaseMultiple;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int MaxPurchases => _maxPurchases;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public float CostIncreaseMultiplier => _costIncreaseMultiplier;
    #endregion

    #region Unity Lifecycle
    private void OnValidate() { }
    #endregion

    #region Public Methods - Validation
    /// <summary>업그레이드 데이터 유효성 검사</summary>
    /// <returns>유효하면 true</returns>
    public bool IsValid()
    {
        if (_effect == null)
        {
            Debug.LogWarning($"[{name}] Effect is null", this);
            return false;
        }

        if (_baseCost <= 0)
        {
            Debug.LogWarning($"[{name}] Base cost must be greater than 0", this);
            return false;
        }

        if (string.IsNullOrEmpty(_displayName))
        {
            Debug.LogWarning($"[{name}] Display name is empty", this);
            return false;
        }

        if (_canPurchaseMultiple && _maxPurchases <= 0)
        {
            Debug.LogWarning($"[{name}] Max purchases must be greater than 0 when multiple purchases allowed", this);
            return false;
        }

        if (_canPurchaseMultiple && _costIncreaseMultiplier < 1f)
        {
            Debug.LogWarning($"[{name}] Cost increase multiplier must be at least 1.0", this);
            return false;
        }

        return true;
    }
    #endregion

    #region Public Methods - Cost Calculation
    /// <summary>현재 구매 횟수 기반 실제 비용 계산</summary>
    /// <param name="currentPurchaseCount">현재 구매 횟수</param>
    /// <returns>실제 구매 비용</returns>
    public int CalculateActualCost(int currentPurchaseCount)
    {
        if (currentPurchaseCount < 0)
        {
            Debug.LogWarning($"[{name}] Purchase count cannot be negative", this);
            return _baseCost;
        }

        if (!_canPurchaseMultiple && currentPurchaseCount > 0)
        {
            Debug.LogWarning($"[{name}] Multiple purchases not allowed", this);
            return int.MaxValue; // 구매 불가능한 높은 비용
        }

        if (_canPurchaseMultiple && currentPurchaseCount >= _maxPurchases)
        {
            Debug.LogWarning($"[{name}] Maximum purchases reached", this);
            return int.MaxValue; // 구매 불가능한 높은 비용
        }

        if (currentPurchaseCount == 0)
        {
            return _baseCost;
        }

        // 누적 배율 계산: 기본 비용 * (배율^구매횟수)
        float costMultiplier = Mathf.Pow(_costIncreaseMultiplier, currentPurchaseCount);
        int actualCost = Mathf.RoundToInt(_baseCost * costMultiplier);

        return actualCost;
    }
    #endregion
}
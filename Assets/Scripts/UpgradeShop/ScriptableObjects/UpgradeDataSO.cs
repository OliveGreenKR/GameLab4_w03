using UnityEngine;

/// <summary>업그레이드 데이터를 정의하는 ScriptableObject</summary>
[CreateAssetMenu(fileName = "New Upgrade", menuName = "Shop/Upgrade Data")]
public class UpgradeDataSO : ScriptableObject
{
    #region Serialized Fields
    [Header("Basic Info")]
    [SerializeField] private string _displayName;
    [SerializeField, TextArea(2, 4)] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private int _baseCost = 100;

    [Header("Upgrade Settings")]
    [SerializeField] private UpgradeType _upgradeType;
    [SerializeField] private float _upgradeValue = 1f;
    [SerializeField] private bool _isPercentageValue = false;

    [Header("Temporary Buff Settings")]
    [SerializeField] private bool _isTemporary = false;
    [SerializeField] private float _durationSeconds = 30f;

    [Header("Purchase Rules")]
    [SerializeField] private bool _canPurchaseMultiple = true;
    [SerializeField] private int _maxPurchases = 999;
    [SerializeField] private float _costIncreaseMultiplier = 1.2f;
    #endregion

    #region Properties
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public int BaseCost => _baseCost;
    public UpgradeType UpgradeType => _upgradeType;
    public float UpgradeValue => _upgradeValue;
    public bool IsPercentageValue => _isPercentageValue;
    public bool IsTemporary => _isTemporary;
    public float DurationSeconds => _durationSeconds;
    public bool CanPurchaseMultiple => _canPurchaseMultiple;
    public int MaxPurchases => _maxPurchases;
    public float CostIncreaseMultiplier => _costIncreaseMultiplier;
    #endregion

    #region Public Methods - Cost Calculation
    /// <summary>구매 횟수에 따른 실제 비용 계산</summary>
    /// <param name="purchaseCount">현재까지 구매 횟수</param>
    /// <returns>실제 지불해야 할 비용</returns>
    public int CalculateActualCost(int purchaseCount)
    {
        if (purchaseCount < 0)
        {
            Debug.LogWarning($"Invalid purchase count: {purchaseCount}. Using base cost.");
            return _baseCost;
        }

        if (!_canPurchaseMultiple && purchaseCount > 0)
            return int.MaxValue; // 중복 구매 불가 시 매우 높은 비용

        float cost = _baseCost;
        for (int i = 0; i < purchaseCount; i++)
        {
            cost *= _costIncreaseMultiplier;
        }

        return Mathf.RoundToInt(cost);
    }
    #endregion

    #region Public Methods - Validation
    /// <summary>업그레이드 데이터 유효성 검사</summary>
    /// <returns>데이터가 유효한지 여부</returns>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(_displayName))
            return false;

        if (_baseCost <= 0)
            return false;

        if (_upgradeValue == 0f)
            return false;

        if (_isTemporary && _durationSeconds <= 0f)
            return false;

        if (_maxPurchases <= 0)
            return false;

        if (_costIncreaseMultiplier <= 0f)
            return false;

        return true;
    }

    /// <summary>추가 구매 가능 여부 확인</summary>
    /// <param name="currentPurchaseCount">현재 구매 횟수</param>
    /// <returns>추가 구매 가능 여부</returns>
    public bool CanPurchaseMore(int currentPurchaseCount)
    {
        if (currentPurchaseCount < 0)
            return false;

        if (!_canPurchaseMultiple && currentPurchaseCount > 0)
            return false;

        return currentPurchaseCount < _maxPurchases;
    }
    #endregion
}
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>상점 시스템 - 상품 관리와 구매 처리</summary>
public class ShopManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Shop Configuration")]
    [SerializeField] private List<UpgradeDataSO> _availableUpgrades = new List<UpgradeDataSO>();

    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogs = true;
    #endregion

    #region Properties
    public static ShopManager Instance { get; private set; }
    public IReadOnlyList<UpgradeDataSO> AvailableUpgrades => _availableUpgrades;
    #endregion

    #region Events
    public static event Action<UpgradeDataSO, int> OnUpgradePurchased;
    public static event Action<UpgradeDataSO, string> OnPurchaseFailed;
    #endregion

    #region Private Fields
    private Dictionary<UpgradeDataSO, int> _purchaseHistory = new Dictionary<UpgradeDataSO, int>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _purchaseHistory = new Dictionary<UpgradeDataSO, int>();
    }
    #endregion

    #region Public Methods - Shop Operations
    /// <summary>업그레이드 구매 시도</summary>
    /// <param name="upgradeData">구매할 업그레이드</param>
    /// <param name="purchaser">구매자</param>
    /// <returns>구매 성공 여부</returns>
    public bool TryPurchaseUpgrade(UpgradeDataSO upgradeData, IPurchaser purchaser)
    {
        if (!ValidatePurchase(upgradeData, purchaser))
            return false;

        int actualCost = GetActualCost(upgradeData);

        if (!purchaser.SpendGold(actualCost))
        {
            OnPurchaseFailed?.Invoke(upgradeData, "Insufficient gold");
            return false;
        }

        ExecuteUpgrade(upgradeData);
        UpdatePurchaseHistory(upgradeData);

        OnUpgradePurchased?.Invoke(upgradeData, actualCost);

        if (_enableDebugLogs)
            Debug.Log($"Purchase successful: {upgradeData.DisplayName} for {actualCost} gold");

        return true;
    }

    /// <summary>실제 구매 비용 계산</summary>
    /// <param name="upgradeData">업그레이드 데이터</param>
    /// <returns>현재 구매 비용</returns>
    public int GetActualCost(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null)
            return int.MaxValue;

        int currentPurchaseCount = GetPurchaseCount(upgradeData);
        return upgradeData.CalculateActualCost(currentPurchaseCount);
    }
    #endregion

    #region Public Methods - Purchase History
    /// <summary>구매 횟수 조회</summary>
    /// <param name="upgradeData">조회할 업그레이드</param>
    /// <returns>구매 횟수</returns>
    public int GetPurchaseCount(UpgradeDataSO upgradeData)
    {
        return _purchaseHistory.TryGetValue(upgradeData, out int count) ? count : 0;
    }

    /// <summary>구매 히스토리 초기화</summary>
    public void ResetPurchaseHistory()
    {
        _purchaseHistory.Clear();

        if (_enableDebugLogs)
            Debug.Log("Purchase history reset");
    }
    #endregion

    #region Private Methods - Purchase Logic
    private bool ValidatePurchase(UpgradeDataSO upgradeData, IPurchaser purchaser)
    {
        if (upgradeData == null || !upgradeData.IsValid())
        {
            OnPurchaseFailed?.Invoke(upgradeData, "Invalid upgrade data");
            return false;
        }

        if (purchaser == null)
        {
            OnPurchaseFailed?.Invoke(upgradeData, "Invalid purchaser");
            return false;
        }

        int currentPurchaseCount = GetPurchaseCount(upgradeData);
        if (!upgradeData.CanPurchaseMore(currentPurchaseCount))
        {
            OnPurchaseFailed?.Invoke(upgradeData, "Maximum purchases reached");
            return false;
        }

        int actualCost = GetActualCost(upgradeData);
        if (!purchaser.CanAfford(actualCost))
        {
            OnPurchaseFailed?.Invoke(upgradeData, "Insufficient gold");
            return false;
        }

        return true;
    }

    private void ExecuteUpgrade(UpgradeDataSO upgradeData)
    {
        // Command 패턴 - 기존 UpgradeEffectSO 시스템 활용 예정
        // 임시 구현: IUpgradable 기반 업그레이드 적용

        if (_enableDebugLogs)
            Debug.Log($"Executing upgrade: {upgradeData.DisplayName}");
    }

    private void UpdatePurchaseHistory(UpgradeDataSO upgradeData)
    {
        if (!_purchaseHistory.ContainsKey(upgradeData))
            _purchaseHistory[upgradeData] = 0;

        _purchaseHistory[upgradeData]++;
    }
    #endregion
}
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>상점 시스템 총괄 관리 - 업그레이드 구매/적용 처리</summary>
public class ShopManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Shop Configuration")]
    [SerializeField] private List<UpgradeDataSO> _availableUpgrades = new List<UpgradeDataSO>();
    [SerializeField] private bool _allowShopDuringWave = false;
    [SerializeField] private float _shopCooldownSeconds = 1f;

    [Header("System References")]
    [SerializeField] private PlayerWeaponController _playerWeaponController;
    [SerializeField] private PlayerBattleEntity _playerBattleEntity;
    [SerializeField] private TemporaryBuffManager _temporaryBuffManager;

    [Header("Debug Settings")]
    [SerializeField] private bool _enableDebugLogs = true;
    #endregion

    #region Properties
    /// <summary>ShopManager 싱글톤 인스턴스</summary>
    public static ShopManager Instance { get; private set; }

    /// <summary>현재 상점 사용 가능 여부</summary>
    public bool IsShopAvailable { get; private set; }

    /// <summary>마지막 구매 시간 (쿨다운 관리용)</summary>
    public float LastPurchaseTime { get; private set; }

    /// <summary>사용 가능한 업그레이드 목록 (읽기 전용)</summary>
    public IReadOnlyList<UpgradeDataSO> AvailableUpgrades => _availableUpgrades;
    #endregion

    #region Events
    /// <summary>업그레이드 구매 성공 시 발생 - (업그레이드 데이터, 실제 지불 비용)</summary>
    public static event Action<UpgradeDataSO, int> OnUpgradePurchased;

    /// <summary>업그레이드 구매 실패 시 발생 - (업그레이드 데이터, 실패 사유)</summary>
    public static event Action<UpgradeDataSO, string> OnPurchaseFailed;

    /// <summary>상점 상태 변경 시 발생 - (사용 가능 여부)</summary>
    public static event Action<bool> OnShopAvailabilityChanged;
    #endregion

    #region Private Fields
    private Dictionary<UpgradeDataSO, int> _purchaseHistory = new Dictionary<UpgradeDataSO, int>();
    private bool _isInitialized = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        InitializeShop();
        CacheSystemReferences();
        ValidateConfiguration();
    }

    private void Update()
    {
        UpdateShopAvailability();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        // 정적 이벤트 정리
        OnUpgradePurchased = null;
        OnPurchaseFailed = null;
        OnShopAvailabilityChanged = null;
    }
    #endregion

    #region Public Methods - Shop Operations
    /// <summary>업그레이드 구매 시도</summary>
    /// <param name="upgradeData">구매할 업그레이드 데이터</param>
    /// <returns>구매 성공 여부</returns>
    public bool TryPurchaseUpgrade(UpgradeDataSO upgradeData)
    {
        if (!ValidatePurchaseRequest(upgradeData))
        {
            return false;
        }

        int actualCost = GetUpgradeActualCost(upgradeData);

        // 골드 차감 시도
        if (!GameManager.Instance.SpendGold(actualCost))
        {
            ProcessFailedPurchase(upgradeData, "골드가 부족합니다");
            return false;
        }

        // 효과 적용
        ApplyUpgradeEffect(upgradeData);

        // 구매 기록 업데이트
        UpdatePurchaseHistory(upgradeData);

        // 성공 처리
        ProcessSuccessfulPurchase(upgradeData, actualCost);

        return true;
    }

    /// <summary>업그레이드 구매 가능 여부 확인</summary>
    /// <param name="upgradeData">확인할 업그레이드 데이터</param>
    /// <returns>구매 가능 여부</returns>
    public bool CanPurchaseUpgrade(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null || !upgradeData.IsValid())
        {
            return false;
        }

        if (!IsShopAvailable)
        {
            return false;
        }

        // 중복 구매 제한 확인
        if (!upgradeData.CanPurchaseMultiple)
        {
            int currentPurchases = GetPurchaseCount(upgradeData);
            if (currentPurchases >= upgradeData.MaxPurchases)
            {
                return false;
            }
        }

        // 골드 충분성 확인
        int requiredCost = GetUpgradeActualCost(upgradeData);
        if (!HasSufficientGold(requiredCost))
        {
            return false;
        }

        // 효과 적용 가능성 확인
        if (upgradeData.Effect != null && !upgradeData.Effect.CanApply(_playerWeaponController, _playerBattleEntity))
        {
            return false;
        }

        return true;
    }

    /// <summary>업그레이드 실제 구매 비용 계산 (중복 구매 고려)</summary>
    /// <param name="upgradeData">비용을 계산할 업그레이드</param>
    /// <returns>실제 지불해야 할 골드</returns>
    public int GetUpgradeActualCost(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null)
        {
            return 0;
        }

        int baseCost = upgradeData.BaseCost;

        if (!upgradeData.CanPurchaseMultiple)
        {
            return baseCost;
        }

        int purchaseCount = GetPurchaseCount(upgradeData);

        // 가격 증가 공식: baseCost * (multiplier ^ purchaseCount)
        float finalCost = baseCost * Mathf.Pow(upgradeData.CostIncreaseMultiplier, purchaseCount);

        return Mathf.RoundToInt(finalCost);
    }

    /// <summary>특정 업그레이드의 구매 횟수 조회</summary>
    /// <param name="upgradeData">조회할 업그레이드</param>
    /// <returns>구매 횟수</returns>
    public int GetPurchaseCount(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null || _purchaseHistory == null)
        {
            return 0;
        }

        return _purchaseHistory.TryGetValue(upgradeData, out int count) ? count : 0;
    }

    /// <summary>상점 강제 활성화/비활성화</summary>
    /// <param name="isAvailable">활성화 여부</param>
    public void SetShopAvailability(bool isAvailable)
    {
        bool wasAvailable = IsShopAvailable;
        IsShopAvailable = isAvailable;

        if (wasAvailable != IsShopAvailable)
        {
            OnShopAvailabilityChanged?.Invoke(IsShopAvailable);
            LogDebugMessage($"Shop availability manually set to: {IsShopAvailable}");
        }
    }
    #endregion

    #region Public Methods - System Management
    /// <summary>상점 시스템 초기화</summary>
    public void InitializeShop()
    {
        if (_isInitialized)
        {
            LogDebugMessage("Shop already initialized, skipping");
            return;
        }

        // 구매 기록 초기화
        if (_purchaseHistory == null)
        {
            _purchaseHistory = new Dictionary<UpgradeDataSO, int>();
        }
        else
        {
            _purchaseHistory.Clear();
        }

        // 초기 상점 상태 설정
        IsShopAvailable = false;
        LastPurchaseTime = 0f;

        _isInitialized = true;
        LogDebugMessage("Shop system initialized successfully");
    }

    /// <summary>구매 기록 초기화 (게임 재시작 시)</summary>
    public void ResetPurchaseHistory()
    {
        if (_purchaseHistory != null)
        {
            int previousCount = _purchaseHistory.Count;
            _purchaseHistory.Clear();
            LogDebugMessage($"Purchase history reset - {previousCount} entries cleared");
        }

        // 상점 쿨다운도 리셋
        LastPurchaseTime = 0f;
    }

    /// <summary>업그레이드 목록 동적 추가</summary>
    /// <param name="upgradeData">추가할 업그레이드</param>
    public void AddAvailableUpgrade(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null)
        {
            Debug.LogWarning("[ShopManager] Cannot add null upgrade data", this);
            return;
        }

        if (!upgradeData.IsValid())
        {
            Debug.LogWarning($"[ShopManager] Cannot add invalid upgrade: {upgradeData.name}", upgradeData);
            return;
        }

        if (_availableUpgrades.Contains(upgradeData))
        {
            LogDebugMessage($"Upgrade already exists in list: {upgradeData.DisplayName}");
            return;
        }

        _availableUpgrades.Add(upgradeData);
        LogDebugMessage($"Added upgrade to available list: {upgradeData.DisplayName}");
    }

    /// <summary>업그레이드 목록에서 제거</summary>
    /// <param name="upgradeData">제거할 업그레이드</param>
    public void RemoveAvailableUpgrade(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null)
        {
            Debug.LogWarning("[ShopManager] Cannot remove null upgrade data", this);
            return;
        }

        bool wasRemoved = _availableUpgrades.Remove(upgradeData);

        if (wasRemoved)
        {
            // 구매 기록도 함께 제거
            if (_purchaseHistory != null && _purchaseHistory.ContainsKey(upgradeData))
            {
                _purchaseHistory.Remove(upgradeData);
            }

            LogDebugMessage($"Removed upgrade from available list: {upgradeData.DisplayName}");
        }
        else
        {
            LogDebugMessage($"Upgrade not found in list: {upgradeData.DisplayName}");
        }
    }
    #endregion

    #region Private Methods - Core Logic
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LogDebugMessage("ShopManager singleton initialized");
        }
        else if (Instance != this)
        {
            LogDebugMessage("Duplicate ShopManager detected, destroying");
            Destroy(gameObject);
        }
    }

    private void ValidateConfiguration()
    {
        if (_availableUpgrades == null || _availableUpgrades.Count == 0)
        {
            Debug.LogWarning("[ShopManager] No upgrades configured in available upgrades list", this);
            return;
        }

        int invalidCount = 0;
        foreach (var upgrade in _availableUpgrades)
        {
            if (upgrade == null)
            {
                invalidCount++;
                continue;
            }

            if (!upgrade.IsValid())
            {
                Debug.LogWarning($"[ShopManager] Invalid upgrade data: {upgrade.name}", upgrade);
                invalidCount++;
            }
        }

        if (invalidCount > 0)
        {
            Debug.LogWarning($"[ShopManager] Found {invalidCount} invalid upgrades out of {_availableUpgrades.Count} total", this);
        }

        LogDebugMessage($"Configuration validated - {_availableUpgrades.Count - invalidCount} valid upgrades");
    }

    private void CacheSystemReferences()
    {
        if (_playerWeaponController == null)
        {
            _playerWeaponController = FindFirstObjectByType<PlayerWeaponController>();
            if (_playerWeaponController == null)
            {
                Debug.LogError("[ShopManager] PlayerWeaponController not found in scene", this);
            }
        }

        if (_playerBattleEntity == null)
        {
            _playerBattleEntity = FindFirstObjectByType<PlayerBattleEntity>();
            if (_playerBattleEntity == null)
            {
                Debug.LogError("[ShopManager] PlayerBattleEntity not found in scene", this);
            }
        }

        if (_temporaryBuffManager == null)
        {
            _temporaryBuffManager = FindFirstObjectByType<TemporaryBuffManager>();
            if (_temporaryBuffManager == null)
            {
                Debug.LogWarning("[ShopManager] TemporaryBuffManager not found - temporary buffs will not work", this);
            }
        }

        bool allSystemsReady = _playerWeaponController != null && _playerBattleEntity != null;
        LogDebugMessage($"System references cached - All systems ready: {allSystemsReady}");
    }

    private void UpdateShopAvailability()
    {
        bool wasAvailable = IsShopAvailable;

        if (GameManager.Instance == null)
        {
            IsShopAvailable = false;
        }
        else
        {
            bool gameStateAllowsShop = !GameManager.Instance.IsGameOver &&
                                     (GameManager.Instance.CurrentState == GameState.WaveCompleted ||
                                      _allowShopDuringWave);

            bool notInCooldown = !IsWithinCooldown();
            bool systemsReady = _playerWeaponController != null && _playerBattleEntity != null;

            IsShopAvailable = gameStateAllowsShop && notInCooldown && systemsReady;
        }

        if (wasAvailable != IsShopAvailable)
        {
            OnShopAvailabilityChanged?.Invoke(IsShopAvailable);
            LogDebugMessage($"Shop availability changed: {IsShopAvailable}");
        }
    }
    #endregion

    #region Private Methods - Purchase Processing
    private bool ValidatePurchaseRequest(UpgradeDataSO upgradeData)
    {
        if (upgradeData == null)
        {
            ProcessFailedPurchase(upgradeData, "업그레이드 데이터가 null입니다");
            return false;
        }

        if (!upgradeData.IsValid())
        {
            ProcessFailedPurchase(upgradeData, "유효하지 않은 업그레이드 데이터입니다");
            return false;
        }

        if (!IsShopAvailable)
        {
            ProcessFailedPurchase(upgradeData, "상점이 현재 사용할 수 없습니다");
            return false;
        }

        if (IsWithinCooldown())
        {
            ProcessFailedPurchase(upgradeData, "구매 쿨다운 중입니다");
            return false;
        }

        if (!CanPurchaseUpgrade(upgradeData))
        {
            ProcessFailedPurchase(upgradeData, "구매 조건을 만족하지 않습니다");
            return false;
        }

        return true;
    }

    private void ApplyUpgradeEffect(UpgradeDataSO upgradeData)
    {
        if (upgradeData.Effect == null)
        {
            Debug.LogWarning($"[ShopManager] Upgrade effect is null: {upgradeData.DisplayName}", upgradeData);
            return;
        }

        try
        {
            if (upgradeData.Effect.IsTemporary)
            {
                // 임시 버프는 TemporaryBuffManager에 등록
                if (_temporaryBuffManager != null)
                {
                    _temporaryBuffManager.StartBuff(upgradeData, _playerWeaponController, _playerBattleEntity);
                    LogDebugMessage($"Started temporary buff: {upgradeData.DisplayName} ({upgradeData.Effect.BuffDuration}s)");
                }
                else
                {
                    Debug.LogError("[ShopManager] TemporaryBuffManager not available for temporary effect", this);
                    // 임시 버프 매니저가 없어도 즉시 적용은 시도
                    //upgradeData.Effect.ApplyUpgrade(_playerWeaponController, _playerBattleEntity);
                }
            }
            else
            {
                // 영구 효과는 즉시 적용
                upgradeData.Effect.ApplyUpgrade(_playerWeaponController, _playerBattleEntity);
                LogDebugMessage($"Applied permanent upgrade: {upgradeData.DisplayName}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShopManager] Failed to apply upgrade effect '{upgradeData.DisplayName}': {ex.Message}", upgradeData);
        }
    }

    private void UpdatePurchaseHistory(UpgradeDataSO upgradeData)
    {
        if (_purchaseHistory == null)
        {
            _purchaseHistory = new Dictionary<UpgradeDataSO, int>();
        }

        if (_purchaseHistory.ContainsKey(upgradeData))
        {
            _purchaseHistory[upgradeData]++;
        }
        else
        {
            _purchaseHistory[upgradeData] = 1;
        }

        LogDebugMessage($"Purchase history updated: {upgradeData.DisplayName} (Total: {_purchaseHistory[upgradeData]})");
    }

    private void ProcessSuccessfulPurchase(UpgradeDataSO upgradeData, int actualCost)
    {
        // 쿨다운 시간 업데이트
        LastPurchaseTime = Time.time;

        // 성공 이벤트 발생
        OnUpgradePurchased?.Invoke(upgradeData, actualCost);

        LogDebugMessage($"Purchase successful: {upgradeData.DisplayName} for {actualCost} gold");
    }

    private void ProcessFailedPurchase(UpgradeDataSO upgradeData, string failureReason)
    {
        // 실패 이벤트 발생
        OnPurchaseFailed?.Invoke(upgradeData, failureReason);

        string upgradeName = upgradeData?.DisplayName ?? "Unknown";
        LogDebugMessage($"Purchase failed: {upgradeName} - {failureReason}");
    }
    #endregion

    #region Private Methods - Utility
    private bool IsWithinCooldown()
    {
        if (_shopCooldownSeconds <= 0f)
        {
            return false;
        }

        return (Time.time - LastPurchaseTime) < _shopCooldownSeconds;
    }

    private bool HasSufficientGold(int requiredAmount)
    {
        if (GameManager.Instance == null)
        {
            return false;
        }

        return GameManager.Instance.CurrentGold >= requiredAmount;
    }

    private void LogDebugMessage(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[ShopManager] {message}", this);
        }
    }
    #endregion
}
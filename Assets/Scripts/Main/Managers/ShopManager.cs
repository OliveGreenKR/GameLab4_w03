using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

/// <summary>
/// 순수 비즈니스 로직
/// 구매 처리, 가격 계산, 아이템 관리
/// UI 독립적인 이벤트 시스템으로 통신
/// </summary>
public class ShopManager : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Core Systems")]
    [Required]
    [SerializeField] private UpgradeManager _upgradeManager;

    [TabGroup("References")]
    [Required]
    [SerializeField] private GameManager _gameManager;

    [TabGroup("Shop Items")]
    [Header("Available Shop Items")]
    [SerializeField] private List<ShopItemSO> _shopItems = new List<ShopItemSO>();

    [TabGroup("Pricing")]
    [Header("Price Scaling")]
    [SerializeField] private Dictionary<ShopItemType, int> _purchaseCount = new Dictionary<ShopItemType, int>();
    #endregion

    #region Events
    /// <summary>아이템 구매 성공 이벤트 (itemIndex, item, finalPrice)</summary>
    public event System.Action<int, ShopItemSO, int> OnItemPurchased;

    /// <summary>아이템 구매 실패 이벤트 (itemIndex, reason)</summary>
    public event System.Action<int, string> OnItemPurchaseFailed;

    /// <summary>가격 변경 이벤트 (itemIndex, newPrice)</summary>
    public event System.Action<int, int> OnPriceChanged;

    /// <summary>상점 초기화 완료 이벤트</summary>
    public event System.Action OnShopInitialized;

    /// <summary>상점 데이터 갱신 이벤트 (아이템 목록이나 가격 정책 변경시)</summary>
    public event System.Action OnShopDataRefreshed;
    #endregion

    #region Serialized Methods - Debug Buttons
    [Button("Test Purchase First Item")]
    [GUIColor(0.8f, 1f, 0.8f)]
    private void TestPurchaseFirstItem()
    {
        if (_shopItems != null && _shopItems.Count > 0)
        {
            bool success = PurchaseItem(0);
            Debug.Log($"[ShopManager] Test purchase result: {success}", this);
        }
        else
        {
            Debug.LogWarning("[ShopManager] No shop items available for testing", this);
        }
    }

    [Button("Test Purchase First Item")]
    [GUIColor(0.8f, 1f, 0.8f)]
    private void TestPurchaseItem(int shopIndex)
    {
        if (_shopItems != null && _shopItems.Count > 0)
        {
            bool success = PurchaseItem(shopIndex);
            Debug.Log($"[ShopManager] Test purchase result: {success}", this);
        }
        else
        {
            Debug.LogWarning("[ShopManager] No shop items available for testing", this);
        }
    }

    [Button("Reset Purchase Counts")]
    [GUIColor(1f, 0.8f, 0.8f)]
    private void ResetPurchaseCounts()
    {
        _purchaseCount.Clear();
        InitializePurchaseCounters();
        Debug.Log("[ShopManager] Purchase counts reset", this);
    }
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int AvailableItemCount => _shopItems?.Count ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentGold => _gameManager?.CurrentGold ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[ShopManager] Required references not assigned!", this);
            enabled = false;
            return;
        }

        InitializeShop();
        OnShopInitialized?.Invoke();
        Debug.Log("[ShopManager] Initialized successfully", this);
    }

    private void OnValidate()
    {
        OnShopDataRefreshed?.Invoke();
    }
    #endregion

    #region Public Methods - Item Management
    /// <summary>상점에 새 아이템 추가</summary>
    /// <param name="item">추가할 상점 아이템</param>
    public void AddShopItem(ShopItemSO item)
    {
        if (!IsInitialized || item == null)
        {
            Debug.LogWarning("[ShopManager] Cannot add item: Shop not initialized or item is null", this);
            return;
        }

        _shopItems.Add(item);

        // 새 아이템 타입의 구매 카운터 초기화
        if (!_purchaseCount.ContainsKey(item.ItemType))
        {
            _purchaseCount[item.ItemType] = 0;
        }

        OnShopDataRefreshed?.Invoke();
        Debug.Log($"[ShopManager] Added shop item: {item.ItemName}", this);
    }

    /// <summary>상점에서 아이템 제거 (인덱스)</summary>
    /// <param name="itemIndex">제거할 아이템 인덱스</param>
    public void RemoveShopItem(int itemIndex)
    {
        if (!IsInitialized || itemIndex < 0 || itemIndex >= _shopItems.Count)
        {
            Debug.LogWarning($"[ShopManager] Cannot remove item: Invalid index {itemIndex}", this);
            return;
        }

        string itemName = _shopItems[itemIndex].ItemName;
        _shopItems.RemoveAt(itemIndex);

        OnShopDataRefreshed?.Invoke();
        Debug.Log($"[ShopManager] Removed shop item: {itemName}", this);
    }

    /// <summary>상점에서 아이템 제거 (직접 참조)</summary>
    /// <param name="item">제거할 상점 아이템</param>
    public void RemoveShopItem(ShopItemSO item)
    {
        if (!IsInitialized || item == null)
        {
            Debug.LogWarning("[ShopManager] Cannot remove item: Shop not initialized or item is null", this);
            return;
        }

        bool removed = _shopItems.Remove(item);
        if (removed)
        {
            OnShopDataRefreshed?.Invoke();
            Debug.Log($"[ShopManager] Removed shop item: {item.ItemName}", this);
        }
        else
        {
            Debug.LogWarning($"[ShopManager] Item not found in shop: {item.ItemName}", this);
        }
    }

    /// <summary>상점의 모든 아이템 제거</summary>
    public void ClearShopItems()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[ShopManager] Cannot clear items: Shop not initialized", this);
            return;
        }

        _shopItems.Clear();
        OnShopDataRefreshed?.Invoke();
        Debug.Log("[ShopManager] Cleared all shop items", this);
    }
    #endregion

    #region Public Methods - Purchase
    /// <summary>아이템 구매 시도</summary>
    /// <param name="itemIndex">구매할 아이템 인덱스</param>
    /// <returns>구매 성공 여부</returns>
    public bool PurchaseItem(int itemIndex)
    {
        if (!CanPurchaseItem(itemIndex))
        {
            string reason = !IsInitialized ? "Shop not initialized" :
                           itemIndex < 0 || itemIndex >= _shopItems.Count ? "Invalid item index" :
                           "Insufficient gold";
            OnItemPurchaseFailed?.Invoke(itemIndex, reason);
            return false;
        }

        ShopItemSO item = _shopItems[itemIndex];
        int currentPrice = GetCurrentPrice(itemIndex);

        if (!_gameManager.SpendGold(currentPrice))
        {
            OnItemPurchaseFailed?.Invoke(itemIndex, "Failed to spend gold");
            return false;
        }

        ProcessPurchase(item, itemIndex);
        OnItemPurchased?.Invoke(itemIndex, item, currentPrice);
        OnPriceChanged?.Invoke(itemIndex, GetCurrentPrice(itemIndex));

        Debug.Log($"[ShopManager] Purchased {item.ItemName} for {currentPrice} gold", this);
        return true;
    }

    /// <summary>아이템 구매 가능 여부 확인</summary>
    /// <param name="itemIndex">확인할 아이템 인덱스</param>
    /// <returns>구매 가능 여부</returns>
    public bool CanPurchaseItem(int itemIndex)
    {
        if (!IsInitialized || itemIndex < 0 || itemIndex >= _shopItems.Count)
            return false;

        int currentPrice = GetCurrentPrice(itemIndex);
        return CurrentGold >= currentPrice;
    }
    #endregion

    #region Public Methods - Price Calculation
    /// <summary>아이템 현재 가격 계산</summary>
    /// <param name="itemIndex">아이템 인덱스</param>
    /// <returns>현재 가격</returns>
    public int GetCurrentPrice(int itemIndex)
    {
        if (!IsInitialized || itemIndex < 0 || itemIndex >= _shopItems.Count)
            return 0;

        ShopItemSO item = _shopItems[itemIndex];
        int purchaseCount = GetPurchaseCount(item.ItemType);

        return CalculateInflatedPrice(item.BasePrice, item.PriceInflationMultiplier, purchaseCount);
    }

    /// <summary>아이템 타입별 구매 횟수 조회</summary>
    /// <param name="itemType">아이템 타입</param>
    /// <returns>구매 횟수</returns>
    public int GetPurchaseCount(ShopItemType itemType)
    {
        return _purchaseCount.ContainsKey(itemType) ? _purchaseCount[itemType] : 0;
    }
    #endregion

    #region Private Methods - Initialization
    private bool ValidateReferences()
    {
        if (_upgradeManager == null)
        {
            Debug.LogError("[ShopManager] UpgradeManager not assigned!", this);
            return false;
        }

        if (_gameManager == null)
        {
            Debug.LogError("[ShopManager] GameManager not assigned!", this);
            return false;
        }

        return true;
    }

    private void InitializeShop()
    {
        InitializePurchaseCounters();
        IsInitialized = true;
        OnShopDataRefreshed?.Invoke();
    }

    private void InitializePurchaseCounters()
    {
        _purchaseCount.Clear();

        foreach (ShopItemType itemType in System.Enum.GetValues(typeof(ShopItemType)))
        {
            _purchaseCount[itemType] = 0;
        }
    }
    #endregion

    #region Private Methods - Purchase Processing
    private void ProcessPurchase(ShopItemSO item, int itemIndex)
    {
        ApplyItemEffect(item);
        UpdatePurchaseCount(item.ItemType);
    }

    private void ApplyItemEffect(ShopItemSO item)
    {
        // SO 타입별 분기 처리
        switch (item)
        {
            case StatUpgradeShopItemSO statItem:
                ApplyStatUpgradeEffect(statItem);
                break;

            case WeaponEffectShopItemSO weaponEffectItem:
                ApplyWeaponEffectItem(weaponEffectItem);
                break;

            case ProjectileEffectShopItemSO projectileEffectItem:
                ApplyProjectileEffectItem(projectileEffectItem);
                break;

            case TemporaryBuffShopItemSO tempBuffItem:
                ApplyTemporaryBuffEffect(tempBuffItem);
                break;

            default:
                Debug.LogWarning($"[ShopManager] Unknown shop item SO type: {item.GetType().Name}", this);
                break;
        }
    }

    private void ApplyStatUpgradeEffect(StatUpgradeShopItemSO statItem)
    {
        switch (statItem.ItemType)
        {
            case ShopItemType.WeaponDamage:
                _upgradeManager.UpgradeWeaponDamage(statItem.UpgradeValue);
                break;

            case ShopItemType.WeaponFireRate:
                _upgradeManager.UpgradeWeaponFireRate(statItem.UpgradeValue);
                break;

            case ShopItemType.ProjectileSpeed:
                _upgradeManager.UpgradeProjectileSpeed(statItem.UpgradeValue);
                break;

            case ShopItemType.ProjectileLifetime:
                _upgradeManager.UpgradeProjectileLifetime(statItem.UpgradeValue);
                break;

            case ShopItemType.PlayerHeal:
                _upgradeManager.HealPlayer(statItem.UpgradeValue);
                break;

            case ShopItemType.PlayerMaxHealth:
                _upgradeManager.UpgradePlayerMaxHealth(statItem.UpgradeValue);
                break;

            case ShopItemType.PlayerMoveSpeed:
                _upgradeManager.UpgradePlayerMoveSpeed(statItem.UpgradeValue);
                break;

            default:
                Debug.LogWarning($"[ShopManager] Unknown stat upgrade type: {statItem.ItemType}", this);
                break;
        }
    }

    private void ApplyWeaponEffectItem(WeaponEffectShopItemSO weaponEffectItem)
    {
        if (weaponEffectItem.WeaponEffect != null)
        {
            _upgradeManager.AddWeaponEffect(weaponEffectItem.WeaponEffect);
        }
        else
        {
            Debug.LogWarning($"[ShopManager] WeaponEffect is null for item: {weaponEffectItem.ItemName}", this);
        }
    }

    private void ApplyProjectileEffectItem(ProjectileEffectShopItemSO projectileEffectItem)
    {
        if (projectileEffectItem.ProjectileEffect != null)
        {
            _upgradeManager.AddProjectileEffect(projectileEffectItem.ProjectileEffect);
        }
        else
        {
            Debug.LogWarning($"[ShopManager] ProjectileEffect is null for item: {projectileEffectItem.ItemName}", this);
        }
    }

    private void ApplyTemporaryBuffEffect(TemporaryBuffShopItemSO tempBuffItem)
    {
        if (!tempBuffItem.HasValidBaseItem)
        {
            Debug.LogWarning($"[ShopManager] Base item is null for temporary buff: {tempBuffItem.ItemName}", this);
            return;
        }

        // 임시 버프 타입별 처리
        switch (tempBuffItem.ItemType)
        {
            case ShopItemType.TemporaryWeaponDamage:
                if (tempBuffItem.BaseItem is StatUpgradeShopItemSO statItem)
                {
                    float buffedValue = statItem.UpgradeValue * tempBuffItem.BuffIntensityMultiplier;
                    _upgradeManager.ApplyTemporaryWeaponDamageBuff(buffedValue, tempBuffItem.BuffDurationSeconds);
                }
                break;

            case ShopItemType.TemporaryMoveSpeed:
                if (tempBuffItem.BaseItem is StatUpgradeShopItemSO moveStatItem)
                {
                    float buffedValue = moveStatItem.UpgradeValue * tempBuffItem.BuffIntensityMultiplier;
                    _upgradeManager.ApplyTemporaryMoveSpeedBuff(buffedValue, tempBuffItem.BuffDurationSeconds);
                }
                break;

            default:
                Debug.LogWarning($"[ShopManager] Unknown temporary buff type: {tempBuffItem.ItemType}", this);
                break;
        }
    }

    private void UpdatePurchaseCount(ShopItemType itemType)
    {
        if (_purchaseCount.ContainsKey(itemType))
        {
            _purchaseCount[itemType]++;
        }
        else
        {
            _purchaseCount[itemType] = 1;
        }
    }
    #endregion

    #region Private Methods - Price Calculation
    private int CalculateInflatedPrice(int basePrice, float inflationMultiplier, int purchaseCount)
    {
        if (purchaseCount <= 0)
            return basePrice;

        float inflatedPrice = basePrice * Mathf.Pow(inflationMultiplier, purchaseCount);
        return Mathf.RoundToInt(inflatedPrice);
    }
    #endregion

}
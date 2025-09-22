using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

/// <summary>
/// 상점 시스템 관리자
/// 업그레이드 구매 처리 및 UI 관리
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
    [SerializeField] private ShopItemSO[] _shopItems;

    [TabGroup("Settings")]
    [SerializeField] private bool _isStopOnOpen = true;

    [TabGroup("Pricing")]
    [Header("Price Scaling")]
    [SerializeField] private Dictionary<ShopItemType, int> _purchaseCount = new Dictionary<ShopItemType, int>();

    [TabGroup("UI")]
    [Header("Shop UI")]
    [SerializeField] private GameObject _shopPanel;

    [TabGroup("UI")]
    [SerializeField] private Transform _shopItemContainer;

    [TabGroup("UI")]
    [SerializeField] private GameObject _shopItemPrefab;
    #endregion

    #region Serialized Methods - Debug Buttons
    [Button("Test Purchase First Item")]
    [GUIColor(0.8f, 1f, 0.8f)]
    private void TestPurchaseFirstItem()
    {
        if (_shopItems != null && _shopItems.Length > 0)
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
        if (_shopItems != null && _shopItems.Length > 0)
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
    public bool IsShopOpen { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int AvailableItemCount => _shopItems?.Length ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentGold => _gameManager?.CurrentGold ?? 0;

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }
    #endregion

    #region Private Fields
    private InputSystem_Actions _inputs;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_inputs == null)
        {
            _inputs = new InputSystem_Actions();
        }
    }
    private void Start()
    {
        if (!ValidateReferences())
        {
            Debug.LogError("[ShopManager] Required references not assigned!", this);
            enabled = false;
            return;
        }

        InitializeShop();
        Debug.Log("[ShopManager] Initialized successfully", this);
    }

    private void OnEnable()
    {
        if (_inputs != null)
        {
            _inputs.Enable();
            _inputs.UI.Shop.performed -= OnShopPerformed;
            _inputs.UI.Shop.performed += OnShopPerformed;
        }
    }

    private void OnDisable()
    {
        if (_inputs != null)
        {
            _inputs.Disable();
            _inputs.UI.Shop.performed -= OnShopPerformed;
        }
    }
    #endregion

    #region Public Methods - Shop Control
    /// <summary>상점 열기</summary>
    public void OpenShop()
    {
        if (!IsInitialized || IsShopOpen) return;

        IsShopOpen = true;

        if (_shopPanel != null)
        {
            _shopPanel.SetActive(true);
        }

        RefreshShopUI();

        if(_isStopOnOpen)
        {
            // 게임 일시정지 (선택사항)
            Time.timeScale = 0f;
        }

        Debug.Log("[ShopManager] Shop opened", this);
    }

    /// <summary>상점 닫기</summary>
    public void CloseShop()
    {
        if (!IsInitialized || !IsShopOpen) return;

        IsShopOpen = false;

        if (_shopPanel != null)
        {
            _shopPanel.SetActive(false);
        }

        if (_isStopOnOpen)
        {
            Time.timeScale = 1f;
        }

        Debug.Log("[ShopManager] Shop closed", this);
    }

    /// <summary>상점 토글</summary>
    public void ToggleShop()
    {
        if (IsShopOpen)
        {
            CloseShop();
        }
        else
        {
            OpenShop();
        }
    }
    #endregion

    #region Public Methods - Purchase
    /// <summary>아이템 구매 시도</summary>
    /// <param name="itemIndex">구매할 아이템 인덱스</param>
    /// <returns>구매 성공 여부</returns>
    public bool PurchaseItem(int itemIndex)
    {
        if (!CanPurchaseItem(itemIndex))
            return false;

        ShopItemSO item = _shopItems[itemIndex];
        int currentPrice = GetCurrentPrice(itemIndex);

        if (!_gameManager.SpendGold(currentPrice))
            return false;

        ProcessPurchase(item, itemIndex);
        RefreshShopUI();

        Debug.Log($"[ShopManager] Purchased {item.ItemName} for {currentPrice} gold", this);
        return true;
    }

    /// <summary>아이템 구매 가능 여부 확인</summary>
    /// <param name="itemIndex">확인할 아이템 인덱스</param>
    /// <returns>구매 가능 여부</returns>
    public bool CanPurchaseItem(int itemIndex)
    {
        if (!IsInitialized || itemIndex < 0 || itemIndex >= _shopItems.Length)
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
        if (!IsInitialized || itemIndex < 0 || itemIndex >= _shopItems.Length)
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

        if (_shopPanel == null)
        {
            Debug.LogWarning("[ShopManager] Shop panel not assigned!", this);
        }

        return true;
    }

    private void InitializeShop()
    {
        InitializePurchaseCounters();

        IsShopOpen = false;
        if (_shopPanel != null)
        {
            _shopPanel.SetActive(false);
        }

        RefreshShopUI();
        IsInitialized = true;
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

    #region Private Methods - UI Management
    private void RefreshShopUI()
    {
        if (_shopItemContainer == null || _shopItemPrefab == null)
            return;

        ClearShopItemUI();

        for (int i = 0; i < _shopItems.Length; i++)
        {
            CreateShopItemUI(_shopItems[i], i);
        }
    }

    private void CreateShopItemUI(ShopItemSO item, int itemIndex)
    {
        if (_shopItemContainer == null || _shopItemPrefab == null)
            return;

        //GameObject itemUI = Instantiate(_shopItemPrefab, _shopItemContainer);

        //// UI 컴포넌트 설정 (실제 UI 구조에 따라 수정 필요)
        //ShopItemUI shopItemUI = itemUI.GetComponent<ShopItemUI>();
        //if (shopItemUI != null)
        //{
        //    int currentPrice = GetCurrentPrice(itemIndex);
        //    bool canAfford = CanPurchaseItem(itemIndex);

        //    shopItemUI.Setup(item, currentPrice, canAfford, () => PurchaseItem(itemIndex));
        //}
    }

    private void ClearShopItemUI()
    {
        if (_shopItemContainer == null)
            return;

        for (int i = _shopItemContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = _shopItemContainer.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
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

    #region Private Methods - Input Handling
    private void OnShopPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        ToggleShop();
    }
    #endregion
}
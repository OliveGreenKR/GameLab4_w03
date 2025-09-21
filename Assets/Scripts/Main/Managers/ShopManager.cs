using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

/// <summary>상점 아이템 데이터</summary>
[System.Serializable]
public struct ShopItem
{
    public string itemName;
    public string description;
    public int basePrice;
    public Sprite itemIcon;
    public ShopItemType itemType;
    public float upgradeValue;
    public WeaponEffectSO weaponEffect;
    public ProjectileEffectSO projectileEffect;
}

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
    [SerializeField] private List<ShopItem> _shopItems = new List<ShopItem>();

    [TabGroup("Settings")]
    [SerializeField] private bool _isStopOnOpen = true; 

    [TabGroup("Pricing")]
    [Header("Price Scaling")]
    [SerializeField] private float _priceInflationRate = 1.1f;

    [TabGroup("Pricing")]
    [SerializeField] private Dictionary<ShopItemType, int> _purchaseCount = new Dictionary<ShopItemType, int>();

    [TabGroup("UI")]
    [Header("Shop UI")]
    [SerializeField] private GameObject _shopPanel;

    [TabGroup("UI")]
    [SerializeField] private Transform _shopItemContainer;

    [TabGroup("UI")]
    [SerializeField] private GameObject _shopItemPrefab;

    [TabGroup("Temporary Buffs")]
    [Header("Temporary Buff Settings")]
    [SerializeField] private float _temporaryBuffDuration = 30f;
    #endregion

    #region Serialized Methods - Debug Buttons
    [Button("Test Weapon Damage")]
    [GUIColor(0.4f, 0.8f, 1f)]
    private void TestWeaponDamage()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.WeaponDamage,
            upgradeValue = 10f
        };
        ApplyItemEffect(testItem);
    }

    [Button("Test Fire Rate")]
    [GUIColor(0.4f, 0.8f, 1f)]
    private void TestFireRate()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.WeaponFireRate,
            upgradeValue = 1f
        };
        ApplyItemEffect(testItem);
    }

    [Button("Test Projectile Speed")]
    [GUIColor(0.8f, 0.4f, 1f)]
    private void TestProjectileSpeed()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.ProjectileSpeed,
            upgradeValue = 5f
        };
        ApplyItemEffect(testItem);
    }

    [Button("Test Projectile Lifetime")]
    [GUIColor(0.8f, 0.4f, 1f)]
    private void TestProjectileLifetime()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.ProjectileLifetime,
            upgradeValue = 1f
        };
        ApplyItemEffect(testItem);
    }

    [Button("Test Heal Player")]
    [GUIColor(1f, 0.4f, 0.4f)]
    private void TestHealPlayer()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.PlayerHeal,
            upgradeValue = 25f
        };
        ApplyItemEffect(testItem);
    }

    [Button("Test Max Health")]
    [GUIColor(1f, 0.4f, 0.4f)]
    private void TestMaxHealth()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.PlayerMaxHealth,
            upgradeValue = 20f
        };
        ApplyItemEffect(testItem);
    }

    [Button("Test Move Speed")]
    [GUIColor(1f, 0.4f, 0.4f)]
    private void TestMoveSpeed()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.PlayerMoveSpeed,
            upgradeValue = 1f
        };
        ApplyItemEffect(testItem);
    }

    [Button("Test Temp Damage")]
    [GUIColor(1f, 0.8f, 0.4f)]
    private void TestTempDamage()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.TemporaryWeaponDamage,
            upgradeValue = 15f
        };
        ApplyItemEffect(testItem);
    }

    [Button("Test Temp Speed")]
    [GUIColor(1f, 0.8f, 0.4f)]
    private void TestTempSpeed()
    {
        ShopItem testItem = new ShopItem
        {
            itemType = ShopItemType.TemporaryMoveSpeed,
            upgradeValue = 2f
        };
        ApplyItemEffect(testItem);
    }
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsShopOpen { get; private set; }

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

        ShopItem item = _shopItems[itemIndex];
        int currentPrice = GetCurrentPrice(itemIndex);

        if (!_gameManager.SpendGold(currentPrice))
            return false;

        ProcessPurchase(item, itemIndex);
        RefreshShopUI();

        Debug.Log($"[ShopManager] Purchased {item.itemName} for {currentPrice} gold", this);
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

        ShopItem item = _shopItems[itemIndex];
        int purchaseCount = GetPurchaseCount(item.itemType);

        return CalculateInflatedPrice(item.basePrice, purchaseCount);
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

        for (int i = 0; i < _shopItems.Count; i++)
        {
            CreateShopItemUI(_shopItems[i], i);
        }
    }

    private void CreateShopItemUI(ShopItem item, int itemIndex)
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
    private void ProcessPurchase(ShopItem item, int itemIndex)
    {
        ApplyItemEffect(item);
        UpdatePurchaseCount(item.itemType);
    }

    private void ApplyItemEffect(ShopItem item)
    {
        switch (item.itemType)
        {
            case ShopItemType.WeaponDamage:
                _upgradeManager.UpgradeWeaponDamage(item.upgradeValue);
                break;

            case ShopItemType.WeaponFireRate:
                _upgradeManager.UpgradeWeaponFireRate(item.upgradeValue);
                break;

            case ShopItemType.ProjectileSpeed:
                _upgradeManager.UpgradeProjectileSpeed(item.upgradeValue);
                break;

            case ShopItemType.ProjectileLifetime:
                _upgradeManager.UpgradeProjectileLifetime(item.upgradeValue);
                break;

            case ShopItemType.PlayerHeal:
                _upgradeManager.HealPlayer(item.upgradeValue);
                break;

            case ShopItemType.PlayerMaxHealth:
                _upgradeManager.UpgradePlayerMaxHealth(item.upgradeValue);
                break;

            case ShopItemType.PlayerMoveSpeed:
                _upgradeManager.UpgradePlayerMoveSpeed(item.upgradeValue);
                break;

            case ShopItemType.WeaponEffect:
                if (item.weaponEffect != null)
                    _upgradeManager.AddWeaponEffect(item.weaponEffect);
                break;

            case ShopItemType.ProjectileEffect:
                if (item.projectileEffect != null)
                    _upgradeManager.AddProjectileEffect(item.projectileEffect);
                break;

            case ShopItemType.TemporaryWeaponDamage:
                _upgradeManager.ApplyTemporaryWeaponDamageBuff(item.upgradeValue, _temporaryBuffDuration);
                break;

            case ShopItemType.TemporaryMoveSpeed:
                _upgradeManager.ApplyTemporaryMoveSpeedBuff(item.upgradeValue, _temporaryBuffDuration);
                break;

            default:
                Debug.LogWarning($"[ShopManager] Unknown item type: {item.itemType}", this);
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
    private int CalculateInflatedPrice(int basePrice, int purchaseCount)
    {
        if (purchaseCount <= 0)
            return basePrice;

        float inflatedPrice = basePrice * Mathf.Pow(_priceInflationRate, purchaseCount);
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
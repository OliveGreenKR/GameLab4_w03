using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 UI 관리자
/// UI 패널 제어, 입력 처리 및 시간정지 관리
/// </summary>
public class ShopUI : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("References")]
    [Header("Core Systems")]
    [Required]
    [SerializeField] private ShopManager _shopManager;

    [TabGroup("References")]
    [Required]
    [SerializeField] private GameManager _gameManager;

    [TabGroup("UI")]
    [Header("Shop UI")]
    [Required]
    [SerializeField] private GameObject _shopPanel;

    [TabGroup("UI")]
    [Required]
    [SerializeField] private Transform _shopItemContainer;

    [TabGroup("UI")]
    [Required]
    [SerializeField] private GameObject _shopItemPrefab;

    [TabGroup("Settings")]
    [Header("Behavior")]
    [SerializeField] private bool _isStopOnOpen = true;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsShopOpen { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CreatedItemUICount { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }
    #endregion

    #region Private Fields
    private InputSystem_Actions _inputs;
    private List<ShopItemUI> _createdItemUIs = new List<ShopItemUI>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
       
    }

    private void Start()
    {
        if (_inputs == null)
        {
            _inputs = GameManager.Instance.InputActions;
        }

        if (!ValidateReferences())
        {
            Debug.LogError("[ShopUI] Required references not assigned!", this);
            enabled = false;
            return;
        }

        InitializeShopUI();
        SubscribeToShopEvents();
        Debug.Log("[ShopUI] Initialized successfully", this);
    }

    private void OnEnable()
    {
        if (_inputs == null)
            _inputs = GameManager.Instance.InputActions;

        if (_inputs != null)
        {
            _inputs.UI.Enable();
            _inputs.UI.Shop.performed -= OnShopTogglePerformed;
            _inputs.UI.Shop.performed += OnShopTogglePerformed;
            _inputs.UI.Cancel.performed -= OnCloseShopPerformed;
            _inputs.UI.Cancel.performed += OnCloseShopPerformed;
        }
    }

    private void OnDisable()
    {
        if (_inputs == null)
            _inputs = GameManager.Instance.InputActions;

        if (_inputs != null)
        {
            _inputs.UI.Disable();
            _inputs.UI.Shop.performed -= OnShopTogglePerformed;
            _inputs.UI.Cancel.performed -= OnCloseShopPerformed;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromShopEvents();

        if (_inputs != null)
        {
            _inputs.UI.Disable();
        }
    }
    #endregion

    #region Public Methods - Shop Control
    /// <summary>상점 열기</summary>
    public void OpenShop()
    {
        if (!IsInitialized || IsShopOpen) return;

        IsShopOpen = true;

        // UI 패널 활성화
        if (_shopPanel != null)
        {
            _shopPanel.SetActive(true);
        }

        // 상점 UI 갱신
        RefreshShopUI();

        // 시간 정지 (설정에 따라)
        if (_isStopOnOpen)
        {
            Time.timeScale = 0f;
            //입력 비활성화
            _inputs.Player.Disable();
        }

        Debug.Log("[ShopUI] Shop opened", this);
    }

    /// <summary>상점 닫기</summary>
    public void CloseShop()
    {
        if (!IsInitialized || !IsShopOpen) return;

        IsShopOpen = false;

        // UI 패널 비활성화
        if (_shopPanel != null)
        {
            _shopPanel.SetActive(false);
        }

        // 시간 재개 (설정에 따라)
        if (_isStopOnOpen)
        {
            Time.timeScale = 1f;
            
        }
        _inputs.Player.Enable();
        Debug.Log("[ShopUI] Shop closed", this);
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

    #region Private Methods - UI Management
    private void RefreshShopUI()
    {
        if (!IsInitialized || _shopItemContainer == null || _shopItemPrefab == null)
            return;

        // 기존 UI 요소들 정리
        ClearShopItemUI();

        // ShopManager에서 아이템 목록 가져오기
        var shopItems = _shopManager.ShopItems;
        if (shopItems == null) return;

        // 각 아이템에 대해 UI 생성
        for (int i = 0; i < shopItems.Count; i++)
        {
            CreateShopItemUI(shopItems[i], i);
        }

        CreatedItemUICount = _createdItemUIs.Count;
    }

    private void CreateShopItemUI(ShopItemSO item, int itemIndex)
    {
        if (_shopItemContainer == null || _shopItemPrefab == null || item == null)
            return;

        Debug.Log($"[ShopUI] Creating UI for item {item.ItemName} at index {itemIndex}", this);

        // 프리팹 인스턴스화
        GameObject itemUIObject = Instantiate(_shopItemPrefab, _shopItemContainer);

        // ShopItemUI 컴포넌트 가져오기
        ShopItemUI shopItemUI = itemUIObject.GetComponent<ShopItemUI>();
        if (shopItemUI == null)
        {
            Debug.LogError("[ShopUI] ShopItemUI component not found on prefab!", this);
            Destroy(itemUIObject);
            return;
        }

        // 현재 가격과 구매 가능성 계산
        int currentPrice = _shopManager.GetCurrentPrice(itemIndex);
        bool canAfford = _shopManager.CanPurchaseItem(itemIndex);

        // ShopItemUI 설정
        shopItemUI.Setup(item, currentPrice, () => OnItemPurchaseRequested(itemIndex));

        // 초기 시각적 상태 설정
        shopItemUI.UpdateVisualState(currentPrice, canAfford);

        // 생성된 UI 목록에 추가
        _createdItemUIs.Add(shopItemUI);
    }

    private void ClearShopItemUI()
    {
        if (_shopItemContainer == null) return;

        // 생성된 UI 목록 정리
        _createdItemUIs.Clear();

        // 컨테이너의 모든 자식 제거
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

        CreatedItemUICount = 0;
    }

    private void UpdateItemUIState(int itemIndex)
    {
        if (!IsInitialized || itemIndex < 0 || itemIndex >= _createdItemUIs.Count)
            return;

        ShopItemUI itemUI = _createdItemUIs[itemIndex];
        if (itemUI == null) return;

        // 현재 가격과 구매 가능성 재계산
        int currentPrice = _shopManager.GetCurrentPrice(itemIndex);
        bool canAfford = _shopManager.CanPurchaseItem(itemIndex);

        // UI 상태 업데이트
        itemUI.UpdateVisualState(currentPrice, canAfford);
    }

    private void UpdateAllItemUIStates()
    {
        for (int i = 0; i < _createdItemUIs.Count; i++)
        {
            UpdateItemUIState(i);
        }
    }

    private void OnItemPurchaseRequested(int itemIndex)
    {
        if (!IsInitialized || _shopManager == null)
            return;

        // ShopManager에 구매 요청
        bool success = _shopManager.PurchaseItem(itemIndex);

        // 결과는 ShopManager 이벤트를 통해 처리됨
        Debug.Log($"[ShopUI] Purchase requested for item {itemIndex}, result: {success}", this);
    }
    #endregion

    #region Private Methods - Event Handling
    private void SubscribeToShopEvents()
    {
        if (_shopManager != null)
        {
            _shopManager.OnShopInitialized -= OnShopInitialized;
            _shopManager.OnShopInitialized += OnShopInitialized;
            _shopManager.OnShopDataRefreshed -= OnShopDataRefreshed;
            _shopManager.OnShopDataRefreshed += OnShopDataRefreshed;
            _shopManager.OnItemPurchased -= OnItemPurchased;
            _shopManager.OnItemPurchased += OnItemPurchased;
            _shopManager.OnItemPurchaseFailed -= OnItemPurchaseFailed;
            _shopManager.OnItemPurchaseFailed += OnItemPurchaseFailed;
            _shopManager.OnPriceChanged -= OnPriceChanged;
            _shopManager.OnPriceChanged += OnPriceChanged;
        }
    }

    private void UnsubscribeFromShopEvents()
    {
        if (_shopManager != null)
        {
            _shopManager.OnShopInitialized -= OnShopInitialized;
            _shopManager.OnShopDataRefreshed -= OnShopDataRefreshed;
            _shopManager.OnItemPurchased -= OnItemPurchased;
            _shopManager.OnItemPurchaseFailed -= OnItemPurchaseFailed;
            _shopManager.OnPriceChanged -= OnPriceChanged;
        }
    }

    private void OnShopInitialized()
    {
        Debug.Log("[ShopUI] Shop initialized, refreshing UI", this);
        RefreshShopUI();
    }

    private void OnShopDataRefreshed()
    {
        Debug.Log("[ShopUI] Shop data refreshed, updating UI", this);
        RefreshShopUI();
    }

    private void OnItemPurchased(int itemIndex, ShopItemSO item, int finalPrice)
    {
        Debug.Log($"[ShopUI] Item purchased: {item.ItemName} for {finalPrice} gold", this);

        // 구매 후 전체 UI 상태 업데이트 (골드 변경으로 인한 구매가능성 변화)
        UpdateAllItemUIStates();
    }

    private void OnItemPurchaseFailed(int itemIndex, string reason)
    {
        Debug.LogWarning($"[ShopUI] Purchase failed for item {itemIndex}: {reason}", this);

        // 실패 피드백 처리 (필요시 UI 효과 추가)
        UpdateItemUIState(itemIndex);
    }

    private void OnPriceChanged(int itemIndex, int newPrice)
    {
        Debug.Log($"[ShopUI] Price changed for item {itemIndex}: {newPrice} gold", this);
        UpdateItemUIState(itemIndex);
    }
    #endregion

    #region Private Methods - Input Handling
    private void OnShopTogglePerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsInitialized) return;

        ToggleShop();
    }

    private void OnCloseShopPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (!IsInitialized || !IsShopOpen) return;

        CloseShop();
    }
    #endregion

    #region Private Methods - Initialization
    private bool ValidateReferences()
    {
        if (_shopManager == null)
        {
            Debug.LogError("[ShopUI] ShopManager not assigned!", this);
            return false;
        }

        if (_gameManager == null)
        {
            Debug.LogError("[ShopUI] GameManager not assigned!", this);
            return false;
        }

        if (_shopPanel == null)
        {
            Debug.LogError("[ShopUI] Shop Panel not assigned!", this);
            return false;
        }

        if (_shopItemContainer == null)
        {
            Debug.LogError("[ShopUI] Shop Item Container not assigned!", this);
            return false;
        }

        if (_shopItemPrefab == null)
        {
            Debug.LogError("[ShopUI] Shop Item Prefab not assigned!", this);
            return false;
        }

        return true;
    }

    private void InitializeShopUI()
    {
        // 초기 상점 상태 설정
        IsShopOpen = false;

        // 상점 패널 비활성화
        if (_shopPanel != null)
        {
            _shopPanel.SetActive(false);
        }

        // 기존 UI 요소들 정리
        ClearShopItemUI();

        IsInitialized = true;
    }
    #endregion
}
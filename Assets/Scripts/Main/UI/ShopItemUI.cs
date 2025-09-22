using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 상점 아이템 UI 컴포넌트
/// 아이템 표시, 상태 관리 및 구매 처리
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    #region Serialized Fields
    [TabGroup("UI Components")]
    [Header("Visual Elements")]
    [Required]
    [SerializeField] private Image _iconImage;

    [TabGroup("UI Components")]
    [Required]
    [SerializeField] private Image _overlayImage;

    [TabGroup("UI Components")]
    [Header("Text Elements")]
    [Required]
    [SerializeField] private TextMeshProUGUI _nameText;

    [TabGroup("UI Components")]
    [Required]
    [SerializeField] private TextMeshProUGUI _priceText;

    [TabGroup("UI Components")]
    [Required]
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [TabGroup("UI Components")]
    [Header("Interaction")]
    [Required]
    [SerializeField] private Button _purchaseButton;

    [TabGroup("Settings")]
    [Header("Visual Settings")]
    [SerializeField] private Color _affordableColor = Color.white;

    [TabGroup("Settings")]
    [SerializeField] private Color _unaffordableColor = Color.gray;

    [TabGroup("Settings")]
    [SerializeField] private float _overlayAlpha = 0.5f;
    #endregion

    #region Properties
    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public ShopItemSO CurrentItem { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public int CurrentPrice { get; private set; }

    [TabGroup("Debug")]
    [ShowInInspector, ReadOnly]
    public bool IsInitialized { get; private set; }
    #endregion

    #region Private Fields
    private System.Action _onPurchaseClicked;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (!ValidateComponents())
        {
            Debug.LogError("[ShopItemUI] Required components not assigned!", this);
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        SubscribeToButtonEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromButtonEvents();
    }
    #endregion

    #region Public Methods - Setup
    /// <summary>아이템 UI 초기 설정</summary>
    /// <param name="item">표시할 아이템 데이터</param>
    /// <param name="price">현재 가격</param>
    /// <param name="onPurchaseCallback">구매 클릭 콜백</param>
    public void Setup(ShopItemSO item, int price, System.Action onPurchaseCallback)
    {
        if (item == null)
        {
            Debug.LogError("[ShopItemUI] Cannot setup with null item", this);
            return;
        }

        CurrentItem = item;
        CurrentPrice = price;
        _onPurchaseClicked = onPurchaseCallback;

        UpdateItemDisplay();
        UpdatePriceDisplay();

        IsInitialized = true;

        Debug.Log($"[ShopItemUI] Setup completed for {item.ItemName}", this);
    }

    /// <summary>아이템 시각적 상태 업데이트</summary>
    /// <param name="newPrice">새로운 가격</param>
    /// <param name="isAffordable">구매 가능 여부 (ShopManager에서 계산됨)</param>
    public void UpdateVisualState(int newPrice, bool isAffordable)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[ShopItemUI] Cannot update state: not initialized", this);
            return;
        }

        CurrentPrice = newPrice;

        UpdatePriceDisplay();
        UpdateAffordableState(isAffordable);
    }
    #endregion

    #region Private Methods - UI Updates
    private void UpdateItemDisplay()
    {
        if (CurrentItem == null) return;

        // 아이템 아이콘 설정
        if (_iconImage != null)
        {
            _iconImage.sprite = CurrentItem.ItemIcon;
        }

        // 아이템 이름 설정
        if (_nameText != null)
        {
            _nameText.text = CurrentItem.ItemName;
        }

        // 아이템 설명 설정
        if (_descriptionText != null)
        {
            _descriptionText.text = CurrentItem.Description;
        }
    }

    private void UpdatePriceDisplay()
    {
        if (_priceText != null)
        {
            _priceText.text = $"{CurrentPrice} Gold";
        }
    }

    private void UpdateAffordableState(bool isAffordable)
    {
        // 오버레이 표시/숨김
        SetOverlayActive(!isAffordable);

        // 버튼 상호작용 설정
        SetButtonInteractable(isAffordable);

        // 텍스트 색상 변경
        Color textColor = isAffordable ? _affordableColor : _unaffordableColor;

        if (_nameText != null)
            _nameText.color = textColor;

        if (_priceText != null)
            _priceText.color = textColor;

        if (_descriptionText != null)
            _descriptionText.color = textColor;
    }

    private void SetOverlayActive(bool active)
    {
        if (_overlayImage != null)
        {
            _overlayImage.gameObject.SetActive(active);

            if (active)
            {
                Color overlayColor = _overlayImage.color;
                overlayColor.a = _overlayAlpha;
                _overlayImage.color = overlayColor;
            }
        }
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (_purchaseButton != null)
        {
            _purchaseButton.interactable = interactable;
        }
    }
    #endregion

    #region Private Methods - Event Handling
    private void OnPurchaseButtonClicked()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[ShopItemUI] Item not initialized, cannot purchase", this);
            return;
        }

        _onPurchaseClicked?.Invoke();
    }

    private void SubscribeToButtonEvents()
    {
        if (_purchaseButton != null)
        {
            _purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
        }
    }

    private void UnsubscribeFromButtonEvents()
    {
        if (_purchaseButton != null)
        {
            _purchaseButton.onClick.RemoveListener(OnPurchaseButtonClicked);
        }
    }
    #endregion

    #region Private Methods - Validation
    private bool ValidateComponents()
    {
        if (_iconImage == null)
        {
            Debug.LogError("[ShopItemUI] Icon Image not assigned!", this);
            return false;
        }

        if (_overlayImage == null)
        {
            Debug.LogError("[ShopItemUI] Overlay Image not assigned!", this);
            return false;
        }

        if (_nameText == null)
        {
            Debug.LogError("[ShopItemUI] Name Text not assigned!", this);
            return false;
        }

        if (_priceText == null)
        {
            Debug.LogError("[ShopItemUI] Price Text not assigned!", this);
            return false;
        }

        if (_descriptionText == null)
        {
            Debug.LogError("[ShopItemUI] Description Text not assigned!", this);
            return false;
        }

        if (_purchaseButton == null)
        {
            Debug.LogError("[ShopItemUI] Purchase Button not assigned!", this);
            return false;
        }

        return true;
    }
    #endregion
}
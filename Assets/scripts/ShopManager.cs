using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Diagnostics; // 添加用于堆栈跟踪

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Configuration")]
    public ShopData shopData;

    [Header("UI References")]
    [SerializeField] private Transform shopContent;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private TMP_Text totalPriceText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button buyButton;

    private ItemData _selectedItem;
    private int _currentQuantity = 1;
    private ShopItemUI _selectedUI;
    public bool IsOpen { get; private set; }

    private NetworkInventory _localPlayerInventory;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UnityEngine.Debug.Log("[ShopManager] Start called");

        // 添加按钮事件监听器的详细日志
        if (buyButton != null)
        {
            UnityEngine.Debug.Log("[ShopManager] Buy button found, adding listener");
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }
        else
        {
            UnityEngine.Debug.LogError("[ShopManager] Buy button is NULL!");
        }

        if (quantityInput != null)
        {
            quantityInput.onValueChanged.AddListener(OnQuantityChanged);
        }
        else
        {
            UnityEngine.Debug.LogError("[ShopManager] Quantity input is NULL!");
        }

        CloseShop();
    }

    // 添加堆栈跟踪的按钮点击处理
    private void OnBuyButtonClicked()
    {
        // 获取调用堆栈
        StackTrace stackTrace = new StackTrace();
        UnityEngine.Debug.Log($"[ShopManager] Buy button clicked! Stack trace:\n{stackTrace}");

        ProcessPurchase();
    }

    public void SetLocalPlayerInventory(NetworkInventory inventory)
    {
        _localPlayerInventory = inventory;
        UnityEngine.Debug.Log($"[ShopManager] Set local player inventory: {(inventory != null ? "Success" : "FAILURE - NULL")}");
    }

    public void InitializeShop()
    {
        UnityEngine.Debug.Log($"[ShopManager] Initializing shop with data: {(shopData != null ? shopData.name : "NULL")}");
        ClearShopContent();

        if (shopData == null)
        {
            UnityEngine.Debug.LogError("ShopData is not assigned!");
            return;
        }

        UnityEngine.Debug.Log($"[ShopManager] Shop has {shopData.itemsForSale.Count} items");

        foreach (var item in shopData.itemsForSale)
        {
            CreateShopItem(item);
        }
    }

    private void CreateShopItem(ItemData itemData)
    {
        if (shopItemPrefab == null)
        {
            UnityEngine.Debug.LogError("Shop item prefab is not assigned!");
            return;
        }

        var itemUI = Instantiate(shopItemPrefab, shopContent).GetComponent<ShopItemUI>();
        if (itemUI != null)
        {
            itemUI.Initialize(itemData, this);
            UnityEngine.Debug.Log($"[ShopManager] Created shop item: {itemData.itemName}");
        }
        else
        {
            UnityEngine.Debug.LogError("Shop item prefab is missing ShopItemUI component!");
        }
    }

    public void SelectItem(ItemData item, ShopItemUI ui)
    {
        UnityEngine.Debug.Log($"[ShopManager] Selected item: {item.itemName}");

        _selectedUI?.Deselect();

        _selectedItem = item;
        _selectedUI = ui;
        _selectedUI.Select();

        UpdateTotalPrice();
    }

    private void OnQuantityChanged(string input)
    {
        if (int.TryParse(input, out int quantity))
        {
            _currentQuantity = Mathf.Clamp(quantity, 1, 999);
            UnityEngine.Debug.Log($"[ShopManager] Quantity changed to: {_currentQuantity}");
            UpdateTotalPrice();
        }
    }

    private void UpdateTotalPrice()
    {
        totalPriceText.text = _selectedItem == null
            ? "Total price: 0"
            : $"Total price: {_selectedItem.price * _currentQuantity}";
    }

    private void ProcessPurchase()
    {
        UnityEngine.Debug.Log("[ShopManager] Purchase process started");

        if (!ValidatePurchase())
        {
            UnityEngine.Debug.Log("[ShopManager] Purchase validation failed");
            return;
        }

        int totalCost = _selectedItem.price * _currentQuantity;
        UnityEngine.Debug.Log($"[ShopManager] Processing purchase: {_currentQuantity}x {_selectedItem.itemName} for {totalCost}");

        if (!MoneyManager.Instance.CanAfford(totalCost))
        {
            UnityEngine.Debug.Log($"[ShopManager] Insufficient funds: {MoneyManager.Instance.GetCurrencyName()}");
            ShowMessage($"{MoneyManager.Instance.GetCurrencyName()} insufficient!", Color.red);
            return;
        }

        CompletePurchase(totalCost);
    }

    private bool ValidatePurchase()
    {
        UnityEngine.Debug.Log("[ShopManager] Validating purchase...");

        if (_selectedItem == null)
        {
            UnityEngine.Debug.LogError("[ShopManager] Validation failed: No item selected");
            ShowMessage("Please select an item first!", Color.red);
            return false;
        }

        if (_localPlayerInventory == null)
        {
            UnityEngine.Debug.LogError("[ShopManager] Validation failed: Local player inventory is NULL");
            ShowMessage("Inventory not found!", Color.red);
            return false;
        }

        UnityEngine.Debug.Log($"[ShopManager] Validation passed");
        return true;
    }

    private void CompletePurchase(int totalCost)
    {
        UnityEngine.Debug.Log("[ShopManager] Completing purchase...");

        // 尝试扣钱
        bool moneyRemoved = MoneyManager.Instance.RemoveMoney(totalCost);
        UnityEngine.Debug.Log($"[ShopManager] Money removal result: {moneyRemoved}");

        if (!moneyRemoved)
        {
            UnityEngine.Debug.LogError("[ShopManager] Money removal failed!");
            ShowMessage("Failed to complete purchase!", Color.red);
            return;
        }

        // 尝试添加物品到库存
        UnityEngine.Debug.Log($"[ShopManager] Adding item to inventory: {_selectedItem.itemID} x {_currentQuantity}");
        _localPlayerInventory.AddItem(_selectedItem.itemID, _currentQuantity);

        UnityEngine.Debug.Log($"[ShopManager] Purchase completed successfully");
        ShowMessage($"Purchased {_currentQuantity}x {_selectedItem.itemName}!", Color.green);
        ResetSelection();
    }

    private void ShowMessage(string text, Color color)
    {
        if (messageText != null)
        {
            messageText.text = text;
            messageText.color = color;
            messageText.gameObject.SetActive(true);
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), 2f);
        }
    }

    private void HideMessage()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }

    private void ResetSelection()
    {
        UnityEngine.Debug.Log("[ShopManager] Resetting selection");

        _selectedUI?.Deselect();
        _selectedItem = null;
        _selectedUI = null;
        if (quantityInput != null)
        {
            quantityInput.text = "1";
        }
        _currentQuantity = 1;
        UpdateTotalPrice();
    }

    private void ClearShopContent()
    {
        UnityEngine.Debug.Log("[ShopManager] Clearing shop content");

        foreach (Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }
    }

    public void SetCurrentShop(ShopData newShopData)
    {
        UnityEngine.Debug.Log($"[ShopManager] Setting current shop: {(newShopData != null ? newShopData.name : "NULL")}");
        shopData = newShopData;
        InitializeShop();
    }

    public void OpenShop()
    {
        UnityEngine.Debug.Log("[ShopManager] Opening shop");
        IsOpen = true;
        if (shopContent != null && shopContent.parent != null)
        {
            shopContent.parent.gameObject.SetActive(true);
        }
        ResetSelection();

        // 尝试在打开商店时查找本地玩家库存
        if (_localPlayerInventory == null)
        {
            UnityEngine.Debug.Log("[ShopManager] Local player inventory not set, attempting to find...");
            FindLocalPlayerInventory();
        }
        else
        {
            UnityEngine.Debug.Log("[ShopManager] Local player inventory already set");
        }
    }

    private void FindLocalPlayerInventory()
    {
        UnityEngine.Debug.Log("[ShopManager] Searching for local player inventory...");

        PhotonView[] views = FindObjectsOfType<PhotonView>();
        UnityEngine.Debug.Log($"[ShopManager] Found {views.Length} PhotonViews in scene");

        foreach (var view in views)
        {
            if (view.IsMine)
            {
                UnityEngine.Debug.Log($"[ShopManager] Found local player PhotonView: {view.ViewID}");

                _localPlayerInventory = view.GetComponent<NetworkInventory>();
                if (_localPlayerInventory != null)
                {
                    UnityEngine.Debug.Log("[ShopManager] Found local player inventory");
                    return;
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"[ShopManager] PhotonView {view.ViewID} has no NetworkInventory component");
                }
            }
        }
        UnityEngine.Debug.LogWarning("[ShopManager] Failed to find local player inventory");
    }

    public void CloseShop()
    {
        UnityEngine.Debug.Log("[ShopManager] Closing shop");
        IsOpen = false;
        if (shopContent != null && shopContent.parent != null)
        {
            shopContent.parent.gameObject.SetActive(false);
        }
        ResetSelection();
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }
    }
}
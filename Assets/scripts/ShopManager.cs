using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

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
        InitializeShop();
        CloseShop();

        buyButton.onClick.AddListener(ProcessPurchase);
        quantityInput.onValueChanged.AddListener(OnQuantityChanged);
    }

    public void SetLocalPlayerInventory(NetworkInventory inventory)
    {
        _localPlayerInventory = inventory;
    }

    private void InitializeShop()
    {
        ClearShopContent();

        if (shopData == null)
        {
            Debug.LogError("ShopData is not assigned!");
            return;
        }

        foreach (var item in shopData.itemsForSale)
        {
            CreateShopItem(item);
        }
    }

    private void CreateShopItem(ItemData itemData)
    {
        if (shopItemPrefab == null)
        {
            Debug.LogError("Shop item prefab is not assigned!");
            return;
        }

        var itemUI = Instantiate(shopItemPrefab, shopContent).GetComponent<ShopItemUI>();
        if (itemUI != null)
        {
            itemUI.Initialize(itemData, this);
        }
        else
        {
            Debug.LogError("Shop item prefab is missing ShopItemUI component!");
        }
    }

    public void SelectItem(ItemData item, ShopItemUI ui)
    {
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
        if (!ValidatePurchase()) return;

        int totalCost = _selectedItem.price * _currentQuantity;

        if (!MoneyManager.Instance.CanAfford(totalCost))
        {
            ShowMessage($"{MoneyManager.Instance.GetCurrencyName()} insufficient!", Color.red);
            return;
        }

        CompletePurchase(totalCost);
    }

    private bool ValidatePurchase()
    {
        if (_selectedItem == null)
        {
            ShowMessage("Please select an item first!", Color.red);
            return false;
        }

        if (_localPlayerInventory == null)
        {
            ShowMessage("Inventory not found!", Color.red);
            Debug.LogError("Local player inventory reference is null!");
            return false;
        }

        return true;
    }

    private void CompletePurchase(int totalCost)
    {
        if (!MoneyManager.Instance.RemoveMoney(totalCost))
        {
            ShowMessage("Failed to complete purchase!", Color.red);
            return;
        }

        _localPlayerInventory.AddItem(_selectedItem, _currentQuantity);
        ShowMessage($"Purchased {_currentQuantity}x {_selectedItem.itemName}!", Color.green);
        ResetSelection();
    }

    private void ShowMessage(string text, Color color)
    {
        messageText.text = text;
        messageText.color = color;
        messageText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 2f);
    }

    private void HideMessage() => messageText.gameObject.SetActive(false);

    private void ResetSelection()
    {
        _selectedUI?.Deselect();
        _selectedItem = null;
        _selectedUI = null;
        quantityInput.text = "1";
        UpdateTotalPrice();
    }

    private void ClearShopContent()
    {
        foreach (Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }
    }

    // 添加回 SetCurrentShop 方法
    public void SetCurrentShop(ShopData newShopData)
    {
        shopData = newShopData;
        InitializeShop();
    }

    public void OpenShop()
    {
        IsOpen = true;
        shopContent.parent.gameObject.SetActive(true);
        ResetSelection();
    }

    public void CloseShop()
    {
        IsOpen = false;
        shopContent.parent.gameObject.SetActive(false);
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
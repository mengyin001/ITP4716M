using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;
    [Header("配置")]
    public ShopData shopData;
    //public Inventory playerInventory;

    [Header("UI组件")]
    [SerializeField] private Transform shopContent;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private TMP_Text totalPriceText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button buyButton;

    private ItemData _selectedItem;
    private int _currentQuantity = 1;
    private ShopItemUI _selectedUI;
    public bool isOpen = false;

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
        quantityInput.onValueChanged.AddListener(UpdateQuantity);
    }

    private void InitializeShop()
    {
        ClearShopContent();
        foreach (Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }
        foreach (ItemData item in shopData.itemsForSale)
        {
            CreateShopItem(item);
        }
    }

    private void CreateShopItem(ItemData itemData)
    {
        if (shopItemPrefab == null)
        {
            Debug.LogError("未设置商品预制体!");
            return;
        }

        GameObject newItem = Instantiate(shopItemPrefab, shopContent);
        ShopItemUI itemUI = newItem.GetComponent<ShopItemUI>();

        if (itemUI != null)
        {
            itemUI.Initialize(itemData, this);
        }
        else
        {
            Debug.LogError("预制体缺少 ShopItemUI 组件!");
        }
    }
    public void SelectItem(ItemData item, ShopItemUI ui)
    {
        // 取消之前的选择
        if (_selectedUI != null) _selectedUI.Deselect();

        _selectedItem = item;
        _selectedUI = ui;
        _selectedUI.Select();

        UpdateTotalPrice();
    }

    private void UpdateQuantity(string input)
    {
        if (int.TryParse(input, out int quantity))
        {
            _currentQuantity = Mathf.Clamp(quantity, 1, 999);
            UpdateTotalPrice();
        }
    }

    private void UpdateTotalPrice()
    {
        if (_selectedItem == null) return;
        totalPriceText.text = $"Total price: {_selectedItem.price * _currentQuantity}";
    }

    private void ProcessPurchase()
    {
        if (_selectedItem == null)
        {
            ShowMessage("Please select the product first!", Color.red);
            return;
        }

        int totalCost = _selectedItem.price * _currentQuantity;

        // 使用MoneyManager进行金钱验证
        if (!MoneyManager.Instance.CanAfford(totalCost))
        {
            ShowMessage($"{MoneyManager.Instance.GetCurrencyName()} insufficient!", Color.red);
            return;
        }

        CompletePurchase(totalCost);
    }

    private void CompletePurchase(int totalCost)
    {
        // 通过MoneyManager扣除金钱
        bool success = MoneyManager.Instance.RemoveMoney(totalCost);

        if (success)
        {
            AddToInventory(_selectedItem, _currentQuantity);
            ShowMessage($"Successful purchase {_currentQuantity}  {_selectedItem.itemName}!", Color.green);
            ResetSelection();
        }
    }
    private void AddToInventory(ItemData item, int quantity)
    {
        /*ItemData existingItem = playerInventory.itemList.Find(i => i.itemID == item.itemID);

        if (existingItem != null)
        {
            existingItem.itemHeld += quantity;
        }
        else
        {
            ItemData newItem = Instantiate(item);
            newItem.itemHeld = quantity;
            playerInventory.itemList.Add(newItem);
        }*/
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
        if (_selectedUI != null) _selectedUI.Deselect();
        _selectedItem = null;
        _selectedUI = null;
        quantityInput.text = "1";
        totalPriceText.text = "total price: 0";
    }

    private void ClearShopContent()
    {
        foreach (Transform child in shopContent)
        {
            Destroy(child.gameObject);
        }
    }
    public void OpenShop()
    {
        isOpen = true;
        shopContent.parent.gameObject.SetActive(true);
        ResetSelection();
        NPCDialogueTrigger[] npcTriggers = FindObjectsOfType<NPCDialogueTrigger>();
        foreach (NPCDialogueTrigger trigger in npcTriggers)
        {
            trigger.HidePrompt();
        }
        Time.timeScale = 0;
    }

    public void CloseShop()
    {
        isOpen = false;
        shopContent.parent.gameObject.SetActive(false);
        ResetSelection();
        Time.timeScale = 1;
    }

    private void Update()
    {
        if (isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseShop();
            }
        }
    }
    public void SetCurrentShop(ShopData newShopData)
    {
        shopData = newShopData;
        InitializeShop();
    }
}
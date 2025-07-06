using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun; // 確保有這個 using

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [Header("配置")]
    public ShopData shopData;

    [Header("UI组件")]
    [SerializeField] private Transform shopContent;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private TMP_Text totalPriceText;
    [SerializeField] private TMP_Text messageText; // 用於顯示購買成功/失敗的短暫訊息
    [SerializeField] private Button buyButton;

    private ItemData _selectedItem;
    private int _currentQuantity = 1;
    private ShopItemUI _selectedUI;
    public bool isOpen = false;

    // 對本地玩家 NetworkInventory 的引用
    private NetworkInventory _localPlayerNetworkInventory;

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
        // 在 Start 中嘗試獲取本地玩家的 NetworkInventory
        // 如果玩家是動態實例化的，這裡可能還找不到，需要在 OpenShop 或玩家實例化後再次嘗試
        FindLocalPlayerNetworkInventory();

        InitializeShop();
        CloseShop(); // 初始關閉商店 UI

        buyButton.onClick.AddListener(ProcessPurchase); // 點擊購買按鈕時直接處理購買
        quantityInput.onValueChanged.AddListener(UpdateQuantity);
    }

    // 查找本地玩家的 NetworkInventory
    private void FindLocalPlayerNetworkInventory()
    {
        if (_localPlayerNetworkInventory != null) return; // 如果已經找到了，就返回

        PhotonView[] allPhotonViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView pv in allPhotonViews)
        {
            if (pv.IsMine) // 找到屬於本地玩家的 PhotonView
            {
                _localPlayerNetworkInventory = pv.GetComponent<NetworkInventory>();
                if (_localPlayerNetworkInventory != null)
                {
                    Debug.Log("[ShopManager] Found local player's NetworkInventory.");
                    return;
                }
            }
        }
        Debug.LogError("[ShopManager] Failed to find local player's NetworkInventory in the scene!");
        // 如果玩家是延遲實例化的，這裡可能需要一個協程來等待
        // 或者在玩家實例化後，由玩家腳本通知 ShopManager (推薦做法)
    }

    // 推薦：由玩家腳本在實例化後呼叫此方法來設置 NetworkInventory
    public void SetLocalPlayerNetworkInventory(NetworkInventory ni)
    {
        _localPlayerNetworkInventory = ni;
        Debug.Log("[ShopManager] Local player's NetworkInventory set by player script.");
    }


    private void InitializeShop()
    {
        ClearShopContent();
        if (shopData == null)
        {
            Debug.LogError("[ShopManager] shopData is null! Please assign a ShopData ScriptableObject in the Inspector.");
            return;
        }
        Debug.Log($"[ShopManager] ShopData assigned. Items for sale count: {shopData.itemsForSale.Count}");

        foreach (ItemData item in shopData.itemsForSale)
        {
            CreateShopItem(item);
        }
    }

    private void CreateShopItem(ItemData itemData)
    {
        if (shopItemPrefab == null)
        {
            Debug.LogError("[ShopManager] 未设置商品预制体! Please assign shopItemPrefab in the Inspector.");
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
            Debug.LogError("[ShopManager] 预制体缺少 ShopItemUI 组件!");
        }
    }

    public void SelectItem(ItemData item, ShopItemUI ui)
    {
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
        if (_selectedItem == null)
        {
            totalPriceText.text = "Total price: 0"; // 如果沒有選擇物品，顯示 0
            return;
        }
        totalPriceText.text = $"Total price: {_selectedItem.price * _currentQuantity}";
    }

    private void ProcessPurchase()
    {
        if (_selectedItem == null)
        {
            ShowMessage("Please select the product first!", Color.red);
            return;
        }

        if (_localPlayerNetworkInventory == null)
        {
            ShowMessage("Player inventory not found! Cannot process purchase.", Color.red);
            Debug.LogError("[ShopManager] _localPlayerNetworkInventory is null. Cannot process purchase.");
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
        bool success = MoneyManager.Instance.RemoveMoney(totalCost);

        if (success)
        {
            // 直接呼叫 NetworkInventory 的 AddItem 方法
            _localPlayerNetworkInventory.AddItem(_selectedItem, _currentQuantity);
            ShowMessage($"Successful purchase {_currentQuantity} {_selectedItem.itemName}!", Color.green);
            ResetSelection();
        }
        else
        {
            ShowMessage("Failed to deduct money! (Unexpected error)", Color.red); // 理論上 CanAfford 已經檢查過，但以防萬一
        }
    }

    private void ShowMessage(string text, Color color)
    {
        messageText.text = text;
        messageText.color = color;
        messageText.gameObject.SetActive(true);
        CancelInvoke(nameof(HideMessage));
        Invoke(nameof(HideMessage), 2f); // 默認顯示 2 秒
    }

    private void HideMessage() => messageText.gameObject.SetActive(false);

    private void ResetSelection()
    {
        if (_selectedUI != null) _selectedUI.Deselect();
        _selectedItem = null;
        _selectedUI = null;
        quantityInput.text = "1";
        totalPriceText.text = "Total price: 0";
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
        // 假設 shopContent 的父物件就是整個商店 UI 面板
        if (shopContent.parent != null)
        {
            shopContent.parent.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[ShopManager] shopContent has no parent. Cannot activate shop UI.");
            gameObject.SetActive(true); // 如果沒有父物件，就激活 ShopManager 所在的 GameObject
        }
        ResetSelection();
        // 在打開商店時，再次嘗試查找本地玩家的 NetworkInventory
        // 以防 Start 時玩家還未實例化
        if (_localPlayerNetworkInventory == null)
        {
            FindLocalPlayerNetworkInventory();
        }

        // 隱藏 NPC 對話提示 (如果有的話)
        NPCDialogueTrigger[] npcTriggers = FindObjectsOfType<NPCDialogueTrigger>();
        foreach (NPCDialogueTrigger trigger in npcTriggers)
        {
            trigger.HidePrompt();
        }
    }

    public void CloseShop()
    {
        isOpen = false;
        if (shopContent.parent != null)
        {
            shopContent.parent.gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
        ResetSelection();
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

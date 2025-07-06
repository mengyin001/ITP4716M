using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

public class ConsumableItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.8f, 0.1f, 1f); // 选中状态的颜色

    private Image itemImage;
    private Color originalColor;
    public static GameObject selectedItem;
    private Slot itemSlot;

    void Awake()
    {
        itemImage = GetComponent<Image>();
        originalColor = itemImage.color;
        itemSlot = GetComponent<Slot>();

        
        // 初始状态更新
        UpdateVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // 检查是否有有效物品
        if (itemSlot == null || string.IsNullOrEmpty(itemSlot.itemID)) return;

        if (selectedItem == null)
        {
            // 第一次点击：选择物品
            SelectItem();
        }
        else if (selectedItem == gameObject)
        {
            // 再次点击已选中的物品：直接使用
            UseItem();
        }
        else
        {
            // 点击其他物品：切换选择
            DeselectPrevious();
            SelectItem();
        }
    }

    void SelectItem()
    {
        selectedItem = gameObject;
        itemImage.color = selectedColor;
    }

    void DeselectPrevious()
    {
        if (selectedItem != null)
        {
            Image prevImage = selectedItem.GetComponent<Image>();
            if (prevImage != null) prevImage.color = originalColor;
            selectedItem = null;
        }
    }

    void UseItem()
    {
        NetworkInventory localNetworkInventory = GetLocalPlayerNetworkInventory();
            if (localNetworkInventory != null && !string.IsNullOrEmpty(itemSlot.itemID))
            {
                // 使用本地玩家的 NetworkInventory 中的物品
                localNetworkInventory.UseItem(itemSlot.itemID);

                // 更新UI
                UpdateVisualState();
                DeselectPrevious();
            }
            else
            {
                Debug.LogWarning("[ConsumableItemUI] Cannot use item: Local player's NetworkInventory not found or itemID is empty.");
            }
        }

    private NetworkInventory GetLocalPlayerNetworkInventory()
    {
        // 最佳方式是通過 InventoryManager.instance.networkInventory 來獲取
        // 因為 InventoryManager 已經負責查找和管理本地玩家的 NetworkInventory
        if (InventoryManager.instance != null && InventoryManager.instance.networkInventory != null)
        {
            return InventoryManager.instance.networkInventory;
        }

        // 如果 InventoryManager 還沒有初始化好，或者沒有找到，則嘗試查找
        // 遍歷所有 PhotonView，找到屬於本地玩家的那個
        foreach (PhotonView pv in FindObjectsOfType<PhotonView>())
        {
            // 檢查這個 PhotonView 的擁有者是否是本地玩家
            if (pv.Owner == PhotonNetwork.LocalPlayer)
            {
                NetworkInventory ni = pv.GetComponent<NetworkInventory>();
                if (ni != null)
                {
                    return ni;
                }
            }
        }

        Debug.LogError("[ConsumableItemUI] GetLocalPlayerNetworkInventory: Could not find local player's NetworkInventory!");
        return null;
    }

    void OnDisable()
    {
        // 当UI被禁用时取消选择
        if (selectedItem == gameObject)
        {
            DeselectPrevious();
        }
    }

    void OnEnable()
    {
        // 当UI启用时更新状态
        UpdateVisualState();
        
        // 如果当前物品被选中但UI被重新启用，取消选择
        if (selectedItem == gameObject)
        {
            DeselectPrevious();
        }
    }

    public void UpdateVisualState()
    {
        if (itemSlot != null)
        {
            // 设置选中状态的颜色
            if (selectedItem == gameObject)
            {
                itemImage.color = selectedColor;
            }
            else
            {
                itemImage.color = originalColor;
            }
        }
    }
}
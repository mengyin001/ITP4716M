using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image slotImage;
    public TextMeshProUGUI slotNum;

    [Header("Item Data")]
    private string _itemID;
    private int _quantity;

    // 使用属性确保数据一致性
    public string itemID
    {
        get => _itemID;
        private set => _itemID = value;
    }

    public int quantity
    {
        get => _quantity;
        private set => _quantity = value;
    }

    private InventoryManager inventoryManager;
    private int slotIndex;

    public bool IsEmpty => string.IsNullOrEmpty(itemID);

    public void Initialize(int index, InventoryManager manager)
    {
        slotIndex = index;
        inventoryManager = manager;
        ClearSlot();
    }

    public void SetItem(string id, int qty, ItemData itemData)
    {
        // 确保更新所有相关数据
        itemID = id;
        quantity = qty;

        // 设置物品图标
        if (itemData != null && itemData.icon != null)
        {
            slotImage.sprite = itemData.icon;
            slotImage.enabled = true;
        }
        else
        {
            slotImage.enabled = false;
        }

        // 更新数量显示
        slotNum.text = qty > 1 ? qty.ToString() : "";
        slotNum.enabled = qty > 1;
    }

    public void ClearSlot()
    {
        itemID = "";
        quantity = 0;

        slotImage.sprite = null;
        slotImage.enabled = false;

        slotNum.text = "";
        slotNum.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        inventoryManager.OnSlotClicked(slotIndex);
    }
}
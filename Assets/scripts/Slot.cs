using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image slotImage;
    public TextMeshProUGUI slotNum;
    // 【核心修改 1】: 添加對高亮物件的引用
    public GameObject selectionHighlight;

    [Header("Debug Info")]
    [SerializeField] private string _itemID = "";
    [SerializeField] private int _quantity = 0;

    public string itemID => _itemID;
    public int quantity => _quantity;
    public bool IsEmpty => string.IsNullOrEmpty(_itemID);

    private InventoryManager inventoryManager;
    private int slotIndex;

    public void Initialize(int index, InventoryManager manager)
    {
        slotIndex = index;
        inventoryManager = manager;
        // 確保高亮在初始化時是隱藏的
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }
        ClearSlot();
    }

    public void SetItem(string id, int qty, ItemData itemData)
    {
        _itemID = id;
        _quantity = qty;

        if (itemData != null && itemData.icon != null)
        {
            slotImage.sprite = itemData.icon;
            slotImage.enabled = true;
        }
        else
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
        }

        UpdateQuantityText();
    }

    private void UpdateQuantityText()
    {
        if (slotNum != null)
        {
            slotNum.text = _quantity > 0 ? _quantity.ToString() : "";
            slotNum.enabled = _quantity > 0;
        }
    }

    public void ClearSlot()
    {
        _itemID = "";
        _quantity = 0;

        slotImage.sprite = null;
        slotImage.enabled = false;

        if (slotNum != null)
        {
            slotNum.text = "";
            slotNum.enabled = false;
        }

        // 清空槽位時，也確保取消高亮
        SetSelected(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 點擊事件的邏輯保持不變：只通知管理者
        if (inventoryManager != null)
        {
            inventoryManager.OnSlotClicked(slotIndex);
        }
    }

    // 【核心修改 2】: 添加這個公共方法
    // 這個方法將由 InventoryManager 調用，用來控制高亮物件的顯示和隱藏
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isSelected);
        }
    }

}

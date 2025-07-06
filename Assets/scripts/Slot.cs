using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image slotImage;
    public TextMeshProUGUI slotNum;

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
        ClearSlot();
    }

    public void SetItem(string id, int qty, ItemData itemData)
    {
        // 确保数据正确设置
        _itemID = id;
        _quantity = qty;

        // 更新UI
        if (itemData != null)
        {
            if (itemData.icon != null)
            {
                slotImage.sprite = itemData.icon;
                slotImage.enabled = true;
            }
            else
            {
                Debug.LogWarning($"No icon for {id}");
                slotImage.enabled = false;
            }
        }
        else
        {
            Debug.LogError($"ItemData is null for {id}");
            slotImage.enabled = false;
        }

        // 修复：更新数量显示 - 当数量为1时也显示文本
        UpdateQuantityText();

        Debug.Log($"Slot {slotIndex} set: {id} x{qty}");
    }

    // 新增方法：更新数量显示
    private void UpdateQuantityText()
    {
        if (slotNum != null)
        {
            // 当数量大于等于1时显示文本
            slotNum.text = _quantity >= 1 ? _quantity.ToString() : "";
            slotNum.enabled = _quantity >= 1;
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
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryManager != null && !IsEmpty)
        {
            inventoryManager.OnSlotClicked(slotIndex);
        }
    }

    // 新增：调试方法，用于检查文本状态
    private void Update()
    {
#if UNITY_EDITOR
        if (!IsEmpty && slotNum != null)
        {
            Debug.Log($"Slot {slotIndex}: Quantity={_quantity}, Text='{slotNum.text}', Enabled={slotNum.enabled}", this.gameObject);
        }
#endif
    }
}
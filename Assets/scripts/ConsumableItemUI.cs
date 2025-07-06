using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

public class ConsumableItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.8f, 0.1f, 1f);
    [SerializeField] private Color zeroQuantityColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 调整为灰色半透明

    private Image itemImage;
    private Color originalColor;
    public static GameObject selectedItem;
    private Slot itemSlot;
    private NetworkInventory networkInventory;
    private ItemDatabase itemDatabase;

    void Awake()
    {
        itemImage = GetComponent<Image>();
        originalColor = itemImage.color;
        itemSlot = GetComponent<Slot>();

        // 获取网络背包和数据库引用
        networkInventory = FindObjectOfType<NetworkInventory>();
        itemDatabase = FindObjectOfType<ItemDatabase>();

        UpdateVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!HasValidItem()) return;

        // 只有本地玩家可以使用物品
        if (!PhotonNetwork.LocalPlayer.IsLocal) return;

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

    bool HasValidItem()
    {
        return itemSlot != null &&
               !string.IsNullOrEmpty(itemSlot.itemID) &&
               itemSlot.quantity > 0;
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
        if (networkInventory != null && !string.IsNullOrEmpty(itemSlot.itemID))
        {
            // 使用网络背包中的物品
            networkInventory.UseItem(itemSlot.itemID);

            // 更新UI
            UpdateVisualState();
            DeselectPrevious();
        }
    }

    void OnDisable()
    {
        if (selectedItem == gameObject)
        {
            DeselectPrevious();
        }
    }

    void OnEnable()
    {
        UpdateVisualState();
        if (selectedItem == gameObject)
        {
            DeselectPrevious();
        }
    }

    public void UpdateVisualState()
    {
        if (itemSlot != null)
        {
            bool hasItem = !string.IsNullOrEmpty(itemSlot.itemID) && itemSlot.quantity > 0;

            // 设置物品图片
            if (itemSlot.slotImage != null)
            {
                itemSlot.slotImage.enabled = hasItem;
                if (hasItem && itemDatabase != null)
                {
                    ItemData itemData = itemDatabase.GetItem(itemSlot.itemID);
                    if (itemData != null)
                    {
                        itemSlot.slotImage.sprite = itemData.icon;
                    }
                }
            }

            // 设置物品数量文本
            if (itemSlot.slotNum != null)
            {
                itemSlot.slotNum.enabled = hasItem;
                if (hasItem)
                {
                    itemSlot.slotNum.text = itemSlot.quantity > 1 ? itemSlot.quantity.ToString() : "";
                }
            }

            // 设置射线检测和颜色
            itemImage.raycastTarget = hasItem;

            if (!hasItem)
            {
                itemImage.color = zeroQuantityColor;
            }
            else if (selectedItem == gameObject)
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
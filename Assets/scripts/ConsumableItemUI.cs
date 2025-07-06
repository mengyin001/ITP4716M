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
    private NetworkInventory networkInventory;

    void Awake()
    {
        itemImage = GetComponent<Image>();
        originalColor = itemImage.color;
        itemSlot = GetComponent<Slot>();

        // 获取网络背包引用
        networkInventory = FindObjectOfType<NetworkInventory>();
        
        // 初始状态更新
        UpdateVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // 只有本地玩家可以使用物品
        if (!PhotonNetwork.LocalPlayer.IsLocal) return;

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
        if (networkInventory != null && !string.IsNullOrEmpty(itemSlot.itemID))
        {
            // 使用物品
            networkInventory.UseItem(itemSlot.itemID);
            
            // 取消选择
            DeselectPrevious();
        }
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
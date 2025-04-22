using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HotbarSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public ItemData assignedItem;
    public Image icon;
    public KeyCode hotkey;
    public int slotIndex;
    public CanvasGroup canvasGroup;

    private void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(hotkey) && assignedItem != null)
        {
            UseItem();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        Slot draggedSlot = eventData.pointerDrag.GetComponent<Slot>();
        if (draggedSlot != null && draggedSlot.slotItem != null)
        {
            AssignItem(draggedSlot.slotItem);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Right-click to clear slot
        if (eventData.button == PointerEventData.InputButton.Right && assignedItem != null)
        {
            ClearSlot();
        }
    }

    public void AssignItem(ItemData item)
    {
        assignedItem = item;
        icon.sprite = item.icon;
        icon.enabled = true;
        canvasGroup.alpha = 1f;
    }

    public void ClearSlot()
    {
        assignedItem = null;
        icon.sprite = null;
        icon.enabled = false;
    }

    private void UseItem()
    {
        if (assignedItem == null) return;

        Debug.Log($"Using {assignedItem.itemName}");

        if (assignedItem.itemType == ItemData.ItemType.Consumable)
        {
            assignedItem.itemHeld--;

            if (assignedItem.itemHeld <= 0)
            {
                ClearSlot();
            }
            InventoryManager.RefreshItem();
        }
    }
}
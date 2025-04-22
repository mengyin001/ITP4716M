using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ItemData slotItem;
    public Image slotImage;
    public TextMeshProUGUI slotNum;
    public CanvasGroup canvasGroup;

    private Transform originalParent;
    private GameObject dragIcon;
    private Canvas rootCanvas;
    private bool isDragging = false;

    private void Awake()
    {
        if (slotImage == null) slotImage = GetComponent<Image>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotItem == null || slotItem.itemHeld <= 0) return;

        isDragging = true;
        originalParent = transform.parent;

        // Create drag icon
        dragIcon = new GameObject("Drag Icon");
        dragIcon.transform.SetParent(rootCanvas.transform, false);
        dragIcon.transform.SetAsLastSibling();

        var image = dragIcon.AddComponent<Image>();
        image.sprite = slotImage.sprite;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = slotImage.rectTransform.sizeDelta;

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragIcon == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        dragIcon.GetComponent<RectTransform>().localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        Destroy(dragIcon);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Check if we dropped on a valid slot
        bool droppedOnSlot = false;

        foreach (var hovered in eventData.hovered)
        {
            // Try hotbar slot first
            HotbarSlot hotbarSlot = hovered.GetComponent<HotbarSlot>();
            if (hotbarSlot != null)
            {
                hotbarSlot.AssignItem(slotItem);
                droppedOnSlot = true;
                break;
            }

            // Then try inventory slot
            Slot inventorySlot = hovered.GetComponent<Slot>();
            if (inventorySlot != null && inventorySlot != this)
            {
                // Swap items if needed
                ItemData temp = inventorySlot.slotItem;
                inventorySlot.slotItem = slotItem;
                slotItem = temp;
                InventoryManager.RefreshItem();
                droppedOnSlot = true;
                break;
            }
        }

        // If not dropped on any slot, return to original position
        if (!droppedOnSlot)
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
        }
    }
}
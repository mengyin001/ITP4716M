using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ConsumableItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Health Settings")]
    [SerializeField] private float healthRestoreAmount = 10f;
    [SerializeField] private float doubleClickTime = 0.3f;

    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.8f, 0.1f, 1f);

    private Image itemImage;
    private Color originalColor;
    private float lastClickTime;
    public static GameObject selectedItem;
    private Slot itemSlot;


    void Awake()
    {
        itemImage = GetComponent<Image>();
        originalColor = itemImage.color;
        itemSlot = GetComponent<Slot>();
        UpdateVisualState();

        if (itemSlot.slotItem != null && itemSlot.slotItem.itemHeld > 0)
        {
            itemImage.color = originalColor;
        }

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!HasValidItem()) return;

        if (selectedItem == null)
        {
            SelectItem();
        }
        else if (selectedItem == gameObject)
        {
            if (Time.unscaledTime - lastClickTime <= doubleClickTime)
            {
                UseItem();
            }
        }
        else
        {
            DeselectPrevious();
            SelectItem();
        }

        lastClickTime = Time.unscaledTime;
    }

    bool HasValidItem()
    {
        return itemSlot != null &&
               itemSlot.slotItem != null &&
               itemSlot.slotItem.itemHeld > 0;
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
            selectedItem.GetComponent<Image>().color = originalColor;
            selectedItem = null;
        }
    }

    void UseItem()
    {
        if (ApplyHealthEffect())
        {
            UpdateItemQuantity();
            UpdateVisualState();
            UpdateLocalUI();
            DeselectPrevious();
        }
    }

    bool ApplyHealthEffect()
    {
        HealthSystem healthSystem = FindObjectOfType<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.Heal(healthRestoreAmount);
            Debug.Log($"Restored {healthRestoreAmount} health!");
            return true;
        }
        Debug.LogWarning("HealthSystem not found!");
        return false;
    }

    void UpdateItemQuantity()
    {
        if (itemSlot.slotItem != null)
        {
            itemSlot.slotItem.itemHeld--;
            itemSlot.slotItem.itemHeld = Mathf.Clamp(itemSlot.slotItem.itemHeld, 0, int.MaxValue);
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
        // Reset visual state when slot is re-enabled
        UpdateVisualState();
        if (selectedItem == gameObject)
        {
            DeselectPrevious();
        }
    }

    public void UpdateVisualState()
    {
        if (itemSlot.slotItem != null)
        {
            bool isValid = itemSlot.slotItem.itemHeld > 0;
            itemImage.raycastTarget = isValid;
            itemImage.color = isValid ? originalColor : selectedColor;

            // Update slot text immediately
            if (itemSlot.slotNum != null)
            {
                itemSlot.slotNum.text = itemSlot.slotItem.itemHeld.ToString();
            }
        }
    }

    void UpdateLocalUI()
    {
        if (itemSlot != null)
        {
            // Update the slot's text display directly
            itemSlot.slotNum.text = itemSlot.slotItem.itemHeld.ToString();
            UpdateVisualState();
        }
    }
}
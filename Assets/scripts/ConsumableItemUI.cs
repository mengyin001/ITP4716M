using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ConsumableItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Visual Feedback")]
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.8f, 0.1f, 1f);
    [SerializeField] private Color zeroQuantityColor = new Color(0.8f, 0.8f, 0.1f, 1f);

    private Image itemImage;
    private Color originalColor;
    public static GameObject selectedItem;
    private Slot itemSlot;

    void Awake()
    {
        itemImage = GetComponent<Image>();
        originalColor = itemImage.color;
        itemSlot = GetComponent<Slot>();
        UpdateVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!HasValidItem()) return;

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
        if (ApplyEffect())
        {
            UpdateItemQuantity();
            UpdateVisualState();
            UpdateLocalUI();
            DeselectPrevious();

        }
    }

    bool ApplyEffect()
    {
        HealthSystem healthSystem = FindObjectOfType<HealthSystem>();
        if (healthSystem == null)
        {
            Debug.LogWarning("HealthSystem not found!");
            return false;
        }

        if (itemSlot.slotItem == null || itemSlot.slotItem.effects == null)
            return false;

        bool effectApplied = false;

        // 应用所有效果
        foreach (var effect in itemSlot.slotItem.effects)
        {
            switch (effect.effectType)
            {
                case ItemData.EffectType.Health:
                    healthSystem.Heal(effect.effectAmount);
                    Debug.Log($"Restored {effect.effectAmount} health!");
                    effectApplied = true;
                    break;
                case ItemData.EffectType.Energy:
                    healthSystem.RestoreEnergy(effect.effectAmount);
                    Debug.Log($"Restored {effect.effectAmount} energy!");
                    effectApplied = true;
                    break;
                case ItemData.EffectType.Attack:
                    // 这里可以添加攻击效果实现
                    Debug.Log($"Attack buff applied: +{effect.effectAmount} attack!");
                    effectApplied = true;
                    break;
            }
        }

        return effectApplied;
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

            if (!isValid)
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
            itemSlot.slotNum.text = itemSlot.slotItem.itemHeld.ToString();
            UpdateVisualState();
        }
    }

    void Update()
    {
        UpdateVisualState();
    }
}
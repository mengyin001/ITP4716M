using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public Inventory myBag;
    public GameObject slotGrid;
    public Slot slotPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        RefreshItem();
    }

    public static void CreateNewItem(ItemData item)
    {
        Slot newItem = Instantiate(Instance.slotPrefab, Instance.slotGrid.transform);
        newItem.slotItem = item;
        newItem.slotImage.sprite = item.icon;
        newItem.slotNum.text = item.itemHeld.ToString();

        // Ensure proper setup
        if (newItem.canvasGroup == null)
        {
            newItem.canvasGroup = newItem.gameObject.AddComponent<CanvasGroup>();
        }
    }

    public static void RefreshItem()
    {
        // Clear all children
        foreach (Transform child in Instance.slotGrid.transform)
        {
            Destroy(child.gameObject);
        }

        // Recreate items
        foreach (ItemData item in Instance.myBag.itemList)
        {
            if (item.itemHeld > 0)
            {
                CreateNewItem(item);
            }
        }
    }
}
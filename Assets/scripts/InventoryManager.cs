using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    static InventoryManager instance;
    public Inventory myBag;
    public GameObject slotGrid;
    private List<Slot> slots = new List<Slot>();
    private const int MAX_SLOTS = 12;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
        InitializeSlots();
    }

    void InitializeSlots()
    {
        // Get existing slots from the scene
        slots.Clear();
        foreach (Transform child in slotGrid.transform)
        {
            Slot slot = child.GetComponent<Slot>();
            if (slot != null)
            {
                slot.slotItem = null;
                slot.slotNum.text = "";
                slot.slotImage.enabled = false;
                slots.Add(slot);
            }
        }
    }

    public static void RefreshItem()
    {
        // Clear existing slot contents
        foreach (Slot slot in instance.slots)
        {
            slot.slotItem = null;
            slot.slotNum.text = "";
            slot.slotImage.enabled = false;
        }

        // Populate slots with inventory items
        for (int i = 0; i < instance.myBag.itemList.Count && i < MAX_SLOTS; i++)
        {
            ItemData item = instance.myBag.itemList[i];
            instance.slots[i].slotItem = item;
            instance.slots[i].slotNum.text = item.itemHeld.ToString();
            instance.slots[i].slotImage.sprite = item.icon;
            instance.slots[i].slotImage.enabled = true;
        }
    }

    public static void AddItem(ItemData newItem, int quantity)
    {
        // Try to stack existing items
        foreach (ItemData existingItem in instance.myBag.itemList)
        {
            if (existingItem == newItem)
            {
                existingItem.itemHeld += quantity;
                RefreshItem();
                return;
            }
        }

        // Add to first empty slot
        if (instance.myBag.itemList.Count < MAX_SLOTS)
        {
            newItem.itemHeld = quantity;
            instance.myBag.itemList.Add(newItem);
            RefreshItem();
        }
        else
        {
            Debug.Log("Inventory is full!");
        }
    }

    public void Start()
    {
        RefreshItem();
    }
}
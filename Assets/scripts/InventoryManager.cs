using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

public class InventoryManager : MonoBehaviour
{
    static InventoryManager instance;
    public Inventory myBag;
    public GameObject slotGrid;
    private List<Slot> slots = new List<Slot>();
    private const int MAX_SLOTS = 12;

    [System.Serializable]
    private class InventorySaveData
    {
        public List<string> itemIDs;
        public List<int> itemCounts;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        InitializeSlots();
        LoadInventory();
    }

    void InitializeSlots()
    {
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
        foreach (Slot slot in instance.slots)
        {
            slot.slotItem = null;
            slot.slotNum.text = "";
            slot.slotImage.enabled = false;
        }

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
        foreach (ItemData existingItem in instance.myBag.itemList)
        {
            if (existingItem.itemName == newItem.itemName)
            {
                existingItem.itemHeld += quantity;
                RefreshItem();
                return;
            }
        }

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

    public void LoadInventory()
    {
        string path = Application.persistentDataPath + "/inventory.json";
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                var saveData = JsonUtility.FromJson<InventorySaveData>(json);

                myBag.itemList.Clear();
                for (int i = 0; i < saveData.itemIDs.Count; i++)
                {
                    // 假设所有的 ItemData 都放在 Resources/Items 文件夹下
                    ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
                    if (allItems.Length == 0)
                    {
                        Debug.LogError("No ItemData resources found in Resources/Items folder!");
                        continue;
                    }
                    bool itemFound = false;
                    foreach (var item in allItems)
                    {
                        if (item.itemID == saveData.itemIDs[i])
                        {
                            ItemData newItem = Instantiate(item);
                            newItem.itemHeld = saveData.itemCounts[i];
                            myBag.itemList.Add(newItem);
                            itemFound = true;
                            break;
                        }
                    }
                    if (!itemFound)
                    {
                        Debug.LogWarning($"Item with ID {saveData.itemIDs[i]} not found in Resources/Items folder!");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading inventory: {e.Message}");
            }
        }
        else
        {
            myBag = ScriptableObject.CreateInstance<Inventory>();
        }
        RefreshItem();
    }

    public void SaveInventory()
    {
        try
        {
            // 创建一个可序列化的临时数据结构
            InventorySaveData saveData = new InventorySaveData();
            saveData.itemIDs = new List<string>();
            saveData.itemCounts = new List<int>();

            foreach (var item in myBag.itemList)
            {
                saveData.itemIDs.Add(item.itemID);
                saveData.itemCounts.Add(item.itemHeld);
            }

            string json = JsonUtility.ToJson(saveData);
            File.WriteAllText(Application.persistentDataPath + "/inventory.json", json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving inventory: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        SaveInventory();
    }

    void Start()
    {
        RefreshItem();
    }
}
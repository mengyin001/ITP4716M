using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems = new List<ItemData>();

    // 通过ID获取物品
    public ItemData GetItem(string itemID)
    {
        foreach (ItemData item in allItems)
        {
            if (item.itemID == itemID)
            {
                return item;
            }
        }

        Debug.LogError($"Item with ID {itemID} not found in database!");
        return null;
    }

    // 验证所有物品ID唯一性
    public void ValidateIDs()
    {
        HashSet<string> usedIDs = new HashSet<string>();

        foreach (ItemData item in allItems)
        {
            if (string.IsNullOrEmpty(item.itemID))
            {
                Debug.LogError($"Item {item.name} has empty ID!");
                continue;
            }

            if (usedIDs.Contains(item.itemID))
            {
                Debug.LogError($"Duplicate item ID: {item.itemID} on item {item.name}");
            }
            else
            {
                usedIDs.Add(item.itemID);
            }
        }
    }
}
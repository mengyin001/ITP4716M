using System.Collections.Generic;
using UnityEngine;

public static class PlayerData
{
    // 玩家基础数据
    public static float Health { get; set; }
    public static int Money { get; set; }
    public static int CurrentGunIndex { get; set; }

    // 库存数据
    public static List<ItemInstance> InventoryItems { get; private set; }

    // 初始化默认值
    static PlayerData()
    {
        Health = 100f;
        Money = 0;
        CurrentGunIndex = 0;
        InventoryItems = new List<ItemInstance>();
    }

    // 保存库存数据
    public static void SaveInventory(List<ItemData> items)
    {
        InventoryItems.Clear();
        foreach (var item in items)
        {
            InventoryItems.Add(new ItemInstance
            {
                data = item,
                quantity = item.itemHeld
            });
        }
    }

    // 加载库存数据
    public static void LoadInventory(Inventory inventory)
    {
        inventory.itemList.Clear();

        foreach (var savedItem in InventoryItems)
        {
            // 创建新的ItemData实例以避免引用问题
            ItemData newItem = Object.Instantiate(savedItem.data);
            newItem.itemHeld = savedItem.quantity;
            inventory.itemList.Add(newItem);
        }
    }
}
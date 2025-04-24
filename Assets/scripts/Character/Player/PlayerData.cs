using System.Collections.Generic;
using UnityEngine;

public static class PlayerData
{
    // ��һ�������
    public static float Health { get; set; }
    public static int Money { get; set; }
    public static int CurrentGunIndex { get; set; }

    // �������
    public static List<ItemInstance> InventoryItems { get; private set; }

    // ��ʼ��Ĭ��ֵ
    static PlayerData()
    {
        Health = 100f;
        Money = 0;
        CurrentGunIndex = 0;
        InventoryItems = new List<ItemInstance>();
    }

    // ����������
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

    // ���ؿ������
    public static void LoadInventory(Inventory inventory)
    {
        inventory.itemList.Clear();

        foreach (var savedItem in InventoryItems)
        {
            // �����µ�ItemDataʵ���Ա�����������
            ItemData newItem = Object.Instantiate(savedItem.data);
            newItem.itemHeld = savedItem.quantity;
            inventory.itemList.Add(newItem);
        }
    }
}
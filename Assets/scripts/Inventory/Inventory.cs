using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory/Inventory")]
public class Inventory : ScriptableObject
{
    public List<ItemData> itemList = new List<ItemData>();

    // 创建背包实例的深拷贝
    public Inventory Clone()
    {
        Inventory clone = CreateInstance<Inventory>();
        clone.itemList = new List<ItemData>();

        foreach (ItemData originalItem in itemList)
        {
            // 使用ItemData的Clone方法创建副本
            clone.itemList.Add(originalItem.Clone());
        }

        return clone;
    }
}

// 扩展ItemData支持深拷贝
public partial class ItemData
{
    public ItemData(ItemData source)
    {
        this.itemID = source.itemID;
        this.itemName = source.itemName;
        this.icon = source.icon;
        this.itemHeld = source.itemHeld;
    }
}
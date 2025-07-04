using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory", menuName = "Inventory/Inventory")]
public class Inventory : ScriptableObject
{
    public List<ItemData> itemList = new List<ItemData>();

    // ��������ʵ�������
    public Inventory Clone()
    {
        Inventory clone = CreateInstance<Inventory>();
        clone.itemList = new List<ItemData>();

        foreach (ItemData originalItem in itemList)
        {
            // ʹ��ItemData��Clone������������
            clone.itemList.Add(originalItem.Clone());
        }

        return clone;
    }
}

// ��չItemData֧�����
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
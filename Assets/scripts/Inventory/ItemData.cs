using UnityEngine;
using System.Collections.Generic; // 如果您使用 List<ItemEffect>

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Information")]
    public string itemID;
    public string itemName;
    public Sprite icon;
    public bool isStackable = true;
    public int maxStack = 99;
    public int price = 0;

    [Header("Item Effects")]
    // 現在，effects 是一個 ItemEffect ScriptableObject 的陣列或列表
    public ItemEffect[] effects; // 或者 public List<ItemEffect> effects;

}

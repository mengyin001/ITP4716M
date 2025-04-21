// ItemData.cs
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public enum ItemType { Consumable, Equipment, Material, Quest }

    [Header("基础信息")]
    public string itemID;
    public string itemName;
    public Sprite icon;
    [NonSerialized] public int itemHeld; 
    [TextArea] public string description;
    public ItemType itemType;

    [Header("属性")]
    public float weight = 0.1f;
    public bool isUnique = false;

    [Header("使用效果")]
    public GameObject usePrefab;
    public int restoreHealth;
    public int attackBonus;
}

// ItemInstance.cs
[System.Serializable]
public class ItemInstance
{
    public ItemData data;
    public int quantity;
}
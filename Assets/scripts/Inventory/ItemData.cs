using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public enum ItemType { Consumable, Equipment, Material, Quest }
    public enum EffectType { Health, Energy, Attack }

    [Header("Basic Info")]
    public string itemID;
    public string itemName;
    public Sprite icon;
    [NonSerialized] public int itemHeld;
    public ItemType itemType;

    [Header("Effects")]
    public EffectType effectType;
    public float effectAmount; // Unified effect value
    public GameObject usePrefab;

    [Header("Other Properties")]
    public float weight = 0.1f;
    public bool isUnique = false;
    public int price;
    [TextArea] public string description;
}
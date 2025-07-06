using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public enum ItemType { Consumable, Equipment, Material, Quest }
    public enum EffectType { Health, Energy, Attack }

    [System.Serializable]
    public class ItemEffect
    {
        public EffectType effectType;
        public float effectAmount;
    }

    [Header("Basic Info")]
    public string itemID;
    public string itemName;
    public Sprite icon;
    public ItemType itemType;

    [Header("Effects")]
    public List<ItemEffect> effects = new List<ItemEffect>();

    [Header("Other Properties")]
    public int price;
    public int maxStack = 99; // 添加最大堆叠数量

    public ItemData Clone()
    {
        ItemData clone = CreateInstance<ItemData>();
        clone.itemID = this.itemID;
        clone.itemName = this.itemName;
        clone.icon = this.icon;
        clone.itemType = this.itemType;
        clone.price = this.price;
        clone.maxStack = this.maxStack;

        clone.effects = new List<ItemEffect>();
        foreach (var effect in this.effects)
        {
            clone.effects.Add(new ItemEffect
            {
                effectType = effect.effectType,
                effectAmount = effect.effectAmount
            });
        }

        return clone;
    }
}
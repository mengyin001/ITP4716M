using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public partial class ItemData : ScriptableObject
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
    public int itemHeld;
    public ItemType itemType;

    [Header("Effects")]
    public List<ItemEffect> effects = new List<ItemEffect>();

    [Header("Other Properties")]
    public int price;

    // 正确的克隆方法（替代构造函数）
    public ItemData Clone()
    {
        // 创建新的ScriptableObject实例
        ItemData clone = CreateInstance<ItemData>();

        // 复制基本属性
        clone.itemID = this.itemID;
        clone.itemName = this.itemName;
        clone.icon = this.icon;
        clone.itemHeld = this.itemHeld;
        clone.itemType = this.itemType;
        clone.price = this.price;

        // 深拷贝效果列表
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
using UnityEngine;

// 效果類型枚舉 (可以放在這裡，也可以放在一個單獨的全局文件)
public enum EffectType
{
    Health,
    Energy,
    Attack,
    MaxHealth,
    MaxEnergy,
    // 可以添加更多效果類型，例如：
    // Speed,
    // Defense,
    // DamageOverTime,
    // HealOverTime
}

[CreateAssetMenu(fileName = "NewItemEffect", menuName = "Inventory/Item Effect")]
public class ItemEffect : ScriptableObject
{
    [Header("Effect Details")]
    public EffectType effectType;
    public float effectAmount;
    public float duration; // 0 表示瞬時效果，大於 0 表示持續效果
}

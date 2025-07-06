using UnityEngine;

// Ч�����ö�e (���Է����@�e��Ҳ���Է���һ���Ϊ���ȫ���ļ�)
public enum EffectType
{
    Health,
    Energy,
    Attack,
    MaxHealth,
    MaxEnergy,
    // ������Ӹ���Ч����ͣ����磺
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
    public float duration; // 0 ��ʾ˲�rЧ������� 0 ��ʾ���mЧ��
}

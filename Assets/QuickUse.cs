using UnityEngine;

public class QuickUse : MonoBehaviour
{
    [Header("Inventory Settings")]
    public Inventory bag01;
    public ItemData[] quickSlots = new ItemData[3]; // Slot 1-3

    [Header("Key Settings")]
    public float keyCooldown = 0.3f;

    private float lastUseTime;
    private HealthSystem healthSystem;

    private void Awake()
    {
        healthSystem = FindObjectOfType<HealthSystem>();

        // Auto-assign if not set
        if (bag01 == null)
        {
            bag01 = Resources.Load<Inventory>("Inventories/bag01");
        }
    }

    private void Update()
    {
        if (Time.time - lastUseTime < keyCooldown) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) UseQuickSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) UseQuickSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) UseQuickSlot(2);
    }

    private void UseQuickSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= quickSlots.Length) return;

        ItemData item = quickSlots[slotIndex];
        if (item == null)
        {
            Debug.Log($"Quick slot {slotIndex + 1} is empty");
            return;
        }

        if (!HasItemInInventory(item.itemID))
        {
            Debug.LogWarning($"Item {item.itemName} not in inventory!");
            return;
        }

        if (item.itemHeld <= 0)
        {
            Debug.LogWarning($"No {item.itemName} remaining!");
            return;
        }

        ApplyItemEffects(item);
        item.itemHeld--;
        lastUseTime = Time.time;

    }

    private bool HasItemInInventory(string itemID)
    {
        foreach (ItemData item in bag01.itemList)
        {
            if (item.itemID == itemID) return true;
        }
        return false;
    }

    private void ApplyItemEffects(ItemData item)
    {
        if (healthSystem == null) return;

        // 遍历物品的所有效果
        foreach (var effect in item.effects)
        {
            switch (effect.effectType)
            {
                case ItemData.EffectType.Health:
                    healthSystem.Heal(effect.effectAmount);
                    Debug.Log($"Restored {effect.effectAmount} health");
                    break;

                case ItemData.EffectType.Energy:
                    healthSystem.RestoreEnergy(effect.effectAmount);
                    Debug.Log($"Restored {effect.effectAmount} energy");
                    break;

                case ItemData.EffectType.Attack:
                    // 实现攻击增益逻辑
                    ApplyAttackBoost(effect.effectAmount);
                    Debug.Log($"Attack boosted by {effect.effectAmount}");
                    break;
            }

        }
    }
    private void ApplyAttackBoost(float amount)
    {
        // 这里添加攻击增益的具体实现
        // 例如：
        // playerStats.attackPower += amount;
        // StartCoroutine(RemoveAttackBoostAfterTime(amount, duration));
    }
}
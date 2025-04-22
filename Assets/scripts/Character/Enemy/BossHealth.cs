using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossHealth : Enemy
{
    public UnityEvent OnBossDeath;

    public override void Die()
    {
        base.Die();
        OnBossDeath?.Invoke();
        // 额外的死亡逻辑（如播放动画、掉落物品等）
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        // 确保不会超过最大生命值
        currentHealth = Mathf.Min(currentHealth, 100f); // 假设最大生命值为100
    }
}
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f; // 最大生命值
    private float currentHealth; // 当前生命值

    private void Start()
    {
        // 初始化当前生命值
        currentHealth = maxHealth;
    }

    // 处理掉血的方法
    public void TakeDamage(float damage)
    {
        currentHealth -= damage; // 减少生命值
        Debug.Log($"Player takes damage: {damage}. Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die(); // 调用死亡方法
        }
    }

    // 处理角色死亡的方法
    private void Die()
    {
        Debug.Log("Player has died!");
        Destroy(this.gameObject);
        // 这里可以添加死亡逻辑，比如禁用玩家或播放死亡动画
    }
}
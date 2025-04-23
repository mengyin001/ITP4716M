using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Character : MonoBehaviour
{
    [Header("Attribute")]
    [SerializeField] public float MaxHealth;
    [SerializeField] public float currentHealth;
    public bool invulnerable;
    public float invulnerableDuration; // 拸菩奀潔

    public bool isEnemy = false;
    public static Action OnEnemyDeath;// 敌人死亡事件

    public UnityEvent OnHurt;
    public UnityEvent OnDie;

    protected virtual void OnEnable()
    {
        currentHealth = MaxHealth;
    }

    public virtual void TakeDamage(float damage)
    {
        if (invulnerable)
            return;
        if (currentHealth - damage > 0f)
        {
            currentHealth -= damage;
            StartCoroutine(nameof(InvulnerableCoroutine));// 雄拸菩奀潔衪最
            // 硒俴褒伎忳夼雄賒
            OnHurt?.Invoke();
        }
        else
        {
            // 侚厗
            Die();
        }
    }

    public virtual void Die()
    {
        currentHealth = 0f;
        OnDie?.Invoke();

        if (isEnemy)
        {
            Debug.Log($"敌人 {gameObject.name} 死亡");
            OnEnemyDeath?.Invoke();
        }
    }

    // 拸菩
    protected virtual IEnumerator InvulnerableCoroutine()
    {
        invulnerable = true;

        // 脹渾拸菩奀潔
        yield return new WaitForSeconds(invulnerableDuration);

        invulnerable = false;
    }

    // 氝樓 AddHealth 源楊汒隴

}
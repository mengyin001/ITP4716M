using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Character : MonoBehaviour
{
    [Header("Attribute")]
    [SerializeField] public float currentHealth = 100f;
    public bool invulnerable;
    public float invulnerableDuration; // 无敌时间

    public UnityEvent OnHurt;
    public UnityEvent OnDie;

    protected virtual void OnEnable()
    {

    }

    public virtual void TakeDamage(float damage)
    {
        if (invulnerable)
            return;
        if (currentHealth - damage > 0f)
        {
            currentHealth -= damage;
            StartCoroutine(nameof(InvulnerableCoroutine));// 启动无敌时间协程
            // 执行角色受伤动画
            OnHurt?.Invoke();
        }
        else
        {
            // 死亡
            Die();
        }
    }

    public virtual void Die()
    {
        currentHealth = 0f;

        // 执行角色死亡动画
        OnDie?.Invoke();
    }

    // 无敌
    protected virtual IEnumerator InvulnerableCoroutine()
    {
        invulnerable = true;

        // 等待无敌时间
        yield return new WaitForSeconds(invulnerableDuration);

        invulnerable = false;
    }

    // 添加 AddHealth 方法声明

}
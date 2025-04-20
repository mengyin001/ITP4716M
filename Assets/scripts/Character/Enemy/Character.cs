using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 角色基类，包含基础生命值管理和受伤/死亡逻辑
/// </summary>
public class Character : MonoBehaviour
{
    [Header("血量设置")]
    [Tooltip("角色的最大生命值")]
    [SerializeField] protected float maxHealth;
    protected Slider healthSlider;

    [Tooltip("角色的当前生命值")]
    [SerializeField] protected float currentHealth;

    [Header("无敌状态")]
    [Tooltip("是否处于无敌状态")]
    public bool invulnerable;

    [Tooltip("受伤后的无敌持续时间（秒）")]
    public float invulnerableDuration;

    [Header("事件")]
    [Tooltip("当角色受伤时触发的事件")]
    public UnityEvent OnHurt;

    [Tooltip("当角色死亡时触发的事件")]
    public UnityEvent OnDie;

    /// <summary>
    /// 当对象启用时的初始化（继承MonoBehaviour的生命周期方法）
    /// </summary>
    protected virtual void OnEnable()
    {
        // 初始化生命值为最大值
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 角色受到伤害的通用处理逻辑
    /// </summary>
    /// <param name="damage">受到的伤害值</param>
    public virtual void TakeDamage(float damage)
    {
        // 如果处于无敌状态则直接返回
        if (invulnerable)
            return;

        // 计算剩余生命值
        if (currentHealth - damage > 0f)
        {
            currentHealth -= damage;

            // 启动无敌状态协程
            StartCoroutine(nameof(InvulnerableCoroutine));

            // 触发受伤事件（可用于播放受伤音效、动画等）
            OnHurt?.Invoke();
        }
        else
        {
            // 生命值不足时执行死亡
            Die();
        }
        UpdateHealthBar();
    }

    /// <summary>
    /// 角色死亡的通用处理逻辑
    /// </summary>
    public virtual void Die()
    {
        // 确保生命值归零
        currentHealth = 0f;

        // 触发死亡事件（可用于播放死亡动画、游戏结束逻辑等）
        OnDie?.Invoke();
        if (healthSlider != null)
        {
            Destroy(healthSlider.gameObject);
        }
    }

    /// <summary>
    /// 无敌状态协程（协程实现定时无敌状态）
    /// </summary>
    protected virtual IEnumerator InvulnerableCoroutine()
    {
        // 进入无敌状态
        invulnerable = true;

        // 等待指定的无敌持续时间
        yield return new WaitForSeconds(invulnerableDuration);

        // 结束无敌状态
        invulnerable = false;
    }

    protected virtual void UpdateHealthBar()
    {
        if (healthSlider == null) return;

        // 更新数值
        healthSlider.value = currentHealth;


        healthSlider.gameObject.SetActive(true);

    }

}
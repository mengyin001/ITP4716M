using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Enemy : MonoBehaviour
{
    [Header("属性")]
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float currentHealth;
    [SerializeField] private Transform player;

    [SerializeField] private float chaseDistance = 3f; // 追击距离
    [SerializeField] private float attackDistance = 0.8f; // 攻击距离
    [SerializeField] private float invulnerableDuration = 1f; // 无敌持续时间

    private bool invulnerable;

    public UnityEvent<Vector2> OnMovementInput;
    public UnityEvent OnHurt;
    public UnityEvent OnDie;
    public UnityEvent OnAttack;

    protected virtual void OnEnable()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float damage)
    {
        if (invulnerable)
            return;

        if (currentHealth - damage > 0f)
        {
            currentHealth -= damage;
            StartCoroutine(InvulnerableCoroutine()); // 启动无敌时间协程
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
        // 这里可以添加其他死亡处理逻辑，比如禁用敌人
        gameObject.SetActive(false); // 假设禁用敌人
    }

    // 无敌
    protected virtual IEnumerator InvulnerableCoroutine()
    {
        invulnerable = true;
        // 等待无敌时间
        yield return new WaitForSeconds(invulnerableDuration);
        invulnerable = false;
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(player.position, transform.position);

        if (distance < chaseDistance) // 小于追击距离
        {
            if (distance <= attackDistance) // 是否处于攻击距离
            {
                // 攻击玩家
                OnMovementInput?.Invoke(Vector2.zero); // 停止移动
                OnAttack?.Invoke();
            }
            else
            {
                // 追击玩家
                Vector2 direction = player.position - transform.position;
                OnMovementInput?.Invoke(direction.normalized);
            }
        }
        else
        {
            // 放弃追击
            OnMovementInput?.Invoke(Vector2.zero);
        }
    }
}
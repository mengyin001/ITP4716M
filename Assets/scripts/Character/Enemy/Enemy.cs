using System.Collections;
using System.IO;
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

    [Header("攻击")]
    public float meleetAttackDamage;//近战攻击伤害
    public LayerMask playerLayer;//表示玩家图层
    public float AttackDelay = 2f;//冷却时间

    private bool isAttack = true;
    private SpriteRenderer sr;
    private bool invulnerable;

    public UnityEvent<Vector2> OnMovementInput;// 移动输入事件
    public UnityEvent OnHurt;// 受伤事件
    public UnityEvent OnDie;// 死亡事件
    public UnityEvent OnAttack;// 攻击事件

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();// 初始化当前生命值
    }
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
            currentHealth -= damage;// 减少生命值
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
        invulnerable = true;// 设置无敌状态
        // 等待无敌时间
        yield return new WaitForSeconds(invulnerableDuration);// 等待无敌时间
        invulnerable = false;// 重置无敌状态
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
                if (isAttack)
                {
                    isAttack = false;
                    OnAttack?.Invoke();// 触发攻击事件
                    StartCoroutine(nameof(AttackCooldownCoroutine));// 启动攻击冷却时间
                }

                //人物翻转
                float x = player.position.x - transform.position.x;
                if (x > 0)
                {
                    sr.flipX = true;
                }
                else
                {
                    sr.flipX = false;
                }
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

    //近战攻击
    private void MeleeAttackEvent()
    {
        //检测碰撞
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<PlayerHealth>().TakeDamage(meleetAttackDamage);
        }
    }

    //攻击冷却时间

    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackDelay);// 等待冷却时间
        isAttack = true;// 重置攻击状态
    }

    public void OnDrawGizmosSelected()
    {
        //显示攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        //显示追击范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

    }
}
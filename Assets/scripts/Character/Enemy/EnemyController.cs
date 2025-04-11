using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("属性")]
    [SerializeField] private float currentSpeed = 0;

    public Vector2 MovementInput { get; set; }
    [Header("攻击")]
    [SerializeField] private bool isAttack = true;
    [SerializeField] private float attackCoolDuration = 1;

    [Header("击退")]
    [SerializeField] private bool isKnokback = true;
    [SerializeField] private float KnokbackForce = 10f;
    [SerializeField] private float KnokbackForceDuration = 0.1f;

    private Rigidbody2D rb;
    private Collider2D enemyCollider;
    private SpriteRenderer sr;
    private Animator anim;

    private bool isHurt;
    private bool isDead;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if(!isHurt && !isDead)
            Move();

        SetAnimation();
    }

    void Move()
    {
        if (MovementInput.magnitude > 0.1f && currentSpeed >= 0)
        {
            rb.linearVelocity = MovementInput * currentSpeed;
            //敌人左右翻转
            if (MovementInput.x < 0)//左
            {
                sr.flipX = false;
            }
            if (MovementInput.x > 0)//右
            {
                sr.flipX = true;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Attack()
    {
        if (isAttack)
        {
            isAttack = false;
            StartCoroutine(nameof(AttackCoroutine));
        }
    }

    IEnumerator AttackCoroutine()//攻击协议
    {
        anim.SetTrigger("isAttack");

        yield return new WaitForSeconds(attackCoolDuration);
        isAttack = true;
    }

    public void EnemyHurt()
    {
        isHurt = true;
        anim.SetTrigger("isHurt");
    }

   /*  public void Knockback(Vector3 pos)
    {
        //施加击退效果
        if (!isKnokback || isDead)
        {
            return;
        }

        StartCoroutine(KnockbackCoroutine(pos));
    }

    IEnumerator KnockbackCoroutine(Vector3 pos)
    {
        var direction = (transform.position - pos).normalized;
        rb.AddForce(direction * KnokbackForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(KnokbackForceDuration);
        isHurt = false;

    }*/

    public void EnemyDead()
    {
        rb.linearVelocity = Vector2.zero;
        isDead = true;
        enemyCollider.enabled = false;//禁用碰撞体
    }

    void SetAnimation()
    {
        anim.SetBool("isWalk", MovementInput.magnitude > 0);
        anim.SetBool("isDead", isDead);
    }

    public void DestroyEnemy()
    {
        Destroy(this.gameObject);
    }
}

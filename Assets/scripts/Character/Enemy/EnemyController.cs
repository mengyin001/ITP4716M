using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("����")]
    [SerializeField] private float currentSpeed = 0;

    public Vector2 MovementInput { get; set; }
    [Header("����")]
    [SerializeField] private bool isAttack = true;
    [SerializeField] private float attackCoolDuration = 1;

    [Header("����")]
    [SerializeField] private bool isKnokback = true;
    [SerializeField] private float KnokbackForce = 10f;
    [SerializeField] private float KnokbackForceDuration = 0.1f;

    private Rigidbody2D rb;
    private Collider2D enemyCollider;
    private SpriteRenderer sr;
    private Animator anim;

    private bool isHurt;
    private bool isDead;

    private Vector2 lastMovementInput = Vector2.zero;
    private const float movementThreshold = 0.1f;
    private const float animationThreshold = 0.05f;

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
            Vector2 targetVelocity = MovementInput * currentSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);            //�������ҷ�ת
            if (MovementInput.x < 0)//��
            {
                sr.flipX = false;
            }
            if (MovementInput.x > 0)//��
            {
                sr.flipX = true;
            }
        }
        else
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 10f);
        }
        lastMovementInput = MovementInput;
    }

    public void Attack()
    {
        if (isAttack)
        {
            isAttack = false;
            StartCoroutine(nameof(AttackCoroutine));
        }
    }

    IEnumerator AttackCoroutine()//����Э��
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

     public void Knockback(Vector3 pos)
    {
        //ʩ�ӻ���Ч��
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
        enemyCollider.enabled = true;

    }

    public void EnemyDead()
    {
        rb.linearVelocity = Vector2.zero;
        isDead = true;
        enemyCollider.enabled = false;//������ײ��
    }

    void SetAnimation()
    {
        bool isMoving = Mathf.Abs(MovementInput.x) > animationThreshold || Mathf.Abs(MovementInput.y) > animationThreshold;
        if (isMoving != anim.GetBool("isWalk"))
        {
            anim.SetBool("isWalk", isMoving);
        }
        anim.SetBool("isDead", isDead);
    }

    public void DestroyEnemy()
    {
        Destroy(this.gameObject);
    }
}

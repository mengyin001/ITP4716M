using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Attack Setting")]
    [SerializeField] private float currentSpeed = 0;
    [SerializeField] private float attackCoolDuration = 1;
    public Vector2 MovementInput { get; set; }
    
    [Header("Sound Effects")]
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioSource audioSource;


    private Rigidbody2D rb;
    private Collider2D enemyCollider;
    private SpriteRenderer sr;
    private Animator anim;

    private bool isAttack = true;
    private bool isHurt;
    private bool isDie;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

    }

    private void FixedUpdate()
    {
        if (!isHurt && !isDie)
            Move();

        SetAnimation();
    }

    void Move()
    {
        if (MovementInput.magnitude > 0.1f && currentSpeed >= 0)
        {
            Vector2 targetVelocity = MovementInput * currentSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 10f);
            if (MovementInput.x < 0)
            {
                sr.flipX = false;
            }
            if (MovementInput.x > 0)
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

    IEnumerator AttackCoroutine()
    {
        anim.SetTrigger("isAttack");

        if (attackSound != null && audioSource != null)
            audioSource.PlayOneShot(attackSound);

        yield return new WaitForSeconds(attackCoolDuration);
        isAttack = true;
    }

    public void EnemyHurt()
    {
        anim.SetTrigger("isHurt");

    }


    public void EnemyDead()
    {
        rb.linearVelocity = Vector2.zero;
        isDie = true;
        enemyCollider.enabled = false;
    }

    void SetAnimation()
    {
        anim.SetBool("isWalk", MovementInput.magnitude > 0);
        anim.SetBool("isDie", isDie);
    }

    public void DestroyEnemy()
    {
        Destroy(this.gameObject);
    }
}

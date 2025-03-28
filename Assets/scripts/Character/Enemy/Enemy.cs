using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Pathfinding;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class Enemy : MonoBehaviour
{
    [Header("ÊôÐÔ")]
    [SerializeField] protected float maxHealth;
    [SerializeField] protected float currentHealth;
    [SerializeField] private Transform player;

    [SerializeField] private float chaseDistance = 3f; // ×·»÷¾àÀë
    [SerializeField] private float attackDistance = 0.8f; // ¹¥»÷¾àÀë
    [SerializeField] private float invulnerableDuration = 1f; // ÎÞµÐ³ÖÐøÊ±¼ä

    [Header("¹¥»÷")]
    public float meleetAttackDamage;//½üÕ½¹¥»÷ÉËº¦
    public LayerMask playerLayer;//±íÊ¾Íæ¼ÒÍ¼²ã
    public float AttackDelay = 2f;//ÀäÈ´Ê±¼ä

    private bool isAttack = true;
    private SpriteRenderer sr;
    private bool invulnerable;

    public UnityEvent<Vector2> OnMovementInput ;// ÒÆ¶¯ÊäÈëÊÂ¼þ
    public UnityEvent OnHurt;// ÊÜÉËÊÂ¼þ
    public UnityEvent OnDie;// ËÀÍöÊÂ¼þ
    public UnityEvent OnAttack;// ¹¥»÷ÊÂ¼þ

    private Seeker seeker;
    private List<Vector3> pathPointList = new List<Vector3>();//Â·¾¶µãÁÐ±í
    private int currentIndex = 0;//Â·¾¶µãµÄË÷Òý
    private float pathGenerateInterval = 0.5f;//Ã¿0.5ÃëÉú³ÉÒ»´ÎÂ·¾¶
    private float pathGenerateTimer = 0f;//¼ÆÊ±Æ÷

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();// ³õÊ¼»¯µ±Ç°ÉúÃüÖµ
        seeker = GetComponent<Seeker>();
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
            currentHealth -= damage;// ¼õÉÙÉúÃüÖµ
            StartCoroutine(InvulnerableCoroutine()); // Æô¶¯ÎÞµÐÊ±¼äÐ­³Ì
            // Ö´ÐÐ½ÇÉ«ÊÜÉË¶¯»­
            OnHurt?.Invoke();
        }
        else
        {
            // ËÀÍö
            Die();
        }
    }

    public virtual void Die()
    {
        currentHealth = 0f;
        // Ö´ÐÐ½ÇÉ«ËÀÍö¶¯»­
        OnDie?.Invoke();
        // ÕâÀï¿ÉÒÔÌí¼ÓÆäËûËÀÍö´¦ÀíÂß¼­£¬±ÈÈç½ûÓÃµÐÈË
        gameObject.SetActive(false); // ¼ÙÉè½ûÓÃµÐÈË
    }

    // ÎÞµÐ
    protected virtual IEnumerator InvulnerableCoroutine()
    {
        invulnerable = true;// ÉèÖÃÎÞµÐ×´Ì¬
        // µÈ´ýÎÞµÐÊ±¼ä
        yield return new WaitForSeconds(invulnerableDuration);// µÈ´ýÎÞµÐÊ±¼ä
        invulnerable = false;// ÖØÖÃÎÞµÐ×´Ì¬
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(player.position, transform.position);

        if (distance < chaseDistance) // Ð¡ÓÚ×·»÷¾àÀë
        {
            AutoPath();
            if (pathPointList == null)
                return;
            if (distance <= attackDistance) // ÊÇ·ñ´¦ÓÚ¹¥»÷¾àÀë
            {

                // ¹¥»÷Íæ¼Ò
                OnMovementInput?.Invoke(Vector2.zero); // Í£Ö¹ÒÆ¶¯
                if (isAttack)
                {
                    isAttack = false;
                    OnAttack?.Invoke();// ´¥·¢¹¥»÷ÊÂ¼þ
                    StartCoroutine(nameof(AttackCooldownCoroutine));// Æô¶¯¹¥»÷ÀäÈ´Ê±¼ä
                }

                //ÈËÎï·­×ª
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
                // ×·»÷Íæ¼Ò
                //Vector2 direction = player.position - transform.position;
                if (currentIndex >= 0 && currentIndex < pathPointList.Count)
                {
                    Vector2 direction = (pathPointList[currentIndex] - transform.position).normalized;
                    OnMovementInput?.Invoke(direction.normalized);
                }
                else
                {
                    // ´¦ÀíË÷ÒýÎÞÐ§µÄÇé¿ö£¬ÀýÈçÖØÖÃË÷Òý
                    currentIndex = 0; // »òÕßÄã¿ÉÒÔÑ¡ÔñÆäËûÂß¼­
                }

            }
        }
        else
        {
            // ·ÅÆú×·»÷
            OnMovementInput?.Invoke(Vector2.zero);
        }
    }

    //×Ô¶¯Ñ°Â·
    private void AutoPath()
    {
        pathGenerateTimer += Time.deltaTime;

        //Ã¿¸ôÒ»¶¨Ê±¼äÀ´»ñÈ¡Â·¾¶µã
        if (pathGenerateTimer >= pathGenerateInterval)
        {
            GeneratePath(player.position);
            pathGenerateTimer = 0;
        }
        //µ±Â·¾¶µãÁÐ±íÎª¿ÕÊ±£¬½øÐÐÂ·¾¶¼ÆËã
        if (pathPointList == null || pathPointList.Count <= 0)
        {
            GeneratePath(player.position);
        } //µ±µÐÈËµ½´ïµ±Ç°Â·¾¶µã£¬µÝÔöË÷ÒýcurrentIndex²¢½øÐÐÂ·¾¶¼ÆËã
        else if (Vector2.Distance(transform.position, pathPointList[currentIndex]) <= 0.1f)
        {
            currentIndex++;
            if (currentIndex >= pathPointList.Count)
                GeneratePath(player.position);
        }
    }

    //»ñÈ¡Â·¾¶µã
    private void GeneratePath(Vector3 target)
    {
        currentIndex = 0;
        //Èý¸ö²ÎÊý£ºÆðµã£¬ÖÕµã£¬»Øµ÷º¯Êý
        seeker.StartPath(transform.position, target, Path =>
        {
            pathPointList = Path.vectorPath;
        });
    }

    //½üÕ½¹¥»÷
    private void MeleeAttackEvent()
    {
        //¼ì²âÅö×²
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackDistance, playerLayer);
        foreach (Collider2D hitCollider in hitColliders)
        {
            hitCollider.GetComponent<PlayerHealth>().TakeDamage(meleetAttackDamage);
        }
    }

    //¹¥»÷ÀäÈ´Ê±¼ä

    IEnumerator AttackCooldownCoroutine()
    {
        yield return new WaitForSeconds(AttackDelay);// µÈ´ýÀäÈ´Ê±¼ä
        isAttack = true;// ÖØÖÃ¹¥»÷×´Ì¬
    }

    public void OnDrawGizmosSelected()
    {
        //ÏÔÊ¾¹¥»÷·¶Î§
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
        //ÏÔÊ¾×·»÷·¶Î§
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

    }
}
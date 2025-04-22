using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private float damage;
    private LayerMask targetLayer;
    private float speed;
    private Vector2 direction;
    private Rigidbody2D rb;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    public void Setup(float damage, LayerMask targetLayer, float speed, Vector2 direction)
    {
        this.damage = damage;
        this.targetLayer = targetLayer;
        this.speed = speed;
        this.direction = direction;

        // 设置子弹的速度
        rb.linearVelocity = direction * speed;

        // 计算并设置子弹的旋转角度，使其重心朝向玩家
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 播放飞行动画
        if (animator != null)
        {
            animator.SetBool("IsFlying", true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (targetLayer == (targetLayer | (1 << other.gameObject.layer)))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            PlayCollisionAnimation();
            ReturnToPool();
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            PlayCollisionAnimation();
            ReturnToPool();
        }
    }

    private void OnBecameInvisible()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // 停止飞行动画
        if (animator != null)
        {
            animator.SetBool("IsFlying", false);
        }

        if (EnemyBulletPool.Instance != null)
        {
            EnemyBulletPool.Instance.ReturnBullet(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (rb != null && direction != Vector2.zero)
        {
            // 确保在启用时也设置速度
            rb.linearVelocity = direction * speed;

            // 计算并设置子弹的旋转角度，使其重心朝向玩家
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // 播放飞行动画
            if (animator != null)
            {
                animator.SetBool("IsFlying", true);
            }
        }
    }

    private void PlayCollisionAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }
}
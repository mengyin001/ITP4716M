using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    private float damage;
    private LayerMask targetLayer;
    private float speed;
    private Vector2 direction;
    private Rigidbody2D rb;
  
    private EnemyBulletPool bulletPool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        bulletPool = transform.root.GetComponentInChildren<EnemyBulletPool>();
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

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (targetLayer == (targetLayer | (1 << other.gameObject.layer)))
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
          
            ReturnToPool();
        }
        else if (other.CompareTag("Wall"))
        {
           
            ReturnToPool();
        }
    }

    private void OnBecameInvisible()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {

        if (bulletPool != null)
        {
            bulletPool.ReturnBullet(gameObject);
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

        }
    }


}
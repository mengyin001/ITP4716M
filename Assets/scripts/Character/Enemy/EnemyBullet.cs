using UnityEngine;
using Photon.Pun;

public class EnemyBullet : MonoBehaviourPun
{
    private float damage;
    private float speed;
    private Vector2 direction;
    private Rigidbody2D rb;
    private EnemyBulletPool bulletPool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        bulletPool = GetComponentInParent<EnemyBulletPool>();
    }

    [PunRPC]
    public void ActivateBullet(Vector3 spawnPosition)
    {
        gameObject.SetActive(true);
        transform.position = spawnPosition;
    }

    [PunRPC]
    public void DeactivateBullet()
    {
        gameObject.SetActive(false);
        rb.linearVelocity = Vector2.zero;
    }

    [PunRPC]
    public void SetupBullet(float damage, float speed, Vector2 direction)
    {
        this.damage = damage;
        this.speed = speed;
        this.direction = direction;

        rb.linearVelocity = direction * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 仅由子弹所有者（MasterClient）处理碰撞逻辑
        if (!photonView.IsMine) return;

        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponent<PhotonView>();
            if (playerView != null)
            {
                // 直接调用玩家的 RPC_TakeDamage，通过 RpcTarget.All 确保所有客户端同步
                playerView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
            }
            bulletPool.ReturnBullet(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            bulletPool.ReturnBullet(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        if (photonView.IsMine)
        {
            bulletPool.ReturnBullet(gameObject);
        }
    }
}
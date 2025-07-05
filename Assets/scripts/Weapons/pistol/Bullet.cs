using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Bullet : MonoBehaviourPun
{
    public float speed = 20f;
    public float lifeTime = 3f;
    public float damage = 5f;

    private PhotonView ownerPhotonView;
    private Rigidbody2D rb;
    private bool isDestroyed = false; // 防止多次销毁

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        rb.linearVelocity = transform.right * speed;

        // 所有客户端都启动生命周期协程
        StartCoroutine(DestroyAfterLifetime());
    }

    /// <summary>
    /// 初始化子弹（由发射者调用）
    /// </summary>
    public void Initialize(float dmg, PhotonView ownerPV)
    {
        damage = dmg;
        ownerPhotonView = ownerPV;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 只有子弹的拥有者处理碰撞逻辑
        if (ownerPhotonView != null && ownerPhotonView.IsMine && !isDestroyed)
        {
            Character character = other.GetComponent<Character>();
            if (character != null && character.photonView.ViewID != ownerPhotonView.ViewID)
            {
                // 对敌人造成伤害
                character.photonView.RPC("TakeDamage", RpcTarget.All, damage);
                NetworkDestroyBullet();
            }
            else if (other.CompareTag("Wall"))
            {
                // 撞墙销毁
                NetworkDestroyBullet();
            }
        }
    }

    private void NetworkDestroyBullet()
    {
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // 生命周期结束后销毁
    private IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(lifeTime);
        NetworkDestroyBullet();
    }
}
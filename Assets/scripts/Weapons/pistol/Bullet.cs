using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifeTime = 3f;
    public float damage = 5f;

    // 記錄是誰發射了這顆子彈，用於進行權威命中判定
    private PhotonView ownerPhotonView;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // 子彈被生成後，立即沿自己的前方飛行
        rb.linearVelocity = transform.right * speed;
        // 使用 Unity 的常規 Destroy 方法，在一定時間後銷毀本地物件
        Destroy(gameObject, lifeTime);
    }

    /// <summary>
    /// 初始化子彈，由生成它的 RPC 調用，傳入傷害值和發射者的 PhotonView。
    /// </summary>
    public void Initialize(float dmg, PhotonView ownerPV)
    {
        this.damage = dmg;
        this.ownerPhotonView = ownerPV;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 關鍵：只有子彈的擁有者 (ownerPhotonView.IsMine) 才能進行傷害判定
        if (ownerPhotonView != null && ownerPhotonView.IsMine)
        {
            Character character = other.GetComponent<Character>();
            // 確保碰撞到的物體是角色，並且不是發射者自己
            if (character != null && character.photonView.ViewID != ownerPhotonView.ViewID)
            {
                // 命中後，由開火者發起 RPC 來同步傷害給所有客戶端
                character.photonView.RPC("TakeDamage", RpcTarget.All, this.damage);

                // 碰撞後立即銷毀本地子彈
                Destroy(gameObject);
            }
            // 如果撞到牆等環境物體
            else if (other.CompareTag("Wall"))
            {
                // 立即銷毀本地子彈
                Destroy(gameObject);
            }
        }
    }
}

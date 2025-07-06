<<<<<<< Updated upstream
﻿using UnityEngine;
=======
﻿// Bullet.cs (modified)
using UnityEngine;
using Photon.Pun;
>>>>>>> Stashed changes

public class Bullet : MonoBehaviour
{
    public float speed;
    public GameObject explosionPrefab;
    new private Rigidbody2D rigidbody;
    public float damage = 5f;

<<<<<<< Updated upstream
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Awake()        //awake much fast
=======
    // Damage popup settings
    [Header("Damage Popup")]
    public GameObject damagePopupPrefab;  // Assign in inspector
    public float popupVerticalOffset = 0.5f;

    // 記錄是誰發射了這顆子彈，用於進行權威命中判定
    private PhotonView ownerPhotonView;
    private Rigidbody2D rb;

    void Awake()
>>>>>>> Stashed changes
    {
        rigidbody = GetComponent<Rigidbody2D>();
    }

    public void SetSpeed(Vector2 direction)        //control flying by direction and speed
    {
        rigidbody.linearVelocity = direction * speed;
    }

    // Update is called once per frame
    void Update()
    {

    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null)
        {
<<<<<<< Updated upstream
            character.TakeDamage(damage);
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    public void SetDamage(float _damage)
    {
        damage=_damage;
=======
            Character character = other.GetComponent<Character>();
            // 確保碰撞到的物體是角色，並且不是發射者自己
            if (character != null && character.photonView.ViewID != ownerPhotonView.ViewID)
            {
                // 命中後，由開火者發起 RPC 來同步傷害給所有客戶端
                character.photonView.RPC("TakeDamage", RpcTarget.All, this.damage);

                // 顯示傷害數字（只有開火者會看到）
                ShowDamagePopup(transform.position, this.damage);

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

    private void ShowDamagePopup(Vector3 position, float damageValue)
    {
        if (damagePopupPrefab == null) return;

        Vector3 popupPosition = position + Vector3.up * popupVerticalOffset;
        GameObject popup = Instantiate(damagePopupPrefab, popupPosition, Quaternion.identity);
        DamagePopup damagePopup = popup.GetComponent<DamagePopup>();
        if (damagePopup != null)
        {
            damagePopup.SetDamage(damageValue);
        }
>>>>>>> Stashed changes
    }
}
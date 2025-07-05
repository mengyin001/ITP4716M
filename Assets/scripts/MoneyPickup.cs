using UnityEngine;
using Photon.Pun;

public class MoneyPickup : MonoBehaviourPun
{
    [SerializeField] private int value = 10;
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponent<PhotonView>();

            // 确保只有触发碰撞的玩家才能捡起
            if (playerView != null && playerView.IsMine)
            {
                MoneyManager.Instance.AddMoney(value);

                if (pickupSound != null)
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);

                // 销毁物品
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
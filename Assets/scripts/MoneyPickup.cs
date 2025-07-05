using UnityEngine;
using Photon.Pun;

public class MoneyPickup : MonoBehaviourPun
{
    [SerializeField] private int value = 10;
    [SerializeField] private AudioClip pickupSound;

    private bool isDestroyed = false; // 防止多次销毁

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;

        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponent<PhotonView>();

            // 确保只有触发碰撞的玩家才能捡起
            if (playerView != null && playerView.IsMine)
            {
                // 直接获取玩家身上的 MoneyManager 组件
                MoneyManager moneyManager = other.GetComponent<MoneyManager>();

                if (moneyManager != null)
                {
                    moneyManager.AddMoney(value);

                    if (pickupSound != null)
                        AudioSource.PlayClipAtPoint(pickupSound, transform.position);

                    // 标记为已销毁
                    isDestroyed = true;

                    // 请求 MasterClient 销毁物品
                    photonView.RPC("RequestDestroyPickup", RpcTarget.MasterClient);
                }
                else
                {
                    Debug.LogWarning("MoneyManager component not found on player!");
                }
            }
        }
    }

    [PunRPC]
    private void RequestDestroyPickup()
    {
        // 只有 MasterClient 执行销毁
        if (PhotonNetwork.IsMasterClient)
        {
            // 确认对象仍然存在
            if (gameObject != null)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
}
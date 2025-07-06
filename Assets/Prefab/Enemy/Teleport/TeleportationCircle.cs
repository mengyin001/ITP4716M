using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TeleportationCircle : MonoBehaviour
{
    public string targetSceneName;
    public string loadingSceneName = "LoadingScence";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponent<PhotonView>();

            // 确保只有本地玩家触发RPC
            if (playerView != null && playerView.IsMine)
            {
                // 调用RPC通知所有客户端（包括主机）有玩家进入传送门
                playerView.RPC("RPC_PlayerEnteredTeleport", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void RPC_PlayerEnteredTeleport()
    {
        // 只有主机有权限加载场景
        if (PhotonNetwork.IsMasterClient)
        {
            // 保存目标场景
            SceneLoader.targetScene = targetSceneName;

            // 主机调用加载场景，所有客户端会自动同步
            PhotonNetwork.LoadLevel(loadingSceneName);
        }
    }
}
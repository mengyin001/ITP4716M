using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TeleportationCircle : MonoBehaviourPun
{
    public string targetSceneName;
    public string loadingSceneName = "LoadingScence";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 只有本地玩家才能触发RPC调用
            PhotonView playerView = other.GetComponent<PhotonView>();
            if (playerView != null && playerView.IsMine)
            {
                // 调用RPC通知所有客户端有玩家进入传送门
                photonView.RPC("RPC_PlayerEnteredTeleport", RpcTarget.AllViaServer);
            }
        }
    }

    [PunRPC]
    private void RPC_PlayerEnteredTeleport()
    {
        // 只有主机可以加载场景
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"主机开始加载场景: {loadingSceneName}");

            // 保存目标场景
            SceneLoader.targetScene = targetSceneName;

            // 主机加载场景，所有客户端会自动同步
            PhotonNetwork.LoadLevel(loadingSceneName);
        }
    }
}
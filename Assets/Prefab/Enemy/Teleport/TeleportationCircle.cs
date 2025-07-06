using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class TeleportationCircle : MonoBehaviourPunCallbacks
{
    public string targetSceneName; // 目标场景名
    public string loadingSceneName; // 加载场景名（可选）

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 只有房主能触发场景切换
            if (PhotonNetwork.IsMasterClient)
            {
                // 发送 RPC 通知所有玩家切换场景
                photonView.RPC("RPC_SyncScene", RpcTarget.AllViaServer);
            }
        }
    }

    [PunRPC]
    private void RPC_SyncScene()
    {
        // 若有加载场景，则先加载加载场景，否则直接加载目标场景
        string sceneToLoad = string.IsNullOrEmpty(loadingSceneName) ? targetSceneName : loadingSceneName;
        PhotonNetwork.LoadLevel(sceneToLoad); // Photon 自动同步所有玩家场景
    }
}
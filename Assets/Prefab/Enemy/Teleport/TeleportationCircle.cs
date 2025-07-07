using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class TeleportationCircle : MonoBehaviourPunCallbacks
{
    public string targetSceneName;
    public string loadingSceneName = "LoadingScene";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 只允许房主触发场景切换
            if (PhotonNetwork.IsMasterClient)
            {
                // 调用RPC通知所有客户端加载目标场景
                photonView.RPC("RPC_LoadSceneForAll", RpcTarget.AllViaServer, targetSceneName);
            }
            else
            {
                Debug.Log("只有房主可以触发场景切换");
            }
        }
    }

    [PunRPC]
    private void RPC_LoadSceneForAll(string sceneName)
    {
        // 保存目标场景
        SceneLoader.targetScene = sceneName;

        // 只有主机可以加载场景，其他客户端会自动同步
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"主机开始加载场景: {sceneName}");
            PhotonNetwork.LoadLevel(loadingSceneName);
        }
    }
}
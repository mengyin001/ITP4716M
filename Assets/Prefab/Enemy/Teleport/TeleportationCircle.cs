using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TeleportationCircle : MonoBehaviourPunCallbacks
{
    public string targetSceneName;
    public string loadingSceneName;
    private bool isTeleporting = false; // 防止重复触发

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTeleporting) return; // 防止重复触发
        
        if (other.CompareTag("Player") && other.TryGetComponent<PhotonView>(out var photonView) && photonView.IsMine)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                isTeleporting = true;
                Debug.Log("Master Client triggered scene change.");
                
                // 确保目标场景已设置
                if (string.IsNullOrEmpty(targetSceneName))
                {
                    Debug.LogError("Target scene name is not set!");
                    return;
                }
                
                // 保存目标场景并通知所有玩家加载过渡场景
                SceneLoader.targetScene = targetSceneName;
                photonView.RPC("RPC_LoadSceneForAll", RpcTarget.AllViaServer, loadingSceneName);
            }
            else
            {
                Debug.Log("Only Master Client can trigger scene change.");
            }
        }
    }

    [PunRPC]
    private void RPC_LoadSceneForAll(string loadingScene)
    {
        if (string.IsNullOrEmpty(loadingScene))
        {
            Debug.LogError("Loading scene name is not set!");
            return;
        }
        
        Debug.Log($"Loading transition scene: {loadingScene}");
        // 使用Photon的场景加载方式，确保所有客户端同步
        PhotonNetwork.LoadLevel(loadingScene);
    }
}

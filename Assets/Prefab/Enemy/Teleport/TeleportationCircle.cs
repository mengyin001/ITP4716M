using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TeleportationCircle : MonoBehaviourPunCallbacks
{
    public string targetSceneName;
    public string loadingSceneName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Master Client triggered scene change.");
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
        Debug.Log($"Loading transition scene: {loadingScene}");
        // 所有玩家（包括房主）加载过渡场景
        PhotonNetwork.LoadLevel(loadingScene);
    }
}

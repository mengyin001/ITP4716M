using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class TeleportationCircle : MonoBehaviourPunCallbacks
{
    public string targetSceneName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.GetComponent<PhotonView>().IsMine)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Master Client triggered scene change to: " + targetSceneName);
                photonView.RPC("RPC_LoadSceneForAll", RpcTarget.AllViaServer, targetSceneName);
            }
            else
            {
                Debug.Log("Only Master Client can trigger scene change.");
            }
        }
    }

    [PunRPC]
    private void RPC_LoadSceneForAll(string sceneName)
    {
        Debug.Log($"Loading target scene: {sceneName}");
        PhotonNetwork.AutomaticallySyncScene = true; // 确保启用自动同步
        PhotonNetwork.LoadLevel(sceneName);
    }
}
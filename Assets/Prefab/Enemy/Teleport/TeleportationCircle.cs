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

            // 确保只有本地玩家触发，并且是主机
            if (playerView != null && playerView.IsMine && PhotonNetwork.IsMasterClient)
            {
                // 保存目标场景
                SceneLoader.targetScene = targetSceneName;

                // 主机调用加载场景，所有客户端会自动同步
                PhotonNetwork.LoadLevel(loadingSceneName);
            }
        }
    }
}
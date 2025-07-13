// ===== GameLevelPlayerSpawner 修改部分 =====
using Photon.Pun;
using UnityEngine;

public class GameLevelPlayerSpawner : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;
    public GameObject playerPrefab;

    void Start()
    {
        // 确保在场景完全加载后生成玩家
        Invoke("SpawnPlayer", 0.5f);
    }

    void SpawnPlayer()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            // 检查是否已有玩家对象
            if (PhotonNetwork.LocalPlayer.TagObject == null)
            {
                int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;

                GameObject player = PhotonNetwork.Instantiate(
                    playerPrefab.name, // 使用预制体名称直接实例化
                    spawnPoints[spawnIndex].position,
                    spawnPoints[spawnIndex].rotation);

                PhotonNetwork.LocalPlayer.TagObject = player;
                Debug.Log($"Player spawned at index {spawnIndex}");
            }
            else
            {
                Debug.Log("Player already exists in game scene");
            }
        }
    }
}
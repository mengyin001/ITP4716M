using Photon.Pun;
using UnityEngine;

public class PlayerNetworkController : MonoBehaviourPunCallbacks
{
    [Header("玩家预制体")]
    public GameObject playerPrefab;

    [Header("生成点")]
    public Transform[] spawnPoints;

    void Start()
    {
        // 确保只在连接状态下生成玩家
        if (PhotonNetwork.IsConnectedAndReady)
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("玩家预制体未分配!");
            return;
        }

        // 随机选择一个生成点
        Transform spawnPoint = GetRandomSpawnPoint();

        // 实例化玩家角色（网络同步）
        PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // 创建默认生成点
            GameObject defaultSpawn = new GameObject("默认生成点");
            defaultSpawn.transform.position = Vector3.zero;
            return defaultSpawn.transform;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
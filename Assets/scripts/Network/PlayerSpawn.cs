using UnityEngine;
using Photon.Pun;
using System;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    public static event Action<HealthSystem> OnLocalPlayerSpawned;

    [Header("Spawn Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    void Start()
    {
        // 确保在正确的场景且已加入房间时生成玩家
        if (PhotonNetwork.InRoom && IsInCorrectScene())
        {
            AttemptSpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        // 加入房间时尝试生成玩家
        if (IsInCorrectScene())
        {
            AttemptSpawnPlayer();
        }
    }

    private bool IsInCorrectScene()
    {
        // 确保在正确的房间场景（安全屋）
        if (NetworkManager.Instance == null) return false;
        return gameObject.scene.name == NetworkManager.Instance.roomSceneName;
    }

    void AttemptSpawnPlayer()
    {
        // 确保必要组件存在
        if (playerPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        // 检查玩家是否已存在
        object playerTag = PhotonNetwork.LocalPlayer.TagObject;
        if (playerTag is GameObject existingPlayer && existingPlayer != null)
        {
            Debug.Log("Player already exists, skipping spawn");
            return;
        }

        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        int playerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int spawnIndex = (playerActorNumber - 1) % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[spawnIndex];

        GameObject playerGO = PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // 关联玩家对象到Photon玩家
        PhotonNetwork.LocalPlayer.TagObject = playerGO;
        Debug.Log($"Spawned player for actor {playerActorNumber} at index {spawnIndex}");

        // 触发本地玩家生成事件
        PhotonView pv = playerGO.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            HealthSystem healthSystem = playerGO.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                OnLocalPlayerSpawned?.Invoke(healthSystem);
            }
        }
    }
}
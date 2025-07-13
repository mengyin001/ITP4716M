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
        // ȷ������ȷ�ĳ������Ѽ��뷿��ʱ�������
        if (PhotonNetwork.InRoom && IsInCorrectScene())
        {
            AttemptSpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        // ���뷿��ʱ�����������
        if (IsInCorrectScene())
        {
            AttemptSpawnPlayer();
        }
    }

    private bool IsInCorrectScene()
    {
        // ȷ������ȷ�ķ��䳡������ȫ�ݣ�
        if (NetworkManager.Instance == null) return false;
        return gameObject.scene.name == NetworkManager.Instance.roomSceneName;
    }

    void AttemptSpawnPlayer()
    {
        // ȷ����Ҫ�������
        if (playerPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        // �������Ƿ��Ѵ���
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

        // ������Ҷ���Photon���
        PhotonNetwork.LocalPlayer.TagObject = playerGO;
        Debug.Log($"Spawned player for actor {playerActorNumber} at index {spawnIndex}");

        // ����������������¼�
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
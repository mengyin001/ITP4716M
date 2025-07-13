using System;
using Photon.Pun;
using UnityEngine;

public class GameLevelPlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("Settings")]
    public GameObject playerPrefab; // ����A�u�w
    public Transform[] spawnPoints; // �����c

    // �����ġ����xһ���o�B�¼������֪ͨ����ϵ�y��������ѽ�����
    // �������f�����ɵı�����ҵ� HealthSystem �M��
    public static event Action<HealthSystem> OnLocalPlayerSpawned;

    void Start()
    {
        // ���t�{���ǂ����e�Ă��÷��������������ķ�ʽ�ǵȴ��B�Ӿ;w
        if (PhotonNetwork.IsConnectedAndReady)
        {
            SpawnPlayer();
        }
        else
        {
            // ���߀�]�B�Ӻã�����������ԇ���� OnConnectedToMaster ���{��̎��
            Debug.LogWarning("GameLevelPlayerSpawner: Not connected to Photon yet. Spawning might be delayed.");
        }
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[GameLevelPlayerSpawner] Player Prefab is not assigned in the inspector!");
            return;
        }

        if (spawnPoints.Length == 0)
        {
            Debug.LogError("[GameLevelPlayerSpawner] No spawn points assigned in the inspector!");
            return;
        }

        // ����߉݋���鱾���������һ����ɫ����
        // PhotonNetwork.Instantiate ���_���@�����ֻ�鮔ǰ�͑��˄������K�ھW�j��ͬ���o������
        // �����ص��Ǆ��������� GameObject
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[spawnIndex];

        GameObject localPlayerObject = PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Debug.Log($"[GameLevelPlayerSpawner] Instantiated player '{localPlayerObject.name}' for local client.");

        // �������ɵ��������P�� Photon ��ҵ� TagObject �ϣ��������m����
        PhotonNetwork.LocalPlayer.TagObject = localPlayerObject;

        // ���P�I���E���@ȡ��������ҵ� HealthSystem �M��
        HealthSystem playerHealthSystem = localPlayerObject.GetComponent<HealthSystem>();
        if (playerHealthSystem != null)
        {
            // �|�l�o�B�¼����K�� HealthSystem �������酢�����f��ȥ
            // ����ӆ����@���¼��Ĺ����� (�� InventoryManager, UIManager) �����յ�֪ͨ
            OnLocalPlayerSpawned?.Invoke(playerHealthSystem);
            Debug.Log($"[GameLevelPlayerSpawner] 'OnLocalPlayerSpawned' event has been invoked for {localPlayerObject.name}.");
        }
        else
        {
            Debug.LogError($"[GameLevelPlayerSpawner] The spawned player prefab '{playerPrefab.name}' is missing the HealthSystem component!");
        }
    }
}

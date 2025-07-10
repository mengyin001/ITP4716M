using UnityEngine;
using Photon.Pun;
using System; // ��Ҫ���� System ����ʹ�� Action

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    // �������޸� 1��: ����һ���o�B�¼�
    // ��������ұ����ɕr���@���¼������|�l
    public static event Action<HealthSystem> OnLocalPlayerSpawned;

    [Header("Spawn Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    void Start()
    {
        if (PhotonNetwork.InRoom && IsInCorrectScene())
        {
            SpawnPlayer();
        }
    }

    public override void OnJoinedRoom()
    {
        if (IsInCorrectScene())
        {
            SpawnPlayer();
        }
    }

    private bool IsInCorrectScene()
    {
        if (NetworkManager.Instance == null) return false;
        return gameObject.scene.name == NetworkManager.Instance.roomSceneName;
    }

    void SpawnPlayer()
    {
        if (playerPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;
        if (PhotonNetwork.LocalPlayer.TagObject != null) return;

        int playerActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        int spawnIndex = (playerActorNumber - 1) % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[spawnIndex];

        GameObject playerGO = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
        PhotonNetwork.LocalPlayer.TagObject = playerGO;

        // �������޸� 2��: �|�l�¼�
        // ����������ᣬ�z�����ǲ����҂�������ҵ�
        PhotonView pv = playerGO.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            HealthSystem healthSystem = playerGO.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                Debug.Log($"[PlayerSpawner] Local player spawned. Firing OnLocalPlayerSpawned event.");
                // �|�l�¼����K�� HealthSystem �����V����ȥ
                OnLocalPlayerSpawned?.Invoke(healthSystem);
            }
        }
    }
}

using UnityEngine;
using Photon.Pun;
using System; // 需要引用 System 才能使用 Action

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    // 【核心修改 1】: 創建一個靜態事件
    // 當本地玩家被生成時，這個事件會被觸發
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

        // 【核心修改 2】: 觸發事件
        // 在生成物件後，檢查它是不是我們本地玩家的
        PhotonView pv = playerGO.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            HealthSystem healthSystem = playerGO.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                Debug.Log($"[PlayerSpawner] Local player spawned. Firing OnLocalPlayerSpawned event.");
                // 觸發事件，並將 HealthSystem 實例廣播出去
                OnLocalPlayerSpawned?.Invoke(healthSystem);
            }
        }
    }
}

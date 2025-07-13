using System;
using Photon.Pun;
using UnityEngine;

public class GameLevelPlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("Settings")]
    public GameObject playerPrefab; // 玩家預製體
    public Transform[] spawnPoints; // 出生點

    // 【核心】定義一個靜態事件，用於通知其他系統本地玩家已經生成
    // 它會傳遞新生成的本地玩家的 HealthSystem 組件
    public static event Action<HealthSystem> OnLocalPlayerSpawned;

    void Start()
    {
        // 延遲調用是個不錯的備用方案，但更穩健的方式是等待連接就緒
        if (PhotonNetwork.IsConnectedAndReady)
        {
            SpawnPlayer();
        }
        else
        {
            // 如果還沒連接好，可以稍後再試或在 OnConnectedToMaster 回調中處理
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

        // 核心邏輯：為本地玩家生成一個角色實例
        // PhotonNetwork.Instantiate 會確保這個物件只為當前客戶端創建，並在網絡上同步給其他人
        // 它返回的是剛剛創建的 GameObject
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[spawnIndex];

        GameObject localPlayerObject = PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Debug.Log($"[GameLevelPlayerSpawner] Instantiated player '{localPlayerObject.name}' for local client.");

        // 將新生成的玩家物件關聯到 Photon 玩家的 TagObject 上，方便後續查找
        PhotonNetwork.LocalPlayer.TagObject = localPlayerObject;

        // 【關鍵步驟】獲取新生成玩家的 HealthSystem 組件
        HealthSystem playerHealthSystem = localPlayerObject.GetComponent<HealthSystem>();
        if (playerHealthSystem != null)
        {
            // 觸發靜態事件，並將 HealthSystem 實例作為參數傳遞出去
            // 所有訂閱了這個事件的管理器 (如 InventoryManager, UIManager) 都會收到通知
            OnLocalPlayerSpawned?.Invoke(playerHealthSystem);
            Debug.Log($"[GameLevelPlayerSpawner] 'OnLocalPlayerSpawned' event has been invoked for {localPlayerObject.name}.");
        }
        else
        {
            Debug.LogError($"[GameLevelPlayerSpawner] The spawned player prefab '{playerPrefab.name}' is missing the HealthSystem component!");
        }
    }
}

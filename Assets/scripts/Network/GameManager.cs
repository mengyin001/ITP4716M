using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("玩家设置")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 添加场景加载回调
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // 移除回调防止内存泄漏
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只在游戏场景生成玩家
        if (scene.name == "SafeHouse" && PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("玩家预制体未分配!");
            return;
        }

        // 检查是否是当前玩家
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("尝试生成玩家但不在房间中");
            return;
        }

        // 随机选择一个生成点
        Transform spawnPoint = GetRandomSpawnPoint();

        // 实例化玩家角色（网络同步）
        PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPoint.position,
            Quaternion.identity,
            0
        );
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // 尝试查找场景中的生成点
            GameObject[] foundPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (foundPoints.Length > 0)
            {
                spawnPoints = new Transform[foundPoints.Length];
                for (int i = 0; i < foundPoints.Length; i++)
                {
                    spawnPoints[i] = foundPoints[i].transform;
                }
            }
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // 创建默认生成点
            GameObject defaultSpawn = new GameObject("默认生成点");
            defaultSpawn.transform.position = Vector3.zero;
            return defaultSpawn.transform;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    public override void OnLeftRoom()
    {
        // 销毁本地玩家对象
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PhotonView view = player.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
            {
                PhotonNetwork.Destroy(player);
            }
        }
    }
}
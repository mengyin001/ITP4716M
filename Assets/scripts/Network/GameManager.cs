using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("�������")]
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

        // ��ӳ������ػص�
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // �Ƴ��ص���ֹ�ڴ�й©
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ֻ����Ϸ�����������
        if (scene.name == "SafeHouse" && PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("���Ԥ����δ����!");
            return;
        }

        // ����Ƿ��ǵ�ǰ���
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("����������ҵ����ڷ�����");
            return;
        }

        // ���ѡ��һ�����ɵ�
        Transform spawnPoint = GetRandomSpawnPoint();

        // ʵ������ҽ�ɫ������ͬ����
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
            // ���Բ��ҳ����е����ɵ�
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
            // ����Ĭ�����ɵ�
            GameObject defaultSpawn = new GameObject("Ĭ�����ɵ�");
            defaultSpawn.transform.position = Vector3.zero;
            return defaultSpawn.transform;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    public override void OnLeftRoom()
    {
        // ���ٱ�����Ҷ���
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
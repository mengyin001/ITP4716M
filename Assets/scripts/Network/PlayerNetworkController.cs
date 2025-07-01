using Photon.Pun;
using UnityEngine;

public class PlayerNetworkController : MonoBehaviourPunCallbacks
{
    [Header("���Ԥ����")]
    public GameObject playerPrefab;

    [Header("���ɵ�")]
    public Transform[] spawnPoints;

    void Start()
    {
        // ȷ��ֻ������״̬���������
        if (PhotonNetwork.IsConnectedAndReady)
        {
            SpawnPlayer();
        }
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("���Ԥ����δ����!");
            return;
        }

        // ���ѡ��һ�����ɵ�
        Transform spawnPoint = GetRandomSpawnPoint();

        // ʵ������ҽ�ɫ������ͬ����
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
            // ����Ĭ�����ɵ�
            GameObject defaultSpawn = new GameObject("Ĭ�����ɵ�");
            defaultSpawn.transform.position = Vector3.zero;
            return defaultSpawn.transform;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}
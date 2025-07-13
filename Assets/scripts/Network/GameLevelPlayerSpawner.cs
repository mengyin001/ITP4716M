// ===== GameLevelPlayerSpawner �޸Ĳ��� =====
using Photon.Pun;
using UnityEngine;

public class GameLevelPlayerSpawner : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;
    public GameObject playerPrefab;

    void Start()
    {
        // ȷ���ڳ�����ȫ���غ��������
        Invoke("SpawnPlayer", 0.5f);
    }

    void SpawnPlayer()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            // ����Ƿ�������Ҷ���
            if (PhotonNetwork.LocalPlayer.TagObject == null)
            {
                int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;

                GameObject player = PhotonNetwork.Instantiate(
                    playerPrefab.name, // ʹ��Ԥ��������ֱ��ʵ����
                    spawnPoints[spawnIndex].position,
                    spawnPoints[spawnIndex].rotation);

                PhotonNetwork.LocalPlayer.TagObject = player;
                Debug.Log($"Player spawned at index {spawnIndex}");
            }
            else
            {
                Debug.Log("Player already exists in game scene");
            }
        }
    }
}
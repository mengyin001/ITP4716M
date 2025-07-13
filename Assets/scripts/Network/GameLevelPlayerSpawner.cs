using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameLevelPlayerSpawner : MonoBehaviourPunCallbacks
{
    public Transform[] spawnPoints;

    void Start()
    {
        ResetPlayerPositions();
    }

    void ResetPlayerPositions()
    {
        if (!PhotonNetwork.InRoom) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                int spawnIndex = (pv.Owner.ActorNumber - 1) % spawnPoints.Length;
                player.transform.position = spawnPoints[spawnIndex].position;
                player.transform.rotation = spawnPoints[spawnIndex].rotation;
            }
        }
    }

    // 当新玩家加入房间时重置位置
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ResetPlayerPositions();
    }
}
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CameraController : MonoBehaviourPun
{
    private Transform player;

    void Start()
    {
        // 查找本地玩家角色
        FindLocalPlayer();
    }

    void Update()
    {
        // 如果玩家对象丢失（例如角色重生），尝试重新查找
        if (player == null)
        {
            FindLocalPlayer();
            return; // 本次帧跳过更新位置
        }

        // 只跟随本地玩家
        transform.position = new Vector3(player.position.x, player.position.y + 0.2f, -10);
    }

    void FindLocalPlayer()
    {
        // 查找场景中所有玩家对象
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject potentialPlayer in players)
        {
            PhotonView pv = potentialPlayer.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                player = potentialPlayer.transform;
                break;
            }
        }
    }
}
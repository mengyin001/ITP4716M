using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CameraController : MonoBehaviourPun
{
    private Transform player;
    private Quaternion fixedRotation; // 存储固定旋转值

    void Start()
    {
        // 初始查找本地玩家
        FindLocalPlayer();

        // 记录初始旋转值并固定
        fixedRotation = Quaternion.identity;
        transform.rotation = fixedRotation;
    }

    void LateUpdate()
    {
        // 确保旋转始终固定（即使其他脚本尝试修改）
        transform.rotation = fixedRotation;

        // 如果玩家对象丢失，尝试重新查找
        if (player == null)
        {
            FindLocalPlayer();
            return; // 本次帧跳过更新位置
        }

        // 只更新位置，不更新旋转
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
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CameraController : MonoBehaviourPun
{
    private Transform player;

    void Start()
    {
        // ���ұ�����ҽ�ɫ
        FindLocalPlayer();
    }

    void Update()
    {
        // �����Ҷ���ʧ�������ɫ���������������²���
        if (player == null)
        {
            FindLocalPlayer();
            return; // ����֡��������λ��
        }

        // ֻ���汾�����
        transform.position = new Vector3(player.position.x, player.position.y + 0.2f, -10);
    }

    void FindLocalPlayer()
    {
        // ���ҳ�����������Ҷ���
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
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CameraController : MonoBehaviourPun
{
    private Transform player;
    private Quaternion fixedRotation; // �洢�̶���תֵ

    void Start()
    {
        // ��ʼ���ұ������
        FindLocalPlayer();

        // ��¼��ʼ��תֵ���̶�
        fixedRotation = Quaternion.identity;
        transform.rotation = fixedRotation;
    }

    void LateUpdate()
    {
        // ȷ����תʼ�չ̶�����ʹ�����ű������޸ģ�
        transform.rotation = fixedRotation;

        // �����Ҷ���ʧ���������²���
        if (player == null)
        {
            FindLocalPlayer();
            return; // ����֡��������λ��
        }

        // ֻ����λ�ã���������ת
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
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class EnemyBulletPool : MonoBehaviourPunCallbacks, IPunObservable
{
    public GameObject bulletPrefab;
    public int poolSize = 10;

    // 可用子弹队列（主机维护）
    private Queue<GameObject> availableBullets = new Queue<GameObject>();
    // 所有子弹列表（用于同步引用）
    private List<GameObject> allBullets = new List<GameObject>();
    // 可用子弹ID列表（用于网络同步）
    private List<int> availableBulletIDs = new List<int>();

    private void Start()
    {
        // 仅主机初始化子弹池
        if (PhotonNetwork.IsMasterClient)
        {
            InitializePool();
        }
    }

    public void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            CreateNewBullet();
        }
    }

    private GameObject CreateNewBullet()
    {
        GameObject bullet = PhotonNetwork.Instantiate(
            bulletPrefab.name,
            transform.position,
            Quaternion.identity
        );

        // 转移所有权给MasterClient
        PhotonView bulletView = bullet.GetComponent<PhotonView>();
        if (bulletView != null)
        {
            bulletView.TransferOwnership(PhotonNetwork.MasterClient);
        }

        bullet.SetActive(false);
        bullet.transform.SetParent(transform);

        if (PhotonNetwork.IsMasterClient)
        {
            availableBullets.Enqueue(bullet);
            availableBulletIDs.Add(bulletView.ViewID);
        }

        allBullets.Add(bullet);
        return bullet;
    }

    // 从池获取子弹
    public GameObject GetBullet(Vector3 spawnPosition)
    {
        if (!PhotonNetwork.IsMasterClient) return null;

        GameObject bullet;
        if (availableBullets.Count == 0)
        {
            // 池为空时创建新子弹
            bullet = CreateNewBullet();
        }
        else
        {
            bullet = availableBullets.Dequeue();
            availableBulletIDs.Remove(bullet.GetComponent<PhotonView>().ViewID);
        }

        // 设置子弹位置并激活
        bullet.transform.position = spawnPosition;
        bullet.SetActive(true);

        // 通过RPC同步子弹激活状态到所有客户端
        bullet.GetComponent<PhotonView>().RPC("ActivateBullet", RpcTarget.All, spawnPosition);
        return bullet;
    }

    // 回收子弹到池
    public void ReturnBullet(GameObject bullet)
    {
        if (!PhotonNetwork.IsMasterClient || bullet == null) return;

        bullet.SetActive(false);
        bullet.transform.position = transform.position;
        availableBullets.Enqueue(bullet);
        availableBulletIDs.Add(bullet.GetComponent<PhotonView>().ViewID);

        // 通过RPC同步子弹禁用状态到所有客户端
        bullet.GetComponent<PhotonView>().RPC("DeactivateBullet", RpcTarget.All);
    }

    // 同步子弹池状态
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 主机发送可用子弹ID列表
            stream.SendNext(availableBulletIDs);
        }
        else
        {
            // 客户端接收可用子弹ID列表
            availableBulletIDs = (List<int>)stream.ReceiveNext();
        }
    }
}
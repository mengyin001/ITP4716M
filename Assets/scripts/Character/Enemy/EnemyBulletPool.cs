using System.Collections.Generic;
using UnityEngine;

public class EnemyBulletPool : MonoBehaviour
{
    public static EnemyBulletPool Instance;
    public GameObject bulletPrefab;
    public int poolSize = 20;

    private Queue<GameObject> availableBullets = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bullet.transform.SetParent(transform);
            availableBullets.Enqueue(bullet);
        }
    }

    public GameObject GetBullet()
    {
        if (availableBullets.Count == 0)
        {
            // ��̬��չ�ش�С
            ExpandPool(5);
        }

        GameObject pooledBullet = availableBullets.Dequeue();
        pooledBullet.SetActive(true);
        return pooledBullet;
    }

    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        bullet.transform.SetParent(transform); // ���ø�����
        availableBullets.Enqueue(bullet);
    }

    private void ExpandPool(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bullet.transform.SetParent(transform);
            availableBullets.Enqueue(bullet);
        }
    }
}
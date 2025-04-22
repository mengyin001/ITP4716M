using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSkill : MonoBehaviour
{
    [Header("Boss Settings")]
    public BossHealth bossHealth; // ֱ������ BossHealth
    public GameObject minionPrefab; // С�ܵ�Ԥ����
    public Transform spawnPoint; // С�����ɵĵص�
    public float spawnInterval = 5f; // ����С�ܵļ��ʱ��
    public int maxMinions = 5; // ���С������

    private List<GameObject> minions = new List<GameObject>();

    private void Start()
    {
        StartCoroutine(SpawnMinions());
    }

    private IEnumerator SpawnMinions()
    {
        while (minions.Count < maxMinions)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnMinion();
        }
    }

    private void SpawnMinion()
    {
        GameObject minion = Instantiate(minionPrefab, spawnPoint.position, Quaternion.identity);
        minions.Add(minion);
        // ��������������С�ܵ����ԣ�������ֵ��
    }

    public void AbsorbMinion(GameObject minion)
    {
        if (minions.Contains(minion))
        {
            // ����С�ܲ���Ѫ
            float healAmount = 10f; // ����С�ܻ�Ѫ����
            bossHealth.Heal(healAmount);
            Destroy(minion); // ����С��
            minions.Remove(minion);
        }
    }
}
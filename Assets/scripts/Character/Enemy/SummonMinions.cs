using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonMinions : MonoBehaviour
{
    [Header("Minion Settings")]
    public GameObject minionPrefab; // С�ܵ�Ԥ����
    public int minionsPerSummon = 3; // ÿ���ٻ���С������
    public float summonRadius = 3f; // С�����ɵİ뾶��Χ

    [Header("Summon Interval")]
    public float summonInterval = 10f; // �ٻ�С�ܵ�ʱ����
    private float summonTimer; // �ٻ���ʱ��

    [Header("Absorb Settings")]
    public float absorbRadius = 1f; // ����С�ܵİ뾶��Χ
    public float healthRestoreAmount = 10f; // ÿ������С�ܻظ�������ֵ

    private Character bossCharacter; // ���� Boss �� Character �ű�

    private List<GameObject> minions = new List<GameObject>(); // �洢��ǰ���ڵ�С��

    private void Awake()
    {
        bossCharacter = GetComponent<Character>();
        summonTimer = summonInterval;
    }

    private void Update()
    {
        if (bossCharacter == null)
        {
            Debug.LogError("Boss Character script not found!");
            return;
        }

        // �ݼ��ٻ���ʱ��
        summonTimer -= Time.deltaTime;
        if (summonTimer <= 0f)
        {
            SummonMinionsBatch();
            summonTimer = summonInterval;
        }

        AbsorbMinions();
    }

    private void SummonMinionsBatch()
    {
        for (int i = 0; i < minionsPerSummon; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * summonRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            GameObject minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
            minions.Add(minion);
        }
    }

    private void AbsorbMinions()
    {
        for (int i = minions.Count - 1; i >= 0; i--)
        {
            GameObject minion = minions[i];
            float distance = Vector2.Distance(transform.position, minion.transform.position);
            if (distance <= absorbRadius)
            {
                // �ָ� Boss ������ֵ
                bossCharacter.currentHealth += healthRestoreAmount;
                if (bossCharacter.currentHealth > 100f) // �����������ֵΪ 100
                {
                    bossCharacter.currentHealth = 100f;
                }

                // ����С��
                Destroy(minion);
                minions.RemoveAt(i);
            }
        }
    }
}
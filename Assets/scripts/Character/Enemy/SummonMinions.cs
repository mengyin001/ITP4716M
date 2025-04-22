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

    [Header("Boundary Settings")]
    public LayerMask walkableLayer; // ����Walk�����Layer
    public float maxSpawnAttempts = 10; // ��������ɴ���

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
            TrySpawnMinion();
        }
    }

    private void TrySpawnMinion()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * summonRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            // ������ɵ��Ƿ���Walk������
            if (IsPositionInWalkableArea(spawnPosition))
            {
                GameObject minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
                minions.Add(minion);
                return;
            }
        }
        
        Debug.LogWarning("Failed to find valid spawn position for minion after " + maxSpawnAttempts + " attempts");
    }

    private bool IsPositionInWalkableArea(Vector3 position)
    {
        // ʹ��Physics2D.OverlapCircle����λ���Ƿ���Walkable��
        Collider2D hit = Physics2D.OverlapCircle(position, 0.1f, walkableLayer);
        return hit != null;
    }

    private void AbsorbMinions()
    {
        for (int i = minions.Count - 1; i >= 0; i--)
        {
            GameObject minion = minions[i];
            
            // ���С���Ƿ��ѱ�����
            if (minion == null)
            {
                minions.RemoveAt(i);
                continue;
            }

            float distance = Vector2.Distance(transform.position, minion.transform.position);
            if (distance <= absorbRadius)
            {
                // �ָ� Boss ������ֵ
                bossCharacter.currentHealth = Mathf.Min(
                    bossCharacter.currentHealth + healthRestoreAmount, 
                    100f);

                // ����С��
                Destroy(minion);
                minions.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // �����ٻ���Χ
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, summonRadius);

        // �������շ�Χ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, absorbRadius);
    }
}
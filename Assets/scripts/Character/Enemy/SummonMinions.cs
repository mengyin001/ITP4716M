using System.Collections.Generic;
using UnityEngine;
using Pathfinding; // ��Ҫ A* Pathfinding Project ���

public class SummonMinions : MonoBehaviour
{
    [Header("Minion Settings")]
    public GameObject minionPrefab; // С��Ԥ����
    public int minionsPerSummon = 3; // ÿ���ٻ�����
    public float summonRadius = 3f; // �ٻ��뾶

    [Header("Summon Interval")]
    public float summonInterval = 10f; // �ٻ����
    private float summonTimer; // ��ʱ��

    [Header("Absorb Settings")]
    public float absorbRadius = 1f; // ���հ뾶
    public float healthRestoreAmount = 10f; // ���ջ�Ѫ��

    [Header("Boundary Settings")]
    public LayerMask walkableLayer; // �����߲㣨���÷�����
    public float maxSpawnAttempts = 10; // ����Դ���

    private Character bossCharacter; // Boss ��ɫ�ű�
    private List<GameObject> minions = new List<GameObject>(); // С���б�

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

        // �ٻ���ʱ
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

            if (IsPositionWalkable(spawnPosition))
            {
                GameObject minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
                minions.Add(minion);
                return;
            }
        }
        Debug.LogWarning("Failed to find valid spawn position after " + maxSpawnAttempts + " attempts");
    }

    /// <summary>
    /// ���λ���Ƿ�����ߣ������� A*������ Physics2D ��⣩
    /// </summary>
    private bool IsPositionWalkable(Vector3 position)
    {
        // ����1��ʹ�� A* Pathfinding Project ���
        if (AstarPath.active != null)
        {
            var node = AstarPath.active.GetNearest(position).node;
            if (node != null && node.Walkable)
                return true;
        }

        // ����2������ Physics2D ��⣨������ Tilemap �� Collider ��ǿ���������
        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f, walkableLayer);
        return hit != null;
    }

    private void AbsorbMinions()
    {
        for (int i = minions.Count - 1; i >= 0; i--)
        {
            GameObject minion = minions[i];
            if (minion == null)
            {
                minions.RemoveAt(i);
                continue;
            }

            float distance = Vector2.Distance(transform.position, minion.transform.position);
            if (distance <= absorbRadius)
            {
                bossCharacter.currentHealth = Mathf.Min(
                    bossCharacter.currentHealth + healthRestoreAmount,
                    100f);

                Destroy(minion);
                minions.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // �ٻ���Χ����ɫ��
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, summonRadius);

        // ���շ�Χ����ɫ��
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, absorbRadius);
    }
}
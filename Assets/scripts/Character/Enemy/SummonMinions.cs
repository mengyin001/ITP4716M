using System.Collections.Generic;
using UnityEngine;
using Pathfinding; // 需要 A* Pathfinding Project 插件

public class SummonMinions : MonoBehaviour
{
    [Header("Minion Settings")]
    public GameObject minionPrefab; // 小弟预制体
    public int minionsPerSummon = 3; // 每次召唤数量
    public float summonRadius = 3f; // 召唤半径

    [Header("Summon Interval")]
    public float summonInterval = 10f; // 召唤间隔
    private float summonTimer; // 计时器

    [Header("Absorb Settings")]
    public float absorbRadius = 1f; // 吸收半径
    public float healthRestoreAmount = 10f; // 吸收回血量

    [Header("Boundary Settings")]
    public LayerMask walkableLayer; // 可行走层（备用方案）
    public float maxSpawnAttempts = 10; // 最大尝试次数

    private Character bossCharacter; // Boss 角色脚本
    private List<GameObject> minions = new List<GameObject>(); // 小弟列表

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

        // 召唤计时
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
    /// 检查位置是否可行走（优先用 A*，备用 Physics2D 检测）
    /// </summary>
    private bool IsPositionWalkable(Vector3 position)
    {
        // 方案1：使用 A* Pathfinding Project 检测
        if (AstarPath.active != null)
        {
            var node = AstarPath.active.GetNearest(position).node;
            if (node != null && node.Walkable)
                return true;
        }

        // 方案2：备用 Physics2D 检测（适用于 Tilemap 或 Collider 标记可行走区域）
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
        // 召唤范围（蓝色）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, summonRadius);

        // 吸收范围（红色）
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, absorbRadius);
    }
}
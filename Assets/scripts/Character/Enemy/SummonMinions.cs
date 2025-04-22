using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummonMinions : MonoBehaviour
{
    [Header("Minion Settings")]
    public GameObject minionPrefab; // 小弟的预制体
    public int minionsPerSummon = 3; // 每次召唤的小弟数量
    public float summonRadius = 3f; // 小弟生成的半径范围

    [Header("Summon Interval")]
    public float summonInterval = 10f; // 召唤小弟的时间间隔
    private float summonTimer; // 召唤计时器

    [Header("Absorb Settings")]
    public float absorbRadius = 1f; // 吸收小弟的半径范围
    public float healthRestoreAmount = 10f; // 每次吸收小弟回复的生命值

    [Header("Boundary Settings")]
    public LayerMask walkableLayer; // 设置Walk区域的Layer
    public float maxSpawnAttempts = 10; // 最大尝试生成次数

    private Character bossCharacter; // 引用 Boss 的 Character 脚本
    private List<GameObject> minions = new List<GameObject>(); // 存储当前存在的小弟

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

        // 递减召唤计时器
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

            // 检查生成点是否在Walk区域内
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
        // 使用Physics2D.OverlapCircle检查该位置是否在Walkable层
        Collider2D hit = Physics2D.OverlapCircle(position, 0.1f, walkableLayer);
        return hit != null;
    }

    private void AbsorbMinions()
    {
        for (int i = minions.Count - 1; i >= 0; i--)
        {
            GameObject minion = minions[i];
            
            // 检查小弟是否已被销毁
            if (minion == null)
            {
                minions.RemoveAt(i);
                continue;
            }

            float distance = Vector2.Distance(transform.position, minion.transform.position);
            if (distance <= absorbRadius)
            {
                // 恢复 Boss 的生命值
                bossCharacter.currentHealth = Mathf.Min(
                    bossCharacter.currentHealth + healthRestoreAmount, 
                    100f);

                // 销毁小弟
                Destroy(minion);
                minions.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制召唤范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, summonRadius);

        // 绘制吸收范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, absorbRadius);
    }
}
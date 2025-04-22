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
                // 恢复 Boss 的生命值
                bossCharacter.currentHealth += healthRestoreAmount;
                if (bossCharacter.currentHealth > 100f) // 假设最大生命值为 100
                {
                    bossCharacter.currentHealth = 100f;
                }

                // 销毁小弟
                Destroy(minion);
                minions.RemoveAt(i);
            }
        }
    }
}
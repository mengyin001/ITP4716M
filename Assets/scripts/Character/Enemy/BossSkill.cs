using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSkill : MonoBehaviour
{
    [Header("Boss Settings")]
    public BossHealth bossHealth; // 直接引用 BossHealth
    public GameObject minionPrefab; // 小弟的预制体
    public Transform spawnPoint; // 小弟生成的地点
    public float spawnInterval = 5f; // 生成小弟的间隔时间
    public int maxMinions = 5; // 最大小弟数量

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
        // 可以在这里设置小弟的属性，如生命值等
    }

    public void AbsorbMinion(GameObject minion)
    {
        if (minions.Contains(minion))
        {
            // 吸收小弟并回血
            float healAmount = 10f; // 吸收小弟回血的量
            bossHealth.Heal(healAmount);
            Destroy(minion); // 销毁小弟
            minions.Remove(minion);
        }
    }
}
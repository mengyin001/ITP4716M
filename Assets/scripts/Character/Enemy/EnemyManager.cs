using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("敌人刷新点")]
    public Transform[] spawnPoints;

    [Header("敌人巡逻点")]
    public Transform[] patrolPoints;

    [Header("该关卡的敌人")]
    public List<EnemyWave> enemyWaves;

    public int currentWaveIndex = 0; // 当前波数的索引
    public int enemyCount = 0;      // 敌人数量
    private Transform playerTarget; // 玩家目标

    public bool GetLastWave() => currentWaveIndex >= enemyWaves.Count;
    [System.Serializable]
    public class EnemyData
    {
        public GameObject enemyPrefab;  // 敌人预制体
        public float spawnInterval;      // 怪物生成间隔
        public int waveEnemyCount;       // 敌人数量
    }
    [System.Serializable]
    public class EnemyWave
    {
        public List<EnemyData> enemies; // 每波敌人列表
    }

    private void Awake()
    {
        Instance = this;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
        else
        {
            Debug.LogError("找不到带有 'Player' 标签的游戏对象！");
        }
    }

    private void Update()
    {
        if (enemyCount == 0 && !GetLastWave()) // 当前波数敌人全部死亡，开始下一波
        {
            StartCoroutine(StartNextWaveCoroutine());
        }
    }

    IEnumerator StartNextWaveCoroutine()
    {
        if (currentWaveIndex >= enemyWaves.Count)
        {
            Debug.Log("已经没有更多波数，停止刷怪");
            yield break;
        }

        Debug.Log($"开始第 {currentWaveIndex + 1} 波敌人的生成");

        List<EnemyData> enemies = enemyWaves[currentWaveIndex].enemies;
        List<Collider2D> enemyColliders = new List<Collider2D>();

        foreach (EnemyData enemyData in enemies)
        {
            for (int i = 0; i < enemyData.waveEnemyCount; i++)
            {
                if (enemyData.enemyPrefab == null)
                {
                    Debug.LogError("敌人预制体为空，跳过生成");
                    continue;
                }

                GameObject enemy = Instantiate(enemyData.enemyPrefab, GetRandomSpawnPoint(), Quaternion.identity);

                Enemy enemyComponent = enemy.GetComponent<Enemy>();
                LongRangeEnemy longRangeEnemyComponent = enemy.GetComponent<LongRangeEnemy>();

                Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
                if (enemyCollider != null)
                {
                    enemyColliders.Add(enemyCollider);
                }

                if (enemyComponent != null)
                {
                    if (patrolPoints != null)
                    {
                        enemyComponent.patrolPoints = new List<Transform>(patrolPoints);
                    }
                    enemyComponent.player = playerTarget;
                    enemyComponent.OnDie.AddListener(() => EnemyDied());
                }
                else if (longRangeEnemyComponent != null)
                {
                    if (patrolPoints != null)
                    {
                        longRangeEnemyComponent.patrolPoints = new List<Transform>(patrolPoints);
                    }
                    longRangeEnemyComponent.player = playerTarget;
                    longRangeEnemyComponent.OnDie.AddListener(() => EnemyDied());
                }

                enemyCount++;
                Debug.Log($"生成敌人: {enemyData.enemyPrefab.name}，当前敌人数量: {enemyCount}");
                yield return new WaitForSeconds(enemyData.spawnInterval);
            }
        }

        for (int i = 0; i < enemyColliders.Count; i++)
        {
            for (int j = i + 1; j < enemyColliders.Count; j++)
            {
                Physics2D.IgnoreCollision(enemyColliders[i], enemyColliders[j], true);
            }
        }

        currentWaveIndex++;
        Debug.Log($"第 {currentWaveIndex} 波敌人生成完成");
    }

    public void EnemyDied()
    {
        enemyCount--;
        if (enemyCount < 0)
        {
            enemyCount = 0;
        }
        Debug.Log($"敌人死亡，当前数量: {enemyCount}");
    }

    private Vector3 GetRandomSpawnPoint()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomIndex].position;
    }
}
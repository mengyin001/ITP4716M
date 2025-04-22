using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    //单例模式
    public static EnemyManager Instance { get; private set; }

    [Header("敌人刷新点")]
    public Transform[] spawnPoints;

    [Header("敌人巡逻点")]
    public Transform[] patrolPoints;

    [Header("该关卡的敌人")]
    public List<EnemyWave> enemyWaves;

    public int currentWaveIndex = 0; //当前波数的索引
    public int enemyCount = 0;      //敌人数量

    private Transform playerTarget; // 新增：玩家目标

    //判断是否为最后一波
    public bool GetLastWave() => currentWaveIndex == enemyWaves.Count;

    private void Awake()
    {
        Instance = this;
        // 查找玩家对象
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
        if (enemyCount == 0) //当前波数敌人全部死亡，开始下一波
        {
            StartCoroutine(nameof(StartNextWaveCoroutine));
        }
    }

    IEnumerator StartNextWaveCoroutine()
    {
        if (currentWaveIndex >= enemyWaves.Count)
            yield break;    //已经没有更多波数，直接退出协程

        List<EnemyData> enemies = enemyWaves[currentWaveIndex].enemies; //获取当前波数对应的敌人列表

        List<Collider2D> enemyColliders = new List<Collider2D>();

        foreach (EnemyData enemyData in enemies)
        {
            for (int i = 0; i < enemyData.waveEnemyCount; i++)
            {
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
                    // 设置巡逻点
                    if (patrolPoints != null)
                    {
                        enemyComponent.patrolPoints = new List<Transform>(patrolPoints);
                    }
                    // 设置攻击目标
                    enemyComponent.player = playerTarget;

                    // 订阅敌人死亡事件
                    enemyComponent.OnDie.AddListener(() => EnemyDied());
                }
                else if (longRangeEnemyComponent != null)
                {
                    // 设置巡逻点
                    if (patrolPoints != null)
                    {
                        longRangeEnemyComponent.patrolPoints = new List<Transform>(patrolPoints);
                    }
                    // 设置攻击目标
                    longRangeEnemyComponent.player = playerTarget;

                    // 订阅敌人死亡事件
                    longRangeEnemyComponent.OnDie.AddListener(() => EnemyDied());
                }

                enemyCount++;
                yield return new WaitForSeconds(enemyData.spawnInterval);
            }
        }

        // 禁用敌人之间的碰撞检测
        for (int i = 0; i < enemyColliders.Count; i++)
        {
            for (int j = i + 1; j < enemyColliders.Count; j++)
            {
                Physics2D.IgnoreCollision(enemyColliders[i], enemyColliders[j], true);
            }
        }

        currentWaveIndex++;
    }

    // 当敌人死亡时调用此方法，减少敌人数量
    public void EnemyDied()
    {
        enemyCount--;
        if (enemyCount < 0)
        {
            enemyCount = 0;
        }
    }

    //从怪物刷新点的位置列表中随机选择一个刷新点
    private Vector3 GetRandomSpawnPoint()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomIndex].position;
    }
}

//因为没继承MonoBehaviour组件，想要序列化得添加[System.Serializable] 
[System.Serializable]
public class EnemyData
{
    public GameObject enemyPrefab;  //敌人预制体
    public float spawnInterval;     //怪物生成间隔
    public int waveEnemyCount;    //敌人数量，修改为 int 类型
}

[System.Serializable]
public class EnemyWave
{
    public List<EnemyData> enemies; //每波敌人列表
}
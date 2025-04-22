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

    [Header("该关卡的敌人波次")]
    public List<EnemyWave> enemyWaves;

    [Header("调试设置")]
    public bool debugMode = true;
    public Color waveStartColor = Color.green;
    public Color waveEndColor = Color.cyan;

    public int CurrentWaveIndex { get; private set; } = 0;
    public int AliveEnemyCount { get; private set; } = 0;
    public bool IsLastWave => CurrentWaveIndex >= enemyWaves.Count;
    public bool IsWaveInProgress { get; private set; } = false;

    private Transform playerTarget;
    private List<GameObject> activeEnemies = new List<GameObject>();

    [System.Serializable]
    public class EnemyData
    {
        public GameObject enemyPrefab;
        public float spawnInterval = 1f;
        public int waveEnemyCount = 5;
    }

    [System.Serializable]
    public class EnemyWave
    {
        public List<EnemyData> enemies;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Initialize();
    }

    private void Initialize()
    {
        AliveEnemyCount = 0;
        playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (playerTarget == null)
        {
            Debug.LogError("找不到玩家对象！请确保场景中有 'Player' 标签的对象。");
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("未设置敌人刷新点！");
        }
    }

    private void Start()
    {
        StartCoroutine(StartNextWaveCoroutine());
    }

    private void Update()
    {
        if (AliveEnemyCount <= 0 && !IsWaveInProgress && !IsLastWave)
        {
            StartCoroutine(StartNextWaveCoroutine());
        }
    }

    private IEnumerator StartNextWaveCoroutine()
    {
        IsWaveInProgress = true;

        if (IsLastWave)
        {
            LogMessage("所有敌人生成完毕，战斗结束！", waveEndColor);
            IsWaveInProgress = false;
            yield break;
        }

        LogMessage($"开始生成第 {CurrentWaveIndex + 1} 波敌人...", waveStartColor);

        List<EnemyData> currentWaveEnemies = enemyWaves[CurrentWaveIndex].enemies;
        List<Collider2D> spawnedEnemyColliders = new List<Collider2D>();

        foreach (EnemyData enemyData in currentWaveEnemies)
        {
            for (int i = 0; i < enemyData.waveEnemyCount; i++)
            {
                if (spawnPoints == null || spawnPoints.Length == 0)
                {
                    Debug.LogError("无法生成敌人：未设置刷新点");
                    yield break;
                }

                Vector3 spawnPos = GetRandomSpawnPoint();
                GameObject enemy = Instantiate(enemyData.enemyPrefab, spawnPos, Quaternion.identity);
                activeEnemies.Add(enemy);

                if (SetupEnemyBehavior(enemy))
                {
                    if (enemy.TryGetComponent(out Collider2D enemyCollider) && enemyCollider.enabled)
                    {
                        spawnedEnemyColliders.Add(enemyCollider);
                    }
                }

                AliveEnemyCount++;
                LogMessage($"生成敌人: {enemy.name}，位置: {spawnPos}，当前敌人数量: {AliveEnemyCount}");

                yield return new WaitForSeconds(enemyData.spawnInterval);
            }
        }

        // 清理可能为null的碰撞体
        spawnedEnemyColliders.RemoveAll(collider => collider == null);
        IgnoreEnemyCollisions(spawnedEnemyColliders);

        CurrentWaveIndex++;
        IsWaveInProgress = false;
        LogMessage($"第 {CurrentWaveIndex} 波敌人生成完成！", waveEndColor);
    }

    private bool SetupEnemyBehavior(GameObject enemy)
    {
        if (enemy == null) return false;

        Enemy meleeEnemy = enemy.GetComponent<Enemy>();
        LongRangeEnemy rangedEnemy = enemy.GetComponent<LongRangeEnemy>();

        if (meleeEnemy != null)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                meleeEnemy.patrolPoints = new List<Transform>(patrolPoints);
            }
            meleeEnemy.player = playerTarget;
            meleeEnemy.OnDie.AddListener(() => OnEnemyDied(meleeEnemy.gameObject));
            return true;
        }
        else if (rangedEnemy != null)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                rangedEnemy.patrolPoints = new List<Transform>(patrolPoints);
            }
            rangedEnemy.player = playerTarget;
            rangedEnemy.OnDie.AddListener(() => OnEnemyDied(rangedEnemy.gameObject));
            return true;
        }

        return false;
    }

    private void IgnoreEnemyCollisions(List<Collider2D> enemyColliders)
    {
        for (int i = 0; i < enemyColliders.Count; i++)
        {
            if (enemyColliders[i] == null) continue;

            for (int j = i + 1; j < enemyColliders.Count; j++)
            {
                if (enemyColliders[j] == null) continue;

                try
                {
                    Physics2D.IgnoreCollision(enemyColliders[i], enemyColliders[j], true);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"忽略碰撞失败: {e.Message}");
                }
            }
        }
    }

    private void OnEnemyDied(GameObject enemy)
    {
        if (enemy == null) return;

        AliveEnemyCount--;
        if (AliveEnemyCount < 0) AliveEnemyCount = 0;

        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }

        LogMessage($"敌人死亡，剩余敌人: {AliveEnemyCount}");
    }

    private Vector3 GetRandomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }

    private void LogMessage(string message, Color? color = null)
    {
        if (!debugMode) return;

        if (color.HasValue)
        {
            Debug.Log($"<color=#{ColorUtility.ToHtmlStringRGB(color.Value)}>{message}</color>");
        }
        else
        {
            Debug.Log(message);
        }
    }

    public void CleanupAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        activeEnemies.Clear();
        AliveEnemyCount = 0;
    }
}
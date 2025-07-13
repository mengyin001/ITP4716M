using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnlimitedEnemyManager : MonoBehaviour
{
    public static UnlimitedEnemyManager Instance { get; private set; }

    [Header("敌人刷新点")]
    public Transform[] spawnPoints;

    [Header("敌人巡逻点")]
    public Transform[] patrolPoints;

    [Header("该关卡的敌人波次")]
    public List<EnemyWave> enemyWaves;

    public int CurrentWaveIndex { get; private set; } = 0; // 当前波数
    public int MaxWaves => enemyWaves.Count;
    public int AliveEnemyCount { get; private set; } = 0;
    public bool IsLastWave => CurrentWaveIndex >= enemyWaves.Count;
    public bool IsWaveInProgress { get; private set; } = false;
    public bool AllWavesCompleted { get; private set; } = false; // 新增：所有波次是否完成

    private Transform playerTarget;
    private List<GameObject> activeEnemies = new List<GameObject>();
    public GameObject teleportationCirclePrefab; // 传送阵预制件
    public Transform teleportationSpawnPoint; // 传送阵生成点

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

        // 注册场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 当加载的场景是SafeHouse时，重置EnemyManager
        if (scene.name == "SafeHouse")
        {
            ResetEnemyManager();
        }
    }

    private void Initialize()
    {
        AliveEnemyCount = 0;
        playerTarget = GameObject.FindGameObjectWithTag("Player")?.transform;
        CurrentWaveIndex = 0;
        IsWaveInProgress = false;
        AllWavesCompleted = false;
        activeEnemies.Clear();
    }

    public void ResetEnemyManager()
    {
        AliveEnemyCount = 0;  // 明确重置AliveEnemyCount变量
        Initialize();
    }

    private void Start()
    {
        StartCoroutine(StartNextWaveCoroutine());
    }

    private void Update()
    {
        if (AliveEnemyCount <= 0 && !IsWaveInProgress && !IsLastWave && !AllWavesCompleted)
        {
            StartCoroutine(StartNextWaveCoroutine());
        }
    }

    private IEnumerator StartNextWaveCoroutine()
    {
        if (IsLastWave || AllWavesCompleted)
        {
            IsWaveInProgress = false;
            yield break;
        }

        IsWaveInProgress = true;

        List<EnemyData> currentWaveEnemies = enemyWaves[CurrentWaveIndex].enemies;
        List<Collider2D> spawnedEnemyColliders = new List<Collider2D>();
        List<(float time, EnemyData enemyData)> spawnQueue = new List<(float time, EnemyData enemyData)>();

        // 构建生成队列
        float currentTime = 0f;
        foreach (EnemyData enemyData in currentWaveEnemies)
        {
            for (int i = 0; i < enemyData.waveEnemyCount; i++)
            {
                spawnQueue.Add((currentTime, enemyData));
                currentTime += enemyData.spawnInterval;
            }
        }

        // 按时间顺序排序
        spawnQueue.Sort((a, b) => a.time.CompareTo(b.time));

        // 依次生成敌人
        for (int i = 0; i < spawnQueue.Count; i++)
        {
            if (i > 0)
            {
                yield return new WaitForSeconds(spawnQueue[i].time - spawnQueue[i - 1].time);
            }
            else if (i == 0 && spawnQueue[i].time > 0)
            {
                yield return new WaitForSeconds(spawnQueue[i].time);
            }

            EnemyData enemyData = spawnQueue[i].enemyData;
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
        }

        // 清理可能为null的碰撞体
        spawnedEnemyColliders.RemoveAll(collider => collider == null);
        IgnoreEnemyCollisions(spawnedEnemyColliders);

        CurrentWaveIndex++;

        // 检查是否是最后一波
        if (IsLastWave)
        {
            AllWavesCompleted = true;
        }

        IsWaveInProgress = false;
    }

    private bool SetupEnemyBehavior(GameObject enemy)
    {
        if (enemy == null) return false;

        Enemy meleeEnemy = enemy.GetComponent<Enemy>();
        LongRangeEnemy rangedEnemy = enemy.GetComponent<LongRangeEnemy>();

        if (meleeEnemy != null)
        {
            meleeEnemy.forceChaseMode = true;
            meleeEnemy.publicMode = false;
            meleeEnemy.shouldPatrol = false;
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                meleeEnemy.patrolPoints = new List<Transform>(patrolPoints);
            }
            meleeEnemy.SetTarget(playerTarget);
            meleeEnemy.OnDie.AddListener(() => OnEnemyDied(meleeEnemy.gameObject));
            return true;
        }
        else if (rangedEnemy != null)
            rangedEnemy.forceChaseMode = true;
            rangedEnemy.publicMode = false;
            rangedEnemy.shouldPatrol = false;
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                rangedEnemy.patrolPoints = new List<Transform>(patrolPoints);
            }
            rangedEnemy.player = playerTarget;
            rangedEnemy.OnDie.AddListener(() => OnEnemyDied(rangedEnemy.gameObject));
            return true;
        }

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
            Destroy(enemy);
        }

        // 只有当所有波次都完成并且没有存活的敌人时才生成传送门
        if (AllWavesCompleted && AliveEnemyCount <= 0)
        {
            GenerateTeleportationCircle();
        }
    }

    private void GenerateTeleportationCircle()
    {
        if (teleportationCirclePrefab != null && teleportationSpawnPoint != null)
        {
            Instantiate(teleportationCirclePrefab, teleportationSpawnPoint.position, Quaternion.identity);
        }
    }

    private Vector3 GetRandomSpawnPoint()
    {
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
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

    private void OnDestroy()
    {
        // 注销场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
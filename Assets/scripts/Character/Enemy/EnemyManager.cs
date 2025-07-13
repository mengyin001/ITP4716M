using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

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
        // 只有主机启动敌人波次
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartNextWaveCoroutine());
        }
    }

    private void Update()
    {
        // 只有主机更新敌人波次逻辑
        if (PhotonNetwork.IsMasterClient && AliveEnemyCount <= 0 && !IsWaveInProgress && !IsLastWave && !AllWavesCompleted)
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

            // 使用PhotonNetwork.Instantiate生成网络对象
            GameObject enemy = PhotonNetwork.Instantiate(
                enemyData.enemyPrefab.name,
                spawnPos,
                Quaternion.identity
            );

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
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                meleeEnemy.patrolPoints = new List<Transform>(patrolPoints);
            }

            meleeEnemy.OnDie.AddListener(() => OnEnemyDied(meleeEnemy.gameObject));
            return true;
        }
        else if (rangedEnemy != null)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                rangedEnemy.patrolPoints = new List<Transform>(patrolPoints);
            }

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

            // 销毁敌人
            PhotonNetwork.Destroy(enemy);
        }

        // 检查是否生成传送门
        if (AllWavesCompleted && AliveEnemyCount <= 0)
        {
            // 改为返回安全屋
            ReturnToSafeHouse();
        }
    }
    private void ReturnToSafeHouse()
    {
        // 确保只有主机执行场景切换
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("所有波次已完成，准备返回安全屋");

        // 使用UIManager启动传送倒计时
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartTeleportCountdown();
        }
        else
        {
            Debug.LogWarning("UIManager实例未找到，直接传送");
            PhotonNetwork.LoadLevel("SafeHouse");
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
                PhotonNetwork.Destroy(enemy);
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
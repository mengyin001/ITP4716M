using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("����ˢ�µ�")]
    public Transform[] spawnPoints;

    [Header("����Ѳ�ߵ�")]
    public Transform[] patrolPoints;

    [Header("�ùؿ��ĵ��˲���")]
    public List<EnemyWave> enemyWaves;

    [Header("��������")]
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
            Debug.LogError("�Ҳ�����Ҷ�����ȷ���������� 'Player' ��ǩ�Ķ���");
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("δ���õ���ˢ�µ㣡");
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
            LogMessage("���е���������ϣ�ս��������", waveEndColor);
            IsWaveInProgress = false;
            yield break;
        }

        LogMessage($"��ʼ���ɵ� {CurrentWaveIndex + 1} ������...", waveStartColor);

        List<EnemyData> currentWaveEnemies = enemyWaves[CurrentWaveIndex].enemies;
        List<Collider2D> spawnedEnemyColliders = new List<Collider2D>();

        foreach (EnemyData enemyData in currentWaveEnemies)
        {
            for (int i = 0; i < enemyData.waveEnemyCount; i++)
            {
                if (spawnPoints == null || spawnPoints.Length == 0)
                {
                    Debug.LogError("�޷����ɵ��ˣ�δ����ˢ�µ�");
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
                LogMessage($"���ɵ���: {enemy.name}��λ��: {spawnPos}����ǰ��������: {AliveEnemyCount}");

                yield return new WaitForSeconds(enemyData.spawnInterval);
            }
        }

        // �������Ϊnull����ײ��
        spawnedEnemyColliders.RemoveAll(collider => collider == null);
        IgnoreEnemyCollisions(spawnedEnemyColliders);

        CurrentWaveIndex++;
        IsWaveInProgress = false;
        LogMessage($"�� {CurrentWaveIndex} ������������ɣ�", waveEndColor);
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
                    Debug.LogWarning($"������ײʧ��: {e.Message}");
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

        LogMessage($"����������ʣ�����: {AliveEnemyCount}");
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
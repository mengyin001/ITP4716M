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

    [Header("�ùؿ��ĵ���")]
    public List<EnemyWave> enemyWaves;

    public int currentWaveIndex = 0; // ��ǰ����������
    public int enemyCount = 0;      // ��������
    private Transform playerTarget; // ���Ŀ��

    public bool GetLastWave() => currentWaveIndex >= enemyWaves.Count;
    [System.Serializable]
    public class EnemyData
    {
        public GameObject enemyPrefab;  // ����Ԥ����
        public float spawnInterval;      // �������ɼ��
        public int waveEnemyCount;       // ��������
    }
    [System.Serializable]
    public class EnemyWave
    {
        public List<EnemyData> enemies; // ÿ�������б�
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
            Debug.LogError("�Ҳ������� 'Player' ��ǩ����Ϸ����");
        }
    }

    private void Update()
    {
        if (enemyCount == 0 && !GetLastWave()) // ��ǰ��������ȫ����������ʼ��һ��
        {
            StartCoroutine(StartNextWaveCoroutine());
        }
    }

    IEnumerator StartNextWaveCoroutine()
    {
        if (currentWaveIndex >= enemyWaves.Count)
        {
            Debug.Log("�Ѿ�û�и��ನ����ֹͣˢ��");
            yield break;
        }

        Debug.Log($"��ʼ�� {currentWaveIndex + 1} �����˵�����");

        List<EnemyData> enemies = enemyWaves[currentWaveIndex].enemies;
        List<Collider2D> enemyColliders = new List<Collider2D>();

        foreach (EnemyData enemyData in enemies)
        {
            for (int i = 0; i < enemyData.waveEnemyCount; i++)
            {
                if (enemyData.enemyPrefab == null)
                {
                    Debug.LogError("����Ԥ����Ϊ�գ���������");
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
                Debug.Log($"���ɵ���: {enemyData.enemyPrefab.name}����ǰ��������: {enemyCount}");
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
        Debug.Log($"�� {currentWaveIndex} �������������");
    }

    public void EnemyDied()
    {
        enemyCount--;
        if (enemyCount < 0)
        {
            enemyCount = 0;
        }
        Debug.Log($"������������ǰ����: {enemyCount}");
    }

    private Vector3 GetRandomSpawnPoint()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomIndex].position;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    //����ģʽ
    public static EnemyManager Instance { get; private set; }

    [Header("����ˢ�µ�")]
    public Transform[] spawnPoints;

    [Header("����Ѳ�ߵ�")]
    public Transform[] patrolPoints;

    [Header("�ùؿ��ĵ���")]
    public List<EnemyWave> enemyWaves;

    public int currentWaveIndex = 0; //��ǰ����������
    public int enemyCount = 0;      //��������

    private Transform playerTarget; // ���������Ŀ��

    //�ж��Ƿ�Ϊ���һ��
    public bool GetLastWave() => currentWaveIndex == enemyWaves.Count;

    private void Awake()
    {
        Instance = this;
        // ������Ҷ���
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
        if (enemyCount == 0) //��ǰ��������ȫ����������ʼ��һ��
        {
            StartCoroutine(nameof(StartNextWaveCoroutine));
        }
    }

    IEnumerator StartNextWaveCoroutine()
    {
        if (currentWaveIndex >= enemyWaves.Count)
            yield break;    //�Ѿ�û�и��ನ����ֱ���˳�Э��

        List<EnemyData> enemies = enemyWaves[currentWaveIndex].enemies; //��ȡ��ǰ������Ӧ�ĵ����б�

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
                    // ����Ѳ�ߵ�
                    if (patrolPoints != null)
                    {
                        enemyComponent.patrolPoints = new List<Transform>(patrolPoints);
                    }
                    // ���ù���Ŀ��
                    enemyComponent.player = playerTarget;

                    // ���ĵ��������¼�
                    enemyComponent.OnDie.AddListener(() => EnemyDied());
                }
                else if (longRangeEnemyComponent != null)
                {
                    // ����Ѳ�ߵ�
                    if (patrolPoints != null)
                    {
                        longRangeEnemyComponent.patrolPoints = new List<Transform>(patrolPoints);
                    }
                    // ���ù���Ŀ��
                    longRangeEnemyComponent.player = playerTarget;

                    // ���ĵ��������¼�
                    longRangeEnemyComponent.OnDie.AddListener(() => EnemyDied());
                }

                enemyCount++;
                yield return new WaitForSeconds(enemyData.spawnInterval);
            }
        }

        // ���õ���֮�����ײ���
        for (int i = 0; i < enemyColliders.Count; i++)
        {
            for (int j = i + 1; j < enemyColliders.Count; j++)
            {
                Physics2D.IgnoreCollision(enemyColliders[i], enemyColliders[j], true);
            }
        }

        currentWaveIndex++;
    }

    // ����������ʱ���ô˷��������ٵ�������
    public void EnemyDied()
    {
        enemyCount--;
        if (enemyCount < 0)
        {
            enemyCount = 0;
        }
    }

    //�ӹ���ˢ�µ��λ���б������ѡ��һ��ˢ�µ�
    private Vector3 GetRandomSpawnPoint()
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        return spawnPoints[randomIndex].position;
    }
}

//��Ϊû�̳�MonoBehaviour�������Ҫ���л������[System.Serializable] 
[System.Serializable]
public class EnemyData
{
    public GameObject enemyPrefab;  //����Ԥ����
    public float spawnInterval;     //�������ɼ��
    public int waveEnemyCount;    //�����������޸�Ϊ int ����
}

[System.Serializable]
public class EnemyWave
{
    public List<EnemyData> enemies; //ÿ�������б�
}
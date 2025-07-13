using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using Photon.Pun;
using Photon.Realtime;

public class SummonMinions : MonoBehaviourPunCallbacks
{
    [Header("Minion Settings")]
    public GameObject minionPrefab; // 直接引用小怪预制体
    public int minionsPerSummon = 3;
    public float summonRadius = 3f;

    [Header("Summon Interval")]
    public float summonInterval = 10f;
    public float summonTimer;

    [Header("Absorb Settings")]
    public float absorbRadius = 1f;
    public float healthRestoreAmount = 10f;

    [Header("Boundary Settings")]
    public LayerMask walkableLayer;
    public float maxSpawnAttempts = 10;

    private Character bossCharacter;
    private List<GameObject> minions = new List<GameObject>();
    private List<int> minionViewIDs = new List<int>();

    private void Awake()
    {
        bossCharacter = GetComponent<Character>();
        summonTimer = summonInterval;
    }

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            summonTimer = summonInterval;
        }

        // 检查预制体是否已赋值
        if (minionPrefab == null)
        {
            Debug.LogError("请在Inspector中为SummonMinions组件赋值minionPrefab！");
        }
        else
        {
            // 检查预制体是否包含必要组件
            if (minionPrefab.GetComponent<PhotonView>() == null)
            {
                Debug.LogWarning("小怪预制体缺少PhotonView组件，网络同步可能异常！");
            }
            if (minionPrefab.GetComponent<Enemy>() == null)
            {
                Debug.LogWarning("小怪预制体缺少Enemy组件，可能无法正常攻击！");
            }
        }
    }

    private void Update()
    {
        if (bossCharacter == null)
        {
            Debug.LogError("Boss Character script not found!");
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            summonTimer -= Time.deltaTime;
            if (summonTimer <= 0f)
            {
                SummonMinionsBatch();
                summonTimer = summonInterval;
            }
        }

        AbsorbMinions();
    }

    // 公共属性：供UI获取冷却进度
    public float SummonTimerRemaining
    {
        get { return summonTimer; }
    }

    public float SummonTimerProgress
    {
        get { return 1 - (summonTimer / summonInterval); }
    }

    [PunRPC]
    private void SummonMinionsBatch()
    {
        // 预制体未赋值时不执行召唤
        if (minionPrefab == null) return;

        for (int i = 0; i < minionsPerSummon; i++)
        {
            TrySpawnMinion();
        }
    }

    private void TrySpawnMinion()
    {
        // 预制体未赋值时直接返回
        if (minionPrefab == null) return;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * summonRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            if (IsPositionWalkable(spawnPosition))
            {
                // 使用Photon网络实例化预制体
                GameObject minion = PhotonNetwork.Instantiate(
                    minionPrefab.name,  // 使用预制体名称作为标识
                    spawnPosition,
                    Quaternion.identity
                );

                if (minion != null)
                {
                    minions.Add(minion);
                    minionViewIDs.Add(minion.GetComponent<PhotonView>().ViewID);

                    // 设置小怪目标
                    Enemy enemyScript = minion.GetComponent<Enemy>();
                    if (enemyScript != null)
                    {
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null)
                        {
                            enemyScript.SetTarget(player.transform);
                        }
                    }
                }

                return;
            }
        }

        Debug.LogWarning("尝试" + maxSpawnAttempts + "次后仍未找到合适的生成位置");
    }

    private bool IsPositionWalkable(Vector3 position)
    {
        if (AstarPath.active != null)
        {
            var node = AstarPath.active.GetNearest(position).node;
            if (node != null && node.Walkable)
                return true;
        }

        Collider2D hit = Physics2D.OverlapCircle(position, 0.5f, walkableLayer);
        return hit != null;
    }

    private void AbsorbMinions()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        for (int i = minions.Count - 1; i >= 0; i--)
        {
            GameObject minion = minions[i];
            if (minion == null)
            {
                minions.RemoveAt(i);
                if (i < minionViewIDs.Count)
                    minionViewIDs.RemoveAt(i);
                continue;
            }

            float distance = Vector2.Distance(transform.position, minion.transform.position);
            if (distance <= absorbRadius)
            {
                bossCharacter.currentHealth = Mathf.Min(
                    bossCharacter.currentHealth + healthRestoreAmount,
                    bossCharacter.MaxHealth);

                PhotonView minionView = minion.GetComponent<PhotonView>();
                if (minionView != null)
                {
                    PhotonNetwork.Destroy(minionView);
                }
                else
                {
                    Destroy(minion);
                }

                minions.RemoveAt(i);
                if (i < minionViewIDs.Count)
                    minionViewIDs.RemoveAt(i);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(minionViewIDs.Count);
            foreach (int viewID in minionViewIDs)
            {
                stream.SendNext(viewID);
            }
        }
        else
        {
            minionViewIDs.Clear();
            int count = (int)stream.ReceiveNext();
            for (int i = 0; i < count; i++)
            {
                int viewID = (int)stream.ReceiveNext();
                minionViewIDs.Add(viewID);

                PhotonView view = PhotonView.Find(viewID);
                if (view != null && !minions.Contains(view.gameObject))
                {
                    minions.Add(view.gameObject);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, summonRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, absorbRadius);
    }
}
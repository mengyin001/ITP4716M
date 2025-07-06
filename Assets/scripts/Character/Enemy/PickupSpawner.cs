using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PickupSpawner : MonoBehaviourPun
{
    public PropPrefab[] propPrefabs;
    [Header("Drop Settings")]
    public float dropRadius = 1.5f; // 掉落范围半径
    public AnimationCurve dropCurve; // 掉落动画曲线
    public float spawnAnimationDuration = 0.5f; // 生成动画持续时间

    public void DropItems()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var propPrefab in propPrefabs)
        {
            if (Random.Range(0f, 100f) <= propPrefab.dropPercentage)
            {
                // 计算随机偏移位置
                Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
                Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

                // 使用PUN实例化道具
                GameObject pickup = PhotonNetwork.Instantiate(
                    propPrefab.prefab.name,
                    spawnPosition,
                    Quaternion.identity
                );

                // 启动掉落动画协程
                photonView.RPC("PlayDropAnimation", RpcTarget.All, pickup.GetComponent<PhotonView>().ViewID);
            }
        }
    }

    [PunRPC]
    private void PlayDropAnimation(int viewId)
    {
        PhotonView pv = PhotonView.Find(viewId);
        if (pv != null && pv.IsMine)
        {
            StartCoroutine(DropAnimationCoroutine(pv.gameObject));
        }
    }

    private IEnumerator DropAnimationCoroutine(GameObject pickup)
    {
        // 初始设置
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = pickup.transform.localScale;
        float timer = 0f;

        // 如果有刚体组件，暂时禁用物理模拟
        Rigidbody2D rb = pickup.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
        }

        // 缩放动画
        while (timer < spawnAnimationDuration)
        {
            timer += Time.deltaTime;
            float curveValue = dropCurve.Evaluate(timer / spawnAnimationDuration);
            pickup.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }

        // 确保最终缩放比例正确
        pickup.transform.localScale = targetScale;

        // 如果有刚体组件，重新启用物理模拟
        if (rb != null)
        {
            rb.simulated = true;
        }
    }
}

[System.Serializable]
public class PropPrefab
{
    public GameObject prefab;
    [Range(0f, 100f)] public float dropPercentage;
}
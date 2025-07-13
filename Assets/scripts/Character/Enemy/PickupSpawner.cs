using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PickupSpawner : MonoBehaviourPun
{
    public PropPrefab[] propPrefabs;
    [Header("Drop Settings")]
    public float dropRadius = 1.5f;
    public AnimationCurve dropCurve;
    public float spawnAnimationDuration = 0.5f;

    public void DropItems()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var propPrefab in propPrefabs)
        {
            if (Random.Range(0f, 100f) <= propPrefab.dropPercentage)
            {
                Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
                Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

                GameObject pickup = PhotonNetwork.Instantiate(
                    propPrefab.prefab.name,
                    spawnPosition,
                    Quaternion.identity
                );

                // 关键修复：立即重置缩放比例
                photonView.RPC("ResetPickupScale", RpcTarget.AllBuffered,
                    pickup.GetComponent<PhotonView>().ViewID);

                photonView.RPC("PlayDropAnimation", RpcTarget.All,
                    pickup.GetComponent<PhotonView>().ViewID);
            }
        }
    }

    [PunRPC]
    private void ResetPickupScale(int viewId)
    {
        PhotonView pv = PhotonView.Find(viewId);
        if (pv != null)
        {
            // 强制重置为正常比例（1,1,1）
            pv.transform.localScale = Vector3.one;
        }
    }

    [PunRPC]
    private void PlayDropAnimation(int viewId)
    {
        PhotonView pv = PhotonView.Find(viewId);
        if (pv != null)
        {
            StartCoroutine(DropAnimationCoroutine(pv.gameObject));
        }
    }

    private IEnumerator DropAnimationCoroutine(GameObject pickup)
    {
        // 保存重置后的缩放值
        Vector3 targetScale = pickup.transform.localScale;

        // 初始设置
        Vector3 startScale = Vector3.zero;
        float timer = 0f;

        Rigidbody2D rb = pickup.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        // 从零开始缩放
        pickup.transform.localScale = startScale;

        while (timer < spawnAnimationDuration)
        {
            timer += Time.deltaTime;
            float curveValue = dropCurve.Evaluate(timer / spawnAnimationDuration);
            pickup.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }

        pickup.transform.localScale = targetScale;

        if (rb != null) rb.simulated = true;
    }
}

[System.Serializable]
public class PropPrefab
{
    public GameObject prefab;
    [Range(0f, 100f)] public float dropPercentage;
}
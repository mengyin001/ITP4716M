using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PickupSpawner : MonoBehaviourPun
{
    public PropPrefab[] propPrefabs;
    [Header("Drop Settings")]
    public float dropRadius = 1.5f; // ���䷶Χ�뾶
    public AnimationCurve dropCurve; // ���䶯������
    public float spawnAnimationDuration = 0.5f; // ���ɶ�������ʱ��

    public void DropItems()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var propPrefab in propPrefabs)
        {
            if (Random.Range(0f, 100f) <= propPrefab.dropPercentage)
            {
                // �������ƫ��λ��
                Vector2 randomOffset = Random.insideUnitCircle * dropRadius;
                Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

                // ʹ��PUNʵ��������
                GameObject pickup = PhotonNetwork.Instantiate(
                    propPrefab.prefab.name,
                    spawnPosition,
                    Quaternion.identity
                );

                // �������䶯��Э��
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
        // ��ʼ����
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = pickup.transform.localScale;
        float timer = 0f;

        // ����и����������ʱ��������ģ��
        Rigidbody2D rb = pickup.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
        }

        // ���Ŷ���
        while (timer < spawnAnimationDuration)
        {
            timer += Time.deltaTime;
            float curveValue = dropCurve.Evaluate(timer / spawnAnimationDuration);
            pickup.transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }

        // ȷ���������ű�����ȷ
        pickup.transform.localScale = targetScale;

        // ����и��������������������ģ��
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
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class WordItem : MonoBehaviourPun, IPunObservable
{
    [Header("物品配置")]
    public string itemID;
    public int quantity = 1;
    public float rotationSpeed = 30f;
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 1f;

    [Header("特效Prefab")]
    public GameObject pickupParticlePrefab;

    private Vector3 startPos;
    private bool isPickedUp = false;
    private float pickupTimer = 0f;
    private const float PICKUP_DURATION = 0.5f;
    private Collider2D itemCollider;
    private bool hasPlayedParticle = false;

    private void Start()
    {
        startPos = transform.position;
        itemCollider = GetComponent<Collider2D>();

        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogError($"物品 {name} 没有设置itemID，无法被拾取！");
        }
    }

    private void Update()
    {
        if (isPickedUp)
        {
            HandlePickupAnimation();
        }
        else
        {
            HandleFloatAnimation();
        }
    }

    private void HandleFloatAnimation()
    {
        float yOffset = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = startPos + new Vector3(0, yOffset, 0);
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void HandlePickupAnimation()
    {
        pickupTimer += Time.deltaTime;
        float progress = pickupTimer / PICKUP_DURATION;
        transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, progress);

        // 已移除粒子播放代码
        if (pickupTimer >= PICKUP_DURATION && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }


    private void PlayParticleEffect()
    {
        if (pickupParticlePrefab == null) return;

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_PlayParticleEffect", RpcTarget.All);
        }
        else
        {
            Instantiate(pickupParticlePrefab, transform.position, Quaternion.identity);
        }
    }

    [PunRPC]
    private void RPC_PlayParticleEffect()
    {
        if (pickupParticlePrefab != null)
        {
            GameObject particle = Instantiate(pickupParticlePrefab, transform.position, Quaternion.identity);
            Destroy(particle, 2f); // 简单粒子销毁
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp) return;

        if (other.CompareTag("Player"))
        {
            PhotonView playerView = other.GetComponentInParent<PhotonView>();
            if (playerView != null && playerView.IsMine)
            {
                photonView.RPC("RPC_PickupItem", RpcTarget.All, playerView.ViewID);
            }
        }
    }

    [PunRPC]
    private void RPC_PickupItem(int playerPhotonViewID)
    {
        // 所有客户端都更新物品状态
        isPickedUp = true;
        if (itemCollider != null) itemCollider.enabled = false;

        // 所有客户端播放粒子
        if (!hasPlayedParticle)
        {
            PlayParticleEffect();
            hasPlayedParticle = true;
        }

        // 仅触发拾取的玩家添加物品
        PhotonView targetPlayerView = PhotonView.Find(playerPhotonViewID);
        if (targetPlayerView != null && targetPlayerView.IsMine)
        {
            NetworkInventory playerInventory = targetPlayerView.GetComponent<NetworkInventory>();
            if (playerInventory != null)
            {
                playerInventory.AddItem(itemID, quantity);
            }
        }
    }

    private void PickupItem(Player player)
    {
        photonView.RPC("RPC_PickupItem", RpcTarget.All, player);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isPickedUp);
            stream.SendNext(transform.position);
            stream.SendNext(itemID);
            stream.SendNext(hasPlayedParticle);
        }
        else
        {
            isPickedUp = (bool)stream.ReceiveNext();
            startPos = (Vector3)stream.ReceiveNext();
            itemID = (string)stream.ReceiveNext();
            hasPlayedParticle = (bool)stream.ReceiveNext();
        }
    }
}
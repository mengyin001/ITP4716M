using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class WordItem : MonoBehaviourPun, IPunObservable
{
    [Header("��Ʒ����")]
    public string itemID;
    public int quantity = 1;
    public float rotationSpeed = 30f;
    public float floatAmplitude = 0.2f;
    public float floatFrequency = 1f;

    [Header("��ЧPrefab")]
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
            Debug.LogError($"��Ʒ {name} û������itemID���޷���ʰȡ��");
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

        if (!hasPlayedParticle && pickupParticlePrefab != null)
        {
            PlayParticleEffect();
            hasPlayedParticle = true;
        }

        if (pickupTimer >= PICKUP_DURATION && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void PlayParticleEffect()
    {
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
            Destroy(particle, 2f); // ����������
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
                // ��ȡ������ҵ�PhotonView ID
                int playerPhotonViewID = playerView.ViewID;

                // ����RPC�������PhotonView ID
                photonView.RPC("RPC_PickupItem", RpcTarget.All, playerPhotonViewID);
            }
        }
    }

    [PunRPC]
    private void RPC_PickupItem(int playerPhotonViewID)
    {
        PhotonView targetPlayerView = PhotonView.Find(playerPhotonViewID);
        if (targetPlayerView == null || !targetPlayerView.IsMine) return;

        isPickedUp = true;
        if (itemCollider != null) itemCollider.enabled = false;

        // ȷ����ȷ�����Ʒ
        NetworkInventory playerInventory = targetPlayerView.GetComponent<NetworkInventory>();
        if (playerInventory != null)
        {
            // ��ӵ�����Ϣ
            Debug.Log($"Adding item to inventory: {itemID} x{quantity}");
            playerInventory.AddItem(itemID, quantity);
        }
        else
        {
            Debug.LogError("Player inventory not found!");
        }

        PlayParticleEffect();
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
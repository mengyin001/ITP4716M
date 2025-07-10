using UnityEngine;
using TMPro;
using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class NPCDialogueTrigger : MonoBehaviourPun
{
    [Header("2D距离设置")]
    [Tooltip("触发对话的平面距离")]
    public float triggerDistance = 2f;
    public KeyCode interactKey = KeyCode.E;
    public TextAsset dialogueFile;

    [Header("范围中心偏移")]
    [Tooltip("调整触发范围的视觉中心点")]
    public Vector2 rangeCenterOffset = Vector2.zero;

    [Header("界面提示")]
    public TextMeshProUGUI interactPrompt;

    [Header("商店设置")]
    public bool openShopAfterDialogue = false;
    public ShopData shopToOpen;

    [Header("PUN2设置")]
    public bool masterOnlyInteraction = false;

    private Transform player;
    private bool isInRange;
    public string npcID;

    private bool isDialogueActive = false;
    private bool isProcessingDialogue = false;

    // 添加玩家移动控制
    private PlayerMovement playerMovement;

    void Start()
    {
        FindLocalPlayer();

        if (interactPrompt)
        {
            interactPrompt.gameObject.SetActive(false);

            RectTransform rt = interactPrompt.GetComponent<RectTransform>();
        }

        // 查找玩家移动组件
        FindPlayerMovement();
    }

    private void FindPlayerMovement()
    {
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        if (player == null)
        {
            FindLocalPlayer();
            return;
        }

        if (isDialogueActive || isProcessingDialogue)
        {
            HidePrompt();
            return;
        }

        Vector2 npcPos = (Vector2)transform.position + rangeCenterOffset;
        Vector2 playerPos = player.position;
        float distance = Vector2.Distance(npcPos, playerPos);

        bool inRange = distance <= triggerDistance;
        bool canInteract = inRange && !IsAnyMenuOpen();

        if (canInteract)
        {
            if (!isInRange) ShowPrompt();

            if (Input.GetKeyDown(interactKey))
            {
                if (CanInteract())
                {
                    StartDialogue();
                }
            }
        }
        else if (isInRange)
        {
            HidePrompt();
        }
    }

    private void FindLocalPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObj in players)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                player = playerObj.transform;
                return;
            }
        }

        StartCoroutine(DelayedFindPlayer());
    }

    private IEnumerator DelayedFindPlayer()
    {
        yield return new WaitForSeconds(1f);
        FindLocalPlayer();
    }

    private bool IsAnyMenuOpen()
    {
        bool shopOpen = ShopManager.Instance != null && ShopManager.Instance.IsOpen;
        bool dialogueOpen = DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive;
        return shopOpen || dialogueOpen;
    }

    private bool CanInteract()
    {
        if (masterOnlyInteraction && !PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Only the master client can interact with this NPC.");
            return false;
        }
        return true;
    }

    void ShowPrompt()
    {
        isInRange = true;
        if (interactPrompt)
        {
            interactPrompt.gameObject.SetActive(true);
            interactPrompt.text = $"Press [{interactKey}] to talk";
        }
    }

    public void HidePrompt()
    {
        isInRange = false;
        if (interactPrompt) interactPrompt.gameObject.SetActive(false);
    }

    void StartDialogue()
    {
        if (dialogueFile == null)
        {
            Debug.LogError("Dialogue file not assigned", this);
            return;
        }

        StartCoroutine(TriggerDialogue());
    }

    private IEnumerator TriggerDialogue()
    {
        isProcessingDialogue = true;

        if (DialogueSystem.Instance == null)
        {
            Debug.LogWarning("DialogueSystem instance not found. Waiting...");
            yield return new WaitUntil(() => DialogueSystem.Instance != null);
        }

        isDialogueActive = true;

        DialogueSystem.Instance.LoadNewDialogue(dialogueFile, npcID);
        DialogueSystem.Instance.StartDialogue();

        HidePrompt();

        // 临时禁用玩家移动（对话系统会处理完整控制）
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        DialogueSystem.Instance.OnDialogueEnd += HandleDialogueEnd;

        isProcessingDialogue = false;
    }

    private void HandleDialogueEnd(string completedNPCID)
    {
        if (completedNPCID != npcID) return;

        DialogueSystem.Instance.OnDialogueEnd -= HandleDialogueEnd;

        isDialogueActive = false;

        // 重新启用玩家移动
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (openShopAfterDialogue)
        {
            OpenShopAfterDialogue();
        }
    }

    private void OpenShopAfterDialogue()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.SetCurrentShop(shopToOpen);
            ShopManager.Instance.OpenShop();
        }
        else
        {
            Debug.LogError("ShopManager instance not found!");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Vector3 drawPosition = transform.position + (Vector3)rangeCenterOffset;
        Gizmos.DrawWireSphere(drawPosition, triggerDistance);
    }
}
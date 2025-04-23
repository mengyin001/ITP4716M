using UnityEngine;
using TMPro;
using System.Collections;

public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("2D距离设置")]
    [Tooltip("触发对话的平面距离")]
    public float triggerDistance = 2f;
    public KeyCode interactKey = KeyCode.E;
    public TextAsset dialogueFile;

    [Header("界面提示")]
    public TextMeshProUGUI interactPrompt;

    [Header("商店设置")]
    public bool openShopAfterDialogue = false;
    public ShopData shopToOpen;

    private Transform player;
    public bool isInRange;
    public string npcID;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (interactPrompt) interactPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;
        if (ShopManager.Instance != null && ShopManager.Instance.isOPen)
        {
            HidePrompt();
            return;
        }

        // 完全忽略Z轴的2D位置计算
        Vector2 npcPos = transform.position;
        Vector2 playerPos = player.position;
        float distance = Vector2.Distance(npcPos, playerPos);

        bool inRange = distance <= triggerDistance;
        bool canInteract = inRange && !DialogueSystem.Instance.isDialogueActive;

        if (canInteract)
        {
            if (!isInRange) ShowPrompt();

            if (Input.GetKeyDown(interactKey))
            {
                StartDialogue();
            }
        }
        else if (isInRange)
        {
            HidePrompt();
        }
    }

    void ShowPrompt()
    {
        isInRange = true;
        if (interactPrompt)
        {
            interactPrompt.gameObject.SetActive(true);
            interactPrompt.text = $"Click [{interactKey}] to talk";
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
            Debug.LogError("对话文件未分配", this);
            return;
        }
        DialogueSystem.Instance.LoadNewDialogue(dialogueFile);
        DialogueSystem.Instance.LoadNewDialogue(dialogueFile,npcID);
        DialogueSystem.Instance.StartDialogue();
        HidePrompt();

        if (openShopAfterDialogue)
        {
            StartCoroutine(WaitForDialogueEnd());
        }
    }

    private IEnumerator WaitForDialogueEnd()
    {
        // 等待直到对话结束
        yield return new WaitWhile(() => DialogueSystem.Instance.isDialogueActive);

        // 打开商店界面
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.SetCurrentShop(shopToOpen);
            ShopManager.Instance.OpenShop();
        }
        else
        {
            Debug.LogError("找不到商店管理器实例!");
        }
    }

    // 纯2D范围可视化
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}
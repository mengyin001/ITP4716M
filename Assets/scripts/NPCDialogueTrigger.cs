using UnityEngine;
using TMPro;

public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("2D距离设置")]
    [Tooltip("触发对话的平面距离")]
    public float triggerDistance = 2f;
    public KeyCode interactKey = KeyCode.E;
    public TextAsset dialogueFile;

    [Header("界面提示")]
    public TextMeshProUGUI interactPrompt;

    private Transform player;
    public bool isInRange;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (interactPrompt) interactPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

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

    void HidePrompt()
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
        DialogueSystem.Instance.StartDialogue();
        HidePrompt();
    }

    // 纯2D范围可视化
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}
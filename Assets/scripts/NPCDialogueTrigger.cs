using UnityEngine;
using TMPro;

public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("对话配置")]
    public TextAsset dialogueFile; // 需要加载的新对话文件
    public float triggerDistance = 2f; // 触发距离
    public KeyCode interactKey = KeyCode.E; // 互动按键

    [Header("提示UI")]
    public TextMeshProUGUI interactPrompt; // 显示"按E对话"的文本

    private GameObject player;
    private bool isInRange;
    private bool hasInteracted;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        interactPrompt.gameObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        // 计算距离
        float distance = Vector3.Distance(transform.position, player.transform.position);

        // 距离检测
        if (distance <= triggerDistance && !DialogueSystem.Instance.isDialogueActive)
        {
            if (!isInRange)
            {
                OnEnterRange();
            }

            // 输入检测
            if (Input.GetKeyDown(interactKey))
            {
                TriggerDialogue();
            }
        }
        else if (isInRange)
        {
            OnExitRange();
        }
    }

    void OnEnterRange()
    {
        isInRange = true;
        interactPrompt.gameObject.SetActive(true);
        interactPrompt.text = $"Click {interactKey} to talk";
    }

    void OnExitRange()
    {
        isInRange = false;
        interactPrompt.gameObject.SetActive(false);
    }

    void TriggerDialogue()
    {
        if (dialogueFile == null)
        {
            Debug.LogError("未分配对话文件！");
            return;
        }

        // 加载新对话
        DialogueSystem.Instance.LoadNewDialogue(dialogueFile);
        DialogueSystem.Instance.StartDialogue();

        // 隐藏提示
        interactPrompt.gameObject.SetActive(false);

        // 防止重复触发
        hasInteracted = true;
    }

    // 可视化触发范围（仅在编辑器显示）
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}
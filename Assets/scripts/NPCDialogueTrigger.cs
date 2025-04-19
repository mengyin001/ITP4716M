using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("交互提示")]
    [SerializeField] private GameObject interactPrompt; // 按E交互提示图标

    [Header("对话配置")]
    [SerializeField] private bool oneTimeDialogue = true; // 是否只触发一次对话

    private bool playerInRange = false;
    private bool hasTriggered = false;

    void Update()
    {
        // 显示/隐藏交互提示
        if (interactPrompt != null)
            interactPrompt.SetActive(playerInRange && !DialogueSystem.Instance.isDialogueActive);

        // 检测交互输入
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (!DialogueSystem.Instance.isDialogueActive && (!oneTimeDialogue || !hasTriggered))
            {
                DialogueSystem.Instance.StartDialogue();
                hasTriggered = true;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
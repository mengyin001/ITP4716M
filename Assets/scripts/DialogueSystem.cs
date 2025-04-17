using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueSystem : MonoBehaviour
{
    [System.Serializable]
    public class DialogueEntry
    {
        public string characterName;     // 角色名字
        [TextArea(3, 10)]
        public string dialogueContent;   // 对话内容
        public float textSpeed = 0.05f;  // 每个字符显示间隔时间（秒）
    }

    [Header("UI组件")]
    [SerializeField] private TextMeshProUGUI nameText;    // 角色名字文本
    [SerializeField] private TextMeshProUGUI contentText;  // 对话内容文本
    [SerializeField] private GameObject dialoguePanel;     // 对话框面板

    [Header("对话配置")]
    [SerializeField] private List<DialogueEntry> dialogues = new List<DialogueEntry>();

    [Header("初始设置")]
    [SerializeField] private bool startOnAwake = true;     // 游戏开始时自动播放
    [SerializeField] private float startDelay = 1f;        // 初始对话延迟时间

    private bool isDialogueActive = false;
    private int currentDialogueIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    IEnumerator Start()
    {
        if (startOnAwake)
        {
            yield return new WaitForSeconds(startDelay); // 延迟启动
            StartDialogue();
        }
    }

    void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.Space))
        {
            HandleDialogueInput();
        }
    }

    void HandleDialogueInput()
    {
        if (isTyping)
        {
            CompleteCurrentSentence();
        }
        else
        {
            ShowNextSentence();
        }
    }

    // ========== 对话控制方法 ==========
    public void StartDialogue()
    {
        dialoguePanel.SetActive(true);
        currentDialogueIndex = 0;
        isDialogueActive = true;
        ShowSentence(currentDialogueIndex);
    }

    private void ShowSentence(int index)
    {
        if (index >= dialogues.Count) return;

        DialogueEntry entry = dialogues[index];
        nameText.text = entry.characterName;
        contentText.text = "";

        typingCoroutine = StartCoroutine(TypeSentence(entry));
    }

    IEnumerator TypeSentence(DialogueEntry entry)
    {
        isTyping = true;
        foreach (char letter in entry.dialogueContent.ToCharArray())
        {
            contentText.text += letter;
            yield return new WaitForSeconds(entry.textSpeed);
        }
        isTyping = false;
    }

    private void CompleteCurrentSentence()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        contentText.text = dialogues[currentDialogueIndex].dialogueContent;
        isTyping = false;
    }

    private void ShowNextSentence()
    {
        currentDialogueIndex++;
        if (currentDialogueIndex < dialogues.Count)
        {
            ShowSentence(currentDialogueIndex);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        Debug.Log("对话流程结束");
    }

    // ========== 示例数据 ==========
    private void Reset()
    {
        dialogues = new List<DialogueEntry>{
            new DialogueEntry{
                characterName = "系统",
                dialogueContent = "欢迎来到冒险世界！",
                textSpeed = 0.05f
            },
            new DialogueEntry{
                characterName = "向导",
                dialogueContent = "按空格键继续对话...",
                textSpeed = 0.03f
            },
            new DialogueEntry{
                characterName = "NPC",
                dialogueContent = "前方城堡有重要线索，小心行动！",
                textSpeed = 0.04f
            }
        };
    }
}
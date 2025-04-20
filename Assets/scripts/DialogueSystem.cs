using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance;

    [System.Serializable]
    public class DialogueEntry
    {
        public string characterID;
        public string characterName;
        [TextArea(3, 10)]
        public string dialogueContent;
        public float textSpeed = 0.05f;
        public Sprite characterIcon;
    }

    [System.Serializable]
    public class CharacterIconConfig
    {
        public string characterID;
        public Sprite icon;
    }

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image characterImage;
    [SerializeField] private Animator iconAnimator;

    [Header("Text Configuration")]
    [SerializeField] private TextAsset dialogueFile;
    [SerializeField] private string entrySeparator = "\\n";
    [SerializeField] private char parameterSeparator = '@';

    [Header("Character Icons")]
    [SerializeField] private List<CharacterIconConfig> iconConfigs = new List<CharacterIconConfig>();

    [Header("Settings")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private bool freezeTimeDuringDialogue = true;

    private List<DialogueEntry> dialogues = new List<DialogueEntry>();
    private int currentDialogueIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private float originalTimeScale;
    public bool isDialogueActive { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            ParseDialogueFile();
            originalTimeScale = Time.timeScale;
            if (iconAnimator != null)
            {
                iconAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator Start()
    {
        if (startOnAwake)
        {
            yield return new WaitForSeconds(startDelay);
            StartDialogue();
        }
    }

    void Update()
    {
        if (isDialogueActive && (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.E)))
        {
            HandleDialogueInput();
        }
    }

    void ParseDialogueFile()
    {
        dialogues.Clear();

        if (dialogueFile == null)
        {
            Debug.LogWarning("Dialogue file not assigned!");
            return;
        }

        string[] entries = Regex.Split(dialogueFile.text, entrySeparator + "(?m)");

        foreach (string entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry)) continue;

            string processedEntry = entry.Trim();
            string[] splitParams = Regex.Split(processedEntry, @"\s*" + parameterSeparator + @"\s*");

            // Parse character ID and name
            string[] roleData = splitParams[0].Split(new[] { '|' }, 2);
            if (roleData.Length < 2)
            {
                Debug.LogWarning($"Invalid role format: {splitParams[0]}");
                continue;
            }

            string characterID = roleData[0].Trim();
            string[] nameContent = roleData[1].Split(new[] { ':' }, 2);

            if (nameContent.Length < 2)
            {
                Debug.LogWarning($"Invalid dialogue format: {roleData[1]}");
                continue;
            }

            DialogueEntry newEntry = new DialogueEntry
            {
                characterID = characterID,
                characterName = nameContent[0].Trim(),
                dialogueContent = nameContent[1].Trim().Replace("\\n", "\n")
            };

            // Find icon configuration
            CharacterIconConfig config = iconConfigs.Find(c => c.characterID == characterID);
            if (config != null)
            {
                newEntry.characterIcon = config.icon;
            }

            // Parse text speed
            if (splitParams.Length > 1)
            {
                if (float.TryParse(splitParams[1], out float speed))
                {
                    newEntry.textSpeed = Mathf.Clamp(speed, 0.01f, 0.2f);
                }
            }

            dialogues.Add(newEntry);
        }
    }

    public void StartDialogue()
    {
        if (dialogues.Count == 0)
        {
            Debug.LogWarning("No dialogues loaded!");
            return;
        }

        dialoguePanel.SetActive(true);
        currentDialogueIndex = 0;
        isDialogueActive = true;
        ShowSentence(currentDialogueIndex);

        if (freezeTimeDuringDialogue)
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (iconAnimator != null)
        {
            iconAnimator.enabled = true;
            iconAnimator.Play("IconPulse", -1, 0f);
        }
    }

    private void ShowSentence(int index)
    {
        if (index < 0 || index >= dialogues.Count) return;

        DialogueEntry entry = dialogues[index];
        nameText.text = entry.characterName;
        contentText.text = "";

        // Set character icon
        if (entry.characterIcon != null)
        {
            characterImage.sprite = entry.characterIcon;
            characterImage.gameObject.SetActive(true);
            UpdateIconAnimation(entry.characterID);
        }
        else
        {
            characterImage.gameObject.SetActive(false);
        }

        typingCoroutine = StartCoroutine(TypeText(entry));
    }

    IEnumerator TypeText(DialogueEntry entry)
    {
        isTyping = true;
        foreach (char c in entry.dialogueContent.ToCharArray())
        {
            contentText.text += c;
            yield return new WaitForSecondsRealtime(entry.textSpeed);
        }
        isTyping = false;
    }

    private void HandleDialogueInput()
    {
        if (isTyping)
        {
            CompleteCurrentLine();
        }
        else
        {
            ShowNextSentence();
        }
    }

    private void CompleteCurrentLine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            contentText.text = dialogues[currentDialogueIndex].dialogueContent;
            isTyping = false;
        }
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

    public void EndDialogue()
    {
        if (freezeTimeDuringDialogue)
        {
            Time.timeScale = originalTimeScale;
        }
        if (iconAnimator != null)
        {
            iconAnimator.StopPlayback();
            iconAnimator.enabled = false;
        }
        dialoguePanel.SetActive(false);
        contentText.text = string.Empty;
        nameText.text = string.Empty;
        characterImage.gameObject.SetActive(false);
        isDialogueActive = false;
        currentDialogueIndex = 0;
    }

    public void LoadNewDialogue(TextAsset newDialogueFile)
    {
        dialogueFile = newDialogueFile;
        ParseDialogueFile();
        currentDialogueIndex = 0;
    }

    void OnDestroy()
    {
        if (freezeTimeDuringDialogue && Time.timeScale == 0f)
        {
            Time.timeScale = originalTimeScale;
        }
    }

    private void UpdateIconAnimation(string characterID)
    {
        if (iconAnimator == null) return;

        // 根据角色ID切换不同动画
        switch (characterID)
        {
            case "hero":
                iconAnimator.Play("HeroIcon");
                break;
            case "npc":
                iconAnimator.Play("NpcIcon");
                break;
            default:
                iconAnimator.Play("DefaultIcon");
                break;
        }
    }

    // Editor helper
#if UNITY_EDITOR
    [ContextMenu("Generate Example TXT")]
    void GenerateExampleTXT()
    {
        string exampleText =
            "hero|勇者: 这里就是魔王城吗？ @0.04\n" +
            "npc|村长: 要小心啊！\\n魔王的陷阱无处不在 @0.06\n" +
            "boss|魔王: 你终于来了... @0.08";

        System.IO.File.WriteAllText(Application.dataPath + "/ExampleDialogue.txt", exampleText);
        UnityEditor.AssetDatabase.Refresh();
    }
#endif
}
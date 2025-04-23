using UnityEngine;

[RequireComponent(typeof(NPCDialogueTrigger))]
public class Merchant : MonoBehaviour
{
    [Header("商店配置")]
    [SerializeField] private ShopManager shopManager;

    private NPCDialogueTrigger _dialogueTrigger;
    private bool _wasDialogueActive;
    private bool _isMyDialogue;

    private void Start()
    {
        _dialogueTrigger = GetComponent<NPCDialogueTrigger>();

        // 自动绑定同对象的ShopManager
        if (shopManager == null)
            shopManager = GetComponent<ShopManager>();

        // 初始隐藏商店
        if (shopManager != null)
            shopManager.CloseShop();
    }

    private void Update()
    {
        HandleDialogueTransition();
    }

    private void HandleDialogueTransition()
    {
        bool isDialogueActive = DialogueSystem.Instance.isDialogueActive;

        // 检测对话开始
        if (!_wasDialogueActive && isDialogueActive)
        {
            _isMyDialogue = _dialogueTrigger.isInRange;
        }

        // 检测对话结束
        if (_wasDialogueActive && !isDialogueActive && _isMyDialogue)
        {
            if (shopManager != null && !shopManager.isOPen)
            {
                shopManager.OpenShop();
            }
            _isMyDialogue = false;
        }

        _wasDialogueActive = isDialogueActive;
    }
}
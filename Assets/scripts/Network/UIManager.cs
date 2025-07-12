using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;
using UnityEngine.SceneManagement;
using Photon.Realtime;

public class UIManager : MonoBehaviourPunCallbacks
{
    public static UIManager Instance { get; private set; }

    [Header("本地玩家专属UI")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private TextMeshProUGUI playerHealthText;
    [SerializeField] private Slider playerEnergySlider;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private GameObject deathOverlay;
    [SerializeField] private TextMeshProUGUI restartPrompt;

    [Header("背包UI")]
    [SerializeField] private GameObject bagUI; // 背包UI引用

    [Header("货币UI")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("背包UI引用")]
    public InventoryManager inventoryManager; // 添加InventoryManager引用

    [Header("队伍UI")]
    [SerializeField] private GameObject teamPanel;

    [Header("准备/开始按钮")]
    [SerializeField] private Button readyStartButton;
    [SerializeField] private TextMeshProUGUI readyStartButtonText;


    private bool isPlayerReady = false;
    public bool IsBagOpen { get; private set; } // 背包状态属性

    private HealthSystem playerHealthSystem;
    private bool isPlayerRegistered = false;

    void Awake()
    {
        // 确保只有一个实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 添加场景加载监听
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // 清理事件监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnregisterPlayer();
    }

    void Start()
    {
        InitializeUI();
        TryFindLocalPlayer();
        FindInventoryManager();
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (readyStartButton != null)
        {
            readyStartButton.onClick.AddListener(OnReadyStartClicked);
            UpdateReadyButton();
        }
    }

    public void UpdateReadyButton()
    {
        if (readyStartButton == null || readyStartButtonText == null) return;

        // 房主显示开始按钮，其他玩家显示准备按钮
        if (PhotonNetwork.IsMasterClient)
        {
            readyStartButtonText.text = "Start";
            readyStartButton.interactable = NetworkManager.Instance.AreAllPlayersReady();
        }
        else
        {
            isPlayerReady = NetworkManager.Instance.IsPlayerReady(PhotonNetwork.LocalPlayer);
            readyStartButtonText.text = isPlayerReady ? "Cancel Ready" : "Ready";
            readyStartButton.interactable = true;
        }
    }

    private void OnReadyStartClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            NetworkManager.Instance.StartGameForAll();
        }
        else
        {
            bool newReadyState = !isPlayerReady;
            NetworkManager.Instance.SetPlayerReady(newReadyState);
            isPlayerReady = newReadyState;
            UpdateReadyButton();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        UpdateReadyButton();
    }

    // 查找InventoryManager
    private void FindInventoryManager()
    {
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogError("InventoryManager not found in scene!");
            }
            else
            {
                Debug.Log("Found InventoryManager");
            }
        }
    }

    // 场景加载完成时调用
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        ReinitializeUI();
        TryFindLocalPlayer();
        FindInventoryManager();
        if (TeamUIManager.Instance != null)
        {
            TeamUIManager.Instance.OnSceneLoaded();
        }
    }

    public void ToggleTeamPanel()
    {
        if (teamPanel != null)
        {
            teamPanel.SetActive(!teamPanel.activeSelf);
            if (teamPanel.activeSelf)
            {
                TeamUIManager.Instance.UpdateTeamUI();
            }
        }
    }

    private void ReinitializeUI()
    {
        // 重新查找UI组件
        playerHealthSlider = GameObject.Find("HealthSlider")?.GetComponent<Slider>();
        // 其他UI组件查找...

        InitializeUI();
    }

    // 尝试查找本地玩家并注册
    private void TryFindLocalPlayer()
    {
        if (isPlayerRegistered) return;

        Debug.Log("Trying to find local player...");

        // 查找所有玩家对象
        HealthSystem[] allPlayers = FindObjectsOfType<HealthSystem>();
        foreach (HealthSystem player in allPlayers)
        {
            if (player.photonView != null && player.photonView.IsMine)
            {
                RegisterLocalPlayer(player);
                break;
            }
        }

        if (!isPlayerRegistered)
        {
            Debug.LogWarning("Local player not found on scene load. Will try again.");
            StartCoroutine(RetryFindPlayer());
        }
    }

    private IEnumerator RetryFindPlayer()
    {
        yield return new WaitForSeconds(1f);
        TryFindLocalPlayer();
    }

    // 注册本地玩家的HealthSystem
    public void RegisterLocalPlayer(HealthSystem healthSystem)
    {
        if (isPlayerRegistered && playerHealthSystem == healthSystem)
            return;

        // 清理旧注册
        UnregisterPlayer();

        playerHealthSystem = healthSystem;
        isPlayerRegistered = true;

        Debug.Log($"Registering local player: {healthSystem.gameObject.name}");

        // 初始化UI
        InitializePlayerUI();

        // 注册事件
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnHealthChanged += UpdateHealthUI;
            playerHealthSystem.OnEnergyChanged += UpdateEnergyUI;
            playerHealthSystem.OnPlayerDeath += ShowDeathUI;
            playerHealthSystem.OnRestartAvailable += ShowRestartPrompt;

            // 立即更新一次UI
            UpdateHealthUI(playerHealthSystem.currentHealth, playerHealthSystem.maxHealth);
            UpdateEnergyUI(playerHealthSystem.currentEnergy, playerHealthSystem.maxEnergy);
            TeamUIManager.Instance?.UpdateTeamUI();
        }
    }

    // 初始化UI元素状态
    private void InitializeUI()
    {
        Debug.Log("Initializing UI");

        // 初始隐藏死亡UI
        if (deathOverlay != null)
        {
            deathOverlay.SetActive(false);
            CanvasGroup cg = deathOverlay.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0;
        }

        if (restartPrompt != null)
        {
            restartPrompt.gameObject.SetActive(false);
        }

        if (bagUI != null)
        {
            bagUI.SetActive(false);
            IsBagOpen = false;
        }

        if (MoneyManager.Instance != null)
        {
            UpdateMoneyUI(MoneyManager.Instance.GetCurrentMoney());
        }
    }

    // 初始化玩家UI
    private void InitializePlayerUI()
    {
        if (playerHealthSystem == null) return;

        if (playerHealthSlider != null)
        {
            playerHealthSlider.maxValue = playerHealthSystem.maxHealth;
            playerHealthSlider.value = playerHealthSystem.currentHealth;
        }

        if (playerEnergySlider != null)
        {
            playerEnergySlider.maxValue = playerHealthSystem.maxEnergy;
            playerEnergySlider.value = playerHealthSystem.currentEnergy;
        }

        UpdateHealthUI(playerHealthSystem.currentHealth, playerHealthSystem.maxHealth);
        UpdateEnergyUI(playerHealthSystem.currentEnergy, playerHealthSystem.maxEnergy);
    }

    // 更新血量UI
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        // 确保在UI线程执行
        if (this == null) return;

        if (playerHealthSlider != null)
        {
            playerHealthSlider.value = currentHealth;
        }

        if (playerHealthText != null)
        {
            playerHealthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }

        TeamUIManager.Instance?.UpdatePlayerStatus(
            PhotonNetwork.LocalPlayer.ActorNumber,
            currentHealth,
            maxHealth,
            playerHealthSystem.currentEnergy,
            playerHealthSystem.maxEnergy
        );
    }

    // 更新能量UI
    private void UpdateEnergyUI(float currentEnergy, float maxEnergy)
    {
        // 确保在UI线程执行
        if (this == null) return;

        if (playerEnergySlider != null)
        {
            playerEnergySlider.value = currentEnergy;
        }

        if (playerEnergyText != null)
        {
            playerEnergyText.text = $"{Mathf.CeilToInt(currentEnergy)}/{Mathf.CeilToInt(maxEnergy)}";
        }

        TeamUIManager.Instance?.UpdatePlayerStatus(
           PhotonNetwork.LocalPlayer.ActorNumber,
           playerHealthSystem.currentHealth,
           playerHealthSystem.maxHealth,
           currentEnergy,
           maxEnergy
       );
    }

    // 显示死亡UI
    private void ShowDeathUI()
    {
        if (deathOverlay != null && !deathOverlay.activeSelf)
        {
            deathOverlay.SetActive(true);
            StartCoroutine(FadeInDeathOverlay());
        }
    }

    private IEnumerator FadeInDeathOverlay()
    {
        CanvasGroup canvasGroup = deathOverlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0;
        float duration = 2f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
    }

    // 显示重启提示
    private void ShowRestartPrompt()
    {
        if (restartPrompt != null && !restartPrompt.gameObject.activeSelf)
        {
            restartPrompt.gameObject.SetActive(true);
        }
    }

    // 清理注册
    public void UnregisterPlayer()
    {
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnHealthChanged -= UpdateHealthUI;
            playerHealthSystem.OnEnergyChanged -= UpdateEnergyUI;
            playerHealthSystem.OnPlayerDeath -= ShowDeathUI;
            playerHealthSystem.OnRestartAvailable -= ShowRestartPrompt;
        }

        playerHealthSystem = null;
        isPlayerRegistered = false;
    }

    public void ToggleBag()
    {
        if (bagUI == null)
        {
            Debug.LogWarning("BagUI reference is null");
            return;
        }

        IsBagOpen = !bagUI.activeSelf;
        bagUI.SetActive(IsBagOpen);

        // 通知InventoryManager背包状态变化
        if (inventoryManager != null)
        {
            inventoryManager.OnBagStateChanged(IsBagOpen);
        }
        else
        {
            Debug.LogWarning("InventoryManager reference is null");
        }
    }

    public void UpdateMoneyUI(int amount)
    {
        if (moneyText != null)
            moneyText.text = amount.ToString();
    }

    // 调试方法
    public void ForceRefreshInventory()
    {
        if (bagUI == null)
        {
            Debug.LogWarning("BagUI reference is null in UIManager");
            return;
        }

        // 切換背包UI的顯示狀態
        IsBagOpen = !bagUI.activeSelf;
        bagUI.SetActive(IsBagOpen);

        // 【核心修正】
        // 不再調用 ForceRefresh()，而是調用 OnBagStateChanged()
        // 將新的背包狀態通知給 InventoryManager
        if (inventoryManager != null)
        {
            inventoryManager.OnBagStateChanged(IsBagOpen);
        }
        else
        {
            // 這個警告現在更有用，因為我們依賴 InventoryManager
            Debug.LogWarning("InventoryManager reference is null in UIManager, cannot notify bag state change.");
        }
    }
}
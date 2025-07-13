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

    [Header("Player Health UI")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private TextMeshProUGUI playerHealthText;
    [SerializeField] private Slider playerEnergySlider;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private GameObject deathOverlay;
    [SerializeField] private TextMeshProUGUI restartPrompt;

    [Header("Inventory UI")]
    [SerializeField] private GameObject bagUI; // 背包UI对象

    [Header("Money UI")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Inventory Reference")]
    public InventoryManager inventoryManager; // 引用InventoryManager组件

    [Header("Team UI")]
    [SerializeField] private GameObject teamPanel;

    [Header("Ready/Start Button")]
    [SerializeField] private Button readyStartButton;
    [SerializeField] private TextMeshProUGUI readyStartButtonText;

    [Header("Button Color Settings")]
    [SerializeField] private Image buttonImage;
    public Color readyColor = new Color(0.2f, 0.8f, 0.2f); // 准备就绪的绿色
    public Color cancelReadyColor = new Color(0.9f, 0.8f, 0.1f); // 取消准备的黄色
    public Color startReadyColor = new Color(0.2f, 0.8f, 0.2f); // 可开始的绿色
    public Color startDisabledColor = new Color(0.5f, 0.5f, 0.5f); // 不可开始的灰色

    [Header("Countdown Settings")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image countdownBackground;
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private AudioClip finalCountdownSound;

    [Header("Teleport Settings")]
    [SerializeField] private float teleportCountdownDuration = 10f;
    [SerializeField] private AudioClip teleportCountdownSound;
    [SerializeField] private AudioClip teleportFinalCountdownSound;
    [SerializeField] private AudioClip teleportCompleteSound;

    [Header("Exit Button")]
    [SerializeField] private Button exitButton;

    private bool isPlayerReady = false;
    public bool IsBagOpen { get; private set; } // 背包状态标志

    private HealthSystem playerHealthSystem;
    private bool isPlayerRegistered = false;
    private Coroutine countdownCoroutine;
    private AudioSource audioSource;
    private Vector3 originalTextScale;
    private Color originalTextColor;
    private PhotonView photonView;

    void Awake()
    {
        photonView = GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = gameObject.AddComponent<PhotonView>();
            // 设置一个唯一的 ViewID
            photonView.ViewID = 999;
        }
        if (photonView.ObservedComponents == null || photonView.ObservedComponents.Count == 0)
        {
            photonView.ObservedComponents = new System.Collections.Generic.List<Component> { this };
        }

        // 确保只有一个实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 添加场景加载监听器
        SceneManager.sceneLoaded += OnSceneLoaded;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // 保存原始文本属性
        if (countdownText != null)
        {
            originalTextScale = countdownText.transform.localScale;
            originalTextColor = countdownText.color;
        }
    }

    void OnDestroy()
    {
        // 移除事件监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnregisterPlayer();
    }

    void Start()
    {
        InitializeUI();
        TryFindLocalPlayer();
        FindInventoryManager();
        NetworkManager.Instance.ResetAllPlayerReadyStates();

        if (readyStartButton != null)
        {
            readyStartButton.onClick.AddListener(OnReadyStartClicked);
            UpdateReadyButton();
        }
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }

    public void UpdateReadyButton()
    {
        // 非安全屋场景直接隐藏准备按钮
        if (SceneTypeManager.CurrentSceneType != SceneTypeManager.SceneType.SafeHouse)
        {
            if (readyStartButton != null && readyStartButton.gameObject.activeSelf)
            {
                readyStartButton.gameObject.SetActive(false);
            }
            return;
        }

        if (readyStartButton == null || readyStartButtonText == null || buttonImage == null)
            return;

        // 房主逻辑
        if (PhotonNetwork.IsMasterClient)
        {
            readyStartButtonText.text = "START";
            bool allReady = PhotonNetwork.CurrentRoom.PlayerCount == 1 ||
                             NetworkManager.Instance.AreAllPlayersReady();

            // 设置按钮状态
            readyStartButton.interactable = allReady;

            // 直接设置按钮图像颜色
            buttonImage.color = allReady ? startReadyColor : startDisabledColor;
        }
        // 成员逻辑
        else
        {
            isPlayerReady = NetworkManager.Instance.IsPlayerReady(PhotonNetwork.LocalPlayer);
            readyStartButtonText.text = isPlayerReady ? "CANCEL READY" : "READY";
            readyStartButton.interactable = true;

            // 直接设置按钮图像颜色
            buttonImage.color = isPlayerReady ? cancelReadyColor : readyColor;
        }
    }

    private void OnExitButtonClicked()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LeaveRoomAndReturnToStartup();
        }
        else
        {
            Debug.LogWarning("NetworkManager instance not found!");
        }
    }


    private void OnReadyStartClicked()
    {
        if (SceneTypeManager.CurrentSceneType != SceneTypeManager.SceneType.SafeHouse)
            return;

        if (PhotonNetwork.IsMasterClient)
        {
            // 房主开始游戏 - 改为启动倒计时
            photonView.RPC("RPC_StartCountdown", RpcTarget.All);
            NetworkManager.Instance.CloseRoom();
            readyStartButton.gameObject.SetActive(false);
        }
        else
        {
            // 成员切换准备状态
            bool newReadyState = !isPlayerReady;
            NetworkManager.Instance.SetPlayerReady(newReadyState);
            isPlayerReady = newReadyState;

            // 立即更新UI，不必等待网络回调
            UpdateReadyButton();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        // 更新按钮状态
        UpdateReadyButton();
        // 更新团队UI
        TeamUIManager.Instance?.UpdateTeamUI();
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

    // 场景加载时调用
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        TryFindLocalPlayer();
        FindInventoryManager();

        // 根据场景类型调整UI
        if (SceneTypeManager.CurrentSceneType == SceneTypeManager.SceneType.GameLevel)
        {
            // 确保在游戏关卡中隐藏准备按钮
            if (readyStartButton != null && readyStartButton.gameObject.activeSelf)
            {
                readyStartButton.gameObject.SetActive(false);
            }
        }
        else
        {
            // 安全屋中更新UI
            UpdateReadyButton();
        }

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

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        if (changedProps.ContainsKey(NetworkManager.PLAYER_READY_KEY))
        {
            UpdateReadyButton();
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

        // 取消旧注册
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

            // 强制更新一次UI
            UpdateHealthUI(playerHealthSystem.currentHealth, playerHealthSystem.maxHealth);
            UpdateEnergyUI(playerHealthSystem.currentEnergy, playerHealthSystem.maxEnergy);
            TeamUIManager.Instance?.UpdateTeamUI();
        }
    }

    // 初始化UI元素状态
    private void InitializeUI()
    {
        Debug.Log("Initializing UI");

        // 初始化死亡UI
        if (deathOverlay != null)
        {
            deathOverlay.SetActive(false);
            CanvasGroup cg = deathOverlay.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0;  // 确保透明度重置为0
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

        if (buttonImage != null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                buttonImage.color = startDisabledColor;
            }
            else
            {
                buttonImage.color = readyColor;
            }
        }

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        // 重置文本属性
        if (countdownText != null)
        {
            countdownText.transform.localScale = originalTextScale;
            countdownText.color = originalTextColor;
        }

        // 只在安全屋显示准备按钮
        if (SceneTypeManager.CurrentSceneType == SceneTypeManager.SceneType.SafeHouse)
        {
            UpdateReadyButton();
        }
        else
        {
            if (readyStartButton != null)
            {
                readyStartButton.gameObject.SetActive(false);
            }
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
        if (deathOverlay != null)
        {
            // 确保覆盖层处于可激活状态
            deathOverlay.SetActive(true);
            // 启动完整的死亡序列
            StartCoroutine(DeathOverlaySequence());
        }
    }

    private IEnumerator DeathOverlaySequence()
    {
        CanvasGroup canvasGroup = deathOverlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            // 如果没有CanvasGroup组件，直接等待3秒然后隐藏
            yield return new WaitForSeconds(3f);
            deathOverlay.SetActive(false);
            yield break;
        }

        // 淡入效果
        canvasGroup.alpha = 0;
        float fadeInDuration = 2f;
        float elapsed = 0f; // 在此处声明elapsed变量

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        // 等待3秒
        yield return new WaitForSeconds(3f);

        // 淡出效果
        float fadeOutDuration = 2f;
        elapsed = 0f; // 重置elapsed变量用于淡出
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(startAlpha - (elapsed / fadeOutDuration));
            yield return null;
        }

        // 重置状态
        deathOverlay.SetActive(false);
    }

    // 显示重启提示
    private void ShowRestartPrompt()
    {
        if (restartPrompt != null && !restartPrompt.gameObject.activeSelf)
        {
            restartPrompt.gameObject.SetActive(true);
        }
    }

    // 取消注册
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

        // 通知InventoryManager状态变化
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

    // 强制刷新
    public void ForceRefreshInventory()
    {
        if (bagUI == null)
        {
            Debug.LogWarning("BagUI reference is null in UIManager");
            return;
        }

        // 切换背包UI显示状态
        IsBagOpen = !bagUI.activeSelf;
        bagUI.SetActive(IsBagOpen);

        // 通知InventoryManager
        if (inventoryManager != null)
        {
            inventoryManager.OnBagStateChanged(IsBagOpen);
        }
        else
        {
            Debug.LogWarning("InventoryManager reference is null in UIManager, cannot notify bag state change.");
        }
    }

    [PunRPC]
    public void RPC_StartCountdown()
    {
        // 防止重复开始
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        // 禁用准备按钮
        if (readyStartButton != null)
        {
            readyStartButton.interactable = false;
        }

        // 激活倒计时面板
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
        }

        // 背景淡入动画
        if (countdownBackground != null)
        {
            countdownBackground.color = new Color(0, 0, 0, 0);
            LeanTween.alpha(countdownBackground.rectTransform, 0.7f, 0.5f)
                .setEase(LeanTweenType.easeOutQuad);
        }

        float timer = countdownDuration;

        // 播放倒计时音效
        if (countdownSound != null)
        {
            audioSource.PlayOneShot(countdownSound);
        }

        while (timer > 0)
        {
            // 更新倒计时文本
            if (countdownText != null)
            {
                int seconds = Mathf.CeilToInt(timer);
                countdownText.text = seconds.ToString();

                // 添加稳定的动画效果
                StableCountdownAnimation(seconds);

                // 最后3秒改变颜色
                if (seconds <= 3)
                {
                    // 播放特殊音效
                    if (finalCountdownSound != null && Mathf.Approximately(timer % 1, 0))
                    {
                        audioSource.PlayOneShot(finalCountdownSound);
                    }
                }
            }

            yield return new WaitForSeconds(0.05f);
            timer -= 0.05f;
        }

        // 倒计时结束 - GO! 动画
        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.color = Color.green;

            // 缩放动画（无位移）
            LeanTween.scale(countdownText.gameObject, originalTextScale * 1.8f, 0.4f)
                .setEase(LeanTweenType.easeOutBack);

            // 淡出效果
            LeanTween.value(countdownText.gameObject, 1f, 0f, 0.6f)
                .setDelay(0.3f)
                .setOnUpdate((float alpha) => {
                    Color c = countdownText.color;
                    c.a = alpha;
                    countdownText.color = c;
                });
        }

        // 背景淡出动画
        if (countdownBackground != null)
        {
            LeanTween.alpha(countdownBackground.rectTransform, 0f, 0.8f)
                .setEase(LeanTweenType.easeInQuad);
        }

        yield return new WaitForSeconds(0.8f);

        // 隐藏面板
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        // 重置文本属性
        if (countdownText != null)
        {
            countdownText.transform.localScale = originalTextScale;
            countdownText.color = originalTextColor;
            countdownText.alpha = 1f; // 重置透明度
        }

        // 隐藏准备按钮（所有客户端）
        photonView.RPC("RPC_HideReadyButton", RpcTarget.All);

        // 房主加载场景
        if (PhotonNetwork.IsMasterClient)
        {
            NetworkManager.Instance.StartGameForAll();
        }
    }

    // 稳定的倒计时动画（无抖动）
    public void StableCountdownAnimation(int seconds)
    {
        if (countdownText == null) return;

        // 取消之前的动画
        LeanTween.cancel(countdownText.gameObject);

        // 缩放动画
        Vector3 targetScale = originalTextScale * (seconds <= 3 ? 1.4f : 1.2f);
        LeanTween.scale(countdownText.gameObject, targetScale, 0.2f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() => {
                LeanTween.scale(countdownText.gameObject, originalTextScale, 0.3f)
                    .setEase(LeanTweenType.easeInOutQuad);
            });

        // 颜色动画
        Color targetColor = seconds <= 3 ? Color.red : Color.white;
        LeanTween.value(countdownText.gameObject, countdownText.color, targetColor, 0.3f)
            .setOnUpdate((Color c) => countdownText.color = c);
    }

    [PunRPC]
    private void RPC_HideReadyButton()
    {
        if (readyStartButton != null && readyStartButton.gameObject.activeSelf)
        {
            readyStartButton.gameObject.SetActive(false);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        Debug.Log($"Player left: {otherPlayer.NickName}");

        // 更新按钮状态
        UpdateReadyButton();

        // 更新队伍UI
        TeamUIManager.Instance?.UpdateTeamUI();
    }

    public void StartTeleportCountdown()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("RPC_StartTeleportCountdown", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_StartTeleportCountdown()
    {
        // 防止重复开始
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        countdownCoroutine = StartCoroutine(TeleportCountdownRoutine());
    }

    private IEnumerator TeleportCountdownRoutine()
    {
        // 激活倒计时面板
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(true);
        }

        // 背景淡入动画
        if (countdownBackground != null)
        {
            countdownBackground.color = new Color(0, 0, 0, 0);
            LeanTween.alpha(countdownBackground.rectTransform, 0.7f, 0.5f)
                .setEase(LeanTweenType.easeOutQuad);
        }

        float timer = teleportCountdownDuration;

        // 播放倒计时音效
        if (teleportCountdownSound != null)
        {
            audioSource.PlayOneShot(teleportCountdownSound);
        }

        while (timer > 0)
        {
            // 更新倒计时文本
            if (countdownText != null)
            {
                int seconds = Mathf.CeilToInt(timer);
                countdownText.text = $"Transmission countdown:{seconds}";

                // 使用稳定的动画效果
                StableCountdownAnimation(seconds);

                // 最后3秒改变颜色和音效
                if (seconds <= 3)
                {
                    countdownText.color = Color.red;

                    // 播放特殊音效
                    if (teleportFinalCountdownSound != null && Mathf.Approximately(timer % 1, 0))
                    {
                        audioSource.PlayOneShot(teleportFinalCountdownSound);
                    }
                }
                else
                {
                    countdownText.color = Color.yellow;
                }
            }

            yield return new WaitForSeconds(0.05f);
            timer -= 0.05f;
        }

        // 倒计时结束 - 传送!
        if (countdownText != null)
        {
            countdownText.text = "Transmitting...";
            countdownText.color = Color.green;

            // 缩放动画
            LeanTween.scale(countdownText.gameObject, originalTextScale * 1.5f, 0.5f)
                .setEase(LeanTweenType.easeOutBack);
        }

        // 播放传送完成音效
        if (teleportCompleteSound != null)
        {
            audioSource.PlayOneShot(teleportCompleteSound);
        }

        yield return new WaitForSeconds(1.5f);

        // 背景淡出动画
        if (countdownBackground != null)
        {
            LeanTween.alpha(countdownBackground.rectTransform, 0f, 0.8f)
                .setEase(LeanTweenType.easeInQuad);
        }

        yield return new WaitForSeconds(0.8f);

        // 隐藏面板
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        // 重置文本属性
        if (countdownText != null)
        {
            countdownText.transform.localScale = originalTextScale;
            countdownText.color = originalTextColor;
        }

        // 主机加载安全屋场景
        if (PhotonNetwork.IsMasterClient)
        {
            NetworkManager.Instance.ReopenRoomIfNeeded();
            NetworkManager.Instance.SavePlayerDataBeforeSceneChange();
            PhotonNetwork.LoadLevel("SafeHouse");
        }
    }

}
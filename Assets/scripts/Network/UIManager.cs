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

    [Header("�������ר��UI")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private TextMeshProUGUI playerHealthText;
    [SerializeField] private Slider playerEnergySlider;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private GameObject deathOverlay;
    [SerializeField] private TextMeshProUGUI restartPrompt;

    [Header("����UI")]
    [SerializeField] private GameObject bagUI; // ����UI����

    [Header("����UI")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("����UI����")]
    public InventoryManager inventoryManager; // ����InventoryManager����

    [Header("����UI")]
    [SerializeField] private GameObject teamPanel;

    [Header("׼��/��ʼ��ť")]
    [SerializeField] private Button readyStartButton;
    [SerializeField] private TextMeshProUGUI readyStartButtonText;

    [Header("按钮颜色设置")]
    [SerializeField] private Image buttonImage;
    public Color readyColor = new Color(0.2f, 0.8f, 0.2f); // 准备就绪的绿色
    public Color cancelReadyColor = new Color(0.9f, 0.8f, 0.1f); // 取消准备的黄色
    public Color startReadyColor = new Color(0.2f, 0.8f, 0.2f); // 可开始的绿色
    public Color startDisabledColor = new Color(0.5f, 0.5f, 0.5f); // 不可开始的灰色

    [Header("倒计时设置")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image countdownBackground;
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private AudioClip countdownSound;
    [SerializeField] private AudioClip finalCountdownSound;

    private bool isPlayerReady = false;
    public bool IsBagOpen { get; private set; } // ����״̬����

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
        // ȷ��ֻ��һ��ʵ��
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ���ӳ������ؼ���
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
        // �����¼�����
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
        if (readyStartButton == null || readyStartButtonText == null || buttonImage == null)
            return;

        // 房主逻辑
        if (PhotonNetwork.IsMasterClient)
        {
            readyStartButtonText.text = "START";
            bool allReady = NetworkManager.Instance.AreAllPlayersReady();

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

    private void OnReadyStartClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 房主开始游戏 - 改为启动倒计时
            photonView.RPC("RPC_StartCountdown", RpcTarget.All);
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

    // ����InventoryManager
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

    // �����������ʱ����
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
        // ���²���UI���
        playerHealthSlider = GameObject.Find("HealthSlider")?.GetComponent<Slider>();
        // ����UI�������...

        InitializeUI();
    }

    // ���Բ��ұ�����Ҳ�ע��
    private void TryFindLocalPlayer()
    {
        if (isPlayerRegistered) return;

        Debug.Log("Trying to find local player...");

        // ����������Ҷ���
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

    // ע�᱾����ҵ�HealthSystem
    public void RegisterLocalPlayer(HealthSystem healthSystem)
    {
        if (isPlayerRegistered && playerHealthSystem == healthSystem)
            return;

        // ������ע��
        UnregisterPlayer();

        playerHealthSystem = healthSystem;
        isPlayerRegistered = true;

        Debug.Log($"Registering local player: {healthSystem.gameObject.name}");

        // ��ʼ��UI
        InitializePlayerUI();

        // ע���¼�
        if (playerHealthSystem != null)
        {
            playerHealthSystem.OnHealthChanged += UpdateHealthUI;
            playerHealthSystem.OnEnergyChanged += UpdateEnergyUI;
            playerHealthSystem.OnPlayerDeath += ShowDeathUI;
            playerHealthSystem.OnRestartAvailable += ShowRestartPrompt;

            // ��������һ��UI
            UpdateHealthUI(playerHealthSystem.currentHealth, playerHealthSystem.maxHealth);
            UpdateEnergyUI(playerHealthSystem.currentEnergy, playerHealthSystem.maxEnergy);
            TeamUIManager.Instance?.UpdateTeamUI();
        }
    }

    // ��ʼ��UIԪ��״̬
    private void InitializeUI()
    {
        Debug.Log("Initializing UI");

        // ��ʼ��������UI
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
        UpdateReadyButton();
    }

    // ��ʼ�����UI
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

    // ����Ѫ��UI
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        // ȷ����UI�߳�ִ��
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

    // ��������UI
    private void UpdateEnergyUI(float currentEnergy, float maxEnergy)
    {
        // ȷ����UI�߳�ִ��
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

    // ��ʾ����UI
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

    // ��ʾ������ʾ
    private void ShowRestartPrompt()
    {
        if (restartPrompt != null && !restartPrompt.gameObject.activeSelf)
        {
            restartPrompt.gameObject.SetActive(true);
        }
    }

    // ����ע��
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

        // ֪ͨInventoryManager����״̬�仯
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

    // ���Է���
    public void ForceRefreshInventory()
    {
        if (bagUI == null)
        {
            Debug.LogWarning("BagUI reference is null in UIManager");
            return;
        }

        // �ГQ����UI���@ʾ��B
        IsBagOpen = !bagUI.activeSelf;
        bagUI.SetActive(IsBagOpen);

        // ������������
        // �����{�� ForceRefresh()�������{�� OnBagStateChanged()
        // ���µı�����B֪ͨ�o InventoryManager
        if (inventoryManager != null)
        {
            inventoryManager.OnBagStateChanged(IsBagOpen);
        }
        else
        {
            // �@������F�ڸ����ã�����҂���ه InventoryManager
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

    // 房主加载场景
    if (PhotonNetwork.IsMasterClient)
    {
        NetworkManager.Instance.StartGameForAll();
        readyStartButton.gameObject.SetActive(false);
    }
}

// 稳定的倒计时动画（无抖动）
private void StableCountdownAnimation(int seconds)
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
   
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;
using UnityEngine.SceneManagement;

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
    [SerializeField] private GameObject bagUI; // 添加背包UI引用
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
    }

    // 场景加载完成时调用
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重新初始化UI并尝试查找本地玩家
        InitializeUI();
        TryFindLocalPlayer();
        ReinitializeUI();
    }
    private void ReinitializeUI()
{
    // 重新查找关键UI组件
    playerHealthSlider = GameObject.Find("HealthSlider")?.GetComponent<Slider>();
    // ...其他UI组件查找
    
    InitializeUI();
    TryFindLocalPlayer();
}

    // 尝试查找本地玩家并注册
    private void TryFindLocalPlayer()
    {
        if (isPlayerRegistered) return;

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
        }
    }

    // 初始化UI元素状态
    private void InitializeUI()
    {
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
        if (bagUI == null) return;
        
        IsBagOpen = !bagUI.activeSelf;
        bagUI.SetActive(IsBagOpen);
        
        // 暂停/恢复游戏时间
        Time.timeScale = IsBagOpen ? 0f : 1f;
    }
}
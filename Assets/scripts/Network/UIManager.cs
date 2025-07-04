using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;
using UnityEngine.SceneManagement;

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
    [SerializeField] private GameObject bagUI; // ��ӱ���UI����
    public bool IsBagOpen { get; private set; } // ����״̬����


    private HealthSystem playerHealthSystem;
    private bool isPlayerRegistered = false;

    void Awake()
    {
        // ȷ��ֻ��һ��ʵ��
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ��ӳ������ؼ���
        SceneManager.sceneLoaded += OnSceneLoaded;
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
    }

    // �����������ʱ����
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ���³�ʼ��UI�����Բ��ұ������
        InitializeUI();
        TryFindLocalPlayer();
        ReinitializeUI();
    }
    private void ReinitializeUI()
{
    // ���²��ҹؼ�UI���
    playerHealthSlider = GameObject.Find("HealthSlider")?.GetComponent<Slider>();
    // ...����UI�������
    
    InitializeUI();
    TryFindLocalPlayer();
}

    // ���Բ��ұ�����Ҳ�ע��
    private void TryFindLocalPlayer()
    {
        if (isPlayerRegistered) return;

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
    }

    // ע�᱾����ҵ�HealthSystem
    public void RegisterLocalPlayer(HealthSystem healthSystem)
    {
        if (isPlayerRegistered && playerHealthSystem == healthSystem)
            return;

        // �����ע��
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
        }
    }

    // ��ʼ��UIԪ��״̬
    private void InitializeUI()
    {
        // ��ʼ��������UI
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
    }

    // ��ʾ����UI
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
        if (bagUI == null) return;
        
        IsBagOpen = !bagUI.activeSelf;
        bagUI.SetActive(IsBagOpen);
        
        // ��ͣ/�ָ���Ϸʱ��
        Time.timeScale = IsBagOpen ? 0f : 1f;
    }
}
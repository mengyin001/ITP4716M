using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class HealthSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("音效配置")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioSource audioSource;

    [Header("血量控制")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("蓝量控制")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TextMeshProUGUI energyText;

    [Header("自动回复血量")]
    [SerializeField] public bool autoRegen = false;
    [SerializeField] public float regenRate = 1f;

    [Header("自动回复蓝量")]
    [SerializeField] public bool autoRegenEn = false;
    [SerializeField] public float regenRateEn = 1f;

    [Header("死亡效果")]
    [SerializeField] private UnityEvent onDeath;
    [SerializeField] private bool disableControlOnDeath = true;
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private Transform characterBody;
    [SerializeField] private CanvasGroup deathImage;
    [SerializeField] private float deathAnimationTime = 2f;

    [Header("重新开始设置")]
    [SerializeField] private TextMeshProUGUI restartPrompt;
    [SerializeField] private float restartDelay = 1f;
    private bool canRestart = false;
    [SerializeField] private string restartSceneName = "Startup";

    private bool isDead = false;
    public bool IsDead
    {
        get { return isDead; }
        private set { isDead = value; }
    }

    // 用于网络同步的变量
    private float networkCurrentHealth;
    private float networkCurrentEnergy;
    private bool healthChanged = false;
    private bool energyChanged = false;

    void Start()
    {
        // 只在本地客户端初始化
        if (photonView.IsMine)
        {
            currentHealth = maxHealth;
            currentEnergy = maxEnergy;
        }

        // 自动获取Slider组件
        if (healthSlider == null)
            healthSlider = GameObject.Find("HealthSlider")?.GetComponent<Slider>();
        if (energySlider == null)
            energySlider = GameObject.Find("EnergySlider")?.GetComponent<Slider>();

        // 初始化UI
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        energySlider.maxValue = maxEnergy;
        energySlider.value = currentEnergy;
        UpdateHealthText();
        UpdateEnergyText();

        if (deathImage != null)
        {
            deathImage.alpha = 0;
            deathImage.gameObject.SetActive(false);
        }

        if (restartPrompt != null)
        {
            restartPrompt.gameObject.SetActive(false);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // 只在本地客户端处理
        if (!photonView.IsMine) return;

        // 自动回复血量
        if (autoRegen && currentHealth < maxHealth)
        {
            currentHealth = Mathf.Clamp(currentHealth + regenRate * Time.deltaTime, 0, maxHealth);
            healthChanged = true;
            UpdateHealthUI();
        }

        // 自动回复蓝量
        if (autoRegenEn && currentEnergy < maxEnergy)
        {
            currentEnergy = Mathf.Clamp(currentEnergy + regenRateEn * Time.deltaTime, 0, maxEnergy);
            energyChanged = true;
            UpdateEnergyUI();
        }

        // 测试按键：按H扣血
        if (Input.GetKeyDown(KeyCode.H))
        {
            photonView.RPC("RPC_TakeDamage", RpcTarget.All, 10f);
        }

        // 测试按键：按J消耗能量
        if (Input.GetKeyDown(KeyCode.J))
        {
            photonView.RPC("RPC_ConsumeEnergy", RpcTarget.All, 5f);
        }

        // 检测重启输入
        if (canRestart && Input.GetMouseButtonDown(0))
        {
            RestartGame();
            canRestart = false;
        }
    }

    // 实现网络同步接口
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 发送数据到其他客户端
            stream.SendNext(currentHealth);
            stream.SendNext(currentEnergy);
            stream.SendNext(isDead);
        }
        else
        {
            object receivedValue; // 使用一個通用的 object 來接收數據

            // 1. 接收第一個值 (Health)
            receivedValue = stream.ReceiveNext();
            if (receivedValue is float) // 檢查接收到的值是否真的是 float
            {
                this.networkCurrentHealth = (float)receivedValue;
            }
            else
            {
                // 如果類型不對，可以打印一個警告，但不要讓程式崩潰
                Debug.LogWarning($"OnPhotonSerializeView: Health 數據類型錯誤。期望 float，收到 {receivedValue.GetType()}", this);
            }

            // 2. 接收第二個值 (Energy)
            receivedValue = stream.ReceiveNext();
            if (receivedValue is float) // 檢查類型
            {
                this.networkCurrentEnergy = (float)receivedValue;
            }
            else
            {
                Debug.LogWarning($"OnPhotonSerializeView: Energy 數據類型錯誤。期望 float，收到 {receivedValue.GetType()}", this);
            }

            // 3. 接收第三個值 (IsDead)
            receivedValue = stream.ReceiveNext();
            if (receivedValue is bool) // 檢查類型
            {
                this.isDead = (bool)receivedValue;
            }
            else
            {
                Debug.LogWarning($"OnPhotonSerializeView: IsDead 數據類型錯誤。期望 bool，收到 {receivedValue.GetType()}", this);
            }

            // 在安全地接收完所有數據後，再更新 UI
            UpdateHealthUI();
            UpdateEnergyUI();
        }
    }

    // 受到伤害 (RPC版本)
    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (isDead) return;

        // 只在本地客户端处理伤害逻辑
        if (photonView.IsMine)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
            healthChanged = true;

            if (currentHealth < previousHealth)
            {
                PlayHurtSound();
            }

            UpdateHealthUI();

            if (currentHealth <= 0)
            {
                Debug.Log("角色死亡！");
                HandleDeath();
            }
        }
        else
        {
            // 非本地客户端更新UI
            healthSlider.value = networkCurrentHealth;
            UpdateHealthText();
        }
    }

    // 消耗能量 (RPC版本)
    [PunRPC]
    public void RPC_ConsumeEnergy(float amount)
    {
        // 只在本地客户端处理能量消耗
        if (photonView.IsMine)
        {
            currentEnergy = Mathf.Clamp(currentEnergy - amount, 0, maxEnergy);
            energyChanged = true;
            UpdateEnergyUI();
        }
        else
        {
            // 非本地客户端更新UI
            energySlider.value = networkCurrentEnergy;
            UpdateEnergyText();
        }
    }

    // 治疗角色
    public void Heal(float amount)
    {
        if (!photonView.IsMine) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        healthChanged = true;
        UpdateHealthUI();
    }

    // 回复蓝量
    public void RestoreEnergy(float amount)
    {
        if (!photonView.IsMine) return;

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        energyChanged = true;
        UpdateEnergyUI();
    }

    // 更新血量显示
    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = photonView.IsMine ? currentHealth : networkCurrentHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(photonView.IsMine ? currentHealth : networkCurrentHealth)}/{maxHealth}";
        }
    }

    // 更新蓝量显示
    private void UpdateEnergyUI()
    {
        if (energySlider != null)
        {
            energySlider.value = photonView.IsMine ? currentEnergy : networkCurrentEnergy;
        }

        if (energyText != null)
        {
            energyText.text = $"{Mathf.CeilToInt(photonView.IsMine ? currentEnergy : networkCurrentEnergy)}/{maxEnergy}";
        }
    }

    // 新增文本更新方法
    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = $"{Mathf.FloorToInt(photonView.IsMine ? currentHealth : networkCurrentHealth)}/{maxHealth}";
        }
    }

    private void UpdateEnergyText()
    {
        if (energyText != null)
        {
            energyText.text = $"{Mathf.FloorToInt(photonView.IsMine ? currentEnergy : networkCurrentEnergy)} / {maxEnergy}";
        }
    }

    public bool HasEnoughEnergy(float amount)
    {
        // 只检查本地客户端的能量
        return photonView.IsMine && currentEnergy >= amount;
    }

    private void HandleDeath()
    {
        isDead = true;
        onDeath.Invoke();

        if (disableControlOnDeath)
        {
            var controller = GetComponent<PlayerMovement>();
            if (controller != null)
            {
                controller.enabled = false;
                Debug.Log("已禁用 PlayerMovement 组件");
            }
            else
            {
                Debug.LogError("未找到 PlayerMovement 组件！");
            }
        }
        StartCoroutine(PlayDeathAnimation());
    }

    private IEnumerator PlayDeathAnimation()
    {
        if (deathImage != null)
        {
            deathImage.gameObject.SetActive(true);
        }

        float elapsed = 0;
        Quaternion startRotation = characterBody.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, 90);

        while (elapsed < deathAnimationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathAnimationTime;

            characterBody.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            if (deathImage != null)
            {
                deathImage.alpha = Mathf.SmoothStep(0, 1, t);
            }

            yield return null;
        }

        characterBody.rotation = targetRotation;

        if (destroyOnDeath)
        {
            yield return new WaitForSeconds(destroyDelay);
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        ShowRestartPrompt();
    }

    private void ShowRestartPrompt()
    {
        canRestart = true;
        if (restartPrompt != null)
        {
            restartPrompt.gameObject.SetActive(true);
        }
    }

    private void RestartGame()
    {
        SceneLoader.targetScene = restartSceneName;
        SceneManager.LoadScene("LoadingScence");
    }

    private void PlayHurtSound()
    {
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
    }
}
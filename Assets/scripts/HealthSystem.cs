using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;

public class HealthSystem : MonoBehaviour
{
    [Header("音效配置")] // 新增音效Header
    [SerializeField] private AudioClip hurtSound;    // 受伤音效
    [SerializeField] private AudioSource audioSource; // 音频组件

    [Header("血量控制")]
    [SerializeField] private float maxHealth = 100f; // 最大血量
    [SerializeField] public float currentHealth;     // 开始前血量
    [SerializeField] private Slider healthSlider;     // 血量Slider组件
    [SerializeField] private TextMeshProUGUI healthText; // TMP血量文本

    [Header("蓝量控制")]
    [SerializeField] private float maxEnergy = 100f;  //最大能量
    [SerializeField] private float currentEnergy;   // 开始前能量
    [SerializeField] private Slider energySlider;   // 蓝量Silder组件
    [SerializeField] private TextMeshProUGUI energyText; // TMP蓝量文本

    [Header("自动回复血量")]
    [SerializeField] private bool autoRegen = false;  // 是否自动回复
    [SerializeField] private float regenRate = 1f;    // 每秒回复量

    [Header("自动回复蓝量")]
    [SerializeField] private bool autoRegenEn = false;  // 是否自动回复
    [SerializeField] private float regenRateEn = 1f;    // 每秒回复量

    [Header("死亡效果")]
    [SerializeField] private UnityEvent onDeath; // 死亡事件
    [SerializeField] private bool disableControlOnDeath = true; // 是否禁用控制
    [SerializeField] private bool destroyOnDeath = false; // 是否销毁对象
    [SerializeField] private float destroyDelay = 3f; // 销毁延迟
    [SerializeField] private Transform characterBody; // 需要旋转的角色身体
    [SerializeField] private CanvasGroup deathImage;  // 渐显的死亡图片
    [SerializeField] private float deathAnimationTime = 2f; // 死亡动画持续时间

    [Header("重新开始设置")]
    [SerializeField] private TextMeshProUGUI restartPrompt; // 重新开始提示文本
    [SerializeField] private float restartDelay = 1f;    // 允许重新开始前的延迟
    private bool canRestart = false;
    [SerializeField] private string restartSceneName = "Startup";

    private bool isDead = false; // 死亡状态标识
                                 // 在 HealthSystem.cs 中添加公共访问器
    public bool IsDead
    {
        get { return isDead; }
        private set { isDead = value; } // 保持内部修改权限
    }

    void OnDisable()
    {
        // 保存生命值
        PlayerData.Health = currentHealth;
    }

    void Start()
    {
        // 初始化血量和蓝量
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
        // 恢复生命值
        currentHealth = PlayerData.Health;
        UpdateHealthUI();
        // 自动获取Slider组件（如果未手赋予值）
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();
        if (energySlider == null)
            energySlider = GetComponent<Slider>();
        // 重置Slider
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        energySlider.maxValue = maxEnergy;
        energySlider.value = currentEnergy;

        // 初始化文本显示
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
        // 自动回复血量
        if (autoRegen && currentHealth < maxHealth)
        {
            currentHealth = Mathf.Clamp(currentHealth + regenRate * Time.deltaTime,0,maxHealth);
            UpdateHealthUI();
        }

        //自动回复蓝量
        if (autoRegenEn && currentEnergy < maxEnergy)
        {
            currentEnergy = Mathf.Clamp(currentEnergy + regenRateEn * Time.deltaTime,0,maxEnergy);
            UpdateEnergyUI();
        }

        // 按H扣血
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }

        // 测试按键：按J消耗能量（新增测试代码）
        if (Input.GetKeyDown(KeyCode.J))
        {
            ConsumeEnergy(5);
        }

        // 检测重启输入
        if (canRestart && Input.GetMouseButtonDown(0))
        {
            RestartGame();
            canRestart = false;
        }




    }

    // 受到伤害
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        float previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        if (currentHealth < previousHealth)
        {
            PlayHurtSound();
        }

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Debug.Log("角色死亡！");
            //这里可以触?死亡事件
            HandleDeath();
        }
    }

    //减少蓝量 
    public void ConsumeEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy - amount, 0, maxEnergy);
        UpdateEnergyUI();
    }

    // 治疗角色
    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthUI();
    }

    //回复蓝量
    public void RestoreEnergy(float amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        UpdateEnergyUI();
    }

    // 更新血量显示
    private void UpdateHealthUI()
    {
        healthSlider.value = currentHealth;
        healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{maxHealth}";
    }

    //更新蓝量显示
    private void UpdateEnergyUI()
    {
        energySlider.value = currentEnergy;
        energyText.text = $"{Mathf.CeilToInt(currentEnergy)}/{maxEnergy}";
    }

    // 新增文本更新方法
    private void UpdateHealthText()
    {
        healthText.text = $"{Mathf.FloorToInt(currentHealth)}/{maxHealth}";
    }

    private void UpdateEnergyText()
    {
        energyText.text = $"{Mathf.FloorToInt(currentEnergy)} / {maxEnergy}";
    }
    public bool HasEnoughEnergy(float amount)
    {
        return currentEnergy >= amount;
    }

    private void InitializeSlider(Slider slider, float maxValue)
    {
        if (slider != null)
        {
            slider.maxValue = maxValue;
            slider.value = maxValue;
        }
    }

    private void HandleDeath()
    {
        isDead = true;
        onDeath.Invoke();

        if (disableControlOnDeath)
        {
            var controller = GetComponent<PlayerMovement>();
            if (controller != null) controller.enabled = false;
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

        // 计算目标旋转（绕Z轴旋转90度）
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, 90);

        while (elapsed < deathAnimationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathAnimationTime;

            // 使用球面插值实现平滑旋转
            characterBody.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                t
            );

            // 图片渐显（使用平滑过渡）
            if (deathImage != null)
            {
                deathImage.alpha = Mathf.SmoothStep(0, 1, t);
            }

            yield return null;
        }

        // 确保最终角度精确
        characterBody.rotation = targetRotation;

        // 显示重启提示
        ShowRestartPrompt();

        // 后续处理
        if (destroyOnDeath)
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
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
        Debug.Log("尝试播放受伤音效");

        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
        else
        {
            Debug.LogWarning("音效未配置：AudioSource或HurtSound为空");
        }

    }


}



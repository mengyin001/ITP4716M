using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class TeamMemberUI : MonoBehaviour
{
    [Header("UI元素")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider energySlider;
    [SerializeField] private GameObject leaderIcon; // 队长图标对象

    [Header("准备状态图标")]
    [SerializeField] private Image readyStatusIcon; // 准备状态图标
    [SerializeField] private Color readyColor = Color.green; // 准备状态颜色
    [SerializeField] private Color notReadyColor = Color.gray; // 未准备状态颜色

    [SerializeField] private Image background;

    private Player player;
    private HealthSystem healthSystem;
    private bool isLeader; // 标记是否是队长

    public void Initialize(Player player, HealthSystem healthSystem)
    {
        this.player = player;
        this.healthSystem = healthSystem;
        playerNameText.text = player.NickName;

        UpdateLeaderStatus();

        // 初始设置准备状态图标
        SetReadyStatus(false);

        // 如果有HealthSystem，订阅事件
        if (healthSystem != null)
        {
            // 初始状态
            UpdateStatus(
                healthSystem.currentHealth,
                healthSystem.maxHealth,
                healthSystem.currentEnergy,
                healthSystem.maxEnergy
            );

            // 订阅健康系统事件
            healthSystem.OnHealthChanged += HandleHealthChanged;
            healthSystem.OnEnergyChanged += HandleEnergyChanged;
        }
        else
        {
            // 使用默认值
            UpdateStatus(100f, 100f, 100f, 100f);
        }
    }

    // 设置队长状态
    public void SetLeaderStatus(bool isLeader)
    {
        this.isLeader = isLeader;

        // 更新队长图标
        if (leaderIcon != null)
        {
            leaderIcon.SetActive(isLeader);
        }

        // 队长不显示准备状态图标
        if (readyStatusIcon != null)
        {
            readyStatusIcon.gameObject.SetActive(!isLeader);
        }
    }

    // 设置准备状态
    public void SetReadyStatus(bool isReady)
    {
        // 如果是队长，不显示准备状态
        if (isLeader || readyStatusIcon == null) return;

        readyStatusIcon.gameObject.SetActive(true);
        readyStatusIcon.color = isReady ? readyColor : notReadyColor;
    }

    // 更新队长状态
    private void UpdateLeaderStatus()
    {
        isLeader = false;

        // 优先使用自定义属性判断
        if (player.CustomProperties.TryGetValue("IsTeamLeader", out object isLeaderObj))
        {
            isLeader = (bool)isLeaderObj;
        }
        // 如果没有自定义属性，使用IsMasterClient作为备用
        else
        {
            isLeader = player.IsMasterClient;
        }

        // 确保leaderIcon不为空
        if (leaderIcon != null)
        {
            leaderIcon.SetActive(isLeader);
        }

        // 队长不显示准备状态图标
        if (readyStatusIcon != null)
        {
            readyStatusIcon.gameObject.SetActive(!isLeader);
        }
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        UpdateHealthStatus(currentHealth, maxHealth);
    }

    private void HandleEnergyChanged(float currentEnergy, float maxEnergy)
    {
        UpdateEnergyStatus(currentEnergy, maxEnergy);
    }

    // 添加接受四个参数的UpdateStatus方法
    public void UpdateStatus(float health, float maxHealth, float energy, float maxEnergy)
    {
        UpdateHealthStatus(health, maxHealth);
        UpdateEnergyStatus(energy, maxEnergy);
    }

    private void UpdateHealthStatus(float health, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }

        // 根据血量百分比改变背景颜色
        float healthPercentage = health / maxHealth;
        if (background != null)
        {
            if (healthPercentage > 0.7f)
            {
                background.color = new Color(0.2f, 0.8f, 0.2f, 0.3f); // 绿色
            }
            else if (healthPercentage > 0.3f)
            {
                background.color = new Color(0.8f, 0.8f, 0.2f, 0.3f); // 黄色
            }
            else
            {
                background.color = new Color(0.8f, 0.2f, 0.2f, 0.3f); // 红色
            }
        }
    }

    private void UpdateEnergyStatus(float energy, float maxEnergy)
    {
        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = energy;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= HandleHealthChanged;
            healthSystem.OnEnergyChanged -= HandleEnergyChanged;
        }
    }

    public void RebindHealthSystem(HealthSystem newHealthSystem)
    {
        // 解除旧事件绑定
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= HandleHealthChanged;
            healthSystem.OnEnergyChanged -= HandleEnergyChanged;
        }

        // 绑定新系统
        healthSystem = newHealthSystem;

        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += HandleHealthChanged;
            healthSystem.OnEnergyChanged += HandleEnergyChanged;

            // 立即更新UI
            UpdateStatus(
                healthSystem.currentHealth,
                healthSystem.maxHealth,
                healthSystem.currentEnergy,
                healthSystem.maxEnergy
            );
        }
    }
}
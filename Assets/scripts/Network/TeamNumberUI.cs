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
    [SerializeField] private GameObject leaderIcon;
    [SerializeField] private Image background;

    private Player player;
    private HealthSystem healthSystem;

    // 修改Initialize方法接受两个参数
    public void Initialize(Player player, HealthSystem healthSystem)
    {
        this.player = player;
        this.healthSystem = healthSystem;
        playerNameText.text = player.NickName;

        UpdateLeaderStatus();

        // 检查是否是队长
        object isLeaderObj;
        bool isLeader = false;
        if (player.CustomProperties.TryGetValue("IsTeamLeader", out isLeaderObj))
        {
            isLeader = (bool)isLeaderObj;
        }
        leaderIcon.gameObject.SetActive(isLeader);

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

   private void UpdateLeaderStatus()
    {
        // 方法1：直接检查是否是Master Client
        bool isLeader = player.IsMasterClient;
        
        // 方法2：检查自定义属性（更可靠）
        if (player.CustomProperties.TryGetValue("IsTeamLeader", out object isLeaderObj))
        {
            isLeader = (bool)isLeaderObj;
        }
        
        leaderIcon.gameObject.SetActive(isLeader);
    }

    private void OnPlayerPropertiesChanged(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer.Equals(player))
        {
            // 如果IsTeamLeader属性变化，更新UI
            if (changedProps.ContainsKey("IsTeamLeader"))
            {
                UpdateLeaderStatus();
            }
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
    public void SetLeaderStatus(bool isLeader)
    {
        if (leaderIcon != null) // 确保有队长图标引用
        {
            leaderIcon.SetActive(isLeader);
        }
    }
}
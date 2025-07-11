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
    [SerializeField] private Image leaderIcon;
    [SerializeField] private Image background;

    private Player player;
    private HealthSystem healthSystem;

    // 修改Initialize方法接受两个参数
    public void Initialize(Player player, HealthSystem healthSystem)
    {
        this.player = player;
        this.healthSystem = healthSystem;
        playerNameText.text = player.NickName;

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
}
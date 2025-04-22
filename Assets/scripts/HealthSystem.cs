using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthSystem : MonoBehaviour
{
    [Header("血量控制")]
    [SerializeField] private float maxHealth = 100f; // 最大血量
    [SerializeField] private float currentHealth;     // 开始前血量
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



    void Start()
    {
        // 初始化血量和蓝量
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
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
    }

    void Update()
    {
        // 自动回复血量
        if (autoRegen && currentHealth < maxHealth)
        {
            currentHealth += regenRate * Time.deltaTime;
            UpdateHealthUI();
        }

        //自动回复蓝量
        if (autoRegenEn && currentEnergy < maxEnergy)
        {
            currentEnergy += regenRateEn * Time.deltaTime;
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
    }

    // 受到伤害
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Debug.Log("角色死亡！");
            //这里可以触?死亡事件

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
        healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {maxHealth}";
    }

    private void UpdateEnergyText()
    {
        energyText.text = $"{Mathf.CeilToInt(currentEnergy)} / {maxEnergy}";
    }
   public bool HasEnoughEnergy(float amount)
    {
        return currentEnergy >= amount;
    }
}



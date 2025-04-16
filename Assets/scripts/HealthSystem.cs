using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("血量?置")]
    [SerializeField] private float maxHealth = 100f; // 最大血量
    [SerializeField] private float currentHealth;     // ?前血量
    [SerializeField] private Slider healthSlider;     // 血?Slider?件

    [Header("自?回复")]
    [SerializeField] private bool autoRegen = false;  // 是否自?回复
    [SerializeField] private float regenRate = 1f;    // 每秒回复量

    void Start()
    {
        // 初始化血量
        currentHealth = maxHealth;

        // 自??取Slider?件（如果未手??值）
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        // ?置Slider范?
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    void Update()
    {
        // 自?回复??
        if (autoRegen && currentHealth < maxHealth)
        {
            currentHealth += regenRate * Time.deltaTime;
            UpdateHealthSlider();
        }

        // ??按?：按H?扣血
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }
    }

    // 受到?害
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthSlider();

        if (currentHealth <= 0)
        {
            Debug.Log("角色死亡！");
            // ?里可以触?死亡事件
        }
    }

    // 治?角色
    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthSlider();
    }

    // 更新血??示
    private void UpdateHealthSlider()
    {
        healthSlider.value = currentHealth;
    }
}
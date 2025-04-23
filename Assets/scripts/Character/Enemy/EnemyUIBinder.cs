using UnityEngine;
using UnityEngine.UI;

public class EnemyUIBinder : MonoBehaviour
{
    [Header("Enemy References")]
    public Enemy enemy; // Reference to the Enemy script
    public SummonMinions summonMinions; // Reference to the SummonMinions script

    [Header("Health UI Elements")]
    public Slider healthSlider;
    public Image healthFillImage;
    public Color healthColor = Color.green; // 统一的生命值颜色

    [Header("Summon Cooldown UI Elements")]
    public Slider summonCooldownSlider;
    public Image summonCooldownFillImage;
    public Color cooldownColor = Color.gray; // 统一的冷却颜色

    private void Start()
    {
        // Initialize health UI
        if (healthSlider != null)
        {
            healthSlider.maxValue = enemy.MaxHealth;
            healthSlider.value = enemy.currentHealth;
        }

        // Initialize cooldown UI
        if (summonCooldownSlider != null && summonMinions != null)
        {
            summonCooldownSlider.maxValue = summonMinions.summonInterval;
            summonCooldownSlider.value = summonMinions.summonTimer;
        }

        UpdateHealthUI();
        UpdateCooldownUI();
    }

    private void Update()
    {
        UpdateHealthUI();
        UpdateCooldownUI();
    }

    private void UpdateHealthUI()
    {
        if (enemy == null) return;

        // Update slider value
        if (healthSlider != null)
        {
            healthSlider.value = enemy.currentHealth;
        }

        // Update color to unified health color
        if (healthFillImage != null)
        {
            healthFillImage.color = healthColor;
        }
    }

    private void UpdateCooldownUI()
    {
        if (summonMinions == null) return;

        // Update slider value
        if (summonCooldownSlider != null)
        {
            summonCooldownSlider.value = summonMinions.summonInterval - summonMinions.summonTimer;
        }

        // Update color to unified cooldown color
        if (summonCooldownFillImage != null)
        {
            summonCooldownFillImage.color = cooldownColor;
        }
    }

    // Optional: Add these if you want to show/hide UI based on game state
    public void ShowUI()
    {
        gameObject.SetActive(true);
    }

    public void HideUI()
    {
        gameObject.SetActive(false);
    }
}
using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("��q?�m")]
    [SerializeField] private float maxHealth = 100f; // �̤j��q
    [SerializeField] private float currentHealth;     // ?�e��q
    [SerializeField] private Slider healthSlider;     // ��?Slider?��

    [Header("��?�^�`")]
    [SerializeField] private bool autoRegen = false;  // �O�_��?�^�`
    [SerializeField] private float regenRate = 1f;    // �C��^�`�q

    void Start()
    {
        // ��l�Ʀ�q
        currentHealth = maxHealth;

        // ��??��Slider?��]�p�G����??�ȡ^
        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        // ?�mSlider�S?
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    void Update()
    {
        // ��?�^�`??
        if (autoRegen && currentHealth < maxHealth)
        {
            currentHealth += regenRate * Time.deltaTime;
            UpdateHealthSlider();
        }

        // ??��?�G��H?����
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }
    }

    // ����?�`
    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateHealthSlider();

        if (currentHealth <= 0)
        {
            Debug.Log("���⦺�`�I");
            // ?���i�H�D?���`�ƥ�
        }
    }

    // �v?����
    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateHealthSlider();
    }

    // ��s��??��
    private void UpdateHealthSlider()
    {
        healthSlider.value = currentHealth;
    }
}
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f; // �������ֵ
    private float currentHealth; // ��ǰ����ֵ

    private void Start()
    {
        // ��ʼ����ǰ����ֵ
        currentHealth = maxHealth;
    }

    // �����Ѫ�ķ���
    public void TakeDamage(float damage)
    {
        currentHealth -= damage; // ��������ֵ
        Debug.Log($"Player takes damage: {damage}. Current health: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die(); // ������������
        }
    }

    // �����ɫ�����ķ���
    private void Die()
    {
        Debug.Log("Player has died!");
        Destroy(this.gameObject);
        // ���������������߼������������һ򲥷���������
    }
}
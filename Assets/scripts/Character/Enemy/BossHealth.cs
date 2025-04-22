using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BossHealth : Enemy
{
    public UnityEvent OnBossDeath;

    public override void Die()
    {
        base.Die();
        OnBossDeath?.Invoke();
        // ����������߼����粥�Ŷ�����������Ʒ�ȣ�
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        // ȷ�����ᳬ���������ֵ
        currentHealth = Mathf.Min(currentHealth, 100f); // �����������ֵΪ100
    }
}
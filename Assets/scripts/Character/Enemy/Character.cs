using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using System;

public class Character : MonoBehaviourPun
{
    [Header("Attributes")]
    [SerializeField] public float MaxHealth;
    [SerializeField] public float currentHealth;
    public bool invulnerable;
    public float invulnerableDuration;
    public bool isEnemy = false;
    public static Action OnEnemyDeath;

    [Header("Events")]
    public UnityEvent OnHurt;
    public UnityEvent OnDie;

    // 添加 Awake 方法
    protected virtual void Awake()
    {
        // 可以在这里添加基础初始化代码
    }

    protected virtual void OnEnable()
    {
        currentHealth = MaxHealth;
    }

    [PunRPC]
    public virtual void TakeDamage(float damage)
    {
        if (invulnerable) return;

        if (photonView.IsMine || (isEnemy && PhotonNetwork.IsMasterClient))
        {
            if (currentHealth - damage > 0f)
            {
                currentHealth -= damage;
                photonView.RPC("RPC_Invulnerable", RpcTarget.All);
                OnHurt?.Invoke();
            }
            else
            {
                photonView.RPC("RPC_Die", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    protected virtual void RPC_Invulnerable()
    {
        StartCoroutine(InvulnerableCoroutine());
    }

    [PunRPC]
    public virtual void RPC_Die()
    {
        currentHealth = 0f;
        OnDie?.Invoke();

        if (isEnemy)
        {
            Debug.Log($"敌人 {gameObject.name} 死亡");
            OnEnemyDeath?.Invoke();
        }
    }

    protected virtual IEnumerator InvulnerableCoroutine()
    {
        invulnerable = true;
        yield return new WaitForSeconds(invulnerableDuration);
        invulnerable = false;
    }

    [PunRPC]
    public void AddHealth(float amount)
    {
        if (photonView.IsMine || (isEnemy && PhotonNetwork.IsMasterClient))
        {
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, MaxHealth);
            photonView.RPC("RPC_UpdateHealth", RpcTarget.All, currentHealth);
        }
    }

    [PunRPC]
    public void RPC_UpdateHealth(float newHealth)
    {
        currentHealth = newHealth;
    }

    public void NetworkTakeDamage(float damage)
    {
        photonView.RPC("TakeDamage", RpcTarget.All, damage);
    }
}
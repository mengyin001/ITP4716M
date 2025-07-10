using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using System;
using TMPro; // 添加TextMeshPro命名空间

public class Character : MonoBehaviourPun
{
    // 新增动画控制参数
    [Header("Animation Parameters")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string dieAnimationTrigger = "isDie";

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

    protected virtual void OnEnable()
    {
        currentHealth = MaxHealth;
    }

    [PunRPC]
    public virtual void TakeDamage(float damage)
    {
        TakeDamageInternal(damage, false);
    }

    [PunRPC]
    public virtual void TakeLaserDamage(float damage)
    {
        TakeDamageInternal(damage, true);
    }

    private void TakeDamageInternal(float damage, bool bypassInvulnerable)
    {
        if (!bypassInvulnerable && invulnerable) return;

        if (photonView.IsMine || (isEnemy && PhotonNetwork.IsMasterClient))
        {
            int damageInt = Mathf.RoundToInt(damage);

            if (currentHealth - damage > 0f)
            {
                currentHealth -= damage;

                if (!bypassInvulnerable)
                {
                    photonView.RPC("RPC_Invulnerable", RpcTarget.All);
                }

                OnHurt?.Invoke();
            }
            else
            {
                photonView.RPC("DieRPC", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    protected virtual void RPC_Invulnerable()
    {
        StartCoroutine(InvulnerableCoroutine());
    }

    [PunRPC]
    public virtual void DieRPC()
    {
        currentHealth = 0f;
        OnDie?.Invoke();

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(dieAnimationTrigger);
        }

        if (isEnemy && photonView.IsMine)
        {
            Debug.Log($"敌人 {gameObject.name} 死亡");
            OnEnemyDeath?.Invoke();

            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(DestroyAfterAnimation());
            }
        }
    }

    private IEnumerator DestroyAfterAnimation()
    {
        yield return new WaitForSeconds(2f);
        PhotonNetwork.Destroy(gameObject);
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
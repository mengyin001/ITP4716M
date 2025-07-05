using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun;
using System;

public class Character : MonoBehaviourPun
{
    // 新增动画控制参数
    [Header("Animation Parameters")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string dieAnimationTrigger = "isDie"; // 匹配您的动画控制器参数

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
    public virtual void TakeDamage(float damage, bool bypassInvulnerable = false)
    {
        // 新增参数：bypassInvulnerable 用于跳过无敌时间检查
        if (!bypassInvulnerable && invulnerable) return;

        if (photonView.IsMine || (isEnemy && PhotonNetwork.IsMasterClient))
        {
            if (currentHealth - damage > 0f)
            {
                currentHealth -= damage;

                // 只有非镭射枪攻击才触发无敌时间
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

        // 新增死亡动画播放逻辑
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
                // 延迟销毁以允许动画播放
                StartCoroutine(DestroyAfterAnimation());
            }
        }
    }

    // 新增协程：等待动画播放后销毁对象
    private IEnumerator DestroyAfterAnimation()
    {
        // 等待动画长度（可调整时间或使用动画事件）
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
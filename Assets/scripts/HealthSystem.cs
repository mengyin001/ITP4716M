using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class HealthSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("音效配置")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioSource audioSource;

    [Header("血量控制")]
    public float maxHealth = 100f;
    public float currentHealth;


    [Header("蓝量控制")]
    public float maxEnergy = 100f;
    public float currentEnergy;

    [Header("自动回复血量")]
    [SerializeField] public bool autoRegen = false;
    [SerializeField] public float regenRate = 1f;

    [Header("自动回复蓝量")]
    [SerializeField] public bool autoRegenEn = false;
    [SerializeField] public float regenRateEn = 1f;

    [Header("死亡效果")]
    [SerializeField] private bool disableControlOnDeath = true;
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private Transform characterBody;
    [SerializeField] private CanvasGroup deathImage;
    [SerializeField] private float deathAnimationTime = 2f;

    [Header("重新开始设置")]
    [SerializeField] private TextMeshProUGUI restartPrompt;
    [SerializeField] private float restartDelay = 1f;
    private bool canRestart = false;
    [SerializeField] private string restartSceneName = "Startup";

    public event System.Action<float, float> OnHealthChanged;
    public event System.Action<float, float> OnEnergyChanged;
    public event System.Action OnPlayerDeath;
    public event System.Action OnRestartAvailable;

    private bool isDead = false;
    public bool IsDead
    {
        get { return isDead; }
        private set { isDead = value; }
    }

    // 用于网络同步的变量
    private float networkCurrentHealth;
    private float networkCurrentEnergy;

    void Start() { 

        // 只在本地客户端初始化玩家状态
        if (photonView.IsMine)
        {
            currentHealth = maxHealth;
            currentEnergy = maxEnergy;
            ForceUpdateUI();
        }

        if (deathImage != null)
        {
            deathImage.alpha = 0;
            deathImage.gameObject.SetActive(false);
        }

        if (restartPrompt != null)
        {
            restartPrompt.gameObject.SetActive(false);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

    }


    void Update()
    {
        // 只在本地客户端处理玩家逻辑
        if (!photonView.IsMine) return;

        // 自动回复血量
        if (autoRegen && currentHealth < maxHealth)
        {
            float newHealth = Mathf.Clamp(currentHealth + regenRate * Time.deltaTime, 0, maxHealth);
            if (newHealth != currentHealth)
            {
                currentHealth = newHealth;
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }

        // 自动回复蓝量
        if (autoRegenEn && currentEnergy < maxEnergy)
        {
            float newEnergy = Mathf.Clamp(currentEnergy + regenRateEn * Time.deltaTime, 0, maxEnergy);
            if (newEnergy != currentEnergy)
            {
                currentEnergy = newEnergy;
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            }
        }

        // 测试按键：按H扣血
        if (Input.GetKeyDown(KeyCode.H) && !isDead)
        {
            photonView.RPC("RPC_TakeDamage", RpcTarget.All, 10f);
        }

        // 测试按键：按J消耗能量
        if (Input.GetKeyDown(KeyCode.J) && !isDead)
        {
            photonView.RPC("RPC_ConsumeEnergy", RpcTarget.All, 5f);
        }

        // 检测重启输入
        if (canRestart && Input.GetMouseButtonDown(0))
        {
            RestartGame();
            canRestart = false;
        }
    }

    // 实现网络同步接口
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 发送数据到其他客户端
            stream.SendNext(currentHealth);
            stream.SendNext(currentEnergy);
            stream.SendNext(isDead);
        }
        else
        {
            // 接收数据
            networkCurrentHealth = (float)stream.ReceiveNext();
            networkCurrentEnergy = (float)stream.ReceiveNext();
            isDead = (bool)stream.ReceiveNext();

            if (!photonView.IsMine)
            {
                currentHealth = networkCurrentHealth;
                currentEnergy = networkCurrentEnergy;
                ForceUpdateUI();
            }
        }
    }

    // 受到伤害 (RPC版本)
    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (isDead) return;

        // 只在本地客户端处理伤害逻辑
        if (photonView.IsMine)
        {
            float previousHealth = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);

            if (currentHealth < previousHealth)
            {
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
                PlayHurtSound();
            }

            if (currentHealth <= 0)
            {
                HandleDeath();
            }
        }
    }

    // 消耗能量 (RPC版本)
    [PunRPC]
    public void RPC_ConsumeEnergy(float amount)
    {
        // 只在本地客户端处理能量消耗
        if (photonView.IsMine && !isDead)
        {
            currentEnergy = Mathf.Clamp(currentEnergy - amount, 0, maxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }
    }

    // 治疗角色
    public void Heal(float amount)
    {
        if (!photonView.IsMine || isDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // 回复蓝量
    public void RestoreEnergy(float amount)
    {
        if (!photonView.IsMine || isDead) return;

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    public bool HasEnoughEnergy(float amount)
    {
        // 只检查本地客户端的能量
        return photonView.IsMine && currentEnergy >= amount;
    }

    private void HandleDeath()
    {
        isDead = true;
        OnPlayerDeath?.Invoke();
        if (disableControlOnDeath)
        {
            var controller = GetComponent<PlayerMovement>();
            if (controller != null) controller.enabled = false;
        }
        StartCoroutine(PlayDeathAnimation());
    }

    private IEnumerator PlayDeathAnimation()
    {
        if (deathImage != null)
        {
            deathImage.gameObject.SetActive(true);
        }

        float elapsed = 0;
        Quaternion startRotation = characterBody.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, 90);

        while (elapsed < deathAnimationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathAnimationTime;

            characterBody.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            if (deathImage != null)
            {
                deathImage.alpha = Mathf.SmoothStep(0, 1, t);
            }

            yield return null;
        }

        characterBody.rotation = targetRotation;

        if (destroyOnDeath)
        {
            yield return new WaitForSeconds(destroyDelay);
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }

        ShowRestartPrompt();
    }

    private void ShowRestartPrompt()
    {
        canRestart = true;
        OnRestartAvailable?.Invoke();
    }

    private void RestartGame()
    {
        SceneLoader.targetScene = restartSceneName;
        SceneManager.LoadScene("LoadingScence");
    }

    private void PlayHurtSound()
    {
        if (audioSource != null && hurtSound != null)
        {
            audioSource.PlayOneShot(hurtSound);
        }
    }

    public void ForceUpdateUI()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

}
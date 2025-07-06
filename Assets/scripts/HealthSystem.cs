using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic; // 新增：用於管理持續效果

public class HealthSystem : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("音效配置")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioSource audioSource;

    [Header("血量控制")]
    public float baseMaxHealth = 100f; // 基礎最大血量
    public float currentHealth;
    private float _maxHealth; // 實際最大血量，會受效果影響
    public float maxHealth // 屬性，用於獲取實際最大血量
    {
        get { return _maxHealth; }
        private set { _maxHealth = value; }
    }

    [Header("蓝量控制")]
    public float baseMaxEnergy = 20f; // 基礎最大藍量
    public float currentEnergy;
    private float _maxEnergy; // 實際最大藍量，會受效果影響
    public float maxEnergy // 屬性，用於獲取實際最大藍量
    {
        get { return _maxEnergy; }
        private set { _maxEnergy = value; }
    }

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
    private float networkMaxHealth; // 新增：同步最大血量
    private float networkMaxEnergy; // 新增：同步最大藍量

    // 持續效果管理
    private List<Coroutine> activeBuffCoroutines = new List<Coroutine>(); // 用於追蹤和停止持續效果的協程

    void Awake()
    {
        // 初始化實際最大值為基礎值
        _maxHealth = baseMaxHealth;
        _maxEnergy = baseMaxEnergy;
    }

    void Start()
    {
        // 只在本地客户端初始化玩家状态
        if (photonView.IsMine)
        {
            currentHealth = maxHealth; // 初始化為當前最大血量
            currentEnergy = maxEnergy; // 初始化為當前最大藍量
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

        // 测试按键：按K回血
        if (Input.GetKeyDown(KeyCode.K) && !isDead)
        {
            Heal(20f);
        }

        // 测试按键：按L回蓝
        if (Input.GetKeyDown(KeyCode.L) && !isDead)
        {
            RestoreEnergy(20f);
        }

        // 测试按键：按U增加最大血量上限
        if (Input.GetKeyDown(KeyCode.U) && !isDead)
        {
            ApplyMaxHealthBuff(30f, 10f); // 增加30血量上限，持續10秒
        }

        // 测试按键：按I增加最大蓝量上限
        if (Input.GetKeyDown(KeyCode.I) && !isDead)
        {
            ApplyMaxEnergyBuff(30f, 10f); // 增加30藍量上限，持續10秒
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
            stream.SendNext(maxHealth); // 新增：同步實際最大血量
            stream.SendNext(maxEnergy); // 新增：同步實際最大藍量
        }
        else
        {
            // 接收数据
            networkCurrentHealth = (float)stream.ReceiveNext();
            networkCurrentEnergy = (float)stream.ReceiveNext();
            isDead = (bool)stream.ReceiveNext();
            networkMaxHealth = (float)stream.ReceiveNext(); // 新增：接收實際最大血量
            networkMaxEnergy = (float)stream.ReceiveNext(); // 新增：接收實際最大藍量

            if (!photonView.IsMine)
            {
                // 非本地客户端直接更新数据
                currentHealth = networkCurrentHealth;
                currentEnergy = networkCurrentEnergy;
                _maxHealth = networkMaxHealth; // 更新非本地客戶端的最大血量
                _maxEnergy = networkMaxEnergy; // 更新非本地客戶端的最大藍量
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

    // 治疗角色 (本地客户端呼叫)
    public void Heal(float amount)
    {
        if (!photonView.IsMine || isDead) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"[HealthSystem] Healed {amount}. Current Health: {currentHealth}/{maxHealth}");
    }

    // 回复蓝量 (本地客户端呼叫)
    public void RestoreEnergy(float amount)
    {
        if (!photonView.IsMine || isDead) return;

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        Debug.Log($"[HealthSystem] Restored {amount} energy. Current Energy: {currentEnergy}/{maxEnergy}");
    }

    public bool HasEnoughEnergy(float amount)
    {
        // 只检查本地客户端的能量
        return photonView.IsMine && currentEnergy >= amount;
    }

    // 應用最大血量上限增加效果 (本地客户端呼叫)
    public void ApplyMaxHealthBuff(float amount, float duration)
    {
        if (!photonView.IsMine || isDead) return;

        // 停止所有現有的血量上限 Buff 協程，避免重複疊加或計時錯誤
        StopAllBuffCoroutinesOfType(EffectType.MaxHealth);

        // 啟動新的 Buff 協程
        Coroutine buffCoroutine = StartCoroutine(MaxHealthBuffRoutine(amount, duration));
        activeBuffCoroutines.Add(buffCoroutine); // 追蹤協程
        Debug.Log($"[HealthSystem] Applied Max Health Buff: +{amount} for {duration} seconds.");
    }

    // 應用最大藍量上限增加效果 (本地客户端呼叫)
    public void ApplyMaxEnergyBuff(float amount, float duration)
    {
        if (!photonView.IsMine || isDead) return;

        // 停止所有現有的藍量上限 Buff 協程
        StopAllBuffCoroutinesOfType(EffectType.MaxEnergy);

        // 啟動新的 Buff 協程
        Coroutine buffCoroutine = StartCoroutine(MaxEnergyBuffRoutine(amount, duration));
        activeBuffCoroutines.Add(buffCoroutine); // 追蹤協程
        Debug.Log($"[HealthSystem] Applied Max Energy Buff: +{amount} for {duration} seconds.");
    }

    // 停止特定類型的 Buff 協程
    private void StopAllBuffCoroutinesOfType(EffectType type)
    {
        // 這裡需要更精確的追蹤方式，例如使用 Dictionary<EffectType, Coroutine>
        // 為了簡化，目前只是停止所有，但如果有多種同類型 Buff，需要更複雜的邏輯
        // 暫時先不實現精確停止，因為目前只允許一種同類型 Buff 同時存在
        // 如果您需要多個同類型 Buff 疊加，則需要重新設計 Buff 管理
        // 目前的設計是，新的 Buff 會覆蓋舊的同類型 Buff

        // 簡單粗暴地停止所有 Buff 協程，然後重新開始
        // 這不是最優解，但對於單一 Buff 類型有效
        // 更好的方法是為每個 Buff 類型維護一個 Coroutine 引用
        // 例如：private Coroutine maxHealthBuffCoroutine;
        // if (maxHealthBuffCoroutine != null) StopCoroutine(maxHealthBuffCoroutine);
    }


    // 最大血量上限 Buff 協程
    // 最大血量上限 Buff 協程
    private IEnumerator MaxHealthBuffRoutine(float amount, float duration)
    {
        float oldMaxHealth = _maxHealth; // 記錄舊的最大血量
        float healthPercentage = currentHealth / oldMaxHealth; // 計算當前血量百分比

        // 增加最大血量
        _maxHealth += amount;

        // 將當前血量按比例提升到新的最大血量
        currentHealth = Mathf.Round(healthPercentage * _maxHealth); // 四捨五入，避免浮點數誤差
        currentHealth = Mathf.Min(currentHealth, _maxHealth); // 確保不超過新上限

        OnHealthChanged?.Invoke(currentHealth, maxHealth); // 更新 UI
        Debug.Log($"[HealthSystem] Max Health Buff: +{amount}. New Max Health: {maxHealth}. Current Health adjusted to: {currentHealth}");

        yield return new WaitForSeconds(duration);

        // 移除最大血量
        _maxHealth -= amount;
        // 確保當前血量不超過移除 Buff 後的最大血量
        currentHealth = Mathf.Min(currentHealth, _maxHealth);
        // 確保最大血量不會低於基礎值
        _maxHealth = Mathf.Max(_maxHealth, baseMaxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth); // 更新 UI
        Debug.Log($"[HealthSystem] Max Health Buff expired. Max Health: {maxHealth}");
    }

    // 最大藍量上限 Buff 協程
    // HealthSystem.cs

    // ... (其他代碼保持不變) ...

    // 最大藍量上限 Buff 協程
    private IEnumerator MaxEnergyBuffRoutine(float amount, float duration)
    {
        float oldMaxEnergy = _maxEnergy; // 記錄舊的最大藍量
        float energyPercentage = currentEnergy / oldMaxEnergy; // 計算當前藍量百分比

        // 增加最大藍量
        _maxEnergy += amount;

        // 將當前藍量按比例提升到新的最大藍量
        currentEnergy = Mathf.Round(energyPercentage * _maxEnergy); // 四捨五入，避免浮點數誤差
        currentEnergy = Mathf.Min(currentEnergy, _maxEnergy); // 確保不超過新上限

        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy); // 更新 UI
        Debug.Log($"[HealthSystem] Max Energy Buff: +{amount}. New Max Energy: {maxEnergy}. Current Energy adjusted to: {currentEnergy}");

        yield return new WaitForSeconds(duration);

        // 移除最大藍量
        _maxEnergy -= amount;
        // 確保當前藍量不超過移除 Buff 後的最大藍量
        currentEnergy = Mathf.Min(currentEnergy, _maxEnergy);
        // 確保最大藍量不會低於基礎值
        _maxEnergy = Mathf.Max(_maxEnergy, baseMaxEnergy);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy); // 更新 UI
        Debug.Log($"[HealthSystem] Max Energy Buff expired. Max Energy: {maxEnergy}");
    }

    private void HandleDeath()
    {
        isDead = true;
        OnPlayerDeath?.Invoke();
        if (disableControlOnDeath)
        {
            var controller = GetComponent<PlayerMovement>(); // 假設 PlayerMovement 是您的玩家控制器
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
        SceneLoader.targetScene = restartSceneName; // 假設您有一個 SceneLoader
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

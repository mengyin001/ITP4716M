using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class TeamMemberUI : MonoBehaviour
{
    [Header("UIԪ��")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider energySlider;
    [SerializeField] private GameObject leaderIcon; // �ӳ�ͼ�����

    [Header("׼��״̬ͼ��")]
    [SerializeField] private Image readyStatusIcon; // ׼��״̬ͼ��
    [SerializeField] private Color readyColor = Color.green; // ׼��״̬��ɫ
    [SerializeField] private Color notReadyColor = Color.gray; // δ׼��״̬��ɫ

    [SerializeField] private Image background;

    private Player player;
    private HealthSystem healthSystem;
    private bool isLeader; // ����Ƿ��Ƕӳ�

    public void Initialize(Player player, HealthSystem healthSystem)
    {
        this.player = player;
        this.healthSystem = healthSystem;
        playerNameText.text = player.NickName;

        UpdateLeaderStatus();

        // ��ʼ����׼��״̬ͼ��
        SetReadyStatus(false);

        // �����HealthSystem�������¼�
        if (healthSystem != null)
        {
            // ��ʼ״̬
            UpdateStatus(
                healthSystem.currentHealth,
                healthSystem.maxHealth,
                healthSystem.currentEnergy,
                healthSystem.maxEnergy
            );

            // ���Ľ���ϵͳ�¼�
            healthSystem.OnHealthChanged += HandleHealthChanged;
            healthSystem.OnEnergyChanged += HandleEnergyChanged;
        }
        else
        {
            // ʹ��Ĭ��ֵ
            UpdateStatus(100f, 100f, 100f, 100f);
        }
    }

    // ���öӳ�״̬
    public void SetLeaderStatus(bool isLeader)
    {
        this.isLeader = isLeader;

        // ���¶ӳ�ͼ��
        if (leaderIcon != null)
        {
            leaderIcon.SetActive(isLeader);
        }

        // �ӳ�����ʾ׼��״̬ͼ��
        if (readyStatusIcon != null)
        {
            readyStatusIcon.gameObject.SetActive(!isLeader);
        }
    }

    // ����׼��״̬
    public void SetReadyStatus(bool isReady)
    {
        // ����Ƕӳ�������ʾ׼��״̬
        if (isLeader || readyStatusIcon == null) return;

        readyStatusIcon.gameObject.SetActive(true);
        readyStatusIcon.color = isReady ? readyColor : notReadyColor;
    }

    // ���¶ӳ�״̬
    private void UpdateLeaderStatus()
    {
        isLeader = false;

        // ����ʹ���Զ��������ж�
        if (player.CustomProperties.TryGetValue("IsTeamLeader", out object isLeaderObj))
        {
            isLeader = (bool)isLeaderObj;
        }
        // ���û���Զ������ԣ�ʹ��IsMasterClient��Ϊ����
        else
        {
            isLeader = player.IsMasterClient;
        }

        // ȷ��leaderIcon��Ϊ��
        if (leaderIcon != null)
        {
            leaderIcon.SetActive(isLeader);
        }

        // �ӳ�����ʾ׼��״̬ͼ��
        if (readyStatusIcon != null)
        {
            readyStatusIcon.gameObject.SetActive(!isLeader);
        }
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        UpdateHealthStatus(currentHealth, maxHealth);
    }

    private void HandleEnergyChanged(float currentEnergy, float maxEnergy)
    {
        UpdateEnergyStatus(currentEnergy, maxEnergy);
    }

    // ��ӽ����ĸ�������UpdateStatus����
    public void UpdateStatus(float health, float maxHealth, float energy, float maxEnergy)
    {
        UpdateHealthStatus(health, maxHealth);
        UpdateEnergyStatus(energy, maxEnergy);
    }

    private void UpdateHealthStatus(float health, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }

        // ����Ѫ���ٷֱȸı䱳����ɫ
        float healthPercentage = health / maxHealth;
        if (background != null)
        {
            if (healthPercentage > 0.7f)
            {
                background.color = new Color(0.2f, 0.8f, 0.2f, 0.3f); // ��ɫ
            }
            else if (healthPercentage > 0.3f)
            {
                background.color = new Color(0.8f, 0.8f, 0.2f, 0.3f); // ��ɫ
            }
            else
            {
                background.color = new Color(0.8f, 0.2f, 0.2f, 0.3f); // ��ɫ
            }
        }
    }

    private void UpdateEnergyStatus(float energy, float maxEnergy)
    {
        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = energy;
        }
    }

    private void OnDestroy()
    {
        // ȡ�������¼�
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= HandleHealthChanged;
            healthSystem.OnEnergyChanged -= HandleEnergyChanged;
        }
    }

    public void RebindHealthSystem(HealthSystem newHealthSystem)
    {
        // ������¼���
        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged -= HandleHealthChanged;
            healthSystem.OnEnergyChanged -= HandleEnergyChanged;
        }

        // ����ϵͳ
        healthSystem = newHealthSystem;

        if (healthSystem != null)
        {
            healthSystem.OnHealthChanged += HandleHealthChanged;
            healthSystem.OnEnergyChanged += HandleEnergyChanged;

            // ��������UI
            UpdateStatus(
                healthSystem.currentHealth,
                healthSystem.maxHealth,
                healthSystem.currentEnergy,
                healthSystem.maxEnergy
            );
        }
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;

public class TeamUIManager : MonoBehaviourPunCallbacks
{
    public static TeamUIManager Instance { get; private set; }

    [Header("����UIԪ��")]
    [SerializeField] private RectTransform teamListPanel;
    [SerializeField] private Transform teamMembersContainer;
    [SerializeField] private GameObject teamMemberPrefab;
    [SerializeField] private TextMeshProUGUI teamStatusText;

    [Header("������ťԪ��")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private Image arrowIcon;
    [SerializeField] private Sprite arrowRightIcon;
    [SerializeField] private Sprite arrowLeftIcon;

    private Dictionary<int, TeamMemberUI> memberUIs = new Dictionary<int, TeamMemberUI>();
    private Dictionary<int, HealthSystem> playerHealthSystems = new Dictionary<int, HealthSystem>();

    private bool isTeamListVisible = true;
    private Vector2 teamListVisiblePosition;
    private Vector2 teamListHiddenPosition;
    private float slideDuration = 0.3f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // ȷ��UIʼ�տɼ�
        gameObject.SetActive(true);
        UpdateTeamUI();
        teamStatusText.text = "Team Player";

        // ��ʼ�� TeamList ��λ�úͰ�ť�¼�
        if (teamListPanel != null)
        {
            teamListVisiblePosition = teamListPanel.anchoredPosition;
            teamListHiddenPosition = new Vector2(
                teamListVisiblePosition.x + teamListPanel.rect.width,
                teamListVisiblePosition.y
            );
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleTeamListVisibility);
        }

        UpdateToggleButtonIcon();
    }

    // ���¶���UI - ��ʾ�������
    public void UpdateTeamUI()
    {
        // ��յ�ǰ��Ա�б�
        foreach (Transform child in teamMembersContainer)
        {
            Destroy(child.gameObject);
        }
        memberUIs.Clear();

        // ȷ�����������ǿɼ���
        teamMembersContainer.gameObject.SetActive(true);

        // ���û����ң���ʾ��ʾ
        if (PhotonNetwork.PlayerList.Length == 0)
        {
            teamStatusText.text = "Team do not have Player";
            return;
        }

        teamStatusText.text = $"Team Player: {PhotonNetwork.PlayerList.Length}";

        // ��ʾ���з������
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            AddTeamMemberUI(player);
        }
    }

    // ��Ӷ����ԱUI
    private void AddTeamMemberUI(Player player)
    {
        GameObject memberUI = Instantiate(teamMemberPrefab, teamMembersContainer);
        TeamMemberUI memberComponent = memberUI.GetComponent<TeamMemberUI>();

        // ���Ի�ȡ��ҵ�HealthSystem
        HealthSystem healthSystem = GetPlayerHealthSystem(player);

        // ����Initialize����
        memberComponent.Initialize(player, healthSystem);

        // ����׼��״̬
        bool isReady = NetworkManager.Instance.IsPlayerReady(player);
        memberComponent.SetReadyStatus(isReady);

        memberUIs.Add(player.ActorNumber, memberComponent);
    }

    // ��ȡ��ҵ�HealthSystem
    private HealthSystem GetPlayerHealthSystem(Player player)
    {
        // ����Ѿ������ˣ�ֱ�ӷ���
        if (playerHealthSystems.ContainsKey(player.ActorNumber))
        {
            return playerHealthSystems[player.ActorNumber];
        }

        // ������Ҷ����HealthSystem���
        GameObject playerObj = FindPlayerObject(player);
        if (playerObj != null)
        {
            HealthSystem healthSystem = playerObj.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                // ������
                playerHealthSystems[player.ActorNumber] = healthSystem;
                return healthSystem;
            }
        }

        return null;
    }

    // �ڳ����в�����Ҷ���
    private GameObject FindPlayerObject(Player player)
    {
        // ����������Ҷ���
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject obj in playerObjects)
        {
            PhotonView pv = obj.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == player.ActorNumber)
            {
                return obj;
            }
        }
        return null;
    }

    // �������׼��״̬
    public void SetPlayerReadyStatus(int actorNumber, bool isReady)
    {
        if (memberUIs.TryGetValue(actorNumber, out TeamMemberUI memberUI))
        {
            memberUI.SetReadyStatus(isReady);
        }
    }

    // �л� TeamList �Ŀɼ���
    public void ToggleTeamListVisibility()
    {
        isTeamListVisible = !isTeamListVisible;
        Vector2 targetPosition = isTeamListVisible ? teamListVisiblePosition : teamListHiddenPosition;

        StartCoroutine(AnimateTeamList(targetPosition));
        UpdateToggleButtonIcon();
    }

    // TeamList ��������Э��
    private IEnumerator AnimateTeamList(Vector2 targetPosition)
    {
        float elapsedTime = 0;
        Vector2 startingPosition = teamListPanel.anchoredPosition;

        while (elapsedTime < slideDuration)
        {
            teamListPanel.anchoredPosition = Vector2.Lerp(
                startingPosition,
                targetPosition,
                (elapsedTime / slideDuration)
            );
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        teamListPanel.anchoredPosition = targetPosition;
    }

    // ���°�ťͼ��
    private void UpdateToggleButtonIcon()
    {
        if (arrowIcon != null)
        {
            arrowIcon.sprite = isTeamListVisible ? arrowLeftIcon : arrowRightIcon;
        }
    }

    // Photon�ص�����Ҽ��뷿��
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateTeamUI();
    }

    // Photon�ص�������뿪����
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // �ӻ������Ƴ�
        if (playerHealthSystems.ContainsKey(otherPlayer.ActorNumber))
        {
            playerHealthSystems.Remove(otherPlayer.ActorNumber);
        }

        UpdateTeamUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        // ����Ƿ��Ƕӳ�״̬����
        if (changedProps.ContainsKey("IsTeamLeader") && memberUIs.ContainsKey(targetPlayer.ActorNumber))
        {
            bool isLeader = (bool)changedProps["IsTeamLeader"];
            memberUIs[targetPlayer.ActorNumber].SetLeaderStatus(isLeader);

            // ͬʱ����׼��״̬��ʾ
            bool isReady = NetworkManager.Instance.IsPlayerReady(targetPlayer);
            memberUIs[targetPlayer.ActorNumber].SetReadyStatus(isReady);
        }

        // ���׼��״̬����
        if (changedProps.ContainsKey(NetworkManager.PLAYER_READY_KEY) &&
            memberUIs.ContainsKey(targetPlayer.ActorNumber))
        {
            bool isReady = (bool)changedProps[NetworkManager.PLAYER_READY_KEY];
            memberUIs[targetPlayer.ActorNumber].SetReadyStatus(isReady);
        }
    }

    // ����������ʱ�������
    public void OnSceneLoaded()
    {
        playerHealthSystems.Clear();
        UpdateTeamUI();
    }

    public void HandlePlayerCreated(GameObject playerObject)
    {
        PhotonView pv = playerObject.GetComponent<PhotonView>();
        if (pv != null && pv.Owner != null)
        {
            int actorNumber = pv.Owner.ActorNumber;

            // ���½���ϵͳ����
            HealthSystem healthSystem = playerObject.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                playerHealthSystems[actorNumber] = healthSystem;
            }

            // ����UI��
            if (memberUIs.ContainsKey(actorNumber))
            {
                memberUIs[actorNumber].RebindHealthSystem(healthSystem);
            }
        }
    }

    public void UpdatePlayerStatus(int actorNumber, float health, float maxHealth, float energy, float maxEnergy)
    {
        if (memberUIs.ContainsKey(actorNumber))
        {
            memberUIs[actorNumber].UpdateStatus(health, maxHealth, energy, maxEnergy);
        }
    }
}
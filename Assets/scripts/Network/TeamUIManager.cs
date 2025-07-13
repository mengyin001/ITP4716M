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

    [Header("队伍UI元素")]
    [SerializeField] private RectTransform teamListPanel;
    [SerializeField] private Transform teamMembersContainer;
    [SerializeField] private GameObject teamMemberPrefab;
    [SerializeField] private TextMeshProUGUI teamStatusText;

    [Header("滑动按钮元素")]
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
        // 确保UI始终可见
        gameObject.SetActive(true);
        UpdateTeamUI();
        teamStatusText.text = "Team Player";

        // 初始化 TeamList 的位置和按钮事件
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

    // 更新队伍UI - 显示所有玩家
    public void UpdateTeamUI()
    {
        // 清空当前成员列表
        foreach (Transform child in teamMembersContainer)
        {
            Destroy(child.gameObject);
        }
        memberUIs.Clear();

        // 确保容器对象是可见的
        teamMembersContainer.gameObject.SetActive(true);

        // 如果没有玩家，显示提示
        if (PhotonNetwork.PlayerList.Length == 0)
        {
            teamStatusText.text = "Team do not have Player";
            return;
        }

        teamStatusText.text = $"Team Player: {PhotonNetwork.PlayerList.Length}";

        // 显示所有房间玩家
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            AddTeamMemberUI(player);
        }
    }

    // 添加队伍成员UI
    private void AddTeamMemberUI(Player player)
    {
        GameObject memberUI = Instantiate(teamMemberPrefab, teamMembersContainer);
        TeamMemberUI memberComponent = memberUI.GetComponent<TeamMemberUI>();

        // 尝试获取玩家的HealthSystem
        HealthSystem healthSystem = GetPlayerHealthSystem(player);

        // 调用Initialize方法
        memberComponent.Initialize(player, healthSystem);

        // 设置准备状态
        bool isReady = NetworkManager.Instance.IsPlayerReady(player);
        memberComponent.SetReadyStatus(isReady);

        memberUIs.Add(player.ActorNumber, memberComponent);
    }

    // 获取玩家的HealthSystem
    private HealthSystem GetPlayerHealthSystem(Player player)
    {
        // 如果已经缓存了，直接返回
        if (playerHealthSystems.ContainsKey(player.ActorNumber))
        {
            return playerHealthSystems[player.ActorNumber];
        }

        // 查找玩家对象的HealthSystem组件
        GameObject playerObj = FindPlayerObject(player);
        if (playerObj != null)
        {
            HealthSystem healthSystem = playerObj.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                // 缓存结果
                playerHealthSystems[player.ActorNumber] = healthSystem;
                return healthSystem;
            }
        }

        return null;
    }

    // 在场景中查找玩家对象
    private GameObject FindPlayerObject(Player player)
    {
        // 查找所有玩家对象
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

    // 更新玩家准备状态
    public void SetPlayerReadyStatus(int actorNumber, bool isReady)
    {
        if (memberUIs.TryGetValue(actorNumber, out TeamMemberUI memberUI))
        {
            memberUI.SetReadyStatus(isReady);
        }
    }

    // 切换 TeamList 的可见性
    public void ToggleTeamListVisibility()
    {
        isTeamListVisible = !isTeamListVisible;
        Vector2 targetPosition = isTeamListVisible ? teamListVisiblePosition : teamListHiddenPosition;

        StartCoroutine(AnimateTeamList(targetPosition));
        UpdateToggleButtonIcon();
    }

    // TeamList 滑动动画协程
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

    // 更新按钮图标
    private void UpdateToggleButtonIcon()
    {
        if (arrowIcon != null)
        {
            arrowIcon.sprite = isTeamListVisible ? arrowLeftIcon : arrowRightIcon;
        }
    }

    // Photon回调：玩家加入房间
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateTeamUI();
    }

    // Photon回调：玩家离开房间
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // 从缓存中移除
        if (playerHealthSystems.ContainsKey(otherPlayer.ActorNumber))
        {
            playerHealthSystems.Remove(otherPlayer.ActorNumber);
        }

        UpdateTeamUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        // 检查是否是队长状态更新
        if (changedProps.ContainsKey("IsTeamLeader") && memberUIs.ContainsKey(targetPlayer.ActorNumber))
        {
            bool isLeader = (bool)changedProps["IsTeamLeader"];
            memberUIs[targetPlayer.ActorNumber].SetLeaderStatus(isLeader);

            // 同时更新准备状态显示
            bool isReady = NetworkManager.Instance.IsPlayerReady(targetPlayer);
            memberUIs[targetPlayer.ActorNumber].SetReadyStatus(isReady);
        }

        // 检查准备状态更新
        if (changedProps.ContainsKey(NetworkManager.PLAYER_READY_KEY) &&
            memberUIs.ContainsKey(targetPlayer.ActorNumber))
        {
            bool isReady = (bool)changedProps[NetworkManager.PLAYER_READY_KEY];
            memberUIs[targetPlayer.ActorNumber].SetReadyStatus(isReady);
        }
    }

    // 当场景加载时清除缓存
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

            // 更新健康系统缓存
            HealthSystem healthSystem = playerObject.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                playerHealthSystems[actorNumber] = healthSystem;
            }

            // 更新UI绑定
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
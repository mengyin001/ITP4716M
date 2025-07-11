using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class TeamUIManager : MonoBehaviourPunCallbacks
{
    public static TeamUIManager Instance { get; private set; }

    [Header("队伍UI元素")]
    [SerializeField] private RectTransform teamListPanel; // 新增：TeamList 的 RectTransform
    [SerializeField] private Transform teamMembersContainer;
    [SerializeField] private GameObject teamMemberPrefab;
    [SerializeField] private TextMeshProUGUI teamStatusText;

    [Header("滑动按钮元素")]
    [SerializeField] private Button toggleButton; // 新增：控制滑动的按钮
    [SerializeField] private Image arrowIcon; // 新增：按钮上的箭头图标
    [SerializeField] private Sprite arrowRightIcon; // 新增：向右箭头的Sprite
    [SerializeField] private Sprite arrowLeftIcon; // 新增：向左箭头的Sprite

    private Dictionary<int, TeamMemberUI> memberUIs = new Dictionary<int, TeamMemberUI>();
    private Dictionary<int, HealthSystem> playerHealthSystems = new Dictionary<int, HealthSystem>();

    private bool isTeamListVisible = true; // 新增：TeamList 的可见状态
    private Vector2 teamListVisiblePosition; // 新增：TeamList 可见时的位置
    private Vector2 teamListHiddenPosition; // 新增：TeamList 隐藏时的位置
    private float slideDuration = 0.3f; // 新增：滑动动画持续时间

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 确保UI始终可见
        gameObject.SetActive(true);
        UpdateTeamUI();
        teamStatusText.text = "Team Player";

        // 新增：初始化 TeamList 的位置和按钮事件
        if (teamListPanel != null)
        {
            teamListVisiblePosition = teamListPanel.anchoredPosition; // 记录初始可见位置
            // 计算隐藏位置：假设向右滑动隐藏，隐藏位置在可见位置的右侧，宽度等于 TeamListPanel 的宽度
            teamListHiddenPosition = new Vector2(teamListVisiblePosition.x + teamListPanel.rect.width, teamListVisiblePosition.y);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleTeamListVisibility); // 绑定按钮点击事件
        }

        UpdateToggleButtonIcon(); // 初始化按钮图标
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

        teamStatusText.text = $"Team Player :{PhotonNetwork.PlayerList.Length}";

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

    // 更新特定玩家的状态
    public void UpdatePlayerStatus(int actorNumber, float health, float maxHealth, float energy, float maxEnergy)
    {
        if (memberUIs.ContainsKey(actorNumber))
        {
            memberUIs[actorNumber].UpdateStatus(health, maxHealth, energy, maxEnergy);
        }
    }

    // 新增：切换 TeamList 的可见性
    public void ToggleTeamListVisibility()
    {
        isTeamListVisible = !isTeamListVisible;
        Vector2 targetPosition = isTeamListVisible ? teamListVisiblePosition : teamListHiddenPosition;

        // 使用 LeanTween 或 DOTween 等第三方动画库可以实现更平滑的动画
        // 这里使用简单的协程实现动画，需要自行添加 using System.Collections; 到文件顶部
        // 或者直接设置位置，如果不需要动画
        // teamListPanel.anchoredPosition = targetPosition;

        StartCoroutine(AnimateTeamList(targetPosition));
        UpdateToggleButtonIcon();
    }

    // 新增：TeamList 滑动动画协程
    private System.Collections.IEnumerator AnimateTeamList(Vector2 targetPosition)
    {
        float elapsedTime = 0;
        Vector2 startingPosition = teamListPanel.anchoredPosition;

        while (elapsedTime < slideDuration)
        {
            teamListPanel.anchoredPosition = Vector2.Lerp(startingPosition, targetPosition, (elapsedTime / slideDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        teamListPanel.anchoredPosition = targetPosition; // 确保最终位置精确
    }

    // 新增：更新按钮图标
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

    // 当场景加载时清除缓存
    public void OnSceneLoaded()
    {
        playerHealthSystems.Clear();
        UpdateTeamUI();
    }
}



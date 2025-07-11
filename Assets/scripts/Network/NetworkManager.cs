using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Settings")]
    public int maxPlayers = 4;
    public string loadingSceneName = "LoadingScene";
    public string roomSceneName = "SafeHouse";

    public static NetworkManager Instance;
    private bool isSwitchingScene = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

      public override void OnEnable()
    {
        // 首先，調用父類的 OnEnable 是個好習慣
        base.OnEnable();
        // 訂閱 SceneManager.sceneLoaded 事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 【核心修正 2】: 使用 OnDisable 取消訂閱，防止內存洩漏
    public override void OnDisable()
    {
        // 同樣，調用父類的 OnDisable
        base.OnDisable();
        // 取消訂閱 SceneManager.sceneLoaded 事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Server");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    // 提供给UI按钮调用的加入房间方法
    public void JoinOrCreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            EmptyRoomTtl = 1000
        };

        PhotonNetwork.JoinOrCreateRoom("GameRoom", roomOptions, TypedLobby.Default);
    }
    public override void OnJoinedRoom()
    {
        Debug.Log($"Successfully joined room: {PhotonNetwork.CurrentRoom.Name}.");

        // 设置当前玩家的队长状态
        UpdatePlayerLeaderStatus();

        // 如果是 Master Client，加载场景
        if (PhotonNetwork.IsMasterClient && !isSwitchingScene)
        {
            isSwitchingScene = true;
            SceneLoader.targetScene = roomSceneName;
            PhotonNetwork.LoadLevel(loadingSceneName);
        }
    }
    private void UpdatePlayerLeaderStatus()
    {
        bool isLeader = PhotonNetwork.LocalPlayer.IsMasterClient;
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "IsTeamLeader", isLeader }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"Master Client switched to {newMasterClient.NickName}");

        // 更新所有玩家的队长状态
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            bool isLeader = player.Equals(newMasterClient);
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "IsTeamLeader", isLeader }
            };
            player.SetCustomProperties(props);
        }
    }


    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 確保這個邏輯只在 Master Client 進入最終的 RoomScene 時執行一次
        if (scene.name == roomSceneName && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master Client has loaded the RoomScene. Opening the room to public.");

            // 開門迎客！
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;

            // 重置標記
            isSwitchingScene = false;
        }
        else if (scene.name != roomSceneName)
        {
            // 如果加載的不是最終場景（例如返回主菜單），也重置標記
            isSwitchingScene = false;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName} has joined the room.");
        // 在這裡可以更新玩家列表等 UI
    }

    // 提供给加载场景的"准备完成"按钮
    public void OnReadyButtonClicked()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(roomSceneName);
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        // 可选：在这里触发UI更新
    }

    public override void OnLeftLobby()
    {
        Debug.Log("Left Lobby");
    }

    // 添加房间加入失败的处理
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Join room failed: {message}");
        isSwitchingScene = false; // 加入失敗，重置狀態
        // 這裡可以加載回主菜單
        // SceneManager.LoadScene("MainMenuScene");
    }

    // 确保连接断开时清理
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"Disconnected: {cause}");
        // 重连逻辑可以加在这里
    }

    public void CreateRoom(string roomName, int playerCount)
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Not connected to Photon server");
            return;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = (byte)playerCount,
            IsVisible = false,
            IsOpen = false,
            EmptyRoomTtl = 1000
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Create room failed: {message}");
        // 可以在这里显示错误提示
    }
    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady || isSwitchingScene) return;

        isSwitchingScene = true;
        SceneLoader.targetScene = roomSceneName; // 先設置好目標
        SceneManager.LoadScene(loadingSceneName); // 手動加載 LoadingScene

        // 在加載 LoadingScene 的同時，異步加入 Photon 房間
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 当玩家属性更新时，通知所有UI更新
        if (changedProps.ContainsKey("IsTeamLeader"))
        {
            // 在实际项目中，这里应该通知UI管理器更新对应玩家的UI
            Debug.Log($"{targetPlayer.NickName} leader status changed to: {changedProps["IsTeamLeader"]}");
        }
    }
}
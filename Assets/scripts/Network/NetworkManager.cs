using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;
using System;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Settings")]
    public int maxPlayers = 4;
    public string loadingSceneName = "LoadingScene";
    public string roomSceneName = "SafeHouse";
    public string gameSceneName = "FirstLevel";

    public static NetworkManager Instance;
    private bool isSwitchingScene = false;
    public const string PLAYER_READY_KEY = "IsReady";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return; // 确保后续代码不会执行
        }

        // 确保在重新连接时不会重复初始化
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
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
        SetPlayerReady(false);
        UpdatePlayerLeaderStatus();

        // 如果是 Master Client，加载场景
        if (PhotonNetwork.IsMasterClient && !isSwitchingScene)
        {
            isSwitchingScene = true;
            SceneLoader.targetScene = roomSceneName;
            PhotonNetwork.LoadLevel(loadingSceneName);
        }
    }

    public void SetPlayerReady(bool isReady)
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { PLAYER_READY_KEY, isReady }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public bool IsPlayerReady(Player player)
    {
        return player.CustomProperties.ContainsKey(PLAYER_READY_KEY) &&
               (bool)player.CustomProperties[PLAYER_READY_KEY];
    }

    public bool AreAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) return false;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            // 房主不需要准备
            if (player.IsMasterClient) continue;

            if (!IsPlayerReady(player)) return false;
        }
        return true;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        if (changedProps.ContainsKey(PLAYER_READY_KEY))
        {
            UIManager.Instance?.UpdateReadyButton();
            TeamUIManager.Instance?.UpdateTeamUI();

            // 通知TeamUIManager更新准备状态
            bool isReady = IsPlayerReady(targetPlayer);
            TeamUIManager.Instance?.SetPlayerReadyStatus(targetPlayer.ActorNumber, isReady);
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
        Debug.Log($"Scene loaded: {scene.name}");
       /* if (scene.name == gameSceneName)
        {
            MovePlayersToScene(scene);
        }*/
        // 确保这个逻辑只在 Master Client 进入最终的 RoomScene 时执行一次
        if (scene.name == roomSceneName && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master Client has loaded the RoomScene. Opening the room to public.");

            // 开门迎客！
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;

            // 重置标记
            isSwitchingScene = false;
        }
        else if (scene.name != roomSceneName)
        {
            // 如果加载的不是最终场景（例如返回主菜单），也重置标记
            isSwitchingScene = false;
        }
        if (this != null && gameObject != null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{newPlayer.NickName} has joined the room.");
        // 在这里可以更新玩家列表等 UI
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
        isSwitchingScene = false; // 加入失败，重置状态
        // 这里可以加载回主菜单
        SceneManager.LoadScene("Startup");
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
        SceneLoader.targetScene = roomSceneName; // 先设置好目标
        SceneManager.LoadScene(loadingSceneName); // 手动加载 LoadingScene

        // 在加载 LoadingScene 的同时，异步加入 Photon 房间
        PhotonNetwork.JoinRoom(roomName);
    }

    public void StartGameForAll()
    {
        if (PhotonNetwork.IsMasterClient && AreAllPlayersReady())
        {
            // 确保所有玩家销毁当前对象
            photonView.RPC("RPC_DestroyPlayerObjects", RpcTarget.All);
            SavePlayerDataBeforeSceneChange();

            // 主客户端加载场景
            PhotonNetwork.LoadLevel(gameSceneName);
        }
    }

    [PunRPC]
    private void RPC_DestroyPlayerObjects()
    {
        // 销毁当前玩家的游戏对象
        if (PhotonNetwork.LocalPlayer.TagObject is GameObject playerObj)
        {
            if (playerObj != null)
            {
                PhotonNetwork.Destroy(playerObj);
            }
            PhotonNetwork.LocalPlayer.TagObject = null;
        }
    }

    public void CloseRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;      // 关闭房间，阻止新玩家加入
            PhotonNetwork.CurrentRoom.IsVisible = false;    // 房间不可见，不会显示在房间列表中
            Debug.Log("房间已关闭，新玩家无法加入");
        }
    }

    public void ReopenRoomIfNeeded()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.CurrentRoom.IsVisible = true;
            Debug.Log("房间已重新开放");
        }
    }
    public void SavePlayerDataBeforeSceneChange()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SavePlayerData", RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_SavePlayerData()
    {
        // 保存金币数据
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.SaveMoney();
        }

        // 保存物品数据
        if (GetComponent<NetworkInventory>() != null)
        {
            GetComponent<NetworkInventory>().SaveInventory();
        }

        Debug.Log("Player data saved before scene change");
    }
}
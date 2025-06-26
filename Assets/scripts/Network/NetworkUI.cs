using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NetworkUI : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public Transform roomListParent;
    public GameObject roomListPrefab;
    public TextMeshProUGUI statusText;
    public GameObject roomPanel;
    public Button actionButton;
    public GameObject createRoom;
    private bool isConnecting = false;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, RoomEntry> roomEntries = new Dictionary<string, RoomEntry>();
    private bool isInRoom = false; 

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            isInRoom = true;
            statusText.text = $"In Room: {PhotonNetwork.CurrentRoom.Name}";
            roomListParent.parent.gameObject.SetActive(false);
            if (roomPanel) roomPanel.SetActive(true);
            if (actionButton) actionButton.gameObject.SetActive(false);
            UpdateRoomPlayerCount();
            return; // 跳过初始连接流程
        }

        statusText.text = "Initializing...";
        if (roomPanel) roomPanel.SetActive(false);
        if (actionButton) actionButton.gameObject.SetActive(false);
        if (createRoom) createRoom.SetActive(false);

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        // 延迟连接确保所有组件初始化完成
        StartCoroutine(DelayedConnect());
    }

    IEnumerator DelayedConnect()
    {
        yield return new WaitForSeconds(0.5f);
        ConnectToPhoton();
    }

    void ConnectToPhoton()
    {
        if (isConnecting || PhotonNetwork.IsConnected) return;

        isConnecting = true;
        statusText.text = "Connecting to server...";

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void OnActionButtonClicked()
    {
        if (createRoom != null)
        {
            createRoom.SetActive(true);
        }
    }

    public override void OnConnectedToMaster()
    {
        isConnecting = false;
        statusText.text = "Connected to server!";

        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(true);
        }

        // 使用更安全的方式加入大厅
        StartCoroutine(SafeJoinLobby());
    }

    IEnumerator SafeJoinLobby()
    {
        // 等待直到客户端准备好加入大厅
        while (PhotonNetwork.NetworkClientState != ClientState.ConnectedToMasterServer)
        {
            yield return null;
        }

        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            // 如果已经在 lobby 中，直接更新 UI
            OnJoinedLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        statusText.text = "In lobby: Waiting for rooms...";
        cachedRoomList.Clear();
        roomEntries.Clear();
        UpdateUI();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // 修复1：正确处理房间更新逻辑
        foreach (RoomInfo info in roomList)
        {
            // 移除无效的房间
            if (info.RemovedFromList || !info.IsOpen || !info.IsVisible)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }

                // 从UI中移除
                if (roomEntries.ContainsKey(info.Name))
                {
                    Destroy(roomEntries[info.Name].gameObject);
                    roomEntries.Remove(info.Name);
                }
            }
            else
            {
                // 添加或更新房间
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList[info.Name] = info;
                }
                else
                {
                    cachedRoomList.Add(info.Name, info);
                }

                // 更新UI条目
                if (roomEntries.ContainsKey(info.Name))
                {
                    roomEntries[info.Name].UpdateRoomInfo(info);
                }
                else
                {
                    CreateRoomUIEntry(info);
                }
            }
        }

        // 更新UI状态
        UpdateUI();
    }

    void CreateRoomUIEntry(RoomInfo room)
    {
        GameObject roomItem = Instantiate(roomListPrefab, roomListParent);
        RoomEntry entry = roomItem.GetComponent<RoomEntry>();

        if (entry != null)
        {
            entry.Initialize(room, this); // 传递NetworkUI引用
            roomEntries[room.Name] = entry;
        }
        else
        {
            // 修复2：确保备用方案也能正确显示房间信息
            TextMeshProUGUI[] texts = roomItem.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = "Name : " + room.Name;
                texts[1].text = "Number : " + $"{room.PlayerCount}/{room.MaxPlayers}";
            }

            Button joinButton = roomItem.GetComponentInChildren<Button>();
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(() => JoinRoom(room.Name));
            }
        }
    }

    // 修复3：添加专门的加入房间方法
    public void JoinRoom(string roomName)
    {
        if (!isInRoom && cachedRoomList.ContainsKey(roomName))
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    void UpdateUI()
    {
        // 清除无效的房间条目
        List<string> keysToRemove = new List<string>();
        foreach (var entry in roomEntries)
        {
            if (entry.Value == null || !cachedRoomList.ContainsKey(entry.Key))
            {
                keysToRemove.Add(entry.Key);
            }
        }

        foreach (string key in keysToRemove)
        {
            if (roomEntries.ContainsKey(key))
            {
                if (roomEntries[key] != null && roomEntries[key].gameObject != null)
                {
                    Destroy(roomEntries[key].gameObject);
                }
                roomEntries.Remove(key);
            }
        }

        // 确保所有缓存的房间都有UI条目
        foreach (var room in cachedRoomList.Values)
        {
            if (!roomEntries.ContainsKey(room.Name))
            {
                CreateRoomUIEntry(room);
            }
        }

        // 更新状态文本
        if (cachedRoomList.Count == 0)
        {
            statusText.text = "No rooms available.";
        }
        else
        {
            statusText.text = $"Found {cachedRoomList.Count} rooms:";
        }
    }

    public override void OnJoinedRoom()
    {
        isInRoom = true; // 标记已加入房间
        statusText.text = $"Joined room: {PhotonNetwork.CurrentRoom.Name}";

        // 修复4：加入房间后隐藏按钮
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(false);
        }

        // 隐藏房间列表，显示房间面板
        roomListParent.parent.gameObject.SetActive(false);
        if (roomPanel) roomPanel.SetActive(true);

        // 更新房间内人数显示
        UpdateRoomPlayerCount();
        LoadGameScene();
    }

    // 更新房间内玩家数量显示
    void UpdateRoomPlayerCount()
    {
        if (roomPanel)
        {
            TextMeshProUGUI countText = roomPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (countText)
            {
                countText.text = $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
            }
        }
    }

    // 玩家加入房间时的回调
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomPlayerCount();
    }

    // 玩家离开房间时的回调
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomPlayerCount();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = $"Join failed: {message}";
    }

    public override void OnLeftRoom()
    {
        isInRoom = false; // 标记离开房间
        statusText.text = "Left room";

        // 清理玩家对象
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLeftRoom();
        }

        // 重新显示按钮
        if (actionButton != null && !actionButton.gameObject.activeSelf)
        {
            actionButton.gameObject.SetActive(true);
        }

        // 显示房间列表，隐藏房间面板
        roomListParent.parent.gameObject.SetActive(true);
        if (roomPanel) roomPanel.SetActive(false);

        // 重新加入大厅
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isInRoom = false;
        statusText.text = $"Disconnected: {cause}";
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLeftRoom();
        }
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(false);
        }
        StartCoroutine(ReconnectAfterDelay(3f));
    }

    IEnumerator ReconnectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ConnectToPhoton();
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"Room created: {PhotonNetwork.CurrentRoom.Name}");
       createRoom.SetActive(false);
        isInRoom = true;

        // 更新UI状态
        statusText.text = $"Room created: {PhotonNetwork.CurrentRoom.Name}";
        roomListParent.parent.gameObject.SetActive(false);

        if (roomPanel) roomPanel.SetActive(true);
        if (actionButton) actionButton.gameObject.SetActive(false);

        // 更新玩家数量显示
        UpdateRoomPlayerCount();
    }
    private void LoadGameScene()
    {
        // 确保只有一个客户端加载场景
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("SafeHouse");
        }
    }
}
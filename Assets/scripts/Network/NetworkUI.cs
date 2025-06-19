using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;

public class NetworkUI : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TextMeshProUGUI connectionStatusText;
    public GameObject roomListPanel;
    public Transform roomListContent;
    public GameObject roomEntryPrefab;
    public TextMeshProUGUI noRoomsText; // 改为文字组件
    public Button createRoomButton; // 创建按钮始终可见
    public GameObject createRoomPanel;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Start()
    {
        // 初始化UI状态
        connectionStatusText.text = "Connecting...";
        roomListPanel.SetActive(false);
        noRoomsText.gameObject.SetActive(false); // 初始隐藏无房间提示
        createRoomPanel.SetActive(false);
        createRoomButton.gameObject.SetActive(true); // 创建按钮始终可见

        // 设置创建房间按钮事件
        createRoomButton.onClick.AddListener(ShowCreateRoomPanel);

        // 清除旧的房间列表
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }
    }

    // 连接到主服务器时的回调
    public override void OnConnectedToMaster()
    {
        connectionStatusText.text = "Server connection successful !";
        roomListPanel.SetActive(true);
        UpdateRoomList();
    }

    // 房间列表更新时的回调
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
        UpdateRoomListUI();
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // 从缓存中移除关闭的房间
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }
                continue;
            }

            // 添加或更新房间信息
            if (cachedRoomList.ContainsKey(info.Name))
            {
                cachedRoomList[info.Name] = info;
            }
            else
            {
                cachedRoomList.Add(info.Name, info);
            }
        }
    }

    private void UpdateRoomList()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby(); // 加入大厅以接收房间列表更新
        }
    }

    private void UpdateRoomListUI()
    {
        // 清除当前UI
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // 根据房间数量更新UI
        if (cachedRoomList.Count == 0)
        {
            noRoomsText.text = "No rooms available currently. Create your own!"; // 设置文字内容
            noRoomsText.gameObject.SetActive(true); // 显示文字提示
        }
        else
        {
            noRoomsText.gameObject.SetActive(false); // 隐藏文字提示

            // 创建新的房间条目
            foreach (KeyValuePair<string, RoomInfo> entry in cachedRoomList)
            {
                GameObject newEntry = Instantiate(roomEntryPrefab, roomListContent);
                RoomEntry entryScript = newEntry.GetComponent<RoomEntry>();

                if (entryScript != null)
                {
                    entryScript.Initialize(entry.Value);
                }
            }
        }
    }

    // 连接失败时的回调
    public override void OnDisconnected(DisconnectCause cause)
    {
        connectionStatusText.text = $"Connection failed: {cause}";
        roomListPanel.SetActive(false);
        noRoomsText.gameObject.SetActive(false);
    }

    // 显示创建房间面板
    private void ShowCreateRoomPanel()
    {
        createRoomPanel.SetActive(true);
    }

}
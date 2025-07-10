using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class RoomListManager : MonoBehaviourPunCallbacks
{
    public static RoomListManager Instance;
    [Header("UI References")]
    public Transform roomListContent;  // ScrollView的Content对象
    public GameObject roomEntryPrefab; // 房间条目Prefab
    public GameObject noRoomsText;    // 无房间时显示的文本

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomEntries = new Dictionary<string, GameObject>();
    void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }


    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"Room list update received. Room count: {roomList.Count}");
        // 1. 更新緩存
        foreach (RoomInfo room in roomList)
        {
            // 房間被移除或關閉
            if (room.RemovedFromList || !room.IsOpen || !room.IsVisible || room.PlayerCount == 0)
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    Debug.Log($"Removing room: {room.Name}");
                    cachedRoomList.Remove(room.Name);
                }
            }
            // 添加或更新房間
            else
            {
                Debug.Log($"Updating or adding room: {room.Name} ({room.PlayerCount}/{room.MaxPlayers})");
                cachedRoomList[room.Name] = room;
            }
        }

        // 2. 更新UI
        UpdateRoomListUI();
    }

  private void UpdateRoomListUI()
{
    // 先清除舊的UI條目
    ClearRoomListUI();

    // 根據緩存創建新的UI條目
    if (cachedRoomList.Count > 0)
    {
        foreach (var roomInfo in cachedRoomList.Values)
        {
            CreateRoomEntry(roomInfo);
        }
    }

    // 根據緩存的最終數量，決定是否顯示 "無房間" 提示
    if (noRoomsText != null)
    {
        noRoomsText.SetActive(cachedRoomList.Count == 0);
    }
}

    private void CreateRoomEntry(RoomInfo room)
    {
        GameObject entry = Instantiate(roomEntryPrefab, roomListContent);
        RoomEntry roomEntry = entry.GetComponent<RoomEntry>();

        if (roomEntry != null)
        {
            roomEntry.Initialize(room);
        }
        else
        {
            Debug.LogError("RoomEntry component missing on prefab!");
        }
        roomEntries[room.Name] = entry;
    }

    private void ClearRoomListUI()
    {
        foreach (var entry in roomEntries.Values)
        {
            Destroy(entry);
        }
        roomEntries.Clear();
    }

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        // 加入房间后关闭房间列表UI
        gameObject.SetActive(false);
    }

    public override void OnLeftLobby()
    {
        cachedRoomList.Clear();
        ClearRoomListUI();
    }

    public override void OnJoinedLobby()
    {
        // 加入大厅时清除缓存并请求房间列表
        cachedRoomList.Clear();
        PhotonNetwork.GetCustomRoomList(TypedLobby.Default, null);
    }
}
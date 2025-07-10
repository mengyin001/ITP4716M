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
    public Transform roomListContent;  // ScrollView��Content����
    public GameObject roomEntryPrefab; // ������ĿPrefab
    public GameObject noRoomsText;    // �޷���ʱ��ʾ���ı�

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Dictionary<string, GameObject> roomEntries = new Dictionary<string, GameObject>();
    void Awake()
    {
        // ������ʼ��
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
        // 1. ���¾���
        foreach (RoomInfo room in roomList)
        {
            // ���g���Ƴ����P�]
            if (room.RemovedFromList || !room.IsOpen || !room.IsVisible || room.PlayerCount == 0)
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    Debug.Log($"Removing room: {room.Name}");
                    cachedRoomList.Remove(room.Name);
                }
            }
            // ��ӻ���·��g
            else
            {
                Debug.Log($"Updating or adding room: {room.Name} ({room.PlayerCount}/{room.MaxPlayers})");
                cachedRoomList[room.Name] = room;
            }
        }

        // 2. ����UI
        UpdateRoomListUI();
    }

  private void UpdateRoomListUI()
{
    // ������f��UI�lĿ
    ClearRoomListUI();

    // �������愓���µ�UI�lĿ
    if (cachedRoomList.Count > 0)
    {
        foreach (var roomInfo in cachedRoomList.Values)
        {
            CreateRoomEntry(roomInfo);
        }
    }

    // �����������K�������Q���Ƿ��@ʾ "�o���g" ��ʾ
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
        // ���뷿���رշ����б�UI
        gameObject.SetActive(false);
    }

    public override void OnLeftLobby()
    {
        cachedRoomList.Clear();
        ClearRoomListUI();
    }

    public override void OnJoinedLobby()
    {
        // �������ʱ������沢���󷿼��б�
        cachedRoomList.Clear();
        PhotonNetwork.GetCustomRoomList(TypedLobby.Default, null);
    }
}
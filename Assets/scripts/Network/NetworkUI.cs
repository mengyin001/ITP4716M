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
            return; // ������ʼ��������
        }

        statusText.text = "Initializing...";
        if (roomPanel) roomPanel.SetActive(false);
        if (actionButton) actionButton.gameObject.SetActive(false);
        if (createRoom) createRoom.SetActive(false);

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        // �ӳ�����ȷ�����������ʼ�����
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

        // ʹ�ø���ȫ�ķ�ʽ�������
        StartCoroutine(SafeJoinLobby());
    }

    IEnumerator SafeJoinLobby()
    {
        // �ȴ�ֱ���ͻ���׼���ü������
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
            // ����Ѿ��� lobby �У�ֱ�Ӹ��� UI
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
        // �޸�1����ȷ����������߼�
        foreach (RoomInfo info in roomList)
        {
            // �Ƴ���Ч�ķ���
            if (info.RemovedFromList || !info.IsOpen || !info.IsVisible)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }

                // ��UI���Ƴ�
                if (roomEntries.ContainsKey(info.Name))
                {
                    Destroy(roomEntries[info.Name].gameObject);
                    roomEntries.Remove(info.Name);
                }
            }
            else
            {
                // ��ӻ���·���
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList[info.Name] = info;
                }
                else
                {
                    cachedRoomList.Add(info.Name, info);
                }

                // ����UI��Ŀ
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

        // ����UI״̬
        UpdateUI();
    }

    void CreateRoomUIEntry(RoomInfo room)
    {
        GameObject roomItem = Instantiate(roomListPrefab, roomListParent);
        RoomEntry entry = roomItem.GetComponent<RoomEntry>();

        if (entry != null)
        {
            entry.Initialize(room, this); // ����NetworkUI����
            roomEntries[room.Name] = entry;
        }
        else
        {
            // �޸�2��ȷ�����÷���Ҳ����ȷ��ʾ������Ϣ
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

    // �޸�3�����ר�ŵļ��뷿�䷽��
    public void JoinRoom(string roomName)
    {
        if (!isInRoom && cachedRoomList.ContainsKey(roomName))
        {
            PhotonNetwork.JoinRoom(roomName);
        }
    }

    void UpdateUI()
    {
        // �����Ч�ķ�����Ŀ
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

        // ȷ�����л���ķ��䶼��UI��Ŀ
        foreach (var room in cachedRoomList.Values)
        {
            if (!roomEntries.ContainsKey(room.Name))
            {
                CreateRoomUIEntry(room);
            }
        }

        // ����״̬�ı�
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
        isInRoom = true; // ����Ѽ��뷿��
        statusText.text = $"Joined room: {PhotonNetwork.CurrentRoom.Name}";

        // �޸�4�����뷿������ذ�ť
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(false);
        }

        // ���ط����б���ʾ�������
        roomListParent.parent.gameObject.SetActive(false);
        if (roomPanel) roomPanel.SetActive(true);

        // ���·�����������ʾ
        UpdateRoomPlayerCount();
        LoadGameScene();
    }

    // ���·��������������ʾ
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

    // ��Ҽ��뷿��ʱ�Ļص�
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomPlayerCount();
    }

    // ����뿪����ʱ�Ļص�
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
        isInRoom = false; // ����뿪����
        statusText.text = "Left room";

        // ������Ҷ���
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLeftRoom();
        }

        // ������ʾ��ť
        if (actionButton != null && !actionButton.gameObject.activeSelf)
        {
            actionButton.gameObject.SetActive(true);
        }

        // ��ʾ�����б����ط������
        roomListParent.parent.gameObject.SetActive(true);
        if (roomPanel) roomPanel.SetActive(false);

        // ���¼������
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

        // ����UI״̬
        statusText.text = $"Room created: {PhotonNetwork.CurrentRoom.Name}";
        roomListParent.parent.gameObject.SetActive(false);

        if (roomPanel) roomPanel.SetActive(true);
        if (actionButton) actionButton.gameObject.SetActive(false);

        // �������������ʾ
        UpdateRoomPlayerCount();
    }
    private void LoadGameScene()
    {
        // ȷ��ֻ��һ���ͻ��˼��س���
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("SafeHouse");
        }
    }
}
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
    public TextMeshProUGUI noRoomsText; // ��Ϊ�������
    public Button createRoomButton; // ������ťʼ�տɼ�
    public GameObject createRoomPanel;

    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    void Start()
    {
        // ��ʼ��UI״̬
        connectionStatusText.text = "Connecting...";
        roomListPanel.SetActive(false);
        noRoomsText.gameObject.SetActive(false); // ��ʼ�����޷�����ʾ
        createRoomPanel.SetActive(false);
        createRoomButton.gameObject.SetActive(true); // ������ťʼ�տɼ�

        // ���ô������䰴ť�¼�
        createRoomButton.onClick.AddListener(ShowCreateRoomPanel);

        // ����ɵķ����б�
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }
    }

    // ���ӵ���������ʱ�Ļص�
    public override void OnConnectedToMaster()
    {
        connectionStatusText.text = "Server connection successful !";
        roomListPanel.SetActive(true);
        UpdateRoomList();
    }

    // �����б����ʱ�Ļص�
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        UpdateCachedRoomList(roomList);
        UpdateRoomListUI();
    }

    private void UpdateCachedRoomList(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            // �ӻ������Ƴ��رյķ���
            if (!info.IsOpen || !info.IsVisible || info.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(info.Name))
                {
                    cachedRoomList.Remove(info.Name);
                }
                continue;
            }

            // ��ӻ���·�����Ϣ
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
            PhotonNetwork.JoinLobby(); // ��������Խ��շ����б����
        }
    }

    private void UpdateRoomListUI()
    {
        // �����ǰUI
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        // ���ݷ�����������UI
        if (cachedRoomList.Count == 0)
        {
            noRoomsText.text = "No rooms available currently. Create your own!"; // ������������
            noRoomsText.gameObject.SetActive(true); // ��ʾ������ʾ
        }
        else
        {
            noRoomsText.gameObject.SetActive(false); // ����������ʾ

            // �����µķ�����Ŀ
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

    // ����ʧ��ʱ�Ļص�
    public override void OnDisconnected(DisconnectCause cause)
    {
        connectionStatusText.text = $"Connection failed: {cause}";
        roomListPanel.SetActive(false);
        noRoomsText.gameObject.SetActive(false);
    }

    // ��ʾ�����������
    private void ShowCreateRoomPanel()
    {
        createRoomPanel.SetActive(true);
    }

}
using UnityEngine;
using Photon.Pun;
using FunkyCode.Lighting2DMaterial;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Setting")]
    public int roomNumber = 4;
    public static NetworkManager Instance;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();         //����Photon������
        Debug.Log("Connect done");
    }
    private void Awake()  //ȷ�����������ʼ�մ���
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            PhotonNetwork.AutomaticallySyncScene = true;
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ���������ͳһ���
    public void CreateRoom(string roomName, int maxPlayers)
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.CreateRoom(roomName, options);
    }

    // ���뷿���ͳһ���
    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
        PhotonNetwork.LoadLevel("SafeHouse");
    }
}

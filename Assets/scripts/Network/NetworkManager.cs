using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Setting")]
    public int roomNumber = 4;
    public static NetworkManager Instance;
    public GameObject player;
    [Header("Space")]
    public Transform spacePoint;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("Welcome to Photon Server");
        PhotonNetwork.JoinOrCreateRoom("Room", new Photon.Realtime.RoomOptions() { MaxPlayers = 4 }, default);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");

        // 检查是否是第一个进入房间的玩家（即房主）
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("You are the Master Client (Host)");
            // 可以在这里设置房主标识或特殊权限
        }

        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        GameObject _player = PhotonNetwork.Instantiate(player.name, spacePoint.position, Quaternion.identity, 0);
    }

    // 当房主变更时调用
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);
        Debug.Log($"New Master Client: {newMasterClient.NickName}");
    }
}
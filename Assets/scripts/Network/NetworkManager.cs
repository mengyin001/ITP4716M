using UnityEngine;
using Photon.Pun;
using FunkyCode.Lighting2DMaterial;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("Setting")]
    public int roomNumber = 4;
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();         //����Photon������
        Debug.Log("Connect done");
    }
}

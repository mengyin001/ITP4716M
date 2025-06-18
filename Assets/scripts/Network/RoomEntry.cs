using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button joinButton;

    private RoomInfo roomInfo;

    public void Initialize(RoomInfo info)
    {
        roomInfo = info;
        roomNameText.text = info.Name;
        playerCountText.text = $"{info.PlayerCount}/{info.MaxPlayers}";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => JoinRoom());
    }

    private void JoinRoom()
    {
        if (roomInfo != null)
        {
            PhotonNetwork.JoinRoom(roomInfo.Name);
        }
    }
}
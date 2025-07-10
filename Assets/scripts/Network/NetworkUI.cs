using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public GameObject nameUI;
    public Button EnterButton;
    public TMP_InputField playerName;
    public GameObject roomListUI;

    public void PlayButton()
    {
        nameUI.SetActive(false);
        PhotonNetwork.NickName = playerName.text;
        roomListUI.SetActive(true);
        Debug.Log("PlayerName:" + playerName.text);
    }

}

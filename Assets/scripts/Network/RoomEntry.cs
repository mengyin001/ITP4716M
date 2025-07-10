using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;

public class RoomEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;
    public Image backgroundImage;

    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    public Color fullColor = new Color(0.4f, 0.1f, 0.1f, 0.7f);
    public Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

    private string _roomName;
    private bool _isFull;

    public void Initialize(RoomInfo roomInfo)
    {
        _roomName = roomInfo.Name;
        _isFull = roomInfo.PlayerCount >= roomInfo.MaxPlayers;

        roomNameText.text = roomInfo.Name;
        playerCountText.text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";

        // 更新UI状态
        joinButton.interactable = !_isFull;
        backgroundImage.color = _isFull ? fullColor : normalColor;

        // 添加点击监听
        joinButton.onClick.AddListener(OnJoinButtonClicked);
    }

    private void OnJoinButtonClicked()
    {
        if (!_isFull)
        {
            NetworkManager.Instance.JoinRoom(_roomName);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isFull)
        {
            backgroundImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        backgroundImage.color = _isFull ? fullColor : normalColor;
    }
}
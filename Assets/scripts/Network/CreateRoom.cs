using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoom : MonoBehaviour
{
    [Header("UI References")]
    public GameObject createRoomPanel;
    public TMP_InputField roomNameInput;
    public Slider maxPlayersSlider; // 使用Slider替代InputField
    public TextMeshProUGUI maxPlayersText; // 显示当前玩家数量的文本
    public Button createButton;
    public Button cancelButton;

    private void Awake()
    {
        // 注册按钮事件
        createButton.onClick.AddListener(CreateNewRoom);
        cancelButton.onClick.AddListener(CancelCreateRoom);

        // 添加Slider值变化监听
        maxPlayersSlider.onValueChanged.AddListener(OnMaxPlayersChanged);
    }

    private void OnEnable()
    {
        // 重置UI状态
        roomNameInput.text = "";
        maxPlayersSlider.value = 4; // 默认4人
        UpdateMaxPlayersText(4); // 更新文本显示
    }

    // Slider值变化时的回调
    private void OnMaxPlayersChanged(float value)
    {
        int players = Mathf.RoundToInt(value);
        UpdateMaxPlayersText(players);
    }

    // 更新玩家数量显示文本
    private void UpdateMaxPlayersText(int players)
    {
        maxPlayersText.text = $"{players}";
    }

    // 创建新房间
    private void CreateNewRoom()
    {
        string roomName = roomNameInput.text;
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("Room name cannot be empty!");
            return;
        }

        // 直接从Slider获取玩家数量
        int maxPlayers = Mathf.RoundToInt(maxPlayersSlider.value);

        // 确保玩家数量在有效范围内 (2-8)
        maxPlayers = Mathf.Clamp(maxPlayers, 1, 4);

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.CreateRoom(roomName, options);
        Debug.Log("CreateDone!");
        createRoomPanel.SetActive(false);
    }

    // 取消创建
    private void CancelCreateRoom()
    {
        createRoomPanel.SetActive(false);
    }
}
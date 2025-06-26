using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoom : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_InputField roomNameInput;
    public Slider maxPlayersSlider;
    public TextMeshProUGUI maxPlayersText;
    public TextMeshProUGUI errorText;
    public Button createButton;
    public Button cancelButton;

    private void Awake()
    {
        createButton.onClick.AddListener(CreateNewRoom);
        cancelButton.onClick.AddListener(CancelCreateRoom);
        maxPlayersSlider.onValueChanged.AddListener(OnMaxPlayersChanged);
    }

    public override void OnEnable()
    {
        // 确保连接到 Photon
        if (!PhotonNetwork.IsConnected)
        {
            ConnectToPhoton();
        }
        else if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        // 重置UI状态
        GenerateAndSetDefaultRoomName();
        maxPlayersSlider.value = 4;
        UpdateMaxPlayersText(4);
        ClearError();

    }

    private void ConnectToPhoton()
    {
        // 确保使用相同的应用版本
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = "1.0";
        PhotonNetwork.ConnectUsingSettings();
    }

   
    // 生成并设置默认房间名称
    private void GenerateAndSetDefaultRoomName()
    {
        string defaultName = $"Room{Random.Range(1000, 9999)}";
        roomNameInput.text = defaultName;
    }

    private void OnMaxPlayersChanged(float value)
    {
        int players = Mathf.RoundToInt(value);
        UpdateMaxPlayersText(players);
    }

    private void UpdateMaxPlayersText(int players)
    {
        maxPlayersText.text = $"{players}";
    }

    // 创建新房间
    private void CreateNewRoom()
    {
        // 检查连接状态
        if (!PhotonNetwork.IsConnected)
        {
            ShowError("Not connected to Photon. Please wait...");
            ConnectToPhoton();
            return;
        }

        if (!PhotonNetwork.InLobby)
        {
            ShowError("Not in lobby. Joining lobby...");
            PhotonNetwork.JoinLobby();
            return;
        }

        string roomName = roomNameInput.text.Trim();

        // 如果名称为空，生成并设置默认名称
        if (string.IsNullOrEmpty(roomName))
        {
            GenerateAndSetDefaultRoomName();
            roomName = roomNameInput.text; // 使用新生成的名称
        }

        // 检查名称是否有效
        if (!IsRoomNameValid(roomName))
        {
            ShowError("Use only letters and numbers.");
            return;
        }

        int maxPlayers = Mathf.RoundToInt(maxPlayersSlider.value);
        maxPlayers = Mathf.Clamp(maxPlayers, 1, 4);

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsOpen = true,
            IsVisible = true,
            PublishUserId = true // 确保用户ID被发布
        };

        // 尝试创建房间

        NetworkManager.Instance.CreateRoom(roomName, maxPlayers);

        // 禁用创建按钮防止重复点击
        createButton.interactable = false;
    }

    // 验证房间名称格式
    private bool IsRoomNameValid(string name)
    {
        // 只允许字母、数字和空格
        foreach (char c in name)
        {
            if (!char.IsLetterOrDigit(c) && c != ' ')
            {
                return false;
            }
        }
        return true;
    }

    // 取消创建
    private void CancelCreateRoom()
    {
        gameObject.SetActive(false);
    }

    // 房间创建失败回调
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Create room failed: {returnCode} - {message}");

        // 启用创建按钮
        createButton.interactable = true;

        // 处理特定错误码
        switch (returnCode)
        {
            case ErrorCode.GameIdAlreadyExists: // 32766
                ShowError("Room name already exists! ");
                break;
            case ErrorCode.GameClosed: // 32758
                ShowError("Cannot create room: Game closed.");
                break;
            case ErrorCode.GameFull: // 32765
                ShowError("Cannot create room: Game full.");
                break;
            default:
                ShowError($"Failed to create room: {message} (Code: {returnCode})");
                break;
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Disconnected from Photon: {cause}");
        ShowError($"Connection lost: {cause}");
    }

    // 显示错误信息
    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);

            // 3秒后自动清除错误信息
            CancelInvoke("ClearError");
            Invoke("ClearError", 3f);
        }

        Debug.LogError(message);
    }

    // 清除错误信息
    private void ClearError()
    {
        if (errorText != null)
        {
            errorText.text = "";
            errorText.gameObject.SetActive(false);
        }
    }
}
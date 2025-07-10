using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class CreateRoomPanel : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField roomNameInput;
    public Slider playerCountSlider;
    public TMP_Text minPlayerText;
    public TMP_Text maxPlayerText;
    public TMP_Text currentPlayerText;
    public Button cancelButton;
    public Button createButton;
    public Button randomNameButton;

    [Header("Settings")]
    public int minPlayers = 1;
    public int maxPlayers = 8;
    public int minRoomId = 1000;
    public int maxRoomId = 9999;
    public string roomPrefix = "Room";

    void Start()
    {
        // 初始化UI
        playerCountSlider.minValue = minPlayers;
        playerCountSlider.maxValue = maxPlayers;
        playerCountSlider.value = Mathf.Clamp(4, minPlayers, maxPlayers); // 默认值
        minPlayerText.text = minPlayers.ToString();
        maxPlayerText.text = maxPlayers.ToString();
        UpdatePlayerCountText();

        // 生成随机房间名
        GenerateRandomRoomName();

        // 添加事件监听
        playerCountSlider.onValueChanged.AddListener(OnPlayerCountChanged);
        cancelButton.onClick.AddListener(ClosePanel);
        createButton.onClick.AddListener(CreateRoom);
        randomNameButton.onClick.AddListener(GenerateRandomRoomName);
    }

    private void OnPlayerCountChanged(float value)
    {
        UpdatePlayerCountText();
    }

    private void UpdatePlayerCountText()
    {
        currentPlayerText.text = Mathf.RoundToInt(playerCountSlider.value).ToString();
    }

    // 生成随机房间名
    private void GenerateRandomRoomName()
    {
        int randomId = Random.Range(minRoomId, maxRoomId + 1);
        roomNameInput.text = $"{roomPrefix}{randomId}";
    }

    private void CreateRoom()
    {
        string roomName = string.IsNullOrWhiteSpace(roomNameInput.text)
            ? $"{roomPrefix}{Random.Range(minRoomId, maxRoomId + 1)}"
            : roomNameInput.text;

        int playerCount = Mathf.RoundToInt(playerCountSlider.value);

        // 调用NetworkManager创建房间
        NetworkManager.Instance.CreateRoom(roomName, playerCount);
        ClosePanel();
    }

    private void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
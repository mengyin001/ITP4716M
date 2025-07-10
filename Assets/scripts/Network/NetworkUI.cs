using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NetworkUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject nameUI;
    public Button enterButton;
    public TMP_InputField playerNameInput;
    public GameObject roomListUI;

    [Header("Hint Text")]
    public TextMeshProUGUI hintText;
    public float hintDisplayTime = 2f;

    [Header("Name Constraints")]
    public int minNameLength = 2;
    public int maxNameLength = 10;

    private Coroutine hintCoroutine;

    void Start()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }

        // ��Ӱ�ť�������������ťʼ�տ��ã�
        enterButton.onClick.AddListener(OnEnterButtonClicked);
    }

    private void OnEnterButtonClicked()
    {
        string currentName = playerNameInput.text.Trim();

        // ֱ����֤������
        if (string.IsNullOrWhiteSpace(currentName))
        {
            ShowHint("Name cannot be empty!");
            return;
        }

        if (currentName.Length < minNameLength)
        {
            ShowHint($"\r\nThe name cannot be less than {minNameLength} characters!");
            return;
        }

        if (currentName.Length > maxNameLength)
        {
            ShowHint($"The name cannot be longer than {maxNameLength} characters!");
            return;
        }

        // ������֤ͨ��
        nameUI.SetActive(false);
        PhotonNetwork.NickName = currentName;
        roomListUI.SetActive(true);
        Debug.Log("PlayerName set to: " + currentName);
    }

    private void ShowHint(string message)
    {
        if (hintText == null) return;

        if (hintCoroutine != null)
        {
            StopCoroutine(hintCoroutine);
        }

        hintText.text = message;
        hintText.gameObject.SetActive(true);
        hintCoroutine = StartCoroutine(HideHintAfterDelay());
    }

    private IEnumerator HideHintAfterDelay()
    {
        yield return new WaitForSeconds(hintDisplayTime);
        hintText.gameObject.SetActive(false);
        hintCoroutine = null;
    }
}
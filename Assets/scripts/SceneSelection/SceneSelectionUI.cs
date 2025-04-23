using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSelectionUI : MonoBehaviour
{
    public GameObject selectionPanel; // ����ѡ�����
    public Button safeHouseButton; // ������Ϣ�Ұ�ť
    public Button firstLevelButton; // ������һ��������ť

    private void Start()
    {
        // �������
        selectionPanel.SetActive(false);

        // ��Ӱ�ť�¼�
        safeHouseButton.onClick.AddListener(() => LoadScene("SafeHouse"));
        firstLevelButton.onClick.AddListener(() => LoadScene("Fristlevel"));
    }

    public void ShowSelectionPanel()
    {
        selectionPanel.SetActive(true); // ��ʾ����ѡ�����
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName); // ����ָ������
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSelectionUI : MonoBehaviour
{
    public GameObject selectionPanel; // 场景选择面板
    public Button safeHouseButton; // 进入休息室按钮
    public Button firstLevelButton; // 进入另一个场景按钮

    private void Start()
    {
        // 隐藏面板
        selectionPanel.SetActive(false);

        // 添加按钮事件
        safeHouseButton.onClick.AddListener(() => LoadScene("SafeHouse"));
        firstLevelButton.onClick.AddListener(() => LoadScene("Fristlevel"));
    }

    public void ShowSelectionPanel()
    {
        selectionPanel.SetActive(true); // 显示场景选择面板
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName); // 加载指定场景
    }
}
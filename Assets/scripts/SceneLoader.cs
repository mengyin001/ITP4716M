using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static string targetScene; // 靜態變量傳遞目標場景名
    public Slider progressBar;
    public TextMeshProUGUI progressText;

    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // 【注意】這裡我們使用 Unity 的 SceneManager 來異步加載
        // 因為場景同步將由 Photon 的 AutomaticallySyncScene 和我們的開/關門邏輯處理
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);

        // 允許場景在加載完成後自動激活
        asyncLoad.allowSceneActivation = true;

        while (!asyncLoad.isDone)
        {
            // Unity 的進度在 0 到 1 之間，0.9 只是個經驗值
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            progressBar.value = progress;
            progressText.text = (progress * 100).ToString("F0") + "%";
            yield return null;
        }
    }
}


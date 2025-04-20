using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static string targetScene; // 静态变量传递目标场景名
    public Slider progressBar;
    public TextMeshProUGUI progressText;

    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        asyncLoad.allowSceneActivation = false; // 禁止自动跳转

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f); // Unity的进度0-0.9
            progressBar.value = progress;
            progressText.text = (progress * 100).ToString("F0") + "%";

            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true; // 手动激活场景
            }

            yield return null;
        }
    }
}
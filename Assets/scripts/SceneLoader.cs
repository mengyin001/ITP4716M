using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static string targetScene; // 静态变量传递目标场景名
    public Slider progressBar;
    public TextMeshProUGUI progressText;

    void Start()
    {
        SaveCurrentSceneData();
        StartCoroutine(LoadSceneAsync());
    }

    void SaveCurrentSceneData()
    {
        // 保存健康数据
        var healthSystem = FindObjectOfType<HealthSystem>();
        if (healthSystem != null)
            PlayerData.Health = healthSystem.currentHealth;

        // 保存金钱数据
        var moneyManager = FindObjectOfType<MoneyManager>();
        if (moneyManager != null)
            PlayerData.Money = moneyManager.GetCurrentMoney();

        // 保存武器数据
        var playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
            PlayerData.CurrentGunIndex = playerMovement.gunNum;
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
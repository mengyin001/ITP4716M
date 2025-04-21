using UnityEngine;
using TMPro;

// 货币管理系统（单例模式）
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance;

    [Header("配置")]
    public MoneyData moneyData;
    public TMP_Text moneyText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadMoney();
            UpdateUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Update()
    {
        UpdateUI();
    }

    // 增加货币
    public void AddMoney(int amount)
    {
        moneyData.itemHeld += amount;
        UpdateUI();
        SaveMoney();
    }

    // 更新UI显示
    public void UpdateUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"{moneyData.itemHeld}";
        }
    }

    // 保存数据
    private void SaveMoney()
    {
        PlayerPrefs.SetInt("PlayerMoney", moneyData.itemHeld);
    }

    // 加载数据
    private void LoadMoney()
    {
        if (PlayerPrefs.HasKey("PlayerMoney"))
        {
            moneyData.itemHeld = PlayerPrefs.GetInt("PlayerMoney");
        }
    }

    private void OnApplicationQuit()
    {
        SaveMoney();
    }
}
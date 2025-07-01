using UnityEngine;
using TMPro;
using Photon.Pun;

public class MoneyManager : MonoBehaviourPun
{
    public static MoneyManager Instance;

    [SerializeField] private MoneyData moneyData; // 在 Inspector 中拖拽赋值
    [SerializeField] private TMP_Text moneyText;  // 在 Inspector 中拖拽赋值

    private void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // 如果需要跨场景保持
        }
        else
        {
            PhotonNetwork.Destroy(gameObject);
            return;
        }

        InitializeMoney();
    }

    private void Start()
    {
        UpdateMoneyDisplay();
    }

    private void InitializeMoney()
    {
        // 可选：初始化金钱（如果 itemHeld 需要默认值）
        if (moneyData.itemHeld < 0)
        {
            moneyData.itemHeld = 0;
        }
    }

    public int GetCurrentMoney()
    {
        return moneyData.itemHeld;
    }

    public void AddMoney(int amount)
    {
        moneyData.itemHeld += amount;
        UpdateMoneyDisplay();
    }

    public bool RemoveMoney(int amount)
    {
        if (CanAfford(amount))
        {
            moneyData.itemHeld -= amount;
            UpdateMoneyDisplay();
            return true;
        }
        return false;
    }

    public bool CanAfford(int amount)
    {
        return moneyData.itemHeld >= amount;
    }

    private void UpdateMoneyDisplay()
    {
        if (moneyText != null)
        {
            moneyText.text = moneyData.itemHeld.ToString();
        }
        else
        {
            Debug.LogWarning("Money Text reference is missing!");
        }
    }

    // 可选：保存/加载功能（使用 PlayerPrefs）
    public void SaveMoney()
    {
        PlayerPrefs.SetInt("PlayerMoney", moneyData.itemHeld);
    }

    public void LoadMoney()
    {
        if (PlayerPrefs.HasKey("PlayerMoney"))
        {
            moneyData.itemHeld = PlayerPrefs.GetInt("PlayerMoney");
            UpdateMoneyDisplay();
        }
    }

    public string GetCurrencyName()
    {
        return moneyData.itemName;
    }
}
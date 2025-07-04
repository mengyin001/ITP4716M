using UnityEngine;
using TMPro;
using Photon.Pun;

public class MoneyManager : MonoBehaviourPun
{
    public static MoneyManager Instance;

    [SerializeField] private MoneyData moneyData; // 在 Inspector 中拖拽赋值

    private void Awake()
    {
        InitializeMoney();
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
        UIManager.Instance.UpdateMoneyUI(moneyData.itemHeld);
    }

    public bool RemoveMoney(int amount)
    {
        if (CanAfford(amount))
        {
            moneyData.itemHeld -= amount;
            UIManager.Instance.UpdateMoneyUI(moneyData.itemHeld);
            return true;
        }
        return false;
    }

    public bool CanAfford(int amount)
    {
        return moneyData.itemHeld >= amount;
    }

    public string GetCurrencyName()
    {
        return moneyData.itemName;
    }
}
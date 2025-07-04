using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMoney", menuName = "Inventory/Money")]
public class MoneyData : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;

    /*[NonSerialized]*/ public int itemHeld; // 使用 NonSerialized 防止数据持久化

    [TextArea] public string description;

    private void OnEnable()
    {
        // 每次加载资源时生成唯一 ID
        if (string.IsNullOrEmpty(itemID))
            itemID = Guid.NewGuid().ToString();
    }
}
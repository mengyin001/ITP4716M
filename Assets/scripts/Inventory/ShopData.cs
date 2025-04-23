using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewShop", menuName = "Inventory/Shop Data")]
public class ShopData : ScriptableObject
{
    [Tooltip("使用的货币类型")]
    public MoneyData currencyType;

    [Tooltip("商品销售列表")]
    public List<ItemData> itemsForSale = new List<ItemData>();

}

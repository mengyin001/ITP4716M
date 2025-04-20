using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Inventory")]
public class Inventory : ScriptableObject
{
    public List<ItemData> itemList = new List<ItemData>();
}

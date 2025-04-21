using UnityEngine;

[CreateAssetMenu(fileName = "New money", menuName = "Inventory/money")]
public class MoneyData : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;
    public int itemHeld = 0;
    [TextArea] public string description;
}

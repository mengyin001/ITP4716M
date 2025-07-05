using UnityEngine;

[CreateAssetMenu(fileName = "NewMoney", menuName = "Inventory/Money")]
public class MoneyData : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;

    [TextArea] public string description;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(itemID))
            itemID = System.Guid.NewGuid().ToString();
    }
}
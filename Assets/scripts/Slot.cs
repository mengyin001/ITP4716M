using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public ItemData slotItem;
    public Image slotImage;
    public TextMeshProUGUI slotNum;
    public TextMeshProUGUI itemInformation;

    public void RefreshSlot()
    {
        if (slotItem != null)
        {
            slotNum.text = slotItem.itemHeld.ToString();
            if (GetComponent<ConsumableItemUI>() != null)
            {
                GetComponent<ConsumableItemUI>().UpdateVisualState();
            }
        }
    }
}
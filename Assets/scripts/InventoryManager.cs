using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    static InventoryManager instance;

    public Inventory myBag;
    public GameObject slotGrid;
    public Slot slotPrefab;


    private void Awake()
    {
        if (instance == null) 
            Destroy(this);
        instance = this;
    }

    public static void CreateNewItem(ItemData item)
    {
        Slot newItem = Instantiate(instance.slotPrefab, instance.slotGrid.transform);

        // 重置缩放和位置
        RectTransform rt = newItem.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;

        newItem.slotItem = item;
        newItem.slotImage.sprite = item.icon;
        newItem.slotNum.text = item.itemHeld.ToString();

        // 强制刷新布局（可选）
        LayoutRebuilder.ForceRebuildLayoutImmediate(instance.slotGrid.GetComponent<RectTransform>());
    }
}

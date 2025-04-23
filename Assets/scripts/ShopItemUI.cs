using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("组件")]
    [SerializeField] private Image background;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button selectButton;

    [Header("状态颜色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.9f, 1f);

    private ItemData _itemData;
    private ShopManager _shopManager;

    public void Initialize(ItemData itemData, ShopManager shopManager)
    {
        _itemData = itemData;
        _shopManager = shopManager;

        itemIcon.sprite = itemData.icon;
        priceText.text = $"Price:  {itemData.price}";

        selectButton.onClick.AddListener(OnSelected);
        Deselect();
    }

    private void OnSelected()
    {
        _shopManager.SelectItem(_itemData, this);
    }

    public void Select()
    {
        background.color = selectedColor;
    }

    public void Deselect()
    {
        background.color = normalColor;
    }
}
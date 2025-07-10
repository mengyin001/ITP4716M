using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Slot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image slotImage;
    public TextMeshProUGUI slotNum;
    // �������޸� 1��: ��ӌ��������������
    public GameObject selectionHighlight;

    [Header("Debug Info")]
    [SerializeField] private string _itemID = "";
    [SerializeField] private int _quantity = 0;

    public string itemID => _itemID;
    public int quantity => _quantity;
    public bool IsEmpty => string.IsNullOrEmpty(_itemID);

    private InventoryManager inventoryManager;
    private int slotIndex;

    public void Initialize(int index, InventoryManager manager)
    {
        slotIndex = index;
        inventoryManager = manager;
        // �_�������ڳ�ʼ���r���[�ص�
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }
        ClearSlot();
    }

    public void SetItem(string id, int qty, ItemData itemData)
    {
        _itemID = id;
        _quantity = qty;

        if (itemData != null && itemData.icon != null)
        {
            slotImage.sprite = itemData.icon;
            slotImage.enabled = true;
        }
        else
        {
            slotImage.sprite = null;
            slotImage.enabled = false;
        }

        UpdateQuantityText();
    }

    private void UpdateQuantityText()
    {
        if (slotNum != null)
        {
            slotNum.text = _quantity > 0 ? _quantity.ToString() : "";
            slotNum.enabled = _quantity > 0;
        }
    }

    public void ClearSlot()
    {
        _itemID = "";
        _quantity = 0;

        slotImage.sprite = null;
        slotImage.enabled = false;

        if (slotNum != null)
        {
            slotNum.text = "";
            slotNum.enabled = false;
        }

        // ��ղ�λ�r��Ҳ�_��ȡ������
        SetSelected(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // �c���¼���߉݋���ֲ�׃��ֻ֪ͨ������
        if (inventoryManager != null)
        {
            inventoryManager.OnSlotClicked(slotIndex);
        }
    }

    // �������޸� 2��: ����@����������
    // �@���������� InventoryManager �{�ã��Á���Ƹ���������@ʾ���[��
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(isSelected);
        }
    }

}

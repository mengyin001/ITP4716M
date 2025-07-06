using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkInventory : MonoBehaviourPunCallbacks, IPunObservable
{
    [System.Serializable]
    public class InventorySlot
    {
        public string itemID;     // ��ƷΨһ��ʶ��
        public int quantity;      // ��Ʒ����

        // �����ݿ��ȡ��Ʒ����
        public ItemData GetItemData(ItemDatabase database)
        {
            return database?.GetItem(itemID);
        }

        // �޸�������޲ι��캯��
        public InventorySlot() { }

        public InventorySlot(string id, int qty)
        {
            itemID = id;
            quantity = qty;
        }
    }

    [Header("Database Reference")]
    public ItemDatabase itemDatabase;  // ��Ʒ���ݿ�����

    [Header("Inventory Data")]
    public List<InventorySlot> items = new List<InventorySlot>(); // ������Ʒ�б�

    // �����仯�¼�
    public event System.Action OnInventoryChanged;

    void Start()
    {
        // ע�ᱳ���仯�¼�
        OnInventoryChanged += () => Debug.Log("Inventory changed");
    }

    // �޸���������Ʒ����
    public void AddItem(string itemID, int amount = 1)
    {
        // ֻ�б���������ҿ����޸�
        if (!photonView.IsMine)
        {
            Debug.LogWarning("Only the owner can modify this inventory");
            return;
        }

        // ��֤��Ʒ������
        if (itemDatabase.GetItem(itemID) == null)
        {
            Debug.LogError($"Item with ID {itemID} does not exist in database");
            return;
        }

        // ����������Ʒ�ѵ�
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemID == itemID)
            {
                items[i].quantity += amount;
                OnInventoryChanged?.Invoke();
                Debug.Log($"Added to stack: {itemID} (+{amount}) Total: {items[i].quantity}");
                return;
            }
        }

        // �������Ʒ�ѵ�
        items.Add(new InventorySlot(itemID, amount));
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory]Added new item: {itemID} x{amount}");
    }

    // �����Ʒ������������ʹ��ItemData��
    public void AddItem(ItemData itemData, int amount = 1)
    {
        if (itemData != null)
        {
            AddItem(itemData.itemID, amount);
        }
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.ForceRefresh();
        }
    }

    // �ӱ����Ƴ���Ʒ
    public bool RemoveItem(string itemID, int amount = 1)
    {
        if (!photonView.IsMine)
        {
            Debug.LogWarning("Only the owner can modify this inventory");
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemID == itemID)
            {
                if (items[i].quantity >= amount)
                {
                    items[i].quantity -= amount;

                    // �������Ϊ0���Ƴ���Ʒ��
                    if (items[i].quantity <= 0)
                    {
                        items.RemoveAt(i);
                        Debug.Log($"Removed entire stack of {itemID}");
                    }
                    else
                    {
                        Debug.Log($"Removed {amount} from stack of {itemID}. Remaining: {items[i].quantity}");
                    }

                    // �����仯�¼�
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                Debug.LogWarning($"Not enough {itemID} to remove ({items[i].quantity} < {amount})");
                return false; // ��������
            }
        }

        Debug.LogWarning($"Item {itemID} not found in inventory");
        return false; // ��Ʒ������
    }

    // �Ƴ���Ʒ������ʹ��ItemData��
    public bool RemoveItem(ItemData itemData, int amount = 1)
    {
        return itemData != null && RemoveItem(itemData.itemID, amount);
    }

    // ��ȡ��Ʒ����
    public int GetItemCount(string itemID)
    {
        foreach (InventorySlot slot in items)
        {
            if (slot.itemID == itemID)
            {
                return slot.quantity;
            }
        }
        return 0;
    }

    // ��ȡ��Ʒ����������ʹ��ItemData��
    public int GetItemCount(ItemData itemData)
    {
        return itemData != null ? GetItemCount(itemData.itemID) : 0;
    }

    // ����Ƿ����㹻��������Ʒ
    public bool HasItem(string itemID, int amount = 1)
    {
        return GetItemCount(itemID) >= amount;
    }

    // �����Ʒ�Ƿ���ڣ�����ʹ��ItemData��
    public bool HasItem(ItemData itemData, int amount = 1)
    {
        return itemData != null && HasItem(itemData.itemID, amount);
    }

    // ʹ����Ʒ������Ʒ��
    public void UseItem(string itemID)
    {
        if (!photonView.IsMine) return;

        ItemData item = itemDatabase.GetItem(itemID);
        if (item == null) return;

        if (RemoveItem(itemID, 1))
        {
            // Ӧ����ƷЧ��
            ApplyItemEffects(item);
        }
    }

    // Ӧ����ƷЧ��
    private void ApplyItemEffects(ItemData item)
    {
        // ����ʵ����ƷЧ��Ӧ���߼�
        Debug.Log($"Using item: {item.itemName}");

        foreach (var effect in item.effects)
        {
            switch (effect.effectType)
            {
                case ItemData.EffectType.Health:
                    Debug.Log($"Restored {effect.effectAmount} health");
                    break;
                case ItemData.EffectType.Energy:
                    Debug.Log($"Restored {effect.effectAmount} energy");
                    break;
                case ItemData.EffectType.Attack:
                    Debug.Log($"Increased attack by {effect.effectAmount}");
                    break;
            }
        }
    }

    // PUN2 ����ͬ��
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // �������ݵ�����
            stream.SendNext(items.Count);

            foreach (InventorySlot slot in items)
            {
                stream.SendNext(slot.itemID);
                stream.SendNext(slot.quantity);
            }
        }
        else
        {
            int count = (int)stream.ReceiveNext();
            List<InventorySlot> newItems = new List<InventorySlot>();

            for (int i = 0; i < count; i++)
            {
                string id = (string)stream.ReceiveNext();
                int qty = (int)stream.ReceiveNext();
                newItems.Add(new InventorySlot(id, qty));
            }

            items = newItems;
            OnInventoryChanged?.Invoke();
            Debug.Log($"Received inventory update: {count} items");
        }
    }

    // �����ã��ڿ���̨��ӡ��������
    [ContextMenu("Print Inventory")]
    public void DebugPrintInventory()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===== Inventory Contents =====");
        sb.AppendLine($"Items: {items.Count}");

        foreach (InventorySlot slot in items)
        {
            sb.AppendLine($"- {slot.itemID} x{slot.quantity}");
        }

        Debug.Log(sb.ToString());
    }
}
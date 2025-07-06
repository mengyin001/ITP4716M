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
        public string itemID;
        public int quantity;

        public ItemData GetItemData(ItemDatabase database)
        {
            return database?.GetItem(itemID);
        }

        public InventorySlot() { }

        public InventorySlot(string id, int qty)
        {
            itemID = id;
            quantity = qty;
        }
    }

    [Header("Database Reference")]
    public ItemDatabase itemDatabase;

    [Header("Inventory Data")]
    public List<InventorySlot> items = new List<InventorySlot>();

    public event System.Action OnInventoryChanged;
    private HealthSystem playerHealthSystem;

    void Start()
    {
        OnInventoryChanged += () => Debug.Log("[NetworkInventory] Inventory changed event fired.");
        playerHealthSystem = GetComponent<HealthSystem>();
        if (playerHealthSystem == null)
        {
            Debug.LogError("[NetworkInventory] HealthSystem component not found on this GameObject!");
        }
    }

    // AddItem �� RemoveItem ���ֲ�׃����������ѽ��� IsMine �z�飬�_��ֻ�Г��������޸Ĕ�����

    public void AddItem(string itemID, int amount = 1)
    {
        if (!photonView.IsMine)
        {
            Debug.LogWarning("[NetworkInventory] AddItem: Only the owner can modify this inventory.");
            return;
        }
        // ... (Add Item ߉݋) ...
        if (itemDatabase.GetItem(itemID) == null)
        {
            Debug.LogError($"[NetworkInventory] AddItem: Item with ID {itemID} does not exist in database.");
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemID == itemID)
            {
                items[i].quantity += amount;
                OnInventoryChanged?.Invoke();
                Debug.Log($"[NetworkInventory] AddItem: Added to stack: {itemID} (+{amount}) Total: {items[i].quantity}");
                return;
            }
        }

        items.Add(new InventorySlot(itemID, amount));
        OnInventoryChanged?.Invoke();
        Debug.Log($"[NetworkInventory] AddItem: Added new item: {itemID} x{amount}");
    }

    public void AddItem(ItemData itemData, int amount = 1)
    {
        if (itemData != null)
        {
            AddItem(itemData.itemID, amount);
        }
        // �@�e����Ҫ�քӺ��� InventoryManager.ForceRefresh()����� OnInventoryChanged ���|�l��
    }

    public bool RemoveItem(string itemID, int amount = 1)
    {
        if (!photonView.IsMine)
        {
            Debug.LogWarning("[NetworkInventory] RemoveItem: Only the owner can modify this inventory.");
            return false;
        }
        // ... (Remove Item ߉݋) ...
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemID == itemID)
            {
                if (items[i].quantity >= amount)
                {
                    items[i].quantity -= amount;

                    if (items[i].quantity <= 0)
                    {
                        items.RemoveAt(i);
                        Debug.Log($"[NetworkInventory] RemoveItem: Removed entire stack of {itemID}");
                    }
                    else
                    {
                        Debug.Log($"[NetworkInventory] RemoveItem: Removed {amount} from stack of {itemID}. Remaining: {items[i].quantity}");
                    }

                    OnInventoryChanged?.Invoke(); // �|�l���� UI ˢ��
                    return true;
                }
                Debug.LogWarning($"[NetworkInventory] RemoveItem: Not enough {itemID} to remove ({items[i].quantity} < {amount})");
                return false;
            }
        }

        Debug.LogWarning($"[NetworkInventory] RemoveItem: Item {itemID} not found in inventory.");
        return false;
    }

    public bool RemoveItem(ItemData itemData, int amount = 1)
    {
        return itemData != null && RemoveItem(itemData.itemID, amount);
    }

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

    public int GetItemCount(ItemData itemData)
    {
        return itemData != null ? GetItemCount(itemData.itemID) : 0;
    }

    public bool HasItem(string itemID, int amount = 1)
    {
        return GetItemCount(itemID) >= amount;
    }

    public bool HasItem(ItemData itemData, int amount = 1)
    {
        return itemData != null && HasItem(itemData.itemID, amount);
    }

    // ��Ʒʹ�÷������F��ֻ�ړ������ψ������ĺ�Ч�����ã��Kͨ�^ RPC ֪ͨ�����͑���
    public void UseItem(string itemID)
    {
        // �_��ֻ�Г����@�� NetworkInventory ����Ҳ��ܰl��ʹ����Ʒ��Ո��
        if (!photonView.IsMine)
        {
            Debug.LogWarning($"[NetworkInventory] UseItem: Attempted to use item {itemID} on a non-owned inventory. Request ignored.");
            return;
        }

        ItemData item = itemDatabase.GetItem(itemID);
        if (item == null)
        {
            Debug.LogError($"[NetworkInventory] UseItem: ItemData not found for ID: {itemID}.");
            return;
        }

        // �Lԇ�Ƴ���Ʒ������ɹ����t����Ч���Kͬ��
        if (RemoveItem(itemID, 1)) // RemoveItem �Ȳ����z�� photonView.IsMine
        {
            // ��Ʒ�ѽ��ړ����߱��صı����б��Ƴ��ˡ�
            // �F�ڣ��҂���Ҫ֪ͨ���п͑��ˣ������������Լ��������@����Ʒ��Ч����
            // ʹ�� RPC_ApplyItemEffects���K��Ŀ���O�Þ� RpcTarget.All��
            // �@�ӣ����п͑��˶����ڸ��Ե� HealthSystem �ϑ���Ч����
            photonView.RPC("RPC_ApplyItemEffects", RpcTarget.All, itemID);
            Debug.Log($"[NetworkInventory] UseItem: Item {itemID} used by owner. RPC sent to all.");
        }
        else
        {
            Debug.LogWarning($"[NetworkInventory] UseItem: Failed to remove item {itemID} from inventory. Effects not applied.");
        }
    }

    // RPC �����������п͑����ϑ�����ƷЧ��
    [PunRPC]
    private void RPC_ApplyItemEffects(string itemID)
    {
        // �@�� RPC �������п͑����ψ��У������l�����Լ�
        // �_�� playerHealthSystem �ѽ������_����
        if (playerHealthSystem == null)
        {
            Debug.LogError($"[NetworkInventory] RPC_ApplyItemEffects: playerHealthSystem is null for item {itemID}.");
            return;
        }

        ItemData item = itemDatabase.GetItem(itemID);
        if (item != null)
        {
            // ����Ч����
            // ע�⣺�@�e����ԓ�ٴΈ��� RemoveItem�������Ʒ�ѽ��ړ������ϱ��Ƴ���
            // �K�ұ���������ͨ�^ OnPhotonSerializeView ͬ��
            ApplyItemEffectsLogic(item); // ����һ���µķ��������Ч������߉݋
            Debug.Log($"[NetworkInventory] RPC_ApplyItemEffects: Item {itemID} effects applied on this client.");
        }
        else
        {
            Debug.LogError($"[NetworkInventory] RPC_ApplyItemEffects: ItemData not found for ID: {itemID}.");
        }
    }

    // ������ƷЧ���Č��H߉݋���F�ڪ��������� UseItem �� RPC ����
    private void ApplyItemEffectsLogic(ItemData item)
    {
        Debug.Log($"[NetworkInventory] Applying effects for item: {item.itemName}");

        if (item.effects == null || item.effects.Length == 0)
        {
            Debug.LogWarning($"[NetworkInventory] Item {item.itemName} has no defined effects.");
            return;
        }

        if (playerHealthSystem == null)
        {
            Debug.LogError("[NetworkInventory] playerHealthSystem is null. Cannot apply effects.");
            return;
        }

        foreach (ItemEffect effect in item.effects)
        {
            if (effect == null) continue;

            switch (effect.effectType)
            {
                case EffectType.Health:
                    playerHealthSystem.Heal(effect.effectAmount);
                    Debug.Log($"[NetworkInventory] Restored {effect.effectAmount} health");
                    break;
                case EffectType.Energy:
                    playerHealthSystem.RestoreEnergy(effect.effectAmount);
                    Debug.Log($"[NetworkInventory] Restored {effect.effectAmount} energy");
                    break;
                case EffectType.MaxHealth:
                    playerHealthSystem.ApplyMaxHealthBuff(effect.effectAmount, effect.duration);
                    Debug.Log($"[NetworkInventory] Increased Max Health by {effect.effectAmount} for {effect.duration} seconds.");
                    break;
                case EffectType.MaxEnergy:
                    playerHealthSystem.ApplyMaxEnergyBuff(effect.effectAmount, effect.duration);
                    Debug.Log($"[NetworkInventory] Increased Max Energy by {effect.effectAmount} for {effect.duration} seconds.");
                    break;
                case EffectType.Attack:
                    Debug.Log($"[NetworkInventory] Increased attack by {effect.effectAmount} (Not implemented in HealthSystem)");
                    break;
                default:
                    Debug.LogWarning($"[NetworkInventory] Unhandled effect type: {effect.effectType} for item {item.itemName}");
                    break;
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // �����߰l���䱳������
            stream.SendNext(items.Count);
            foreach (InventorySlot slot in items)
            {
                stream.SendNext(slot.itemID);
                stream.SendNext(slot.quantity);
            }
        }
        else
        {
            // �����͑��˽��ձ�������
            int count = (int)stream.ReceiveNext();
            List<InventorySlot> newItems = new List<InventorySlot>();
            for (int i = 0; i < count; i++)
            {
                string id = (string)stream.ReceiveNext();
                int qty = (int)stream.ReceiveNext();
                newItems.Add(new InventorySlot(id, qty));
            }
            items = newItems;
            OnInventoryChanged?.Invoke(); // �|�l UI ˢ��
            Debug.Log($"[NetworkInventory] Received inventory update: {count} items");
        }
    }

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

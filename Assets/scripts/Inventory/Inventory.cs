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

    // AddItem 和 RemoveItem 保持不變，因為它們已經有 IsMine 檢查，確保只有擁有者能修改數據。

    public void AddItem(string itemID, int amount = 1)
    {
        if (!photonView.IsMine)
        {
            Debug.LogWarning("[NetworkInventory] AddItem: Only the owner can modify this inventory.");
            return;
        }
        // ... (Add Item 邏輯) ...
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
        // 這裡不需要手動呼叫 InventoryManager.ForceRefresh()，因為 OnInventoryChanged 會觸發它
    }

    public bool RemoveItem(string itemID, int amount = 1)
    {
        if (!photonView.IsMine)
        {
            Debug.LogWarning("[NetworkInventory] RemoveItem: Only the owner can modify this inventory.");
            return false;
        }
        // ... (Remove Item 邏輯) ...
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

                    OnInventoryChanged?.Invoke(); // 觸發本地 UI 刷新
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

    // 物品使用方法：現在只在擁有者上執行消耗和效果應用，並通過 RPC 通知其他客戶端
    public void UseItem(string itemID)
    {
        // 確保只有擁有這個 NetworkInventory 的玩家才能發起使用物品的請求
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

        // 嘗試移除物品。如果成功，則應用效果並同步
        if (RemoveItem(itemID, 1)) // RemoveItem 內部會檢查 photonView.IsMine
        {
            // 物品已經在擁有者本地的背包中被移除了。
            // 現在，我們需要通知所有客戶端（包括擁有者自己）應用這個物品的效果。
            // 使用 RPC_ApplyItemEffects，並將目標設置為 RpcTarget.All。
            // 這樣，所有客戶端都會在各自的 HealthSystem 上應用效果。
            photonView.RPC("RPC_ApplyItemEffects", RpcTarget.All, itemID);
            Debug.Log($"[NetworkInventory] UseItem: Item {itemID} used by owner. RPC sent to all.");
        }
        else
        {
            Debug.LogWarning($"[NetworkInventory] UseItem: Failed to remove item {itemID} from inventory. Effects not applied.");
        }
    }

    // RPC 方法：在所有客戶端上應用物品效果
    [PunRPC]
    private void RPC_ApplyItemEffects(string itemID)
    {
        // 這個 RPC 會在所有客戶端上執行，包括發送者自己
        // 確保 playerHealthSystem 已經被正確引用
        if (playerHealthSystem == null)
        {
            Debug.LogError($"[NetworkInventory] RPC_ApplyItemEffects: playerHealthSystem is null for item {itemID}.");
            return;
        }

        ItemData item = itemDatabase.GetItem(itemID);
        if (item != null)
        {
            // 應用效果。
            // 注意：這裡不應該再次執行 RemoveItem，因為物品已經在擁有者上被移除了
            // 並且背包數據會通過 OnPhotonSerializeView 同步
            ApplyItemEffectsLogic(item); // 呼叫一個新的方法來執行效果應用邏輯
            Debug.Log($"[NetworkInventory] RPC_ApplyItemEffects: Item {itemID} effects applied on this client.");
        }
        else
        {
            Debug.LogError($"[NetworkInventory] RPC_ApplyItemEffects: ItemData not found for ID: {itemID}.");
        }
    }

    // 應用物品效果的實際邏輯，現在獨立出來，供 UseItem 和 RPC 呼叫
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
            // 擁有者發送其背包數據
            stream.SendNext(items.Count);
            foreach (InventorySlot slot in items)
            {
                stream.SendNext(slot.itemID);
                stream.SendNext(slot.quantity);
            }
        }
        else
        {
            // 其他客戶端接收背包數據
            int count = (int)stream.ReceiveNext();
            List<InventorySlot> newItems = new List<InventorySlot>();
            for (int i = 0; i < count; i++)
            {
                string id = (string)stream.ReceiveNext();
                int qty = (int)stream.ReceiveNext();
                newItems.Add(new InventorySlot(id, qty));
            }
            items = newItems;
            OnInventoryChanged?.Invoke(); // 觸發 UI 刷新
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

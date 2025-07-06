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
        public string itemID;     // 物品唯一标识符
        public int quantity;      // 物品数量

        // 从数据库获取物品数据
        public ItemData GetItemData(ItemDatabase database)
        {
            return database?.GetItem(itemID);
        }

        // 修复：添加无参构造函数
        public InventorySlot() { }

        public InventorySlot(string id, int qty)
        {
            itemID = id;
            quantity = qty;
        }
    }

    [Header("Database Reference")]
    public ItemDatabase itemDatabase;  // 物品数据库引用

    [Header("Inventory Data")]
    public List<InventorySlot> items = new List<InventorySlot>(); // 背包物品列表

    // 背包变化事件
    public event System.Action OnInventoryChanged;

    void Start()
    {
        // 注册背包变化事件
        OnInventoryChanged += () => Debug.Log("Inventory changed");
    }

    // 修复后的添加物品方法
    public void AddItem(string itemID, int amount = 1)
    {
        // 只有背包所属玩家可以修改
        if (!photonView.IsMine)
        {
            Debug.LogWarning("Only the owner can modify this inventory");
            return;
        }

        // 验证物品存在性
        if (itemDatabase.GetItem(itemID) == null)
        {
            Debug.LogError($"Item with ID {itemID} does not exist in database");
            return;
        }

        // 查找现有物品堆叠
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

        // 添加新物品堆叠
        items.Add(new InventorySlot(itemID, amount));
        OnInventoryChanged?.Invoke();
        Debug.Log($"[Inventory]Added new item: {itemID} x{amount}");
    }

    // 添加物品到背包（重载使用ItemData）
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

    // 从背包移除物品
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

                    // 如果数量为0，移除物品槽
                    if (items[i].quantity <= 0)
                    {
                        items.RemoveAt(i);
                        Debug.Log($"Removed entire stack of {itemID}");
                    }
                    else
                    {
                        Debug.Log($"Removed {amount} from stack of {itemID}. Remaining: {items[i].quantity}");
                    }

                    // 触发变化事件
                    OnInventoryChanged?.Invoke();
                    return true;
                }
                Debug.LogWarning($"Not enough {itemID} to remove ({items[i].quantity} < {amount})");
                return false; // 数量不足
            }
        }

        Debug.LogWarning($"Item {itemID} not found in inventory");
        return false; // 物品不存在
    }

    // 移除物品（重载使用ItemData）
    public bool RemoveItem(ItemData itemData, int amount = 1)
    {
        return itemData != null && RemoveItem(itemData.itemID, amount);
    }

    // 获取物品数量
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

    // 获取物品数量（重载使用ItemData）
    public int GetItemCount(ItemData itemData)
    {
        return itemData != null ? GetItemCount(itemData.itemID) : 0;
    }

    // 检查是否有足够数量的物品
    public bool HasItem(string itemID, int amount = 1)
    {
        return GetItemCount(itemID) >= amount;
    }

    // 检查物品是否存在（重载使用ItemData）
    public bool HasItem(ItemData itemData, int amount = 1)
    {
        return itemData != null && HasItem(itemData.itemID, amount);
    }

    // 使用物品（消耗品）
    public void UseItem(string itemID)
    {
        if (!photonView.IsMine) return;

        ItemData item = itemDatabase.GetItem(itemID);
        if (item == null) return;

        if (RemoveItem(itemID, 1))
        {
            // 应用物品效果
            ApplyItemEffects(item);
        }
    }

    // 应用物品效果
    private void ApplyItemEffects(ItemData item)
    {
        // 这里实现物品效果应用逻辑
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

    // PUN2 网络同步
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 发送数据到网络
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

    // 调试用：在控制台打印背包内容
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
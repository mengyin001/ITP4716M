using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using static NetworkInventory;

public class InventoryManager : MonoBehaviourPunCallbacks
{
    public static InventoryManager instance;

    [Header("References")]
    public NetworkInventory networkInventory; // 背包數據的引用
    public ItemDatabase itemDatabase;
    public GameObject slotGrid;
    public GameObject slotPrefab;
    public GameObject inventoryPanel;

    [Header("Settings")]
    public int maxSlots = 12;
    public KeyCode toggleKey = KeyCode.Tab;

    private List<Slot> slots = new List<Slot>();
    private bool isPlayerBound = false; // 用於標記是否已成功綁定玩家
    private int currentlySelectedIndex = -1;

    public static InventoryManager Instance { get; internal set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // 【核心】訂閱來自 PlayerSpawner 的靜態事件
        PlayerSpawner.OnLocalPlayerSpawned += BindToLocalPlayer;
        GameLevelPlayerSpawner.OnLocalPlayerSpawned += BindToLocalPlayer;
    }

    private void OnDestroy()
    {
        // 【核心】清理事件訂閱
        PlayerSpawner.OnLocalPlayerSpawned -= BindToLocalPlayer;
        GameLevelPlayerSpawner.OnLocalPlayerSpawned -= BindToLocalPlayer;
        UnbindFromLocalPlayer(); // 確保銷毀時也清理引用
    }

    private void Start()
    {
        // 僅執行一次的UI初始化
        InitializeSlots();
        if (inventoryPanel) inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    // 這個方法是事件處理函數，由 PlayerSpawner 在生成本地玩家後調用
    private void BindToLocalPlayer(HealthSystem playerHealthSystem)
    {
        // 獲取玩家物件上的 NetworkInventory 組件
        NetworkInventory ni = playerHealthSystem.GetComponent<NetworkInventory>();

        if (ni != null)
        {
            // 如果之前綁定過，先解綁
            UnbindFromLocalPlayer();

            networkInventory = ni;
            networkInventory.OnInventoryChanged += HandleInventoryChanged;
            isPlayerBound = true;
            Debug.Log($"[InventoryManager] Successfully bound to local player's NetworkInventory on {playerHealthSystem.gameObject.name}.");

            // 如果此時背包是打開的，立即刷新一次
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                RefreshInventory();
            }
        }
        else
        {
            Debug.LogError($"[InventoryManager] Player object {playerHealthSystem.gameObject.name} was spawned, but it's missing the NetworkInventory component!");
        }
    }

    // 解除與玩家的綁定
    private void UnbindFromLocalPlayer()
    {
        if (networkInventory != null)
        {
            networkInventory.OnInventoryChanged -= HandleInventoryChanged;
            networkInventory = null;
        }
        isPlayerBound = false;
    }

    // 初始化所有UI槽位
    void InitializeSlots()
    {
        foreach (Transform child in slotGrid.transform)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotGrid.transform);
            Slot slot = slotObj.GetComponent<Slot>();
            slot.Initialize(i, this);
            slots.Add(slot);
        }
    }

    // 切換背包UI
    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        bool newState = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(newState);
        OnBagStateChanged(newState);
    }

    public void OnBagStateChanged(bool isOpen)
    {
        if (isOpen)
        {
            RefreshInventory();
        }
        else
        {
            DeselectCurrentSlot(); // 關閉背包時取消選擇
        }
    }

    // 處理來自 NetworkInventory 的數據變更通知
    private void HandleInventoryChanged()
    {
        // 只有當背包UI是打開的時候才刷新，避免不必要的計算
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            RefreshInventory();
        }
    }

    // 刷新整個背包的UI顯示
    public void RefreshInventory()
    {
        if (!isPlayerBound || networkInventory == null || itemDatabase == null)
        {
            Debug.LogWarning("[InventoryManager] Refresh skipped: Player is not bound or required references are missing.");
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < networkInventory.items.Count)
            {
                InventorySlot invSlot = networkInventory.items[i];
                ItemData itemData = itemDatabase.GetItem(invSlot.itemID);
                if (itemData != null)
                {
                    slots[i].SetItem(invSlot.itemID, invSlot.quantity, itemData);
                }
                else
                {
                    slots[i].ClearSlot(); // 如果物品ID無效，則清空槽位
                }
            }
            else
            {
                slots[i].ClearSlot(); // 超出庫存數量的槽位也清空
            }
        }
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        // 獲取被點擊的槽位
        Slot clickedSlot = slots[slotIndex];
        if (clickedSlot.IsEmpty)
        {
            // 如果點擊的是空槽位，取消任何選擇
            DeselectCurrentSlot();
            return;
        }

        if (currentlySelectedIndex == slotIndex)
        {
            // 如果點擊的是已經被選中的槽位，則使用物品
            Debug.Log($"[InventoryManager] Using item in slot {slotIndex}.");
            if (networkInventory != null)
            {
                networkInventory.UseItem(clickedSlot.itemID);
            }

            // 使用後取消選擇
            DeselectCurrentSlot();
        }
        else
        {
            // 如果點擊的是一個新的、未被選中的槽位
            Debug.Log($"[InventoryManager] Selecting slot {slotIndex}.");
            // 先取消上一個選擇
            DeselectCurrentSlot();
            // 再選擇新的槽位
            SelectSlot(slotIndex);
        }
    }

    // 添加兩個新的輔助方法到 InventoryManager.cs 中

    private void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        currentlySelectedIndex = slotIndex;
        slots[slotIndex].SetSelected(true); // 通知 Slot UI 更新外觀
    }

    private void DeselectCurrentSlot()
    {
        if (currentlySelectedIndex != -1)
        {
            slots[currentlySelectedIndex].SetSelected(false); // 通知 Slot UI 更新外觀
            currentlySelectedIndex = -1;
        }
    }

    public void ClearInventory()
    {
        Debug.Log("[InventoryManager] Clearing inventory");

        // 1. 清空UI显示
        foreach (Slot slot in slots)
        {
            slot.ClearSlot();
        }

        // 2. 重置选中状态
        DeselectCurrentSlot();

        // 3. 清空网络数据（如果已绑定玩家）
        if (isPlayerBound && networkInventory != null)
        {
            networkInventory.ClearInventory();
        }
        else
        {
            Debug.LogWarning("[InventoryManager] Cannot clear network inventory - player not bound or networkInventory missing");
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class InventoryManager : MonoBehaviourPunCallbacks
{
    public static InventoryManager instance;

    [Header("References")]
    public NetworkInventory networkInventory;
    public ItemDatabase itemDatabase;
    public GameObject slotGrid;

    [Header("UI Settings")]
    public GameObject slotPrefab;
    public int maxSlots = 12;

    private List<Slot> slots = new List<Slot>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 如果需要跨场景保留
        }
    }
    private void OnEnable()
    {
        if (networkInventory != null)
        {
            // 注册背包变化事件
            networkInventory.OnInventoryChanged += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if (networkInventory != null)
        {
            networkInventory.OnInventoryChanged -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged()
    {
        RefreshInventory();
        Debug.Log("Inventory changed - refreshing UI");
    }

    private void Start()
    {
        FindLocalPlayerInventory();
        InitializeSlots();
        RefreshInventory();
    }

    void InitializeSlots()
    {
        // 清空现有槽位
        foreach (Transform child in slotGrid.transform)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();

        // 创建新槽位
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotGrid.transform);
            Slot slot = slotObj.GetComponent<Slot>();
            slot.Initialize(i, this);
            slots.Add(slot);
        }
    }

    private void FindLocalPlayerInventory()
    {
        if (networkInventory != null) return;

        // 查找本地玩家的NetworkInventory组件
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in photonViews)
        {
            if (view.IsMine)
            {
                networkInventory = view.GetComponent<NetworkInventory>();
                if (networkInventory != null) break;
            }
        }

        if (networkInventory == null)
        {
            Debug.LogError("NetworkInventory not found on local player!");
        }
    }

    // 添加同步刷新
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.LocalPlayer.Equals(newPlayer))
        {
            FindLocalPlayerInventory();
            RefreshInventory();
        }
    }

    public void RefreshInventory()
    {
        // 重置所有槽位
        foreach (Slot slot in slots)
        {
            slot.ClearSlot();
        }

        if (networkInventory == null || itemDatabase == null)
        {
            Debug.LogWarning("Inventory or database not set!");
            return;
        }

        // 使用新索引确保正确填充
        int slotIndex = 0;
        foreach (NetworkInventory.InventorySlot invSlot in networkInventory.items)
        {
            if (slotIndex >= maxSlots)
            {
                Debug.LogWarning("Inventory overflow! Max slots reached.");
                break;
            }

            ItemData itemData = itemDatabase.GetItem(invSlot.itemID);
            if (itemData != null)
            {
                slots[slotIndex].SetItem(invSlot.itemID, invSlot.quantity, itemData);
                slotIndex++;
            }
            else
            {
                Debug.LogWarning($"Item ID {invSlot.itemID} not found in database");
            }
        }

        Debug.Log($"Refreshed inventory. {slotIndex} items displayed");
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        Slot slot = slots[slotIndex];
        if (!slot.IsEmpty && photonView != null && photonView.IsMine) // 添加 null 检查
        {
            // 使用物品（本地玩家）
            networkInventory.UseItem(slot.itemID);
            RefreshInventory();
        }
    }

    // 添加物品到背包
    public void AddItemToInventory(string itemID, int amount = 1)
    {
        if (photonView != null && photonView.IsMine) // 添加 null 检查
        {
            networkInventory.AddItem(itemID, amount);
            RefreshInventory();
        }
    }

    // 网络同步回调
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // 当背包数据更新时刷新UI
        if (targetPlayer == PhotonNetwork.LocalPlayer)
        {
            RefreshInventory();
        }
    }
}
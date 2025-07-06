using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using static NetworkInventory;
using System.Collections;

public class InventoryManager : MonoBehaviourPunCallbacks
{
    public static InventoryManager instance;

    [Header("References")]
    public NetworkInventory networkInventory;
    public ItemDatabase itemDatabase;
    public GameObject slotGrid;
    public GameObject slotPrefab;
    public GameObject inventoryPanel;

    [Header("Settings")]
    public int maxSlots = 12;
    public KeyCode toggleKey = KeyCode.Tab;

    private List<Slot> slots = new List<Slot>();
    private bool isInitialized = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (inventoryPanel) inventoryPanel.SetActive(false);
        InitializeSlots();
        Debug.Log("[InventoryManager] Start method finished.");
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        FindLocalPlayerInventory();
        isInitialized = true;
        Debug.Log($"[InventoryManager] InitializeAfterDelay finished. isInitialized: {isInitialized}. NetworkInventory is null? {networkInventory == null}");
        if (inventoryPanel && inventoryPanel.activeSelf)
        {
            RefreshInventory();
        }
    }
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        Debug.Log("[InventoryManager] OnJoinedRoom called. Attempting to find local player inventory.");
        // 在這裡呼叫查找邏輯，因為此時本地玩家的角色應該已經被實例化了
        StartCoroutine(FindLocalPlayerInventoryRoutine());
    }

    private IEnumerator FindLocalPlayerInventoryRoutine()
    {
        // 等待一小段時間，確保玩家角色完全實例化並準備好
        yield return new WaitForSeconds(0.5f); // 可以根據需要調整這個延遲時間

        FindLocalPlayerInventory(); // 呼叫實際的查找方法

        if (networkInventory != null)
        {
            isInitialized = true;
            Debug.Log("[InventoryManager] InventoryManager successfully initialized with NetworkInventory.");
            if (inventoryPanel && inventoryPanel.activeSelf)
            {
                RefreshInventory();
            }
        }
        else
        {
            Debug.LogError("[InventoryManager] Failed to find NetworkInventory after OnJoinedRoom. Retrying in 1 second...");
            // 如果還是沒找到，可以考慮重試幾次，或者在更晚的時機再次嘗試
            yield return new WaitForSeconds(1f);
            StartCoroutine(FindLocalPlayerInventoryRoutine()); // 再次嘗試
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        bool newState = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(newState);

        if (newState && isInitialized)
        {
            RefreshInventory();
        }
    }

    // InventoryManager.cs
    private void FindLocalPlayerInventory()
    {
        if (networkInventory != null)
        {
            Debug.Log("[InventoryManager] networkInventory already assigned (possibly from a previous retry), skipping FindLocalPlayerInventory.");
            return;
        }

        PhotonView[] allPhotonViews = FindObjectsOfType<PhotonView>();
        Debug.Log($"[InventoryManager] Searching among {allPhotonViews.Length} PhotonViews in the scene.");

        foreach (PhotonView pv in allPhotonViews)
        {
            if (pv.IsMine) // 找到屬於本地玩家的 PhotonView
            {
                NetworkInventory ni = pv.GetComponent<NetworkInventory>();
                if (ni != null)
                {
                    networkInventory = ni;
                    networkInventory.OnInventoryChanged += HandleInventoryChanged;
                    Debug.Log($"[InventoryManager] Found local player's NetworkInventory on GameObject: {pv.gameObject.name} and subscribed to OnInventoryChanged.");
                    return; // 找到後就返回
                }
                else
                {
                    Debug.LogWarning($"[InventoryManager] Found local player's PhotonView on GameObject: {pv.gameObject.name}, but no NetworkInventory component found on it.");
                }
            }
        }

        Debug.LogError("[InventoryManager] Failed to find local player's NetworkInventory in the current search attempt!");
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
            Debug.Log($"Created slot {i}");
        }
    }

    private void HandleInventoryChanged()
    {
        Debug.Log("[InventoryManager] HandleInventoryChanged called.");
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            RefreshInventory();
        }
        else
        {
            Debug.Log("[InventoryManager] Inventory panel is not active, skipping immediate refresh.");
        }
    }

    public void RefreshInventory()
    {
        if (!isInitialized || networkInventory == null || itemDatabase == null)
        {
            Debug.LogWarning("Refresh skipped - not initialized");
            return;
        }

        Debug.Log($"Refreshing inventory with {networkInventory.items.Count} items");

        // 先重置所有槽位
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].ClearSlot();
        }

        // 填充槽位
        for (int i = 0; i < networkInventory.items.Count && i < slots.Count; i++)
        {
            InventorySlot invSlot = networkInventory.items[i];
            ItemData itemData = itemDatabase.GetItem(invSlot.itemID);

            if (itemData != null)
            {
                slots[i].SetItem(invSlot.itemID, invSlot.quantity, itemData);
                Debug.Log($"Slot {i} set: {invSlot.itemID} x{invSlot.quantity}");
            }
            else
            {
                Debug.LogWarning($"Item not found: {invSlot.itemID}");
            }
        }

        // 强制UI更新
        LayoutRebuilder.ForceRebuildLayoutImmediate(slotGrid.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
    }

    public void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        Slot slot = slots[slotIndex];
        if (!slot.IsEmpty && photonView != null && photonView.IsMine)
        {
            networkInventory.UseItem(slot.itemID);
            RefreshInventory();
        }
    }

    public void OnBagStateChanged(bool isOpen)
    {
        if (isOpen)
        {
            Debug.Log("Bag opened - forcing refresh");
            ForceRefresh();
        }
        else
        {
            Debug.Log("Bag closed");
        }
    }

    // 在InventoryManager中添加强制刷新方法
    public void ForceRefresh()
    {
        if (!isInitialized) return;

        StopAllCoroutines();
        StartCoroutine(DelayedForceRefresh());
    }

    private IEnumerator DelayedForceRefresh()
    {
        yield return null; // 等待一帧
        RefreshInventory();
    }

}
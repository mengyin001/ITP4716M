using UnityEngine;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager Instance;
    public HotbarSlot[] hotbarSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Assign hotkeys (1-9)
        for (int i = 0; i < hotbarSlots.Length && i < 9; i++)
        {
            hotbarSlots[i].slotIndex = i;
            hotbarSlots[i].hotkey = KeyCode.Alpha1 + i;
        }
    }
}
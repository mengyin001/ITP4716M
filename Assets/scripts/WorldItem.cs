using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WordItem : MonoBehaviour
{
    public ItemData thisItem;
    public Inventory playerInventory;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            AddNewItem();
            Destroy(gameObject);
        }
    }

    public void AddNewItem()
    {
        InventoryManager.AddItem(thisItem, 1);

    }
}
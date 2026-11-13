
using System.Collections.Generic;
using UnityEngine;

public class InventoryController : MonoBehaviour
{
    public GameObject inventoryPanel;
    public GameObject slotPrefab;
    public int slotCount;
    public GameObject[] itemPrefabs;

    private void Start()
    {
        InitializeInventory();
    }

    private void InitializeInventory()
    {
        for (int i = 0; i < slotCount; i++)
        {
            var initial = itemPrefabs != null && i < itemPrefabs.Length ? itemPrefabs[i] : null;
            InventoryStacking.CreateSlot(slotPrefab, inventoryPanel.transform,
                initial ? new InventoryItem { itemPrefab = initial, amount = 1 } : null);
        }
    }

    public InventoryItem[] GetInventoryItems()
    {
        List<InventoryItem> items = new List<InventoryItem>();
        Slot[] slots = inventoryPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            if (slot.inventoryItem != null &&
                slot.inventoryItem.itemPrefab != null)
            {
                items.Add(slot.inventoryItem);
            }
        }

        return items.ToArray();
    }

    public bool RemoveItem(InventoryItem item)
    {
        return InventoryStacking.RemoveItem(
            inventoryPanel.transform,
            item
        );
    }

    public bool AddItem(InventoryItem item)
    {
        return InventoryStacking.AddItem(
            inventoryPanel.transform,
            item
        );
    }

    public void CleanEmptySlots()
    {
        InventorySlotCleaner.Clean(inventoryPanel.transform);
    }
}

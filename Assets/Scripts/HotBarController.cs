
using System.Collections.Generic;
using UnityEngine;

public class HotbarController : MonoBehaviour
{
    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public int slotCount;

    private void Start()
    {
        CreateSlots();
    }

    private void CreateSlots()
    {
        for (int i = 0; i < slotCount; i++)
        {
            Slot slot = Instantiate(
                slotPrefab,
                hotbarPanel.transform
            ).GetComponent<Slot>();

            slot.currentItem = null;
            slot.inventoryItem = null;

            slot.UpdateQuantityDisplay();
        }
    }

    public InventoryItem[] GetHotbarItems()
    {
        List<InventoryItem> items =
            new List<InventoryItem>();

        Slot[] slots =
            hotbarPanel.GetComponentsInChildren<Slot>(true);

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
            hotbarPanel.transform,
            item
        );
    }

    public bool AddItem(InventoryItem item)
    {
        bool success = InventoryStacking.AddItem(
            hotbarPanel.transform,
            item
        );

        if (success)
        {
            Debug.Log(
                "Item adicionado à hotbar: " +
                item.itemPrefab.name +
                " ×" + item.amount
            );
        }
        else
        {
            Debug.Log("Hotbar sem espaço suficiente.");
        }

        return success;
    }

    public void CleanEmptySlots()
    {
        InventorySlotCleaner.Clean(hotbarPanel.transform);
    }
}

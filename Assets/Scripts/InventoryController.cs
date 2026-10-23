
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
        for (int i = 0; i < slotCount; i++)
        {
            Slot slot = Instantiate(
                slotPrefab,
                inventoryPanel.transform
            ).GetComponent<Slot>();

            if (itemPrefabs != null &&
                i < itemPrefabs.Length &&
                itemPrefabs[i] != null)
            {
                GameObject item = Instantiate(
                    itemPrefabs[i],
                    slot.transform
                );

                RectTransform rect =
                    item.GetComponent<RectTransform>();

                if (rect != null)
                    rect.anchoredPosition = Vector2.zero;

                slot.currentItem = item;

                slot.inventoryItem = new InventoryItem
                {
                    itemPrefab = itemPrefabs[i],
                    amount = 1
                };
            }

            slot.UpdateQuantityDisplay();
        }
    }

    public InventoryItem[] GetInventoryItems()
    {
        List<InventoryItem> items =
            new List<InventoryItem>();

        Slot[] slots =
            inventoryPanel.GetComponentsInChildren<Slot>(true);

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
}

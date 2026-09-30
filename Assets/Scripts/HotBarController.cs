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
            Instantiate(
                slotPrefab,
                hotbarPanel.transform
            ).GetComponent<Slot>();
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
        if (item == null)
            return false;

        if (item.itemPrefab == null)
            return false;

        Slot[] slots =
            hotbarPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            if (slot.inventoryItem == null)
                continue;

            if (slot.inventoryItem.itemPrefab ==
                item.itemPrefab)
            {
                if (slot.currentItem != null)
                {
                    Destroy(slot.currentItem);
                }

                slot.currentItem = null;
                slot.inventoryItem = null;

                return true;
            }
        }

        return false;
    }

    public bool AddItem(InventoryItem item)
    {
        if (item == null)
            return false;

        if (item.itemPrefab == null)
            return false;

        Slot[] slots =
            hotbarPanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            if (slot.currentItem != null)
                continue;

            GameObject newItem =
                Instantiate(
                    item.itemPrefab,
                    slot.transform
                );

            RectTransform rect =
                newItem.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchoredPosition =
                    Vector2.zero;
            }

            slot.currentItem = newItem;

            slot.inventoryItem =
                new InventoryItem();

            slot.inventoryItem.itemPrefab =
                item.itemPrefab;

            slot.inventoryItem.amount =
                item.amount;

            return true;
        }

        Debug.Log("Hotbar cheia.");

        return false;
    }
}
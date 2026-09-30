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

            if (i < itemPrefabs.Length)
            {
                GameObject item = Instantiate(
                    itemPrefabs[i],
                    slot.transform
                );

                item.GetComponent<RectTransform>()
                    .anchoredPosition = Vector2.zero;

                // Item visual do slot
                slot.currentItem = item;

                // Dados do item para o sistema de cozinha
                slot.inventoryItem =
                    new InventoryItem();

                slot.inventoryItem.itemPrefab =
                    itemPrefabs[i];

                slot.inventoryItem.amount = 1;
            }
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
        if (item == null)
            return false;

        if (item.itemPrefab == null)
            return false;

        Slot[] slots =
            inventoryPanel.GetComponentsInChildren<Slot>(true);

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
            inventoryPanel.GetComponentsInChildren<Slot>(true);

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

        Debug.Log("Inventário cheio.");

        return false;
    }
}
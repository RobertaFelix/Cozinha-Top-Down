
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
        // Remove slots antigos, caso já existam.
        foreach (Transform child in inventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject = Instantiate(
                slotPrefab,
                inventoryPanel.transform
            );

            Slot slot = slotObject.GetComponent<Slot>();

            if (slot == null)
            {
                Debug.LogError("O prefab Slot não possui Slot.cs!");
                continue;
            }

            // Inicializa o slot completamente vazio.
            slot.currentItem = null;
            slot.inventoryItem = null;

            slot.UpdateQuantityDisplay();

            // Só adiciona itens iniciais se estiverem configurados.
            if (itemPrefabs != null &&
                i < itemPrefabs.Length &&
                itemPrefabs[i] != null)
            {
                GameObject item = Instantiate(
                    itemPrefabs[i],
                    slot.transform
                );

                RectTransform rect = item.GetComponent<RectTransform>();

                if (rect != null)
                    rect.anchoredPosition = Vector2.zero;

                slot.currentItem = item;

                slot.inventoryItem = new InventoryItem
                {
                    itemPrefab = itemPrefabs[i],
                    amount = 1
                };

                slot.UpdateQuantityDisplay();
            }
        }

        Debug.Log("[INVENTÁRIO] " + slotCount + " slots inicializados.");
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
}

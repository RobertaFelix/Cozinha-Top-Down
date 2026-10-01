using System.Collections.Generic;
using UnityEngine;

public class StoveInventoryController : MonoBehaviour
{
    [Header("Interface do fogão")]
    public GameObject stovePanel;

    [Header("Slots")]
    public GameObject slotPrefab;
    public int slotCount = 6;

    public bool IsOpen
    {
        get
        {
            return stovePanel != null &&
                   stovePanel.activeSelf;
        }
    }

    private void Start()
    {
        if (stovePanel != null)
        {
            stovePanel.SetActive(false);
        }
    }

    public void OpenStoveInventory()
    {
        ClearSlots();

        stovePanel.SetActive(true);

        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(
                slotPrefab,
                stovePanel.transform
            ).GetComponent<Slot>();
        }
    }

    public void CloseStoveInventory()
    {
        ClearSlots();

        stovePanel.SetActive(false);
    }

    public InventoryItem[] GetStoveItems()
    {
        List<InventoryItem> items =
            new List<InventoryItem>();

        Slot[] slots =
            stovePanel.GetComponentsInChildren<Slot>(true);

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

    public void ClearIngredients()
    {
        Slot[] slots =
            stovePanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            if (slot.currentItem != null)
            {
                Destroy(slot.currentItem);
            }

            slot.currentItem = null;
            slot.inventoryItem = null;
        }
    }

    private void ClearSlots()
    {
        if (stovePanel == null)
            return;

        Slot[] slots =
            stovePanel.GetComponentsInChildren<Slot>(true);

        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
    }
}
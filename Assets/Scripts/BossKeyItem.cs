using UnityEngine;

// Marker on the inventory prefab, also used by the save catalog and tooltip system.
public sealed class BossKeyItem : MonoBehaviour
{
    public static bool TryGive(GameObject prefab)
    {
        var inventory = Object.FindAnyObjectByType<InventoryController>();
        return inventory != null && inventory.AddItem(new InventoryItem { itemPrefab = prefab, amount = 1 });
    }

    public static bool Has(GameObject prefab)
    {
        foreach (var slot in PlayerSlots())
            if (slot.inventoryItem?.itemPrefab == prefab && prefab != null && slot.inventoryItem.amount > 0) return true;
        return false;
    }

    public static bool TryConsume(GameObject prefab)
    {
        InventoryStackCursor.Cancel();
        ItemDragHandler.CancelAllDrags();
        foreach (var slot in PlayerSlots())
        {
            if (prefab == null || slot.inventoryItem?.itemPrefab != prefab || slot.inventoryItem.amount <= 0) continue;
            slot.inventoryItem.amount--;
            if (slot.inventoryItem.amount == 0)
            {
                if (slot.currentItem != null) { slot.currentItem.SetActive(false); Object.Destroy(slot.currentItem); }
                slot.currentItem = null;
                slot.inventoryItem = null;
            }
            slot.UpdateQuantityDisplay();
            return true;
        }
        return false;
    }

    private static System.Collections.Generic.IEnumerable<Slot> PlayerSlots()
    {
        var inventory = Object.FindAnyObjectByType<InventoryController>();
        if (inventory != null && inventory.inventoryPanel != null)
            foreach (var slot in inventory.inventoryPanel.GetComponentsInChildren<Slot>(true)) yield return slot;
        var hotbar = Object.FindAnyObjectByType<HotbarController>();
        if (hotbar != null && hotbar.hotbarPanel != null)
            foreach (var slot in hotbar.hotbarPanel.GetComponentsInChildren<Slot>(true)) yield return slot;
    }
}


using UnityEngine;

public static class InventoryStacking
{
    public static int MaxStackSize => UpgradePurchases.Has(UpgradePurchases.Backpack) ? 99 : 10;

    public static Slot CreateSlot(GameObject prefab, Transform parent, InventoryItem item = null)
    {
        var slot = Object.Instantiate(prefab, parent).GetComponent<Slot>();
        slot.inventoryItem = null;
        if (item?.itemPrefab != null && item.amount > 0)
        {
            slot.inventoryItem = new InventoryItem { itemPrefab = item.itemPrefab, amount = item.amount };
            slot.currentItem = Object.Instantiate(item.itemPrefab, slot.transform);
            if (slot.currentItem.TryGetComponent<RectTransform>(out var rect)) rect.anchoredPosition = Vector2.zero;
        }
        slot.UpdateQuantityDisplay();
        return slot;
    }

    public static void Transfer(Slot source, Transform destination, bool existingStacksOnly = false)
    {
        var item = source.inventoryItem;
        if (item?.itemPrefab == null || destination == null || source.transform.parent == destination) return;
        int capacity = 0;
        foreach (Transform child in destination)
        {
            if (!child.TryGetComponent<Slot>(out var slot)) continue;
            if (slot.inventoryItem?.itemPrefab == null) capacity += existingStacksOnly ? 0 : MaxStackSize;
            else if (slot.inventoryItem.itemPrefab == item.itemPrefab)
                capacity += Mathf.Max(0, MaxStackSize - slot.inventoryItem.amount);
        }
        int moved = Mathf.Min(capacity, item.amount);
        if (moved == 0 || !AddItem(destination, new InventoryItem { itemPrefab = item.itemPrefab, amount = moved })) return;
        item.amount -= moved;
        if (item.amount == 0)
        {
            source.currentItem.SetActive(false);
            Object.Destroy(source.currentItem);
            source.currentItem = null;
            source.inventoryItem = null;
        }
        source.UpdateQuantityDisplay();
    }

    public static bool AddItem(
        Transform container,
        InventoryItem item)
    {
        if (container == null ||
            item == null ||
            item.itemPrefab == null ||
            item.amount <= 0)
        {
            Debug.LogError(
                "InventoryStacking: container, item ou quantidade inválidos."
            );
            return false;
        }

        return AddToSlots(container.GetComponentsInChildren<Slot>(true), item);
    }

    internal static bool AddToSlots(Slot[] slots, InventoryItem item)
    {
        // Corrige slots com referências visuais inconsistentes.
        foreach (Slot slot in slots)
        {
            if (slot.inventoryItem == null)
            {
                if (slot.currentItem != null)
                {
                    Object.Destroy(slot.currentItem);
                    slot.currentItem = null;
                }

                slot.UpdateQuantityDisplay();
            }
            else if (slot.inventoryItem.itemPrefab == null ||
                     slot.inventoryItem.amount <= 0)
            {
                if (slot.currentItem != null)
                    Object.Destroy(slot.currentItem);

                slot.currentItem = null;
                slot.inventoryItem = null;
                slot.UpdateQuantityDisplay();
            }
        }

        int availableCapacity = 0;

        foreach (Slot slot in slots)
        {
            if (slot.inventoryItem == null)
            {
                availableCapacity += MaxStackSize;
            }
            else if (slot.inventoryItem.itemPrefab == item.itemPrefab)
            {
                availableCapacity += Mathf.Max(
                    0,
                    MaxStackSize - slot.inventoryItem.amount
                );
            }
        }

        Debug.Log(
            "InventoryStacking: item=" + item.itemPrefab.name +
            ", quantidade=" + item.amount +
            ", capacidade disponível=" + availableCapacity +
            ", slots encontrados=" + slots.Length
        );

        if (availableCapacity < item.amount)
        {
            Debug.LogWarning(
                "Não há espaço suficiente. Necessário: " +
                item.amount + "; disponível: " + availableCapacity
            );
            return false;
        }

        int remaining = item.amount;

        // Primeiro, completa pilhas do mesmo item.
        foreach (Slot slot in slots)
        {
            if (remaining <= 0)
                break;

            if (slot.inventoryItem == null ||
                slot.inventoryItem.itemPrefab != item.itemPrefab)
                continue;

            int space =
                MaxStackSize - slot.inventoryItem.amount;

            if (space <= 0)
                continue;

            int amountToAdd = Mathf.Min(space, remaining);

            slot.inventoryItem.amount += amountToAdd;
            remaining -= amountToAdd;

            slot.UpdateQuantityDisplay();
        }

        // Depois, utiliza os slots vazios.
        foreach (Slot slot in slots)
        {
            if (remaining <= 0)
                break;

            if (slot.inventoryItem != null)
                continue;

            int amountToAdd =
                Mathf.Min(MaxStackSize, remaining);

            GameObject visualItem = Object.Instantiate(
                item.itemPrefab,
                slot.transform
            );

            RectTransform rect =
                visualItem.GetComponent<RectTransform>();

            if (rect != null)
                rect.anchoredPosition = Vector2.zero;

            slot.currentItem = visualItem;

            slot.inventoryItem = new InventoryItem
            {
                itemPrefab = item.itemPrefab,
                amount = amountToAdd
            };

            remaining -= amountToAdd;

            slot.UpdateQuantityDisplay();
        }

        return remaining == 0;
    }

    public static bool RemoveItem(
        Transform container,
        InventoryItem item)
    {
        if (container == null ||
            item == null ||
            item.itemPrefab == null ||
            item.amount <= 0)
        {
            return false;
        }

        Slot[] slots =
            container.GetComponentsInChildren<Slot>(true);

        int availableAmount = 0;

        foreach (Slot slot in slots)
        {
            if (slot.inventoryItem != null &&
                slot.inventoryItem.itemPrefab == item.itemPrefab)
            {
                availableAmount += slot.inventoryItem.amount;
            }
        }

        if (availableAmount < item.amount)
        {
            Debug.LogWarning(
                "InventoryStacking: quantidade insuficiente para remover."
            );
            return false;
        }

        int remaining = item.amount;

        foreach (Slot slot in slots)
        {
            if (remaining <= 0)
                break;

            if (slot.inventoryItem == null ||
                slot.inventoryItem.itemPrefab != item.itemPrefab)
                continue;

            int amountToRemove = Mathf.Min(
                slot.inventoryItem.amount,
                remaining
            );

            slot.inventoryItem.amount -= amountToRemove;
            remaining -= amountToRemove;

            if (slot.inventoryItem.amount <= 0)
            {
                if (slot.currentItem != null)
                    Object.Destroy(slot.currentItem);

                slot.currentItem = null;
                slot.inventoryItem = null;
            }

            slot.UpdateQuantityDisplay();
        }

        return remaining == 0;
    }
}

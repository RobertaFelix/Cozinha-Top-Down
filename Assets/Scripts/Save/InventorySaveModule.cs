using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class SavedSlot { public string itemId; public int amount; }
[Serializable] public sealed class InventorySaveState { public SavedSlot[] slots; }
public sealed class InventorySaveModule : SaveParticipant<InventorySaveState>
{
    [SerializeField] private string inventoryId;
    [SerializeField] private Transform container;
    [SerializeField] private GameObject slotPrefab;
    public override string SaveKey => "inventory/" + inventoryId;
    public override int RestoreOrder => 30;
    private List<Slot> Slots()
    {
        var slots = new List<Slot>();
        foreach (Transform child in container)
            if (child.TryGetComponent<Slot>(out var slot)) slots.Add(slot);
        return slots;
    }
    protected override InventorySaveState Capture()
    {
        var slots = Slots();
        var state = new InventorySaveState { slots = new SavedSlot[slots.Count] };
        for (int i = 0; i < slots.Count; i++)
        {
            var item = slots[i].inventoryItem;
            state.slots[i] = item?.itemPrefab != null && item.amount > 0
                ? new SavedSlot { itemId = SaveItemCatalog.Instance.Id(item.itemPrefab), amount = item.amount }
                : new SavedSlot { itemId = "", amount = 0 };
        }
        return state;
    }
    protected override void Validate(InventorySaveState state)
    {
        Require(state.slots != null && state.slots.Length <= 10000, "Invalid saved inventory.");
        foreach (var slot in state.slots)
        {
            Require(slot != null && slot.amount >= 0 && (string.IsNullOrEmpty(slot.itemId) == (slot.amount == 0)), "Invalid saved stack.");
            SaveItemCatalog.Instance.Prefab(slot.itemId);
        }
    }
    protected override void Restore(InventorySaveState state)
    {
        var slots = Slots();
        // Old backpacks had an extra row. Compact occupied overflow into empty slots first.
        var saved = (SavedSlot[])state.slots.Clone();
        if (inventoryId == "backpack")
        {
            for (int i = slots.Count; i < saved.Length; i++)
            {
                if (saved[i].amount == 0) continue;
                int empty = Array.FindIndex(saved, 0, slots.Count, entry => entry.amount == 0);
                if (empty < 0) break;
                (saved[i], saved[empty]) = (saved[empty], saved[i]);
            }
        }
        int needed = Array.FindLastIndex(saved, entry => entry.amount > 0) + 1;
        while (slots.Count < needed) slots.Add(Instantiate(slotPrefab, container).GetComponent<Slot>());
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.currentItem != null) { slot.currentItem.SetActive(false); Destroy(slot.currentItem); }
            slot.currentItem = null;
            slot.inventoryItem = null;
            if (i < saved.Length && saved[i].amount > 0)
            {
                var data = saved[i];
                var prefab = SaveItemCatalog.Instance.Prefab(data.itemId);
                slot.inventoryItem = new InventoryItem { itemPrefab = prefab, amount = data.amount };
                slot.currentItem = Instantiate(prefab, slot.transform);
                if (slot.currentItem.TryGetComponent<RectTransform>(out var rect)) rect.anchoredPosition = Vector2.zero;
            }
            slot.UpdateQuantityDisplay();
        }
    }
}

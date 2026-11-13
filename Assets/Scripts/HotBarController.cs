
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class HotbarController : MonoBehaviour
{
    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public int slotCount;

    [SerializeField] private GameObject potionUser;

    private int equippedIndex = -1;
    public int EquippedIndex => equippedIndex;
    public Slot EquippedSlot
    {
        get
        {
            var slot = GetPhysicalSlot(equippedIndex);
            var prefab = slot?.inventoryItem?.itemPrefab;
            return prefab != null && slot.inventoryItem.amount > 0 && slot.currentItem != null &&
                slot.currentItem.transform.parent == slot.transform &&
                (prefab.GetComponent<FarmToolItem>() != null || prefab.GetComponent<SeedItem>() != null) ? slot : null;
        }
    }

    private Slot GetPhysicalSlot(int index)
    {
        if (index < 0 || hotbarPanel == null) return null;
        int count = 0;
        foreach (Transform child in hotbarPanel.transform)
            if (child.TryGetComponent<Slot>(out var slot) && count++ == index) return slot;
        return null;
    }

    private static readonly Key[] numberKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
    private static readonly Key[] numpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6 };

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        for (int i = 0; i < numberKeys.Length; i++)
            if (keyboard[numberKeys[i]].wasPressedThisFrame || keyboard[numpadKeys[i]].wasPressedThisFrame)
                TryUseSlot(i);
    }

    private bool CanUseHotbar()
    {
        if (!isActiveAndEnabled || hotbarPanel == null || !hotbarPanel.activeInHierarchy ||
            Time.timeScale <= 0 || ItemTooltipView.IsDragging) return false;
        var transition = FindFirstObjectByType<EnvironmentTransition>();
        if (transition != null && transition.IsTransitioning) return false;
        var scenes = FindFirstObjectByType<SceneTransitionController>();
        if (scenes != null && scenes.IsLoading) return false;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected == null || (selected.GetComponentInParent<TMP_InputField>() == null &&
            selected.GetComponentInParent<InputField>() == null);
    }

    // Zero-based index; consume only the stack in this physical hotbar slot.
    public bool TryUseSlot(int index)
    {
        if (!CanUseHotbar() || index < 0 || index >= numberKeys.Length || index >= slotCount) return false;
        equippedIndex = -1;
        Slot slot = GetPhysicalSlot(index);
        if (slot == null || !slot.gameObject.activeInHierarchy || slot.inventoryItem?.itemPrefab == null ||
            slot.inventoryItem.amount <= 0 || slot.currentItem == null ||
            slot.currentItem.transform.parent != slot.transform) return false;

        if (slot.inventoryItem.itemPrefab.GetComponent<FarmToolItem>() != null || slot.inventoryItem.itemPrefab.GetComponent<SeedItem>() != null)
        {
            equippedIndex = index;
            return true;
        }

        var potion = slot.inventoryItem.itemPrefab.GetComponent<PotionItem>();
        if (potion == null) return false;
        if (potionUser == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) potionUser = player.gameObject;
        }
        if (!potion.TryUse(potionUser)) return false;

        slot.inventoryItem.amount--;
        if (slot.inventoryItem.amount == 0)
        {
            slot.currentItem.SetActive(false);
            Destroy(slot.currentItem);
            slot.currentItem = null;
            slot.inventoryItem = null;
        }
        slot.UpdateQuantityDisplay();
        return true;
    }

    private void Start()
    {
        CreateSlots();
        if (GetComponent<HotbarEquippedIndicator>() == null) gameObject.AddComponent<HotbarEquippedIndicator>();
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

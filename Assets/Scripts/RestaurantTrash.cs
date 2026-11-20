using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RestaurantTrash : MonoBehaviour
{
    [SerializeField] private ConfirmableInventoryPanel inventory;
    [SerializeField] private Vector2 interactionSize = new Vector2(1.15f, 1.8f);
    private BoxCollider2D area;
    private Collider2D player;

    private void Awake()
    {
        area = gameObject.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = interactionSize;
        area.offset = new Vector2(0, -.25f);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (player || !other.CompareTag("Player")) return;
        player = other;
        InteractionPromptManager.Instance?.Show(transform);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != player || area.IsTouching(other)) return;
        player = null;
        InteractionPromptManager.Instance?.Hide(transform);
    }

    private void Update()
    {
        if (!player || Time.timeScale <= 0 || Keyboard.current == null ||
            !Keyboard.current.eKey.wasPressedThisFrame) return;
        if (inventory.IsOpen)
        {
            inventory.Close();
            return;
        }
        InventoryPanelSession.Bind(inventory.panelRoot, area, player);
        inventory.Open(Discard, ReturnItems);
    }

    private void Discard()
    {
        if (!inventory.IsOpen) return;
        InventoryStackCursor.Cancel();
        ItemDragHandler.CancelAllDrags();
        foreach (var slot in inventory.panelRoot.GetComponentsInChildren<Slot>(true))
        {
            if (slot.currentItem) Destroy(slot.currentItem);
            slot.currentItem = null;
            slot.inventoryItem = null;
            slot.UpdateQuantityDisplay();
        }
        inventory.Close();
    }

    private void ReturnItems()
    {
        var bag = FindAnyObjectByType<InventoryController>();
        var backpack = bag.inventoryPanel.transform;
        var hotbar = FindAnyObjectByType<HotbarController>().hotbarPanel.transform;
        foreach (var slot in inventory.panelRoot.GetComponentsInChildren<Slot>(true))
        {
            if (slot.inventoryItem?.itemPrefab == null) continue;
            InventoryStacking.Transfer(slot, backpack, true);
            InventoryStacking.Transfer(slot, hotbar, true);
            InventoryStacking.Transfer(slot, backpack);
            InventoryStacking.Transfer(slot, hotbar);
            // Other pickups may have filled the spaces freed during this session.
            while (slot.inventoryItem?.itemPrefab != null)
            {
                InventoryStacking.CreateSlot(bag.slotPrefab, backpack);
                InventoryStacking.Transfer(slot, backpack);
            }
        }
    }
}

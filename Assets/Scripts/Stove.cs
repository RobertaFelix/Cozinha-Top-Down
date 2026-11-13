using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Stove : MonoBehaviour
{
    [SerializeField] private StoveController stoveController;
    [SerializeField] private StoveInventoryController stoveInventoryController;
    [SerializeField] private HotbarController hotbarController;
    [SerializeField] private Sprite flameSprite;
    private SpriteRenderer flame;
    private SpriteRenderer stoveSprite, utensil;
    private InteractionPromptManager Prompt => InteractionPromptManager.Instance;
    private Collider2D interactionArea;
    private Collider2D nearbyPlayer;
    private bool showingPrompt;
    private bool nearby => nearbyPlayer && interactionArea.IsTouching(nearbyPlayer);

    private void Awake()
    {
        stoveSprite = transform.parent.GetComponent<SpriteRenderer>();
        interactionArea = GetComponent<Collider2D>();
        var visual = new GameObject("Cookware");
        visual.transform.SetParent(transform, false);
        utensil = visual.AddComponent<SpriteRenderer>();
        utensil.sortingLayerID = stoveSprite.sortingLayerID;
        utensil.sortingOrder = stoveSprite.sortingOrder + 1;
        flame = new GameObject("Burner flame").AddComponent<SpriteRenderer>();
        flame.transform.SetParent(transform, false);
        flame.sprite = flameSprite;
        flame.sortingLayerID = utensil.sortingLayerID;
        flame.sortingOrder = utensil.sortingOrder;
        utensil.sortingOrder++;
        flame.enabled = false;
        utensil.transform.localScale = Vector3.one;
    }

    internal static bool CanClickWorld() => Time.timeScale > 0 && !InventoryPanelSession.HasOpenPanel &&
        !InventoryStackCursor.IsHolding && !ItemTooltipView.IsDragging &&
        (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());

    internal static bool GiveItem(InventoryItem item)
    {
        var inventory = FindAnyObjectByType<InventoryController>();
        if (inventory && inventory.AddItem(item)) return true;
        var bar = FindAnyObjectByType<HotbarController>();
        return bar && bar.AddItem(item);
    }

    private void Update()
    {
        var mounted = stoveController.Mounted;
        utensil.sprite = mounted ? mounted.GetComponent<Image>().sprite : null;
        float bounce = stoveController.IsCooking ? .04f * Mathf.Abs(Mathf.Sin(Time.time * 9f)) : 0;
        if (utensil.sprite)
        {
            var surface = stoveSprite.bounds;
            var center = surface.center;
            center.y = surface.max.y - surface.size.y * .16f + bounce;
            utensil.transform.position += center - utensil.bounds.center;
        }
        flame.enabled = stoveController.IsCooking;
        flame.transform.position = utensil.transform.position - Vector3.up * .17f;
        flame.flipX = (int)(Time.time * 6f) % 2 == 0;
        bool canOpen = nearby && mounted && !stoveController.IsCooking && !stoveController.CookingFinished;
        SetPrompt(canOpen && !stoveInventoryController.IsOpen);
        if (!nearby || Time.timeScale <= 0) return;
        if (CookwareCounter.InteractPressed && !InventoryStackCursor.IsHolding && !ItemTooltipView.IsDragging &&
            (!InventoryPanelSession.HasOpenPanel || stoveInventoryController.IsOpen))
        {
            if (stoveController.CookingFinished) { Serve(); return; }
            if (!mounted) { UseUtensil(); return; }
        }
        if (canOpen && CookwareCounter.InteractPressed && !InventoryStackCursor.IsHolding && !ItemTooltipView.IsDragging &&
            (!InventoryPanelSession.HasOpenPanel || stoveInventoryController.IsOpen))
        {
            if (stoveInventoryController.IsOpen) stoveInventoryController.CloseStoveInventory();
            else
            {
                InventoryPanelSession.Bind(stoveInventoryController.stovePanel, interactionArea, nearbyPlayer);
                stoveInventoryController.Open(Cook);
            }
            return;
        }
        if (InventoryPanelSession.HasOpenPanel || stoveController.IsCooking || stoveController.CookingFinished) return;
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) { UseUtensil(); return; }
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || !CanClickWorld() || !Camera.main) return;
        var point = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        point.z = stoveSprite.bounds.center.z;
        if (!stoveSprite.bounds.Contains(point)) return;
        UseUtensil();
    }

    private void Serve()
    {
        var slot = hotbarController.EquippedSlot;
        var held = slot?.inventoryItem?.itemPrefab;
        if (stoveController.CookingFinished)
        {
            if (!held || !held.TryGetComponent<CookwareItem>(out var item) || !item.isPlate) return;
            var food = stoveController.GetCookedFood();
            if (slot.inventoryItem.amount == 1)
            {
                ConsumeOne(slot);
                InventoryStacking.AddItem(slot.transform, food);
            }
            else
            {
                if (!GiveItem(food)) return;
                ConsumeOne(slot);
            }
            stoveController.UnlockCurrentRecipe();
            stoveController.ClearCookedFood();
        }
    }

    private void UseUtensil()
    {
        var mounted = stoveController.Mounted;
        var slot = hotbarController.EquippedSlot;
        var held = slot?.inventoryItem?.itemPrefab;
        if (!stoveController.IsCooking && mounted)
        {
            if (hotbarController.AddItem(new InventoryItem { itemPrefab = mounted, amount = 1 })) stoveController.Mounted = null;
        }
        else if (!mounted && held && held.TryGetComponent<CookwareItem>(out var tool) && !tool.isPlate)
        {
            stoveController.Mounted = held;
            ConsumeOne(slot);
        }
    }

    internal static void ConsumeOne(Slot slot)
    {
        if (--slot.inventoryItem.amount == 0)
        {
            slot.currentItem.SetActive(false);
            Destroy(slot.currentItem);
            slot.currentItem = null;
            slot.inventoryItem = null;
        }
        slot.UpdateQuantityDisplay();
    }

    private void Cook()
    {
        if (!nearby || !stoveController.Mounted || stoveController.IsCooking || stoveController.CookingFinished) return;
        var tool = new InventoryItem { itemPrefab = stoveController.Mounted, amount = 1 };
        var items = new List<InventoryItem>(stoveInventoryController.GetStoveItems()) { tool };
        foreach (var ingredient in stoveInventoryController.GetStoveItems())
            if (ingredient.itemPrefab.GetComponent<CookwareItem>()) return;
        var recipe = stoveController.FindRecipe(items.ToArray());
        if (recipe == null) return;
        stoveController.StartCooking(recipe, tool.itemPrefab == FindAnyObjectByType<Pan>(FindObjectsInactive.Include)?.InitialItem,
            tool.itemPrefab == FindAnyObjectByType<Pot>(FindObjectsInactive.Include)?.InitialItem);
        if (!stoveController.IsCooking) return;
        foreach (var ingredient in recipe.ingredients)
            if (ingredient.itemPrefab != tool.itemPrefab)
                InventoryStacking.RemoveItem(stoveInventoryController.stovePanel.transform,
                    new InventoryItem { itemPrefab = ingredient.itemPrefab, amount = Mathf.Max(1, ingredient.amount) });
        stoveInventoryController.CloseStoveInventory();
    }

    private void SetPrompt(bool visible)
    {
        if (showingPrompt == visible || !Prompt) return;
        showingPrompt = visible;
        if (visible) Prompt.Show(transform.parent, Vector3.down * .4f);
        else Prompt.Hide(transform.parent);
    }

    private void OnDisable() => SetPrompt(false);
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player")) nearbyPlayer = other;
    }
}

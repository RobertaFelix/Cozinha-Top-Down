using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Handle a press on a countertop appliance before the stove beneath its artwork.
[DefaultExecutionOrder(-40)]
public sealed class KitchenAppliance : MonoBehaviour
{
    public string StationKey { get; private set; }
    public string PieceId { get; private set; }
    public CookingRecipe CurrentRecipe { get; private set; }
    public bool CanStore => CurrentRecipe == null && !Ingredients.Any(i => i?.amount > 0);
    private SavedSlot[] ingredients = System.Array.Empty<SavedSlot>();
    internal SavedSlot[] Ingredients => panel && panel.IsOpen ? panel.Capture() : ingredients;
    internal void RestoreIngredients(SavedSlot[] items) => ingredients = items ?? System.Array.Empty<SavedSlot>();
    public float Remaining => CurrentRecipe == null ? 0f : Mathf.Max(0f, (float)(readyAt - Time.timeAsDouble));
    internal float BurnElapsed => CurrentRecipe == null ? 0f : Mathf.Max(0f, (float)(Time.timeAsDouble - readyAt));
    internal bool Burnt => CurrentRecipe != null && BurnElapsed >= stove.CurrentBurnDuration;
    public bool Ready => CurrentRecipe != null && Remaining <= 0f && !Burnt;
    public static bool HandledThisFrame => handledFrame == Time.frameCount;

    private static int handledFrame = -1;
    private static readonly List<KitchenAppliance> nearby = new List<KitchenAppliance>();
    private static readonly List<Collider2D> contacts = new List<Collider2D>();
    private static readonly ContactFilter2D interactionFilter = new ContactFilter2D { useTriggers = true };
    private CircleCollider2D useArea;
    private Collider2D player;
    private SpriteRenderer art;
    private StoveController stove;
    private CookingProgressUI progress;
    private ApplianceRecipePanel panel;
    private double readyAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { nearby.Clear(); handledFrame = -1; }

    public static KitchenAppliance Install(GameObject root, string stationKey, string pieceId)
    {
        if (stationKey == "sink") return null;
        var appliance = root.GetComponent<KitchenAppliance>();
        if (!appliance) appliance = root.AddComponent<KitchenAppliance>();
        appliance.StationKey = stationKey;
        appliance.PieceId = pieceId;
        if (appliance.useArea) return appliance;
        appliance.art = root.GetComponent<SpriteRenderer>();
        appliance.useArea = root.AddComponent<CircleCollider2D>();
        appliance.useArea.isTrigger = true;
        appliance.useArea.radius = 1.18f;
        appliance.useArea.offset = (Vector2)appliance.art.sprite.bounds.center + Vector2.down * .55f;
        return appliance;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || other.isTrigger || !useArea || !useArea.IsTouching(other)) return;
        player = other;
        if (!nearby.Contains(this)) nearby.Add(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == player && !useArea.IsTouching(other)) LeaveRange();
    }

    private void LeaveRange()
    {
        nearby.Remove(this);
        player = null;
        InteractionPromptManager.Instance?.Hide(transform);
        if (panel) panel.Close();
    }

    private void OnDisable() => LeaveRange();

    private static KitchenAppliance Nearest(Vector2 point, bool click)
    {
        KitchenAppliance best = null;
        float distance = float.PositiveInfinity;
        foreach (var candidate in nearby)
        {
            if (!candidate || !candidate.player || !candidate.useArea.IsTouching(candidate.player)) continue;
            var bounds = candidate.art.bounds;
            if (click && !bounds.Contains(new Vector3(point.x, point.y, bounds.center.z))) continue;
            Vector2 anchor = click ? (Vector2)bounds.center : (Vector2)candidate.useArea.bounds.center;
            float score = (anchor - point).sqrMagnitude;
            if (score < distance) { best = candidate; distance = score; }
        }
        if (best && !click)
        {
            // Keep E usable on the fridge, burner and utensil stands even when the player
            // can also reach a neighbouring machine. Clicking still selects the exact sprite.
            best.player.Overlap(interactionFilter, contacts);
            foreach (var other in contacts)
            {
                if (!other.isTrigger || other.GetComponent<KitchenAppliance>()) continue;
                if (((Vector2)other.bounds.center - point).sqrMagnitude >= distance) continue;
                if (other.GetComponent<Stove>() || other.GetComponent<CookwareCounter>() ||
                    other.GetComponent<PlateCounter>() || other.GetComponent<Frizer>() ||
                    other.GetComponent<Refrigerator>() || other.GetComponent<Vegetables>() ||
                    other.GetComponent<RestaurantTrash>() || other.GetComponent<MenuBoardInteraction>() ||
                    other.GetComponent<InteractionPromptTrigger>()) return null;
            }
        }
        return best;
    }

    private void Update()
    {
        if (CurrentRecipe != null)
        {
            if (Burnt) SetJob(null, 0);
            else
            {
                progress.SetProgress(1f - Remaining / stove.PreparationSeconds(CurrentRecipe));
                if (Ready) progress.SetBurnProgress(BurnElapsed / stove.CurrentBurnDuration);
            }
        }
        if (!player || Time.timeScale <= 0 || InventoryPanelSession.HasOpenPanel || HandledThisFrame ||
            InventoryStackCursor.IsHolding || ItemTooltipView.IsDragging) return;

        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main && Stove.CanClickWorld();
        Vector2 point = click ? (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) : (Vector2)player.bounds.center;
        if (Nearest(point, click) != this)
        {
            InteractionPromptManager.Instance?.Hide(transform);
            return;
        }
        if (InteractionPromptManager.Instance && InteractionPromptManager.Instance.CurrentTarget != transform)
            InteractionPromptManager.Instance.Show(transform);
        if (!click && !CookwareCounter.InteractPressed) return;
        handledFrame = Time.frameCount;
        if (Ready)
        {
            if (!TryCollect()) MarketHoursNotice.Show("Make room in your inventory to collect the dish.");
        }
        else if (CurrentRecipe != null)
            MarketHoursNotice.Show($"{ItemTooltipCatalog.GetName(CurrentRecipe.result.itemPrefab)} • {Mathf.CeilToInt(Remaining)}s");
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        if (panel || !player || !useArea.IsTouching(player) || CurrentRecipe != null || Time.timeScale <= 0) return;
        panel = ApplianceRecipePanel.Show(this, useArea, player);
        InteractionPromptManager.Instance?.Hide(transform);
    }

    internal Slot[] PlayerSlots()
    {
        var bag = FindAnyObjectByType<InventoryController>();
        HotbarController bar = null;
        HotbarController.Resolve(ref bar);
        var slots = new List<Slot>();
        if (bag && bag.inventoryPanel) slots.AddRange(bag.inventoryPanel.GetComponentsInChildren<Slot>(true));
        if (bar && bar.hotbarPanel) slots.AddRange(bar.hotbarPanel.GetComponentsInChildren<Slot>(true));
        return slots.ToArray();
    }

#if UNITY_EDITOR
    // The F7 demos start with one dish already cooking.
    // Plan every withdrawal before touching a slot. Ingredients may span both containers
    // or occur more than once in the same recipe.
    public bool TryStart(CookingRecipe recipe)
    {
        if (!isActiveAndEnabled || !CanStore || recipe?.result?.itemPrefab == null ||
            !ApplianceRecipeCatalog.ForStation(StationKey).Contains(recipe)) return false;
        var slots = PlayerSlots();
        var plan = new Dictionary<Slot, int>();
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient?.itemPrefab == null || ingredient.amount < 1) return false;
            int needed = ingredient.amount;
            foreach (var slot in slots)
            {
                if (slot.inventoryItem?.itemPrefab != ingredient.itemPrefab) continue;
                plan.TryGetValue(slot, out int reserved);
                int take = Mathf.Min(needed, slot.inventoryItem.amount - reserved);
                if (take <= 0) continue;
                plan[slot] = reserved + take;
                needed -= take;
                if (needed == 0) break;
            }
            if (needed > 0) return false;
        }
        if (plan.Count == 0) return false;
        foreach (var pair in plan)
        {
            var slot = pair.Key;
            slot.inventoryItem.amount -= pair.Value;
            if (slot.inventoryItem.amount == 0)
            {
                if (slot.currentItem) { slot.currentItem.SetActive(false); Destroy(slot.currentItem); }
                slot.currentItem = null;
                slot.inventoryItem = null;
            }
            slot.UpdateQuantityDisplay();
        }
        BeginCooking(recipe);
        return true;
    }

#endif

    internal void BeginCooking(CookingRecipe recipe)
    {
        var stove = FindAnyObjectByType<StoveController>(FindObjectsInactive.Include);
        float seconds = stove ? stove.PreparationSeconds(recipe) : Mathf.Max(.01f, recipe.cookingTime);
        SetJob(recipe, seconds);
    }

    public bool TryCollect()
    {
        if (!isActiveAndEnabled || !Ready || !InventoryStacking.AddToSlots(PlayerSlots(), CurrentRecipe.result)) return false;
        RecipeBookController.Instance?.UnlockRecipe(CurrentRecipe);
        SetJob(null, 0f);
        return true;
    }

    internal void SetJob(CookingRecipe recipe, float seconds)
    {
        CurrentRecipe = recipe;
        readyAt = Time.timeAsDouble + seconds;
        if (recipe == null)
        {
            if (progress) progress.Hide();
            return;
        }
        if (!stove) stove = FindAnyObjectByType<StoveController>(FindObjectsInactive.Include);
        if (!progress)
        {
            var position = art.transform.TransformPoint(art.sprite.bounds.center);
            position.y += art.sprite.bounds.extents.y * Mathf.Abs(art.transform.lossyScale.y) + .55f;
            progress = stove.CreateProgressDisplay(transform, position);
        }
        progress.Show(ItemTooltipCatalog.ForItem(recipe.result.itemPrefab).Icon,
            1f - Remaining / stove.PreparationSeconds(recipe));
        if (Ready) progress.SetBurnProgress(BurnElapsed / stove.CurrentBurnDuration);
    }
}

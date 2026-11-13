using System;
using UnityEngine;

[Serializable] public sealed class CookingSaveState
{
    public string recipeId;
    public string mountedId;
    public SavedSlot[] ingredients;
    public bool hasCounterContents;
    public string panSpot, potSpot;
    public bool panTaken, potTaken;
    public float timer;
    public float burnTimer;
    public bool cooking;
    public bool finished;
    public bool usedPan;
    public bool usedPot;
}
public sealed class CookingSaveModule : SaveParticipant<CookingSaveState>, ISceneSaveParticipant
{
    public override string SaveKey => "cooking";
    public override int RestoreOrder => 50;
    public bool SceneStateAvailable => Stove != null;
    private StoveController Stove => FindAnyObjectByType<StoveController>();
    private StoveInventoryController Panel => FindAnyObjectByType<StoveInventoryController>(FindObjectsInactive.Include);
    protected override CookingSaveState Capture()
    {
        var state = Stove.CaptureCookingState();
        var items = Panel.GetStoveItems();
        state.ingredients = Array.ConvertAll(items, item => new SavedSlot
            { itemId = SaveItemCatalog.Instance.Id(item.itemPrefab), amount = item.amount });
        var pan = FindAnyObjectByType<Pan>(FindObjectsInactive.Include);
        var pot = FindAnyObjectByType<Pot>(FindObjectsInactive.Include);
        state.hasCounterContents = true;
        state.panSpot = SaveItemCatalog.Instance.Id(pan ? pan.Stored : null);
        state.potSpot = SaveItemCatalog.Instance.Id(pot ? pot.Stored : null);
        return state;
    }
    protected override void Validate(CookingSaveState state)
    {
        var mounted = SaveItemCatalog.Instance.Prefab(state.mountedId);
        Require(!mounted || (mounted.TryGetComponent<CookwareItem>(out var tool) && !tool.isPlate), "Invalid saved cookware.");
        if (state.ingredients != null)
        {
            Require(state.ingredients.Length <= Panel.slotCount, "Invalid stove inventory.");
            foreach (var item in state.ingredients)
                Require(item != null && item.amount > 0 && item.amount <= 99 && SaveItemCatalog.Instance.Prefab(item.itemId), "Invalid ingredient.");
        }
        Require(!float.IsNaN(state.timer) && !float.IsInfinity(state.timer) && state.timer >= 0 &&
            !float.IsNaN(state.burnTimer) && !float.IsInfinity(state.burnTimer) && state.burnTimer >= 0 &&
            !(state.cooking && state.finished), "Invalid saved cooking state.");
        Require(string.IsNullOrEmpty(state.recipeId) ? !state.cooking && !state.finished :
            RecipeSaveModule.FindRecipe(Stove, state.recipeId) != null && (state.cooking || state.finished), "Invalid saved cooking recipe.");
    }
    protected override void Restore(CookingSaveState state)
    {
        Stove.RestoreCookingState(state);
        var panel = Panel;
        panel.EnsureSlots();
        var slots = panel.stovePanel.GetComponentsInChildren<Slot>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot.currentItem) { slot.currentItem.SetActive(false); Destroy(slot.currentItem); }
            slot.currentItem = null;
            slot.inventoryItem = null;
            if (state.ingredients != null && i < state.ingredients.Length)
            {
                var data = state.ingredients[i];
                var prefab = SaveItemCatalog.Instance.Prefab(data.itemId);
                slot.inventoryItem = new InventoryItem { itemPrefab = prefab, amount = data.amount };
                slot.currentItem = Instantiate(prefab, slot.transform);
            }
            slot.UpdateQuantityDisplay();
        }
        var pan = FindAnyObjectByType<Pan>(FindObjectsInactive.Include);
        var pot = FindAnyObjectByType<Pot>(FindObjectsInactive.Include);
        RestoreSpot(pan, state.hasCounterContents, state.panSpot, state.panTaken || state.usedPan);
        RestoreSpot(pot, state.hasCounterContents, state.potSpot, state.potTaken || state.usedPot);
    }

    private static void RestoreSpot(CookwareCounter spot, bool saved, string id, bool taken)
    {
        if (spot) spot.Store(saved ? SaveItemCatalog.Instance.Prefab(id) : taken ? null : spot.InitialItem);
    }
}

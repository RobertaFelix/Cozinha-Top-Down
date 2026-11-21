using System.Linq;
using UnityEngine;

public sealed class ApplianceRecipePanel : ConfirmableInventoryPanel
{
    public static ApplianceRecipePanel Show(KitchenAppliance appliance, Collider2D area, Collider2D actor)
    {
        var template = FindAnyObjectByType<StoveInventoryController>(FindObjectsInactive.Include);
        var window = Instantiate(template.panelRoot, template.panelRoot.transform.parent);
        window.SetActive(false);
        window.name = ApplianceRecipeCatalog.StationLabel(appliance.StationKey);
        // Keep the grid and frame, but never copy items or callbacks from the stove.
        foreach (Transform child in window.transform.Cast<Transform>().ToArray())
            if (child.GetComponent<Slot>() || child.name == "Confirm" || child.name == "Cancel")
            {
                child.SetParent(null);
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        var view = window.AddComponent<ApplianceRecipePanel>();
        view.panelRoot = window;
        view.CopyStyle(template);
        view.EnsureSlots();
        foreach (var item in appliance.Ingredients)
            InventoryStacking.AddItem(window.transform, new InventoryItem {
                itemPrefab = SaveItemCatalog.Instance.Prefab(item.itemId), amount = item.amount
            });
        InventoryPanelSession.Bind(window, area, actor);
        view.Open(() => view.Cook(appliance), () => {
            appliance.RestoreIngredients(view.Capture());
            Destroy(window);
        });
        return view;
    }

    internal SavedSlot[] Capture() => Items().Select(i => new SavedSlot {
        itemId = SaveItemCatalog.Instance.Id(i.itemPrefab), amount = i.amount
    }).ToArray();

    private InventoryItem[] Items() => panelRoot.GetComponentsInChildren<Slot>(true)
        .Select(slot => slot.inventoryItem).Where(item => item?.itemPrefab != null).ToArray();

    private void Cook(KitchenAppliance appliance)
    {
        var recipe = StoveController.MatchIngredients(Items(), ApplianceRecipeCatalog.ForStation(appliance.StationKey));
        if (recipe == null)
        {
            MarketHoursNotice.Show("These ingredients do not make a dish here.");
            return;
        }
        foreach (var item in recipe.ingredients) InventoryStacking.RemoveItem(panelRoot.transform, item);
        appliance.BeginCooking(recipe);
        Close();
    }
}

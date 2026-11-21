using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEngine.Object;

public static class KitchenShowcase
{
    public static void Register(Action<string, string, Func<IEnumerator>> add)
    {
        foreach (var offer in RestaurantApplianceOffer.All)
        {
            string station = offer.id;
            var recipes = ApplianceRecipeCatalog.ForStation(station);
            if (recipes.Count < 2) continue;
            string first = ItemTooltipCatalog.GetName(recipes[0].result.itemPrefab);
            string second = ItemTooltipCatalog.GetName(recipes[1].result.itemPrefab);
            add(offer.label, $"{first} is cooking. When the green bar fills, collect the dish before the red bar fills. Open the appliance, add the ingredients for {second} and confirm.",
                () => CookAt(station));
        }
        add("Home computer shop", "A blender was bought and delivered to Furniture. Try another purchase, or return to Upgrades.", ComputerShop);
        add("Coffee beans at the market", "Coffee beans are in your cart. Confirm to buy them, or change the quantity.", CoffeeBeans);
        add("Recipe book and menu", "All appliance recipes are unlocked. Two are on the menu. Click a dish to change the menu.", RecipeBook);
        add("Reload two working blenders", "Two purchased blenders were saved and reloaded on separate counters. Collect the juice and smoothie from their respective machines.", ReloadBlenders);
    }

    private static EnvironmentTransition GoTo(string interior, Vector3 position)
    {
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        if (!environment || !environment.RestorePlayerCheckpoint(interior, position))
            throw new InvalidOperationException("Could not enter " + interior + ".");
        return environment;
    }

    internal static RestaurantLayout EnterRestaurant()
    {
        var layout = RestaurantLayout.FindLayout();
        var marker = layout.GetComponentsInParent<Transform>(true).Last()
            .GetComponentsInChildren<Transform>(true).First(t => t.name == "InteriorSpawnRestaurant");
        GoTo("InteriorRestaurant", marker.position);
        return layout;
    }

    private static void Supply(CookingRecipe recipe)
    {
        var inventory = FindAnyObjectByType<InventoryController>();
        foreach (var ingredient in recipe.ingredients)
            if (!inventory.AddItem(new InventoryItem { itemPrefab = ingredient.itemPrefab, amount = ingredient.amount }))
                throw new InvalidOperationException("There is no room for the demo ingredients.");
    }

    private static void StandNear(KitchenAppliance station)
    {
        var trigger = station.GetComponents<CircleCollider2D>().First(c => c.isTrigger);
        Vector3 desired = trigger.bounds.center + Vector3.down * trigger.bounds.extents.y * .58f;
        var player = FindAnyObjectByType<PlayerController>();
        Vector3 feetOffset = player.GetComponent<Collider2D>().bounds.center - player.transform.position;
        GoTo("InteriorRestaurant", desired - feetOffset);
    }

    private static IEnumerator CookAt(string key)
    {
        var layout = EnterRestaurant();
        var station = layout.GetComponentsInChildren<KitchenAppliance>()
            .First(a => a.StationKey == key && a.PieceId == "kitchen/" + key);
        StandNear(station);
        var recipes = ApplianceRecipeCatalog.ForStation(key);
        Supply(recipes[0]);
        Supply(recipes[1]);
        if (!station.TryStart(recipes[0])) throw new InvalidOperationException("Could not start the preparation.");
        yield return null;
    }

    private static void SetBudget(int coins)
    {
        var wallet = CoinManager.Instance;
        wallet.RestoreEconomy(coins, wallet.RewardMultiplier, wallet.AreDishPricesDoubled, wallet.PendingCoinFraction);
    }

    private static IEnumerator ComputerShop()
    {
        var station = FindAnyObjectByType<KeyboardUpgradeStation>(FindObjectsInactive.Include);
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        if (!environment.RestoreHomeCheckpoint(station.transform.position)) throw new InvalidOperationException("Could not enter the house.");
        var player = FindAnyObjectByType<PlayerController>();
        var offset = player.GetComponent<Collider2D>().bounds.center - player.transform.position;
        environment.RestoreHomeCheckpoint(station.GetComponent<Collider2D>().bounds.center - offset);
        SetBudget(1500);
        yield return new WaitForFixedUpdate();
        var ui = FindAnyObjectByType<UpgradeUIController>(FindObjectsInactive.Include);
        ui.Open();
        var panel = InventoryPanelSession.CurrentPanel.transform;
        panel.Find("Appliances").GetComponent<Button>().onClick.Invoke();
        var page = panel.Find("Appliance shop");
        var offer = RestaurantApplianceOffer.All.First(o => o.id == "blender");
        page.Find("Appliance catalog/" + offer.label).GetComponent<Button>().onClick.Invoke();
        var buy = page.Find("Buy").GetComponent<Button>();
        if (!buy.IsInteractable()) throw new InvalidOperationException("Cannot purchase the demo blender.");
        buy.onClick.Invoke();
        if (!RestaurantLayout.FindLayout().Pieces.Any(p => p.purchasedType == offer.id && !p.Placed))
            throw new InvalidOperationException("The blender was not delivered.");
    }

    private static IEnumerator CoffeeBeans()
    {
        FindAnyObjectByType<GameClock>().SetTotalGameMinutes(12 * 60);
        var market = FindObjectsByType<MarketUIController>(FindObjectsInactive.Include).First(store =>
        {
            var products = new SerializedObject(store).FindProperty("products");
            for (int i = 0; i < products.arraySize; i++)
            {
                var item = products.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue;
                if (item && item.name == "Coffee Beans") return true;
            }
            return false;
        });
        var vendor = (GameObject)new SerializedObject(market).FindProperty("vendorInteractionArea").objectReferenceValue;
        GoTo("InteriorMarket", vendor.transform.position);
        SetBudget(120);
        yield return null;
        var actor = FindAnyObjectByType<PlayerController>();
        var feetOffset = actor.GetComponent<Collider2D>().bounds.center - actor.transform.position;
        GoTo("InteriorMarket", vendor.GetComponent<Collider2D>().bounds.center - feetOffset);
        yield return new WaitForFixedUpdate();
        market.OpenMarket();
        if (!market.IsTradingAllowed) throw new InvalidOperationException("The market did not open.");
        var bean = FindObjectsByType<MarketIngredientSlot>().First(slot => slot.ingredientPrefab && slot.ingredientPrefab.name == "Coffee Beans");
        bean.GetComponent<Button>().onClick.Invoke();
    }

    private static IEnumerator RecipeBook()
    {
        EnterRestaurant();
        foreach (var offer in RestaurantApplianceOffer.All)
            foreach (var recipe in ApplianceRecipeCatalog.ForStation(offer.id)) RecipeBookController.Instance.UnlockRecipe(recipe);
        var menu = RestaurantMenuController.Instance;
        menu.ClearMenu();
        menu.TryAddRecipe(ApplianceRecipeCatalog.ForStation("coffee maker")[0]);
        menu.TryAddRecipe(ApplianceRecipeCatalog.ForStation("blender")[0]);
        FindAnyObjectByType<MenuController>().OpenRestaurantMenu();
        yield return null;
    }

    private static IEnumerator ReloadBlenders()
    {
        var saves = SaveGameService.Instance;
        string sandbox = SaveGameService.TestDirectoryOverride;
        if (string.IsNullOrEmpty(sandbox) || !Path.GetFullPath(saves.SaveFilePath).StartsWith(
                Path.GetFullPath(sandbox).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Start a sandbox session to test saving.");
        var layout = EnterRestaurant();
        var baseline = layout.Capture();
        var offer = RestaurantApplianceOffer.All.First(o => o.id == "blender");
        var floor = layout.transform.Find("TileBackGroundFloor").GetComponent<Tilemap>();
        var actor = FindAnyObjectByType<PlayerController>();
        Vector3 focus = actor.transform.position;
        var pair = new string[2];
        var recipes = ApplianceRecipeCatalog.ForStation(offer.id);
        SetBudget(offer.price * 2);
        for (int i = 0; i < pair.Length; i++)
        {
            if (!layout.TryBuyAppliance(offer)) throw new InvalidOperationException("Blender purchase failed.");
            var blender = layout.Pieces.Last(p => p.purchasedType == offer.id);
            var counter = layout.Pieces.First(p => p.stock && p.acceptsItem && !p.Placed);
            var cells = floor.cellBounds.allPositionsWithin;
            var positions = new System.Collections.Generic.List<Vector3>();
            foreach (var cell in cells) if (floor.HasTile(cell)) positions.Add(floor.GetCellCenterWorld(cell));
            var placed = positions.OrderBy(p => (p - focus).sqrMagnitude).Any(p => layout.Place(counter, p));
            if (!placed || !layout.Place(blender, counter.Anchor)) throw new InvalidOperationException("No available counter position.");
            pair[i] = blender.id;
            focus = counter.Anchor;
            Supply(recipes[i]);
            if (!blender.root.GetComponent<KitchenAppliance>().TryStart(recipes[i])) throw new InvalidOperationException("Could not start blender.");
        }
        StandNear(layout.Pieces.First(p => p.id == pair[0]).root.GetComponent<KitchenAppliance>());
        if (!saves.SaveCheckpoint()) throw new InvalidOperationException(saves.LastError);
        layout.Restore(baseline);
        yield return null;
        saves.LoadCheckpoint();
        if (!saves.IsReady || saves.LoadFailed) throw new InvalidOperationException(saves.LastError);
        for (int i = 0; i < pair.Length; i++)
        {
            var appliance = layout.Pieces.First(p => p.id == pair[i]).root.GetComponent<KitchenAppliance>();
            if (appliance.CurrentRecipe != recipes[i]) throw new InvalidOperationException("The blender did not recover its dish.");
        }
    }
}

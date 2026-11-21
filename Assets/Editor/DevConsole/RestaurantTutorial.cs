using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Object;

public static class RestaurantTutorial
{
    public static IEnumerator Run()
    {
        var layout = KitchenShowcase.EnterRestaurant();
        var tablePiece = layout.Pieces.Where(p => p.Placed && p.root.GetComponent<Table>())
            .OrderByDescending(p => p.root.GetComponent<Table>().Capacity).First();
        layout.RestrictToStarterSet(p => p == tablePiece || p.id == "kitchen/stove" ||
            p.id == "kitchen/pan" || p.id == "kitchen/pot" || p.id == "kitchen/dishes" ||
            p.id == "kitchen/vegetables" || p.id == "kitchen/refrigerator" ||
            p.id == "kitchen/counter" || p.id.StartsWith("kitchen/front counter") ||
            p.id.StartsWith("kitchen/back counter"));
        var table = tablePiece.root.GetComponent<Table>();
        var seats = new SerializedObject(table);
        seats.FindProperty("availableChairCount").intValue = 4;
        var chairs = seats.FindProperty("chairs");
        for (int i = 0; i < chairs.arraySize; i++)
            ((Transform)chairs.GetArrayElementAtIndex(i).objectReferenceValue).gameObject.SetActive(true);
        seats.ApplyModifiedPropertiesWithoutUndo();
        var spawner = FindAnyObjectByType<CustomerSpawner>();
        spawner.enabled = false;
        spawner.StopAllCoroutines();
        spawner.SetRestaurantOpen(false);
        var stove = FindAnyObjectByType<StoveController>();
        var cookware = FindAnyObjectByType<Pan>().InitialItem;
        var recipe = stove.recipes.Where(r => r.ingredients.Any(i => i.itemPrefab == cookware) &&
            r.ingredients.Any(i => i.itemPrefab.name == "lettuce")).First();
        var ingredients = recipe.ingredients.Where(i => i.itemPrefab != cookware).ToArray();
        // Keep the lesson available while the player reads directions.
        var settings = new SerializedObject(stove);
        settings.FindProperty("burnDuration").floatValue = 3600;
        settings.ApplyModifiedPropertiesWithoutUndo();
        var fridge = FindAnyObjectByType<InventoryRefrigeratorController>();
        var produce = FindAnyObjectByType<InventoryVegetablesController>();
        var blenderRecipe = ApplianceRecipeCatalog.ForStation("blender")[0];
        var carrots = blenderRecipe.ingredients[0];
        var greens = ingredients.Where(i => i.itemPrefab.name == "lettuce").ToArray();
        var chilled = ingredients.Except(greens).ToArray();
        Fill(fridge.refrigeratorPanel, fridge.slotPrefab, fridge.slotCount, chilled);
        Fill(produce.vegetablesPanel, produce.slotPrefab, produce.slotCount,
            greens.Concat(new[]{carrots}).ToArray());
        var menu = RestaurantMenuController.Instance;
        menu.ClearMenu();
        var offer = RestaurantApplianceOffer.All.First(o => o.id == "blender");
        var wallet = CoinManager.Instance;
        wallet.RestoreEconomy(Mathf.Max(0, offer.price - wallet.GetDishSalePrice(recipe.basePrice)), 1, false);
        var bag = FindAnyObjectByType<InventoryController>().inventoryPanel;
        HotbarController hotbar = null;
        if (!HotbarController.Resolve(ref hotbar)) throw new InvalidOperationException("The player hotbar is unavailable.");
        var bar = hotbar.hotbarPanel;
        int Held(GameObject prefab) => new[]{bag,bar}.SelectMany(panel => panel.GetComponentsInChildren<Slot>(true))
            .Distinct().Where(slot => slot.inventoryItem?.itemPrefab == prefab).Sum(slot => slot.inventoryItem.amount);
        string dish = ItemTooltipCatalog.GetName(recipe.result.itemPrefab);
        string list = string.Join(", ", ingredients.Select(Describe));
        Guide("Gather ingredients", "Fridge: " + string.Join(", ", chilled.Select(Describe)) + ". Produce counter: " +
            string.Join(", ", greens.Select(Describe)) + ". Press E and shift-click to take items.");
        yield return new WaitUntil(() => ingredients.All(i => Held(i.itemPrefab) >= i.amount));
        Guide("Mount the frying pan", "Press E at the cookware counter. Select the frying pan in your hotbar, then click the stove.");
        yield return new WaitUntil(() => stove.Mounted == cookware);
        Guide("Cook " + dish, "Press E at the stove. Add exactly " + list + " and confirm.");
        yield return new WaitUntil(() => stove.CurrentRecipe == recipe || Held(recipe.result.itemPrefab) > 0);
        Guide("Plate the food", "Press E at the plate stack. Select the plate, then press E at the stove when the food is ready.");
        yield return new WaitUntil(() => Held(recipe.result.itemPrefab) > 0);
        Guide("Set the menu", "Open the restaurant menu board and add " + dish + " from your discovered recipes.");
        yield return new WaitUntil(() => menu.IsInMenu(recipe));
        Guide("Open for business", "Go outside and press E at the restaurant sign. Then return to the dining room.");
        yield return new WaitUntil(() => spawner.IsRestaurantOpen);
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        var data = new SerializedObject(spawner);
        var prefab = (GameObject)data.FindProperty("customerPrefabA").objectReferenceValue;
        var outside = (Transform)data.FindProperty("restaurantOutside").objectReferenceValue;
        var door = (Transform)data.FindProperty("restaurantInside").objectReferenceValue;
        var stops = data.FindProperty("villageStops");
        var origins = Enumerable.Range(0, stops.arraySize)
            .Select(i => (Transform)stops.GetArrayElementAtIndex(i).objectReferenceValue)
            .Where(t => t).OrderBy(t => (t.position - outside.position).sqrMagnitude);
        Physics2D.SyncTransforms();
        var origin = outside.position;
        List<Vector3> approach = null;
        foreach (var stop in origins)
        {
            var route = CustomerFootpath.Find(stop.position, outside.position, outside.gameObject.activeInHierarchy);
            if (route == null) continue;
            origin = stop.position;
            approach = route;
            break;
        }
        var guest = Instantiate(prefab, origin, Quaternion.identity).GetComponent<CustomerNPC>();
        guest.SetSeated(false);
        Guide("Your first customer", "Your first guest is on the way. Meet them in the dining room.");
        yield return WalkGuest(guest, approach);
        guest.transform.position = door.position;
        var chair = table.GetRandomFreeChair();
        var target = chair.position + Vector3.down * .25f;
        if (door.gameObject.activeInHierarchy)
            yield return WalkGuest(guest, CustomerFootpath.Find(door.position, target, true, chair));
        var order = guest.GetComponent<CustomerOrderController>();
        var patience = new SerializedObject(order);
        patience.FindProperty("patienceDuration").floatValue = 3600;
        patience.ApplyModifiedPropertiesWithoutUndo();
        // Only the tutorial dish is offered to the first guest.
        menu.ClearMenu();
        menu.TryAddRecipe(recipe);
        order.Initialize(stove);
        guest.SitAt(table, chair);
        Guide("Take the order", "Walk to the seated guest and press E to hear their order.");
        yield return new WaitUntil(() => order.OrderRevealed);
        int balance = wallet.TotalCoins;
        Guide("Serve " + dish, "Select the dish and press E beside the guest, click the guest or their table, or drag the dish onto them.");
        yield return new WaitUntil(() => order.OrderCompleted);
        Guide("Collect payment", "Once your guest finishes eating, walk over the coin by their seat.");
        yield return new WaitUntil(() => wallet.TotalCoins > balance);
        var computer = FindAnyObjectByType<KeyboardUpgradeStation>(FindObjectsInactive.Include);
        environment.RestoreHomeCheckpoint(computer.transform.position + Vector3.down * .6f);
        Guide("Buy a blender", "Press E at the home computer. Open Appliances, choose Blender and buy it.");
        yield return new WaitUntil(() => layout.Pieces.Any(p => p.purchasedType == "blender"));
        Guide("Return to the restaurant", "Close the computer to return to the kitchen.");
        yield return new WaitUntil(() => !InventoryPanelSession.HasOpenPanel);
        KitchenShowcase.EnterRestaurant();
        Guide("Place the blender", "Press B to edit. Select your blender and click an empty counter. Press B to finish.");
        yield return new WaitUntil(() => layout.Pieces.Any(p => p.purchasedType == "blender" && p.Placed) && !RestaurantFurnitureEditor.Editing);
        Guide("Make carrot juice", "Take two carrots from the produce counter. Open the blender, add exactly two carrots, confirm and collect the juice.");
        yield return new WaitUntil(() => Held(blenderRecipe.result.itemPrefab) > 0);
    }

    private static IEnumerator WalkGuest(CustomerNPC guest, List<Vector3> path)
    {
        if (path == null) yield break;
        foreach (var point in path)
            while (Vector2.Distance(guest.transform.position, point) > .02f)
            {
                guest.transform.position = Vector3.MoveTowards(guest.transform.position, point, 3 * Time.deltaTime);
                yield return null;
            }
    }

    private static string Describe(InventoryItem item) => item.amount + " × " + ItemTooltipCatalog.GetName(item.itemPrefab);
    private static void Guide(string title, string body) => ShowcaseLauncher.SetStep(title + "\n" + body);

    private static void Fill(GameObject panel, GameObject slotPrefab, int count, InventoryItem[] items)
    {
        foreach (var slot in panel.GetComponentsInChildren<Slot>(true))
        {
            slot.transform.SetParent(null);
            slot.gameObject.SetActive(false);
            Destroy(slot.gameObject);
        }
        for (int i = 0; i < Mathf.Max(count,items.Length); i++)
            InventoryStacking.CreateSlot(slotPrefab, panel.transform, i < items.Length ? items[i] : null);
    }
}

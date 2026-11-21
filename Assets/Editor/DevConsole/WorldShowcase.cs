using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.Object;

// Registered by the in-game editor test menu. Each launch gets a fresh village and its own save.
public static class WorldShowcase
{
    public static void Register(Action<string, string, Func<IEnumerator>> add)
    {
        add("Restaurant furniture", "Click a piece or scroll the construction bar. Place it on the grid. Press B to finish and walk around it.", Furniture);
        add("Garden soil and planting", "Front row, left to right: plain dirt, tilled soil and watered soil. Select a tool or seeds, then left-click a nearby plot.", Garden);
        add("Residents and orders", "Watch the lunch guests enter and take a seat. Approach a seated guest and press E to reveal their order. The menu dishes are in your hotbar.", () => Restaurant(false));
        add("Restaurant rating", "This demo compares an empty menu with a stocked, well-served restaurant, then opens for lunch. The rating is normally hidden.", () => Restaurant(true));
        foreach (VillageWeatherKind kind in Enum.GetValues(typeof(VillageWeatherKind)))
        {
            string name = kind == VillageWeatherKind.AfterRain ? "After rain" : kind.ToString();
            add("Weather / " + name, "Explore the village at noon. Choose another weather demo to compare the same area under a different sky.", () => Weather(kind));
        }
        add("Water reflection", "Walk along the bank and turn near the water. The rippled reflection follows your current animation.", Reflection);
        add("Procedural dungeon", "Explore the new rooms, torchlight and passages. Launch this demo again for a different expedition.", () => Dungeon(0));
        add("Fairy / falling stars", "Dodge the marked falling stars and frost cone. Counterattack with your sword or press Space to roll.", () => Dungeon(1));
        add("Fairy / ice and fire", "Escape the expanding blast, weave between spiral stars and defeat the fairy. Open her treasure chest, then use the green gate.", () => Dungeon(2));
    }

    private static void Command(string command)
    {
        var result = DevConsoleCommands.Execute(command);
        if (!result.Success) throw new InvalidOperationException(string.Join(" ", result.Lines));
    }

    private static IEnumerator Furniture()
    {
        Command("tp restaurant");
        yield return null;
        var editor = FindAnyObjectByType<RestaurantFurnitureEditor>();
        editor.SendMessage("Toggle", SendMessageOptions.RequireReceiver);
        if (!RestaurantFurnitureEditor.Editing) throw new InvalidOperationException("Could not open the furniture editor.");
    }

    private static IEnumerator Garden()
    {
        Command("tp village");
        yield return null;
        var area = FindAnyObjectByType<FarmArea>();
        var farm = FarmingSaveModule.Instance;
        if (!area || !farm) throw new InvalidOperationException("The village garden is unavailable.");
        var front = area.PlantableCells.OrderBy(cell => cell.y).ThenBy(cell => cell.x)
            .First(cell => area.CanPlantAt(cell + Vector3Int.right) && area.CanPlantAt(cell + 2 * Vector3Int.right));
        var dry = front + Vector3Int.right;
        var wet = dry + Vector3Int.right;
        FindAnyObjectByType<GameClock>().SetTotalGameMinutes(720);
        if (!farm.TryTill(area, dry) || !farm.TryTill(area, wet) || !farm.TryWater(area, wet))
            throw new InvalidOperationException("Could not prepare the three soil samples.");
        if (!FindAnyObjectByType<EnvironmentTransition>().RestorePlayerCheckpoint("", area.CellCenter(dry) + Vector3.up * .85f))
            throw new InvalidOperationException("Could not reach the garden.");

        var bar = DemoHotbar();
        int Supply(string prefabName, int amount)
        {
            var item = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Farming/{prefabName}.prefab");
            if (!item || !bar.AddItem(new InventoryItem { itemPrefab = item, amount = amount }))
                throw new InvalidOperationException("Could not supply " + prefabName + ".");
            return Array.FindIndex(bar.hotbarPanel.GetComponentsInChildren<Slot>(true), slot => slot.inventoryItem?.itemPrefab == item);
        }
        int hoe = Supply("Hoe", 1);
        int can = Supply("WateringCan", 1);
        int seed = Supply("CarrotSeeds", 8);
        if (!bar.TryUseSlot(hoe)) throw new InvalidOperationException("Could not equip the hoe.");
        ShowcaseLauncher.Note($"{hoe + 1}: Hoe     {can + 1}: Watering can     {seed + 1}: Carrot seeds");
    }

    private static IEnumerator Weather(VillageWeatherKind kind)
    {
        Command("tp village");
        FindAnyObjectByType<GameClock>().SetTotalGameMinutes(720);
        Command("/weather " + kind);
        yield return null;
        if (kind != VillageWeatherKind.AfterRain) yield break;
        var avatar = GameObject.FindGameObjectWithTag("Player");
        var puddle = FindAnyObjectByType<VillageWeatherEffects>().GetComponentsInChildren<SpriteRenderer>()
            .Where(p => p.enabled && p.sprite && p.sprite.name == "Puddle")
            .OrderBy(p => (p.transform.position - avatar.transform.position).sqrMagnitude)
            .FirstOrDefault(p => !Physics2D.OverlapCircleAll((Vector2)p.transform.position + Vector2.down * .45f, .25f).Any(c => !c.isTrigger));
        if (puddle) FindAnyObjectByType<EnvironmentTransition>().RestorePlayerCheckpoint("", puddle.transform.position + Vector3.down * .45f);
    }

    private static IEnumerator Reflection()
    {
        Command("tp village");
        FindAnyObjectByType<GameClock>().SetTotalGameMinutes(720);
        var bank = FindObjectsByType<Tilemap>().First(t => t.name == "TileFloorHouse");
        var point = bank.GetCellCenterWorld(new Vector3Int(0, -11, 0)) + Vector3.up * .55f;
        if (!FindAnyObjectByType<EnvironmentTransition>().RestorePlayerCheckpoint("", point))
            throw new InvalidOperationException("Could not reach the riverbank.");
        yield return null;
    }

    private static HotbarController DemoHotbar()
    {
        HotbarController bar = null;
        if (!HotbarController.Resolve(ref bar)) throw new InvalidOperationException("The hotbar is unavailable.");
        return bar;
    }

    private static IEnumerator Dungeon(int phase)
    {
        var sword = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Sword.prefab");
        var potions = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Potions/red.prefab");
        var bar = DemoHotbar();
        if (!bar.AddItem(new InventoryItem { itemPrefab = sword, amount = 1 }) ||
            !bar.AddItem(new InventoryItem { itemPrefab = potions, amount = 5 }))
            throw new InvalidOperationException("Could not equip the dungeon kit.");
        int swordIndex = Array.FindIndex(bar.hotbarPanel.GetComponentsInChildren<Slot>(true), s => s.inventoryItem?.itemPrefab == sword);
        bar.TryUseSlot(swordIndex);
        var transition = FindAnyObjectByType<SceneTransitionController>();
        transition.LoadDungeon();
        float until = Time.realtimeSinceStartup + 15f;
        while (transition.IsLoading || !FindAnyObjectByType<DungeonExpedition>())
        {
            if (Time.realtimeSinceStartup > until) throw new InvalidOperationException("Dungeon did not finish loading.");
            yield return null;
        }
        if (phase == 0)
        {
            var run = FindAnyObjectByType<DungeonExpedition>();
            ShowcaseLauncher.Note($"Expedition {run.Seed}  •  {(run.OriginalArea + run.AddedArea) / (float)run.OriginalArea:F1}× floor area");
            yield break;
        }
        var door = FindAnyObjectByType<DungeonLayout>().GetComponentInChildren<DungeonBossDoor>();
        door.RestoreCheckpoint(true, door.Destination);
        if (!door.TryEnter(GameObject.FindGameObjectWithTag("Player"))) throw new InvalidOperationException("The boss room is unavailable.");
        var fairy = FindAnyObjectByType<FairyBoss>();
        if (!fairy) throw new InvalidOperationException("The fairy did not appear.");
        if (phase == 2) fairy.RestoreHealth(Mathf.Max(1, fairy.Health / 2));
    }

    private static IEnumerator Restaurant(bool rating)
    {
        Command("tp restaurant");
        var menu = RestaurantMenuController.Instance;
        var history = SaveGameService.Instance.GetComponent<RestaurantHistoryModule>();
        int seats = FindObjectsByType<Table>().Sum(t => t.Capacity);
        menu.ClearMenu();
        float before = history.Rating(0, seats);
        var bar = DemoHotbar();
        foreach (var recipe in FindAnyObjectByType<StoveController>().recipes.Where(r => r?.result?.itemPrefab).Take(4))
        {
            RecipeBookController.Instance.UnlockRecipe(recipe);
            if (!menu.TryAddRecipe(recipe) || !bar.AddItem(new InventoryItem { itemPrefab = recipe.result.itemPrefab, amount = 3 }))
                throw new InvalidOperationException("Could not prepare lunch.");
        }
        if (rating)
        {
            for (int i = 0; i < 8; i++) { history.RecordArrival(true); history.RecordService(true); }
            float after = history.Rating(menu.SelectedDishCount, seats);
            ShowcaseLauncher.Note($"Demo rating: {before:P0} → {after:P0} after adding dishes and eight on-time meals.");
        }
        // Compress travel and make the visit window predictable in the disposable demo only.
        var spawner = FindAnyObjectByType<CustomerSpawner>();
        var staging = new SerializedObject(spawner);
        var schedules = staging.FindProperty("schedules");
        for (int i = 0; i < schedules.arraySize; i++)
        {
            var schedule = schedules.GetArrayElementAtIndex(i);
            schedule.FindPropertyRelative("interest").floatValue = 1f;
            schedule.FindPropertyRelative("firstMeal").intValue = 12;
            schedule.FindPropertyRelative("secondMeal").intValue = 18;
        }
        staging.FindProperty("walkSpeed").floatValue = 8f;
        staging.FindProperty("spawnInterval").floatValue = .5f;
        staging.ApplyModifiedPropertiesWithoutUndo();
        FindAnyObjectByType<GameClock>().SetTotalGameMinutes(12 * 60 + 40);
        spawner.SetRestaurantOpen(true);
        spawner.SpawnCustomer();
        yield return null;
    }
}

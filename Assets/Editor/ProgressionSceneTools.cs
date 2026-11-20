using System.Collections;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;

[InitializeOnLoad]
public static class ProgressionSceneTools
{
    private const string Flag = "UnderGrub.ProgressionChecks";
    private const string PlaygroundFlag = "UnderGrub.ProgressionPlayground";
    private const string Report = "Library/progression-checks.txt";
    static ProgressionSceneTools()
    {
        if (SessionState.GetBool(Flag, false)) Isolate();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode && SessionState.GetBool(Flag, false)) Isolate();
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false))
            {
                SessionState.SetBool(Flag, false);
                Application.runInBackground = true;
                EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
                bool playground = SessionState.GetBool(PlaygroundFlag, false);
                SessionState.SetBool(PlaygroundFlag, false);
                FindFirstObjectByType<PlayerController>().StartCoroutine(playground ? PreparePlayground() : Guarded());
            }
            if (state == PlayModeStateChange.EnteredEditMode) SaveGameService.TestDirectoryOverride = null;
        };
    }
    private static void Isolate() => SaveGameService.TestDirectoryOverride = Path.GetFullPath("Library/ProgressionChecks/" + Guid.NewGuid());
    [MenuItem("UnderGrub/Progression/Start village playground")]
    public static void RunPlayground()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        SessionState.SetBool(PlaygroundFlag, true);
        Run();
    }
    private static IEnumerator PreparePlayground()
    {
        yield return new WaitForSecondsRealtime(1);
        if (!SaveGameService.Instance.IsReady) yield break;
        var bar = FindAnyObjectByType<HotbarController>();
        foreach (string name in new[] { "Potions/pink", "Farming/Hoe", "Farming/WateringCan", "Farming/CarrotSeeds", "Farming/PotatoSeeds", "Farming/BeetrootSeeds" })
            InventoryStacking.AddItem(bar.hotbarPanel.transform, new InventoryItem {
                itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab"), amount = name.EndsWith("Seeds") ? 10 : name.EndsWith("pink") ? 5 : 1 });
        CoinManager.Instance.AddCoins(5000);
        var clock = FindAnyObjectByType<GameClock>(); clock.SetTotalGameMinutes(7 * 60);
        var area = FindAnyObjectByType<FarmArea>(); var cell = area.PlantableCells[0];
        var farm = FarmingSaveModule.Instance; var crop = farm.Catalog.crops[0];
        var plots = new FarmSaveState();
        plots.plots.Add(new FarmPlotData { areaId = area.AreaId, x = cell.x, y = cell.y, cropId = crop.id,
            stage = crop.growthStages.Length - 1, processedDay = farm.CurrentDay });
        farm.RestoreState(JsonUtility.ToJson(plots), farm.SaveVersion);
        var player = FindAnyObjectByType<PlayerController>(); var position = area.CellCenter(cell) + Vector3.down;
        player.transform.position = position; player.GetComponent<Rigidbody2D>().position = position;
        var camera = FindAnyObjectByType<CinemachineCamera>(); if (camera) camera.PreviousStateIsValid = false;
        Physics2D.SyncTransforms();
        EditorApplication.isPaused = false;
        Time.timeScale = 1;
    }
    [MenuItem("UnderGrub/Progression/Run Play Mode checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        SessionState.SetBool(Flag, true);
        if (!SessionState.GetBool(PlaygroundFlag, false)) File.WriteAllText(Report, "Starting isolated progression checks\n");
        EditorApplication.EnterPlaymode();
    }
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        File.AppendAllText(Report, "PASS " + message + "\n");
    }
    private static Slot BarSlot(HotbarController bar, int index) => bar.hotbarPanel.GetComponentsInChildren<Slot>(true)[index];
    private static IEnumerator Guarded()
    {
        var flow = Flow();
        while (true)
        {
            object next;
            try { if (!flow.MoveNext()) break; next = flow.Current; }
            catch (Exception e) { File.AppendAllText(Report, "FAIL " + e + "\n"); Debug.LogException(e); yield break; }
            yield return next;
        }
        File.AppendAllText(Report, "ALL CHECKS PASSED\n");
        EditorApplication.isPaused = true;
    }
    private static IEnumerator Flow()
    {
        yield return new WaitForSecondsRealtime(1);
        var service = SaveGameService.Instance;
        Check(service != null && service.IsReady, "isolated save is ready");
        var clock = FindFirstObjectByType<GameClock>();
        var lighting = FindFirstObjectByType<VillageDayNight>();
        Check(lighting != null, "village lighting is authored");
        Check(lighting.EvaluateDarkness(12) == 0 && lighting.EvaluateDarkness(17) == 0 &&
            Mathf.Approximately(lighting.EvaluateDarkness(17.5f), .5f) && lighting.EvaluateDarkness(18) == 1 &&
            lighting.EvaluateDarkness(0) == 1 && lighting.EvaluateDarkness(7) == 0, "day, sunset, midnight and morning lighting");
        clock.SetTotalGameMinutes(18 * 60); yield return null;
        Check(lighting.Darkness > .99f, "clock updates night lighting");
        Check(Mathf.Approximately(lighting.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity, .28f),
            "night uses the darker configured intensity");
        var recipe = FindFirstObjectByType<StoveController>().recipes.First(r => r.result?.itemPrefab != null);
        Check(!ItemTooltipCatalog.ForRecipe(recipe, false).Body.Contains("Ingredients:"), "locked recipes hide ingredients");
        var recipeController = FindFirstObjectByType<StoveController>();
        var exactIngredients = recipe.ingredients.Select(i => new InventoryItem { itemPrefab = i.itemPrefab, amount = i.amount }).ToArray();
        Check(recipeController.FindRecipe(exactIngredients) == recipe, "merged stove accepts exact recipe quantities");
        exactIngredients[0].amount++;
        Check(recipeController.FindRecipe(exactIngredients) == null, "merged stove rejects surplus ingredients");
        Check(ItemTooltipCatalog.ForRecipe(recipe, true).Body.Contains("Preparation:") &&
            ItemTooltipCatalog.ForRecipe(recipe, true).Body.Contains(" x1"), "unlocked recipes show quantities and preparation");
        var area = FindFirstObjectByType<FarmArea>();
        var farm = FarmingSaveModule.Instance;
        var crop = AssetDatabase.FindAssets("t:CropDefinition").Select(id => AssetDatabase.LoadAssetAtPath<CropDefinition>(AssetDatabase.GUIDToAssetPath(id))).First();
        var cell = area.PlantableCells[0];
        clock.SetTotalGameMinutes(7 * 60); farm.SynchronizeDay();
        Check(farm.TryTill(area, cell) && farm.TryPlant(area, cell, crop), "prepare isolated crop");
        clock.SetTotalGameMinutes(1440 + 420); farm.SynchronizeDay();
        Check(farm.GetPlot(area.AreaId, cell).stage == 0, "dry crops do not grow");
        farm.TryWater(area, cell);
        clock.SetTotalGameMinutes(2880 + 420); farm.SynchronizeDay();
        Check(farm.GetPlot(area.AreaId, cell).stage == 1, "watered crop advances one stage without glove");
        UpgradePurchases.RegisterPurchase(UpgradePurchases.GrowthGlove);
        farm.TryWater(area, cell);
        clock.SetTotalGameMinutes(4320 + 420); farm.SynchronizeDay();
        Check(farm.GetPlot(area.AreaId, cell).stage == Mathf.Min(3, crop.growthStages.Length - 1), "glove advances two stages and clamps maturity");
        while (!crop.IsMature(farm.GetPlot(area.AreaId, cell).stage))
        {
            farm.TryWater(area, cell); clock.SetTotalGameMinutes(clock.TotalGameMinutes + 1440); farm.SynchronizeDay();
        }
        var player = FindFirstObjectByType<PlayerController>();
        player.transform.position = area.CellCenter(cell);
        player.GetComponent<Rigidbody2D>().position = player.transform.position;
        Physics2D.SyncTransforms(); yield return null;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.E)); yield return new WaitForSecondsRealtime(.15f);
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
        Check(string.IsNullOrEmpty(farm.GetPlot(area.AreaId, cell).cropId), "E harvests nearest mature crop and retains tilled soil");
        var region = CameraBoundaryRegion.Find(player.transform.position);
        Check(region != null, "garden has camera bounds");
        foreach (float aspect in new[] { 4f / 3, 16f / 9, 21f / 9 })
        {
            var b = region.WorldBounds;
            var confined = region.Confine(new Vector3(b.xMax + 100, b.yMax + 100, -10), 5, aspect);
            Check(confined.x + 5 * aspect <= b.xMax + .01f && confined.y + 5 <= b.yMax + .01f, "camera viewport confined at aspect " + aspect);
        }
        var bar = FindFirstObjectByType<HotbarController>();
        var potion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Potions/pink.prefab");
        Check(potion.GetComponent<PotionItem>().Effect is SavePotionEffect, "pink potion has checkpoint effect");
        Check(InventoryStacking.AddItem(bar.hotbarPanel.transform, new InventoryItem { itemPrefab = potion, amount = 3 }), "pink potion added to hotbar");
        int index = Enumerable.Range(0, 6).First(i => BarSlot(bar, i).inventoryItem?.itemPrefab == potion);
        double before = clock.TotalGameMinutes;
        Check(bar.TryUseSlot(index), "pink potion saves from hotbar in village");
        Check(BarSlot(bar, index).inventoryItem.amount == 2 && Math.Abs(clock.TotalGameMinutes - before) < 1, "save consumes one potion without advancing day");
        service.LoadCheckpoint(); yield return null;
        Check(service.IsReady && BarSlot(bar, index).inventoryItem.amount == 2, "village reload preserves potion consumption");
        string blockedTemp = service.SaveFilePath + ".tmp";
        Directory.CreateDirectory(blockedTemp);
        try { Check(!bar.TryUseSlot(index) && BarSlot(bar, index).inventoryItem.amount == 2, "failed save returns potion to its original stack"); }
        finally { Directory.Delete(blockedTemp); }
        var transitions = FindFirstObjectByType<SceneTransitionController>();
        transitions.LoadDungeon(); yield return new WaitForSecondsRealtime(.5f);
        foreach (var enemy in FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None)) enemy.enabled = false;
        var health = player.GetComponent<PlayerHealth>();
        health.TakeDamage(10); yield return new WaitForSecondsRealtime(.7f);
        var chest = FindFirstObjectByType<DungeonChest>();
        Check(chest.TryOpen(), "open a chest before dungeon checkpoint");
        var regeneration = TimedPotionEffects.Instance.Definitions.First(effect => effect.Kind == TimedPotionKind.Regeneration);
        Check(TimedPotionEffects.Instance.TryApply(regeneration, health), "activate regeneration before dungeon checkpoint");
        int enemyCount = FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None).Length;
        var savedPosition = player.transform.position;
        var savedLayout = FindFirstObjectByType<DungeonLayout>().DisplayName;
        Check(bar.TryUseSlot(index), "pink potion saves in dungeon with village modules cached");
        Check(service.IsReady, "dungeon save remains usable");
        service.LoadCheckpoint(); yield return new WaitForSecondsRealtime(.8f);
        Check(service.IsReady && !transitions.IsLoading && SceneManager.GetActiveScene().name == "Dungeon", "reload restores dungeon scene");
        Check(FindFirstObjectByType<DungeonLayout>().DisplayName == savedLayout && Vector3.Distance(player.transform.position, savedPosition) < .1f,
            "reload preserves layout and exact player location");
        Check(health.Current == 90 && FindObjectsByType<DungeonChest>(FindObjectsSortMode.None).Any(c => c.IsOpened), "reload preserves health and opened chests");
        Check(BarSlot(bar, index).inventoryItem.amount == 1, "dungeon reload does not duplicate save potion");
        Check(FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None).Length == enemyCount &&
            TimedPotionEffects.Instance.RemainingSeconds(regeneration.EffectId) > 0, "reload preserves enemies and regeneration buff");
        transitions.ReturnToFirstScene(); yield return new WaitForSecondsRealtime(.8f);
        Check(TimedPotionEffects.Instance.RemainingSeconds(regeneration.EffectId) == 0, "regeneration still ends on returning to village");
        Check(FindFirstObjectByType<StoveController>() != null && service.SaveCheckpoint(), "village modules still save after returning from restored dungeon");
        for (int visit = 0; visit < 2; visit++)
        {
            if (visit > 0)
            {
                transitions.LoadDungeon(); yield return new WaitForSecondsRealtime(.5f);
                transitions.ReturnToFirstScene(); yield return new WaitForSecondsRealtime(.8f);
            }
            Check(FindObjectsByType<InventoryWorkspace>(FindObjectsSortMode.None).Length == 1,
                "only the persistent inventory workspace survives village reload " + visit);
            var environment = FindAnyObjectByType<EnvironmentTransition>();
            var pan = FindFirstObjectByType<Pan>(FindObjectsInactive.Include);
            Check(environment.RestorePlayerCheckpoint("InteriorRestaurant", pan.transform.position + Vector3.down), "enter restaurant after reload " + visit);
            yield return new WaitForSecondsRealtime(.2f);
            var areaCollider = pan.GetComponent<Collider2D>();
            var playerCollider = player.GetComponent<Collider2D>();
            var target = areaCollider.bounds.center + Vector3.down * .15f;
            player.transform.position += target - playerCollider.bounds.center;
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            Physics2D.SyncTransforms(); yield return new WaitForSecondsRealtime(.15f);
            var panItem = pan.Stored;
            Check(panItem != null && areaCollider.IsTouching(playerCollider), "player overlaps frying pan interaction area " + visit);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.E)); yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Check(pan.Stored == null && bar.GetHotbarItems().Any(i => i.itemPrefab == panItem), "E picks up frying pan into live hotbar after reload " + visit);
            var panSlot = Enumerable.Range(0, 6).First(i => BarSlot(bar, i).inventoryItem?.itemPrefab == panItem);
            Check(bar.TryUseSlot(panSlot), "equip frying pan after reload " + visit);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.E)); yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Check(pan.Stored == panItem, "E returns frying pan to counter after reload " + visit);
            var cooking = FindAnyObjectByType<StoveController>();
            var stoveInteraction = FindAnyObjectByType<Stove>();
            cooking.Mounted = null;
            Check(bar.AddItem(new InventoryItem { itemPrefab = panItem, amount = 1 }), "prepare stove utensil " + visit);
            panSlot = Enumerable.Range(0, 6).First(i => BarSlot(bar, i).inventoryItem?.itemPrefab == panItem);
            bar.TryUseSlot(panSlot);
            target = stoveInteraction.GetComponent<Collider2D>().bounds.center;
            player.transform.position += target - playerCollider.bounds.center;
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            Physics2D.SyncTransforms(); yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.E)); yield return new WaitForSecondsRealtime(.15f);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Check(cooking.Mounted == panItem, "E mounts frying pan on stove after reload " + visit);
            var refrigerator = FindAnyObjectByType<InventoryRefrigeratorController>();
            var freezer = FindAnyObjectByType<InventoryFrizerController>();
            var vegetables = FindAnyObjectByType<InventoryVegetablesController>();
            var stove = FindAnyObjectByType<StoveInventoryController>();
            var panels = new[] { refrigerator.refrigeratorPanel, freezer.frizerPanel, vegetables.vegetablesPanel, stove.stovePanel };
            var bag = FindAnyObjectByType<InventoryController>();
            for (int p = 0; p < panels.Length; p++)
            {
                InventoryPanelSession.Bind(panels[p], null);
                if (p == 0) refrigerator.OpenRefrigerator(null);
                else if (p == 1) freezer.OpenFrizer(null);
                else if (p == 2) vegetables.OpenVegetables(null);
                else stove.Open(() => {});
                yield return null;
                Check(bag.inventoryPanel.activeInHierarchy && bar.hotbarPanel.activeInHierarchy &&
                    panels[p].activeInHierarchy && bag.inventoryPanel.transform.parent == panels[p].transform.parent,
                    panels[p].name + " opens beside backpack and hotbar after reload " + visit);
                var source = bag.inventoryPanel.GetComponentsInChildren<Slot>(true).First(s => s.inventoryItem?.itemPrefab != null);
                var item = source.inventoryItem.itemPrefab;
                int total = source.inventoryItem.amount;
                InventoryWorkspace.QuickMove(source);
                var deposited = panels[p].GetComponentsInChildren<Slot>(true).FirstOrDefault(s => s.inventoryItem?.itemPrefab == item);
                Check(deposited != null && deposited.inventoryItem.amount == total, panels[p].name + " receives backpack items " + visit);
                InventoryWorkspace.QuickMove(deposited);
                Check(deposited.inventoryItem == null || deposited.inventoryItem.amount == 0, panels[p].name + " returns items to player " + visit);
                InventoryPanelSession.CloseCurrent(); yield return null;
                Check(!panels[p].activeInHierarchy && !bag.inventoryPanel.activeInHierarchy, panels[p].name + " closes both inventories " + visit);
            }
            if (visit == 0)
            {
                refrigerator.OpenRefrigerator(null);
                yield return null;
                Check(bag.inventoryPanel.activeInHierarchy, "leave storage open before scene transition regression");
            }
            else
            {
                var trash = FindAnyObjectByType<RestaurantTrash>();
                var trashPanel = new SerializedObject(trash).FindProperty("inventory").objectReferenceValue as ConfirmableInventoryPanel;
                Check(trashPanel != null, "merged restaurant trash retains its panel reference");
                var trashSlots = trashPanel.panelRoot.GetComponentsInChildren<Slot>(true);
                trashPanel.EnsureSlots();
                trashSlots = trashPanel.panelRoot.GetComponentsInChildren<Slot>(true);
                var source = bag.inventoryPanel.GetComponentsInChildren<Slot>(true).First(s => s.inventoryItem?.itemPrefab != null);
                var ingredient = source.inventoryItem.itemPrefab;
                int amount = source.inventoryItem.amount;
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                Action returnItems = () => typeof(RestaurantTrash).GetMethod("ReturnItems", flags).Invoke(trash, null);
                Action discard = () => typeof(RestaurantTrash).GetMethod("Discard", flags).Invoke(trash, null);
                InventoryPanelSession.Bind(trashPanel.panelRoot, null);
                trashPanel.Open(discard, returnItems);
                InventoryWorkspace.QuickMove(source);
                Check(trashSlots.Any(s => s.inventoryItem?.itemPrefab == ingredient), "trash receives pending items after dungeon return");
                trashPanel.Close(); yield return null;
                Check(trashSlots.All(s => s.inventoryItem?.itemPrefab == null) &&
                    bag.GetInventoryItems().Where(i => i.itemPrefab == ingredient).Sum(i => i.amount) == amount,
                    "closing merged trash returns pending items to backpack");
                trashPanel.Open(discard, returnItems);
                source = bag.inventoryPanel.GetComponentsInChildren<Slot>(true).First(s => s.inventoryItem?.itemPrefab == ingredient);
                InventoryWorkspace.QuickMove(source);
                int hotbarCount = bar.GetHotbarItems().Sum(i => i.amount);
                discard(); yield return null;
                Check(trashSlots.All(s => s.inventoryItem?.itemPrefab == null) && bar.GetHotbarItems().Sum(i => i.amount) == hotbarCount,
                    "confirming merged trash removes only pending items");
                Check(service.SaveCheckpoint(), "village save remains valid after merged trash interactions");
            }
        }
    }
}

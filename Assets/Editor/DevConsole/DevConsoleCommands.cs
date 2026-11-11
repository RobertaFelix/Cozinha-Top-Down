#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DevConsoleResult
{
    public bool Success;
    public bool Clear;
    public bool CheckpointLoaded;
    public readonly List<string> Lines = new List<string>();
}

// Command parsing and execution are independent of the window and never compiled for players.
public static class DevConsoleCommands
{
    private sealed class Command
    {
        public string Usage;
        public string Description;
        public bool ReadOnly;
        public Action<string[], DevConsoleResult> Run;
    }
    private static readonly Dictionary<string, Command> commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, string> upgradeNames = new Dictionary<int, string>
    {
        {0, "cooking"}, {1, "shoes"}, {2, "backpack"}, {3, "patience"},
        {4, "wallet"}, {7, "key"}, {9, "dish-prices"}, {11, "advertising"}, {12, "magnet"}
    };
    public static IEnumerable<string> CommandNames => commands.Keys.OrderBy(k => k);

    static DevConsoleCommands()
    {
        Register("help", "help [command]", "List commands or show usage.", Help, true);
        Register("clear", "clear", "Clear console output.", (a, r) => { Count(a, 0); r.Clear = true; }, true);
        Register("status", "status", "Show scene, position, clock and coins.", Status, true);
        Register("money", "money add|remove|set <amount>", "Change the exact coin amount (ignores reward multipliers).", Money);
        Register("items", "items [filter]", "List item identifiers, display names and types.", Items, true);
        Register("give", "give <item> [amount=1] [inventory|hotbar]", "Add an item to the selected container.", (a, r) => ChangeItem(a, r, false, false));
        Register("remove", "remove <item> [amount=1] [inventory|hotbar]", "Remove an item from the selected container.", (a, r) => ChangeItem(a, r, true, false));
        Register("dish", "dish give|remove <dish> [amount=1] [inventory|hotbar]", "Add or remove cooked dishes.", Dish);
        Register("inventory", "inventory [inventory|hotbar]", "Show exact slot contents.", Inventory, true);
        Register("tp", "tp <x> <y> [z] | tp home|village|restaurant|market|dungeon", "Teleport to coordinates or a named destination.", Teleport);
        Register("recipes", "recipes [filter]", "List current village recipes and unlock status.", Recipes, true);
        Register("recipe", "recipe unlock <dish|all>", "Unlock recipe-book entries.", UnlockRecipe);
        Register("upgrade", "upgrade list | upgrade add|remove <name|index>", "Grant or revoke implemented upgrades without payment.", Upgrade);
        Register("time", "time set <HH:mm>", "Set time in the current day, using a 24-hour clock.", SetTime);
        Register("day", "day set|add <number>", "Set a one-based day, or add days, keeping the current time.", SetDay);
        Register("save", "save [path|backup|reset]", "Save, inspect the path, archive a backup, or reset to a fresh game.", Save);
        Register("load", "load", "Reload the last checkpoint in the village scene.", Load);
    }

    public static void Register(string name, string usage, string description, Action<string[], DevConsoleResult> run, bool readOnly = false)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsWhiteSpace) || run == null || commands.ContainsKey(name))
            throw new ArgumentException("Command names must be unique, nonempty words.");
        commands.Add(name, new Command { Usage = usage, Description = description, Run = run, ReadOnly = readOnly });
    }

    public static DevConsoleResult Execute(string input)
    {
        var result = new DevConsoleResult();
        try
        {
            var tokens = Tokenize(input);
            if (tokens.Count == 0) { result.Success = true; return result; }
            Require(commands.TryGetValue(tokens[0], out var command), "Unknown command. Type help.");
            if (!command.ReadOnly)
            {
                Require(Application.isPlaying, "Enter Play Mode before changing game state.");
                Require(!DevConsoleBootstrap.IsResetting, "A new game is already being prepared.");
                bool saveMaintenance = tokens[0].Equals("save", StringComparison.OrdinalIgnoreCase) && tokens.Count == 2 &&
                    new[] { "path", "backup", "reset" }.Contains(tokens[1], StringComparer.OrdinalIgnoreCase);
                Require(saveMaintenance || SaveGameService.Instance == null || SaveGameService.Instance.IsReady, "Wait for the checkpoint to finish loading.");
                var summary = UnityEngine.Object.FindAnyObjectByType<DayEndScreen>();
                Require(saveMaintenance || summary == null || !summary.IsOpen, "Close the end-of-day/save screen before changing game state.");
                var environment = UnityEngine.Object.FindAnyObjectByType<EnvironmentTransition>();
                var scenes = UnityEngine.Object.FindAnyObjectByType<SceneTransitionController>();
                Require((environment == null || !environment.IsTransitioning) && (scenes == null || !scenes.IsLoading), "Wait for the scene transition to finish.");
                Require(!ItemTooltipView.IsDragging, "Finish dragging the item first.");
            }
            command.Run(tokens.Skip(1).ToArray(), result);
            result.Success = true;
        }
        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException || error is OverflowException || error is FormatException || error is System.IO.IOException || error is UnauthorizedAccessException)
        {
            result.Lines.Add("Error: " + error.Message);
        }
        return result;
    }

    public static List<string> Tokenize(string input)
    {
        Require(input != null && input.Length <= 2048, "Command is too long.");
        var tokens = new List<string>();
        var word = new StringBuilder();
        char quote = '\0';
        bool started = false;
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '\\' && i + 1 < input.Length && (input[i + 1] == quote || input[i + 1] == '\\'))
            { word.Append(input[++i]); started = true; continue; }
            if (quote != '\0')
            {
                if (c == quote) quote = '\0'; else word.Append(c);
            }
            else if (c == '"' || c == '\'') { quote = c; started = true; }
            else if (char.IsWhiteSpace(c))
            {
                if (started) { tokens.Add(word.ToString()); word.Clear(); started = false; }
            }
            else { word.Append(c); started = true; }
        }
        Require(quote == '\0', "Unclosed quote.");
        if (started) tokens.Add(word.ToString());
        return tokens;
    }

    private static void Require(bool valid, string message) { if (!valid) throw new ArgumentException(message); }
    private static void Count(string[] args, int min, int max = -1)
    { Require(args.Length >= min && args.Length <= (max < 0 ? min : max), "Wrong argument count. Type help <command>."); }
    private static int Integer(string value, bool zeroAllowed = false)
    {
        Require(int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number >= (zeroAllowed ? 0 : 1), zeroAllowed ? "Expected a non-negative integer." : "Expected a positive integer.");
        return number;
    }
    private static float Coordinate(string value)
    {
        Require(float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) && !float.IsNaN(number) && !float.IsInfinity(number), "Expected a finite coordinate, using a dot for decimals.");
        return number;
    }
    private static T Need<T>() where T : UnityEngine.Object
    {
        var found = UnityEngine.Object.FindAnyObjectByType<T>();
        if (found == null) throw new InvalidOperationException(typeof(T).Name + " is unavailable in this scene.");
        return found;
    }
    private static GameObject Controller()
    {
        var go = GameObject.Find("GameController");
        Require(go != null, "GameController is unavailable. Enter Play Mode first.");
        return go;
    }
    private static Transform Container(string name)
    {
        var go = Controller();
        if (name.Equals("inventory", StringComparison.OrdinalIgnoreCase)) return go.GetComponent<InventoryController>().inventoryPanel.transform;
        if (name.Equals("hotbar", StringComparison.OrdinalIgnoreCase)) return go.GetComponent<HotbarController>().hotbarPanel.transform;
        throw new ArgumentException("Container must be inventory or hotbar.");
    }
    private static SaveItemCatalog Catalog()
    {
        var catalog = SaveItemCatalog.Instance;
        Require(catalog != null, "Rebuild the item catalog using UnderGrub > Save > Rebuild item catalog.");
        return catalog;
    }
    private static GameObject ResolveItem(string id)
    {
        var entries = Catalog().entries.Where(e => e.prefab != null).ToList();
        var exact = entries.Where(e => e.id.Equals(id, StringComparison.OrdinalIgnoreCase) || e.prefab.name.Equals(id, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count == 1) return exact[0].prefab;
        var named = entries.Where(e => ItemTooltipCatalog.GetName(e.prefab).Equals(id, StringComparison.OrdinalIgnoreCase)).ToList();
        Require(named.Count == 1, named.Count > 1 ? "Ambiguous item name; use its prefab identifier from items." : "Unknown item. Use items [filter] to find its identifier.");
        return named[0].prefab;
    }
    private static bool IsDish(GameObject prefab) => AssetDatabase.GetAssetPath(prefab).StartsWith("Assets/Prefabs/Food/Dinner/", StringComparison.OrdinalIgnoreCase);

    private static void Help(string[] args, DevConsoleResult result)
    {
        Count(args, 0, 1);
        if (args.Length == 1)
        {
            Require(commands.TryGetValue(args[0], out var command), "Unknown command.");
            result.Lines.Add(command.Usage + " — " + command.Description);
            return;
        }
        foreach (var name in CommandNames) result.Lines.Add(commands[name].Usage + " — " + commands[name].Description);
        result.Lines.Add("Use quoted display names (give \"Cyan Potion\" 2 hotbar) or prefab IDs (give cyan 2 hotbar). Changes are not automatically saved; use the bed or save explicitly.");
    }
    private static void Status(string[] args, DevConsoleResult result)
    {
        Count(args, 0);
        result.Lines.Add("Play Mode: " + Application.isPlaying + "; scene: " + SceneManager.GetActiveScene().name);
        if (!Application.isPlaying) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) result.Lines.Add("Player: " + player.transform.position.ToString("F3"));
        var environment = UnityEngine.Object.FindAnyObjectByType<EnvironmentTransition>();
        if (environment != null) result.Lines.Add("Interior: " + (string.IsNullOrEmpty(environment.CurrentInteriorId) ? "exterior" : environment.CurrentInteriorId));
        if (CoinManager.Instance != null) result.Lines.Add("Coins: " + CoinManager.Instance.TotalCoins);
        var clock = UnityEngine.Object.FindAnyObjectByType<GameClock>();
        if (clock != null) result.Lines.Add("Day " + ((int)(clock.TotalGameMinutes / 1440) + 1) + ", " + ((int)clock.TotalGameMinutes % 1440 / 60).ToString("00") + ":" + ((int)clock.TotalGameMinutes % 60).ToString("00"));
    }
    private static void Money(string[] args, DevConsoleResult result)
    {
        Count(args, 2);
        var coins = Need<CoinManager>();
        int amount = Integer(args[1], true);
        long total;
        switch (args[0].ToLowerInvariant())
        {
            case "add": total = (long)coins.TotalCoins + amount; break;
            case "remove": total = (long)coins.TotalCoins - amount; break;
            case "set": total = amount; break;
            default: throw new ArgumentException("Use money add|remove|set <amount>.");
        }
        Require(total >= 0 && total <= int.MaxValue, "Not enough coins, or the amount exceeds the supported range.");
        coins.RestoreEconomy((int)total, coins.RewardMultiplier, coins.AreDishPricesDoubled);
        result.Lines.Add("Coins: " + total);
    }
    private static void Items(string[] args, DevConsoleResult result)
    {
        Count(args, 0, 1);
        foreach (var entry in Catalog().entries.Where(e => e.prefab != null).OrderBy(e => e.prefab.name))
        {
            string line = entry.prefab.name + " — " + ItemTooltipCatalog.GetName(entry.prefab) + (IsDish(entry.prefab) ? " [dish]" : entry.prefab.GetComponent<PotionItem>() != null ? " [potion]" : "") + " — " + entry.id;
            if (args.Length == 0 || line.IndexOf(args[0], StringComparison.OrdinalIgnoreCase) >= 0) result.Lines.Add(line);
        }
        if (result.Lines.Count == 0) result.Lines.Add("No matching items.");
    }
    private static void ChangeItem(string[] args, DevConsoleResult result, bool remove, bool dishOnly)
    {
        Count(args, 1, 3);
        var prefab = ResolveItem(args[0]);
        Require(!dishOnly || IsDish(prefab), "That item is not a cooked dish. Use give/remove for ingredients or potions.");
        int amount = args.Length >= 2 ? Integer(args[1]) : 1;
        string target = args.Length == 3 ? args[2] : "inventory";
        var container = Container(target);
        var item = new InventoryItem { itemPrefab = prefab, amount = amount };
        // Check with 64-bit arithmetic before invoking gameplay stacking, which uses ints.
        long available = 0;
        foreach (Transform child in container)
            if (child.TryGetComponent<Slot>(out var slot))
            {
                var data = slot.inventoryItem;
                if (remove) { if (data?.itemPrefab == prefab) available += Math.Max(0, data.amount); }
                else if (data?.itemPrefab == null || data.amount <= 0) available += InventoryStacking.MaxStackSize;
                else if (data.itemPrefab == prefab) available += Math.Max(0, InventoryStacking.MaxStackSize - data.amount);
            }
        Require(available >= amount, remove ? "Not enough of that item in " + target + ". Nothing removed." : "Not enough space in " + target + ". Nothing added.");
        bool success = remove ? InventoryStacking.RemoveItem(container, item) : InventoryStacking.AddItem(container, item);
        Require(success, "Inventory operation was rejected.");
        result.Lines.Add((remove ? "Removed " : "Added ") + amount + " × " + prefab.name + " (" + target + ").");
    }
    private static void Dish(string[] args, DevConsoleResult result)
    {
        Count(args, 2, 4);
        Require(args[0].Equals("give", StringComparison.OrdinalIgnoreCase) || args[0].Equals("remove", StringComparison.OrdinalIgnoreCase), "Use dish give|remove.");
        ChangeItem(args.Skip(1).ToArray(), result, args[0].Equals("remove", StringComparison.OrdinalIgnoreCase), true);
    }
    private static void Inventory(string[] args, DevConsoleResult result)
    {
        Count(args, 0, 1);
        string target = args.Length == 1 ? args[0] : "inventory";
        int index = 1;
        foreach (Transform child in Container(target))
            if (child.TryGetComponent<Slot>(out var slot))
                result.Lines.Add(index++ + ": " + (slot.inventoryItem?.itemPrefab == null ? "empty" : slot.inventoryItem.itemPrefab.name + " × " + slot.inventoryItem.amount));
    }

    private static Transform Marker(string name)
    {
        var marker = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == name);
        Require(marker != null, "Destination marker is unavailable in this scene: " + name);
        return marker;
    }
    private static void Teleport(string[] args, DevConsoleResult result)
    {
        Count(args, 1, 3);
        if (args.Length >= 2)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            Require(player != null, "Player is unavailable.");
            Vector3 destination = new Vector3(Coordinate(args[0]), Coordinate(args[1]), args.Length == 3 ? Coordinate(args[2]) : player.transform.position.z);
            var environment = UnityEngine.Object.FindAnyObjectByType<EnvironmentTransition>();
            if (environment != null) Require(environment.RestorePlayerCheckpoint(environment.CurrentInteriorId, destination), "Teleport was rejected.");
            else
            {
                Vector3 previous = player.transform.position;
                if (player.TryGetComponent<Rigidbody2D>(out var rb)) { rb.position = destination; rb.linearVelocity = Vector2.zero; }
                player.transform.position = destination;
                var camera = UnityEngine.Object.FindAnyObjectByType<CinemachineCamera>();
                if (camera != null) { camera.OnTargetObjectWarped(player.transform, destination - previous); camera.PreviousStateIsValid = false; }
                Physics2D.SyncTransforms();
            }
            result.Lines.Add("Teleported to " + destination.ToString("F3"));
            return;
        }
        string target = args[0].ToLowerInvariant();
        var scenes = Need<SceneTransitionController>();
        var env = UnityEngine.Object.FindAnyObjectByType<EnvironmentTransition>();
        switch (target)
        {
            case "dungeon": scenes.LoadDungeon(); break;
            case "village":
                if (env == null) scenes.ReturnToFirstScene();
                else Require(env.RestorePlayerCheckpoint("", Marker("VillageSpawn").position), "Teleport was rejected.");
                break;
            case "home":
                if (env == null) Require(scenes.TryReturnHome(), "Return home was rejected.");
                else Require(env.RestoreHomeCheckpoint(Marker("InteriorSpawnHouse").position), "Teleport was rejected.");
                break;
            case "restaurant":
            case "market":
                Require(env != null, "Use tp village before entering this interior.");
                string suffix = target == "market" ? "Market" : "Restaurant";
                Require(env.RestorePlayerCheckpoint("Interior" + suffix, Marker("InteriorSpawn" + suffix).position), "Teleport was rejected.");
                break;
            default: throw new ArgumentException("Unknown destination. Use home, village, restaurant, market, dungeon, or numeric coordinates.");
        }
        result.Lines.Add("Teleport requested: " + target);
    }
    private static bool InRecipeBook(CookingRecipe recipe) => UnityEngine.Object.FindObjectsByType<RecipeBookSlot>(FindObjectsInactive.Include)
        .Any(slot => slot.GetRecipe() == recipe);

    private static void Recipes(string[] args, DevConsoleResult result)
    {
        Count(args, 0, 1);
        var stove = Need<StoveController>();
        var book = Need<RecipeBookController>();
        foreach (var recipe in stove.recipes.Where(r => r?.result?.itemPrefab != null))
        {
            string line = recipe.result.itemPrefab.name + " — " + ItemTooltipCatalog.GetName(recipe.result.itemPrefab) + (!InRecipeBook(recipe) ? " [not in configured book]" : book.HasUnlockedRecipe(recipe) ? " [unlocked]" : " [locked]");
            if (args.Length == 0 || line.IndexOf(args[0], StringComparison.OrdinalIgnoreCase) >= 0) result.Lines.Add(line);
        }
    }
    private static void UnlockRecipe(string[] args, DevConsoleResult result)
    {
        Count(args, 2);
        Require(args[0].Equals("unlock", StringComparison.OrdinalIgnoreCase), "Use recipe unlock <dish|all>.");
        var stove = Need<StoveController>();
        var book = Need<RecipeBookController>();
        GameObject prefab = args[1].Equals("all", StringComparison.OrdinalIgnoreCase) ? null : ResolveItem(args[1]);
        var matches = stove.recipes.Where(r => r?.result?.itemPrefab != null && InRecipeBook(r) && (prefab == null || r.result.itemPrefab == prefab)).ToArray();
        Require(matches.Length > 0, "No matching entry in the configured recipe book. Use recipes to list entries, or dish give to create the dish directly.");
        foreach (var recipe in matches) book.UnlockRecipe(recipe);
        result.Lines.Add("Unlocked " + matches.Length + " recipe(s).");
    }
    private static void Upgrade(string[] args, DevConsoleResult result)
    {
        Count(args, 1, 2);
        if (args.Length == 1)
        {
            Require(args[0].Equals("list", StringComparison.OrdinalIgnoreCase), "Use upgrade list.");
            foreach (var pair in upgradeNames) result.Lines.Add(pair.Key + " — " + pair.Value + (UpgradePurchases.Has(pair.Key) ? " [owned]" : ""));
            return;
        }
        bool add = args[0].Equals("add", StringComparison.OrdinalIgnoreCase);
        Require(add || args[0].Equals("remove", StringComparison.OrdinalIgnoreCase), "Use upgrade add|remove.");
        int index;
        if (!int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out index))
        {
            var matches = upgradeNames.Where(p => p.Value.Equals(args[1], StringComparison.OrdinalIgnoreCase)).ToArray();
            Require(matches.Length == 1, "Unknown upgrade. Use upgrade list.");
            index = matches[0].Key;
        }
        Require(upgradeNames.ContainsKey(index), "That upgrade has no implemented effect. Use upgrade list.");
        var owned = new HashSet<int>(UpgradePurchases.GetPurchased());
        if (add) owned.Add(index); else owned.Remove(index);
        UpgradePurchases.RestorePurchases(owned.OrderBy(i => i).ToArray());
        var coins = Need<CoinManager>();
        coins.RestoreEconomy(coins.TotalCoins, coins.RewardMultiplier, owned.Contains(9));
        result.Lines.Add((add ? "Granted " : "Revoked ") + upgradeNames[index] + ". Existing item stacks are preserved.");
    }
    private static void SetTime(string[] args, DevConsoleResult result)
    {
        Count(args, 2);
        Require(args[0].Equals("set", StringComparison.OrdinalIgnoreCase), "Use time set HH:mm.");
        string[] parts = args[1].Split(':');
        Require(parts.Length == 2 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out _) && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out _), "Use a 24-hour time: HH:mm.");
        int hour = Integer(parts[0], true), minute = Integer(parts[1], true);
        Require(hour < 24 && minute < 60, "Time must be between 00:00 and 23:59.");
        var clock = Need<GameClock>();
        clock.SetTotalGameMinutes(Math.Floor(clock.TotalGameMinutes / 1440) * 1440 + hour * 60 + minute);
        result.Lines.Add("Time: " + hour.ToString("00") + ":" + minute.ToString("00"));
    }
    private static void SetDay(string[] args, DevConsoleResult result)
    {
        Count(args, 2);
        int number = Integer(args[1]);
        var clock = Need<GameClock>();
        double day = Math.Floor(clock.TotalGameMinutes / 1440);
        if (args[0].Equals("set", StringComparison.OrdinalIgnoreCase)) day = number - 1;
        else if (args[0].Equals("add", StringComparison.OrdinalIgnoreCase)) day += number;
        else throw new ArgumentException("Use day set|add <positive integer>.");
        double total = day * 1440 + clock.TotalGameMinutes % 1440;
        Require(total < int.MaxValue - 1440, "Day exceeds the supported clock range.");
        clock.SetTotalGameMinutes(total);
        result.Lines.Add("Day: " + ((long)day + 1));
    }
    private static SaveGameService CheckpointService(string[] args)
    {
        Count(args, 0);
        Need<EnvironmentTransition>();
        var saves = Need<SaveGameService>();
        Require(saves.IsReady, "The save service is not ready.");
        return saves;
    }
    private static void Save(string[] args, DevConsoleResult result)
    {
        Count(args, 0, 1);
        if (args.Length == 1)
        {
            var savesService = Need<SaveGameService>();
            switch (args[0].ToLowerInvariant())
            {
                case "path": result.Lines.Add(savesService.SaveFilePath); return;
                case "backup": result.Lines.Add("Backup: " + DevConsoleSaveTools.Archive(savesService.SaveFilePath, false)); return;
                case "reset": DevConsoleBootstrap.RequestReset(); result.Lines.Add("Starting a new game. Existing save files will be archived first."); return;
                default: throw new ArgumentException("Use save, save path, save backup, or save reset.");
            }
        }
        var saves = CheckpointService(args);
        Require(saves.SaveCheckpoint(), "Checkpoint could not be saved: " + saves.LastError);
        result.Lines.Add("Saved: " + saves.SaveFilePath);
    }
    private static void Load(string[] args, DevConsoleResult result)
    {
        var saves = CheckpointService(args);
        Require(System.IO.File.Exists(saves.SaveFilePath) || System.IO.File.Exists(saves.SaveFilePath + ".bak"), "No checkpoint exists yet.");
        saves.LoadCheckpoint();
        result.CheckpointLoaded = true;
        Require(saves.IsReady, "Checkpoint could not be loaded: " + saves.LastError);
        result.Lines.Add("Checkpoint loaded.");
    }
}
#endif

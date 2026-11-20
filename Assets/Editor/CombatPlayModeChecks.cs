using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using static UnityEngine.Object;

[InitializeOnLoad]
public static class CombatPlayModeChecks
{
    private const string Flag = "UnderGrub.CombatChecks";
    private const string PlaygroundFlag = "UnderGrub.CombatPlayground";
    private static readonly string Report = "Library/combat-checks.txt";
    static CombatPlayModeChecks()
    {
        if (SessionState.GetBool(Flag, false)) Isolate();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode && SessionState.GetBool(Flag, false)) Isolate();
            if (state == PlayModeStateChange.EnteredPlayMode) StartChecks();
            if (state == PlayModeStateChange.EnteredEditMode) SaveGameService.TestDirectoryOverride = null;
        };
    }
    [MenuItem("UnderGrub/Combat/Run Play Mode checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        SessionState.SetBool(Flag, true);
        File.WriteAllText(Report, "Starting isolated Play Mode checks\n");
        EditorApplication.EnterPlaymode();
    }
    [MenuItem("UnderGrub/Combat/Start dungeon playground")]
    public static void RunPlayground()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        SessionState.SetBool(PlaygroundFlag, true);
        SessionState.SetBool(Flag, true);
        EditorApplication.EnterPlaymode();
    }
    private static void Isolate()
    {
        if (SessionState.GetBool(Flag, false)) SaveGameService.TestDirectoryOverride = Path.GetFullPath("Library/CombatChecks/" + Guid.NewGuid());
    }
    private static void StartChecks()
    {
        if (!SessionState.GetBool(Flag, false)) return;
        SessionState.SetBool(Flag, false);
        bool playground = SessionState.GetBool(PlaygroundFlag, false);
        SessionState.SetBool(PlaygroundFlag, false);
        FindFirstObjectByType<PlayerCombat>().StartCoroutine(playground ? PreparePlayground() : Guarded());
    }
    private static IEnumerator PreparePlayground()
    {
        yield return new WaitForSeconds(1);
        var hotbar = FindFirstObjectByType<HotbarController>();
        foreach (string path in new[] { "Combat/Sword", "Potions/red", "Potions/green", "Potions/orange" })
            InventoryStacking.AddItem(hotbar.hotbarPanel.transform, new InventoryItem {
                itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + path + ".prefab"),
                amount = path.EndsWith("Sword") ? 1 : 10 });
        hotbar.TryUseSlot(0);
        FindFirstObjectByType<SceneTransitionController>().LoadDungeon();
        yield return new WaitForSeconds(.3f);
        EditorApplication.isPaused = true;
    }
    private static IEnumerator Guarded()
    {
        var tests = CheckFlow();
        while (true)
        {
            object next;
            try { if (!tests.MoveNext()) break; next = tests.Current; }
            catch (Exception error) { File.AppendAllText(Report, "FAIL " + error + "\n"); Debug.LogException(error); yield break; }
            yield return next;
        }
        File.AppendAllText(Report, "ALL CHECKS PASSED\n");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        File.AppendAllText(Report, "PASS " + message + "\n");
    }
    private static IEnumerator CheckFlow()
    {
        yield return new WaitForSeconds(1);
        Check(SaveGameService.Instance && SaveGameService.Instance.IsReady && !SaveGameService.Instance.LoadFailed, "Save startup in isolated directory");
        Check(SaveGameService.Instance.SaveFilePath.Contains("CombatChecks"), "Test save is isolated from player save");
        var player = FindFirstObjectByType<PlayerHealth>();
        var combat = player.GetComponent<PlayerCombat>();
        var stamina = player.GetComponent<PlayerStamina>();
        var hotbar = FindFirstObjectByType<HotbarController>();
        var transitions = FindFirstObjectByType<SceneTransitionController>();
        var sword = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Sword.prefab");
        var orange = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Potions/orange.prefab");
        Check(InventoryStacking.AddItem(hotbar.hotbarPanel.transform, new InventoryItem {itemPrefab=sword,amount=1}), "Sword in hotbar");
        Check(InventoryStacking.AddItem(hotbar.hotbarPanel.transform, new InventoryItem {itemPrefab=orange,amount=3}), "Orange potion in hotbar");
        hotbar.TryUseSlot(0);
        Check(!combat.TryAttack(Vector2.right) && !combat.TryRoll(Vector2.right), "Combat inactive in village");
        transitions.LoadDungeon();
        yield return new WaitForSeconds(2);
        Check(player.InDungeon && player.Current == player.Maximum && stamina.Current == stamina.Maximum, "Dungeon starts with full health and stamina");
        var spawned = FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None);
        Check(spawned.Length > 0, "Enemies spawn in selected layout: " + spawned.Length);
        foreach (var enemy in spawned) enemy.gameObject.SetActive(false);
        Physics2D.SyncTransforms();
        Vector3 arena = player.transform.position;
        bool foundArena = false;
        foreach (var point in FindFirstObjectByType<DungeonChestSpawner>().SpawnPoints)
        {
            if (!point) continue;
            for (int y = -1; y <= 1 && !foundArena; y++)
                for (int x = -1; x <= 1 && !foundArena; x++)
                {
                    var candidate = point.position + new Vector3(x, y, 0);
                    if (Physics2D.OverlapCircleAll(candidate, 2).Any(c => !c.isTrigger && !c.GetComponentInParent<PlayerHealth>())) continue;
                    arena = candidate; foundArena = true;
                }
            if (foundArena) break;
        }
        Check(foundArena, "Found clear dungeon room for combat checks");
        Check(player.GetComponent<CapsuleCollider2D>() && player.GetComponent<Collider2D>().bounds.size.x < .7f,
            "Wall collider is a narrow capsule at the feet");
        Check(combat.Hurtbox && combat.Hurtbox.isTrigger && combat.Hurtbox.transform != player.transform,
            "Damage hurtbox is separate and does not block movement");
        hotbar.TryUseSlot(2);
        Check(!combat.TryAttack(Vector2.right), "Empty hand cannot attack with sword");
        hotbar.TryUseSlot(0);
        var menu = FindFirstObjectByType<MenuController>();
        menu.OpenRestaurantMenu();
        yield return null;
        Check(!combat.TryAttack(Vector2.right) && !combat.TryRoll(Vector2.right), "Open inventory blocks combat input");
        menu.CloseMenu();
        yield return new WaitForSeconds(.3f);
        Check(!hotbar.TryUseSlot(1), "Full stamina does not consume orange potion");
        hotbar.TryUseSlot(0);
        Check(combat.TryRoll(Vector2.right), "Roll starts");
        float spent = stamina.Current;
        Check(Mathf.Approximately(spent, 75), "Roll costs 25 stamina");
        player.TakeDamage(10);
        Check(player.Current == 100, "Early roll invulnerability prevents damage");
        yield return new WaitForSeconds(.4f);
        Check(combat.State == PlayerCombat.ActionState.Ready, "Roll returns movement control");
        Check(hotbar.TryUseSlot(1) && stamina.Current == 100, "Orange hotbar potion restores stamina");
        hotbar.TryUseSlot(0);
        Check(stamina.TrySpend(100) && !combat.TryRoll(Vector2.right) && stamina.Current == 0, "Insufficient stamina blocks roll without negative balance");
        Check(hotbar.TryUseSlot(1) && stamina.Current == 50, "Orange potion restores exactly 50 stamina");
        hotbar.TryUseSlot(0);
        yield return new WaitForSeconds(.9f);
        Check(stamina.Current > 50 && stamina.Current <= 100, "Stamina regenerates after delay");
        var body = player.GetComponent<Rigidbody2D>();
        var sandbox = arena + Vector3.right * 1000;
        body.position = sandbox; player.transform.position = sandbox;
        Physics2D.SyncTransforms();
        Check(combat.TryRoll(Vector2.right), "Long roll begins in clear space");
        yield return new WaitForSeconds(.4f);
        float rollDistance = Vector2.Distance(body.position, sandbox);
        Check(rollDistance > 2.4f && rollDistance < 3, "Roll travels approximately 2.7 units: " + rollDistance);
        body.position = sandbox; player.transform.position = sandbox;
        var leftWall = new GameObject("Narrow passage left");
        var rightWall = new GameObject("Narrow passage right");
        leftWall.transform.position = sandbox + new Vector3(-.5f, 1, 0);
        rightWall.transform.position = sandbox + new Vector3(.5f, 1, 0);
        leftWall.AddComponent<BoxCollider2D>().size = rightWall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 6);
        Physics2D.SyncTransforms();
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W));
        yield return new WaitForSeconds(.4f);
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        Check(body.position.y - sandbox.y > 1, "Player moves through 0.8-unit passage without body or hat blocking");
        Destroy(leftWall); Destroy(rightWall);
        body.position = arena; player.transform.position = arena;
        Physics2D.SyncTransforms();
        yield return null;
        player.TakeDamage(10);
        Check(player.Current == 90, "Enemy damage reaches health");
        player.TakeDamage(10);
        Check(player.Current == 90, "Hit invulnerability prevents repeated damage");
        yield return new WaitForSeconds(.8f);
        player.GetComponent<Rigidbody2D>().position = arena;
        player.transform.position = arena;
        Physics2D.SyncTransforms();
        var prefab = AssetDatabase.LoadAssetAtPath<EnemyCombat>("Assets/Prefabs/Combat/Water Slime.prefab");
        var target = Instantiate(prefab, player.transform.position + Vector3.right * .8f, Quaternion.identity);
        target.enabled = false;
        Physics2D.SyncTransforms();
        Check(combat.TryAttack(Vector2.right), "Equipped sword begins attack");
        yield return new WaitForSeconds(.6f);
        Check(target.CurrentHealth == 25, "Sword damages once per swing");
        Check(combat.TryAttack(Vector2.right), "Attack can repeat after recovery");
        yield return new WaitForSeconds(.6f);
        Check(target.CurrentHealth == 0, "Second swing kills slime");
        Destroy(target.gameObject);
        yield return null;
        var ai = Instantiate(prefab, player.transform.position + Vector3.right * 1.8f, Quaternion.identity);
        int beforeAI = player.Current;
        yield return new WaitForSeconds(3.5f);
        Check(player.Current < beforeAI, "Slime pursues and attacks player");
        Vector2 beforeKnockback = ai.transform.position;
        ai.TakeDamage(1, Vector2.right);
        yield return new WaitForEndOfFrame();
        Check(ai.GetComponent<SpriteRenderer>().color.g < .3f, "Mob flashes red when damaged");
        yield return new WaitForSeconds(.26f);
        Check(ai.transform.position.x - beforeKnockback.x > .7f, "Mob knockback travels farther than previous 0.4 units");
        Check(ai.GetComponent<SpriteRenderer>().color == Color.white, "Damage flash restores mob color");
        Destroy(ai.gameObject);
        yield return new WaitForSeconds(.8f);
        var wall = new GameObject("Temporary combat test wall");
        wall.transform.position = player.transform.position + Vector3.right * .65f;
        wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 2);
        var behindWall = Instantiate(prefab, player.transform.position + Vector3.right * 1.1f, Quaternion.identity);
        behindWall.enabled = false;
        behindWall.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        Physics2D.SyncTransforms();
        Check(combat.TryAttack(Vector2.right), "Sword attack starts near wall");
        yield return new WaitForSeconds(.6f);
        Check(behindWall.CurrentHealth == 50, "Walls block sword damage");
        float wallX = wall.transform.position.x;
        Check(combat.TryRoll(Vector2.right), "Roll starts toward wall");
        yield return new WaitForSeconds(.4f);
        Check(player.transform.position.x < wallX, "Roll cannot cross walls");
        Destroy(wall); Destroy(behindWall.gameObject);
        int amount = hotbar.GetHotbarItems().Where(i => i != null).Sum(i => i.amount);
        CoinManager.Instance.AddCoins(137);
        int coins = CoinManager.Instance.TotalCoins;
        player.TakeDamage(1000);
        Check(combat.State == PlayerCombat.ActionState.Dead, "Lethal damage starts death animation");
        yield return new WaitForSeconds(3);
        Check(SceneManager.GetActiveScene().name == "SampleScene" && !player.InDungeon, "Death returns home");
        Check(hotbar.GetHotbarItems().Where(i => i != null).Sum(i => i.amount) == amount, "Death preserves inventory");
        Check(CoinManager.Instance.TotalCoins == coins, "Death preserves coins");
        transitions.LoadDungeon();
        yield return new WaitForSeconds(2);
        Check(player.Current == 100 && stamina.Current == 100, "Re-entry resets health and stamina");
        foreach (var enemy in FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
        Check(Keyboard.current != null, "Keyboard device available");
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Space));
        yield return null;
        yield return null;
        bool keyboardRoll = combat.State == PlayerCombat.ActionState.Roll && stamina.Current == 75;
        InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
        Check(keyboardRoll, "Space input starts roll through Input System");
        yield return new WaitForSeconds(.4f);
        // Leave a safe, isolated session open for visual checks.
    }
}

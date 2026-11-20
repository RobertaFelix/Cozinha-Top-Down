using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable] public sealed class SavedDungeonChest
{
    public Vector3 position;
    public bool opened, hasKey;
}
[Serializable] public sealed class SavedDungeonEnemy
{
    public int spawnIndex, health;
    public Vector3 position;
}
[Serializable] public sealed class SavedDungeonDoor
{
    public string path, destination;
    public bool unlocked;
}
[Serializable] public sealed class DungeonRunState
{
    public string layout;
    public int health;
    public float stamina;
    public List<SavedDungeonChest> chests = new List<SavedDungeonChest>();
    public List<SavedDungeonEnemy> enemies = new List<SavedDungeonEnemy>();
    public List<SavedDungeonDoor> doors = new List<SavedDungeonDoor>();

    public static string PathOf(Transform t) => t == null ? "" : t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    public static Transform FindPath(Scene scene, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        int slash = path.IndexOf('/'); string rootName = slash < 0 ? path : path.Substring(0, slash);
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == rootName) return slash < 0 ? root.transform : root.transform.Find(path.Substring(slash + 1));
        return null;
    }
    public static DungeonRunState Capture(GameObject player)
    {
        var active = UnityEngine.Object.FindFirstObjectByType<DungeonLayout>();
        if (active == null) throw new InvalidOperationException("No active dungeon layout.");
        var state = new DungeonRunState { layout = active.DisplayName, health = player.GetComponent<PlayerHealth>().Current,
            stamina = player.GetComponent<PlayerStamina>().Current };
        foreach (var chest in active.GetComponentsInChildren<DungeonChest>())
            state.chests.Add(new SavedDungeonChest { position = chest.transform.position, opened = chest.IsOpened, hasKey = chest.ContainsBossKey });
        foreach (var enemy in active.GetComponentsInChildren<EnemyCombat>())
            if (enemy.CurrentHealth > 0) state.enemies.Add(new SavedDungeonEnemy { spawnIndex = enemy.SpawnIndex, health = enemy.CurrentHealth, position = enemy.transform.position });
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var door in root.GetComponentsInChildren<DungeonBossDoor>(true))
                state.doors.Add(new SavedDungeonDoor { path = PathOf(door.transform), destination = PathOf(door.Destination), unlocked = door.IsUnlocked });
        return state;
    }
    public void Restore(Scene scene)
    {
        DungeonLayout selected = null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var candidate in root.GetComponentsInChildren<DungeonLayout>(true))
            {
                bool match = candidate.DisplayName == layout;
                candidate.gameObject.SetActive(match);
                if (match) selected = candidate;
            }
        if (selected == null) throw new InvalidOperationException("Saved dungeon layout is unavailable: " + layout);
        selected.GetComponent<DungeonChestSpawner>().RestoreRun(chests);
        selected.GetComponent<DungeonEnemySpawner>().RestoreRun(enemies);
        foreach (var saved in doors)
        {
            var target = FindPath(scene, saved.path)?.GetComponent<DungeonBossDoor>();
            if (target == null) throw new InvalidOperationException("Saved dungeon door is unavailable.");
            target.RestoreCheckpoint(saved.unlocked, FindPath(scene, saved.destination));
        }
        Physics2D.SyncTransforms();
    }
}

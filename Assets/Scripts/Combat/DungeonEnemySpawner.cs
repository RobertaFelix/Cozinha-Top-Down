using UnityEngine;

[RequireComponent(typeof(DungeonLayout))]
public sealed class DungeonEnemySpawner : MonoBehaviour
{
    [System.Serializable] public sealed class Spawn
    {
        public Transform point;
        public EnemyCombat prefab;
    }
    [SerializeField] private Spawn[] spawns;
    [SerializeField, Min(0)] private float playerExclusionRadius = 3;
    [SerializeField, Min(.1f)] private float clearanceRadius = .35f;
    private bool generated;
    private int authoredCount;
    public void DisableAuthoredPoints()
    {
        foreach (var entry in spawns) if (entry.point) entry.point.gameObject.SetActive(false);
    }

    public void AddExpeditionPoint(Transform point, int variant)
    {
        if (spawns == null || spawns.Length == 0) return;
        if (authoredCount == 0) authoredCount = spawns.Length;
        var entry = new Spawn { point = point, prefab = spawns[variant % authoredCount].prefab };
        System.Array.Resize(ref spawns, spawns.Length + 1);
        spawns[spawns.Length - 1] = entry;
    }
    public void GenerateForRun(Transform playerSpawn)
    {
        if (generated || spawns == null) return;
        generated = true;
        var overlaps = new Collider2D[16];
        var root = new GameObject("Spawned Enemies").transform;
        root.SetParent(transform, false);
        for (int index = 0; index < spawns.Length; index++)
        {
            var spawn = spawns[index];
            if (spawn.point == null || spawn.prefab == null || !spawn.point.IsChildOf(transform) ||
                !spawn.point.gameObject.activeInHierarchy ||
                (playerSpawn && Vector2.Distance(playerSpawn.position, spawn.point.position) < playerExclusionRadius)) continue;
            int count = Physics2D.OverlapCircle(spawn.point.position, clearanceRadius, new ContactFilter2D { useTriggers = false }, overlaps);
            if (count > 0) continue;
            Instantiate(spawn.prefab, spawn.point.position, Quaternion.identity, root).SpawnIndex = index;
        }
    }
    public void RestoreRun(System.Collections.Generic.List<SavedDungeonEnemy> saved)
    {
        foreach (var old in GetComponentsInChildren<EnemyCombat>()) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
        generated = true;
        var root = new GameObject("Restored Enemies").transform; root.SetParent(transform, false);
        foreach (var entry in saved)
        {
            if (spawns == null || entry.spawnIndex < 0 || entry.spawnIndex >= spawns.Length || spawns[entry.spawnIndex].prefab == null)
                throw new System.InvalidOperationException("Saved enemy spawn is unavailable.");
            var enemy = Instantiate(spawns[entry.spawnIndex].prefab, entry.position, Quaternion.identity, root);
            enemy.SpawnIndex = entry.spawnIndex;
            enemy.RestoreCheckpoint(entry.health);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (spawns == null) return;
        Gizmos.color = new Color(1, .3f, .3f);
        foreach (var spawn in spawns) if (spawn.point) Gizmos.DrawWireSphere(spawn.point.position, clearanceRadius);
    }
}

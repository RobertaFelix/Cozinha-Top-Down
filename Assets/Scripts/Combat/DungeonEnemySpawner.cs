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
    public void GenerateForRun(Transform playerSpawn)
    {
        if (generated || spawns == null) return;
        generated = true;
        var overlaps = new Collider2D[16];
        var root = new GameObject("Spawned Enemies").transform;
        root.SetParent(transform, false);
        foreach (var spawn in spawns)
        {
            if (spawn.point == null || spawn.prefab == null || !spawn.point.IsChildOf(transform) ||
                !spawn.point.gameObject.activeInHierarchy ||
                (playerSpawn && Vector2.Distance(playerSpawn.position, spawn.point.position) < playerExclusionRadius)) continue;
            int count = Physics2D.OverlapCircle(spawn.point.position, clearanceRadius, new ContactFilter2D { useTriggers = false }, overlaps);
            if (count > 0) continue;
            Instantiate(spawn.prefab, spawn.point.position, Quaternion.identity, root);
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (spawns == null) return;
        Gizmos.color = new Color(1, .3f, .3f);
        foreach (var spawn in spawns) if (spawn.point) Gizmos.DrawWireSphere(spawn.point.position, clearanceRadius);
    }
}

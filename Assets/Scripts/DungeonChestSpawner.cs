using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DungeonLayout))]
public sealed class DungeonChestSpawner : MonoBehaviour
{
    [SerializeField] private DungeonChest chestPrefab;
    [SerializeField] private GameObject bossKeyPrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField, Min(1)] private int minimumChests = 4;
    [SerializeField, Min(1)] private int maximumChests = 6;

    [SerializeField, Min(0)] private float playerSpawnExclusionRadius = 2.5f;
    [SerializeField, Min(0)] private float minimumChestSpacing = 4f;
    [SerializeField, Range(1f, 8f)] private float spreadBias = 4f;

    private readonly List<DungeonChest> spawnedChests = new List<DungeonChest>();
    private bool generated;
    public bool HasBossKey => BossKeyItem.Has(bossKeyPrefab);
    public IReadOnlyList<Transform> SpawnPoints => spawnPoints;
    public IReadOnlyList<DungeonChest> SpawnedChests => spawnedChests;

    public List<Transform> GetValidSpawnPoints()
    {
        var valid = new List<Transform>();
        foreach (var point in spawnPoints)
        {
            if (point == null || !point.gameObject.activeInHierarchy || !point.IsChildOf(transform)) continue;
            var room = point.GetComponentInParent<DungeonChestRoom>();
            if (room == null || !room.enabled || !room.transform.IsChildOf(transform)) continue;
            bool duplicate = valid.Exists(other => (other.position - point.position).sqrMagnitude < .01f);
            if (!duplicate) valid.Add(point);
        }
        return valid;
    }

    // Called only after the layout has been selected, never by OnEnable on both layouts.
    public void GenerateForRun(Transform playerSpawn = null)
    {
        if (generated) return;
        var points = GetValidSpawnPoints();
        if (playerSpawn != null)
            points.RemoveAll(point => Vector2.Distance(point.position, playerSpawn.position) < playerSpawnExclusionRadius);
        if (chestPrefab == null || points.Count == 0)
        {
            Debug.LogWarning("Configure a chest prefab and valid chest spawn points grouped under DungeonChestRoom objects.", this);
            return;
        }
        int minimum = Mathf.Clamp(minimumChests, 1, points.Count);
        int maximum = Mathf.Clamp(maximumChests, minimum, points.Count);
        int count = Random.Range(minimum, maximum + 1);
        var selected = new List<Transform>();
        var roomCounts = new Dictionary<DungeonChestRoom, int>();
        while (selected.Count < count && points.Count > 0)
        {
            var availableRooms = new List<DungeonChestRoom>();
            int lowestCount = int.MaxValue;
            foreach (var point in points)
            {
                var room = point.GetComponentInParent<DungeonChestRoom>();
                int used = roomCounts.TryGetValue(room, out int value) ? value : 0;
                if (used >= room.MaximumChests || used > lowestCount) continue;
                if (used < lowestCount) { availableRooms.Clear(); lowestCount = used; }
                if (!availableRooms.Contains(room)) availableRooms.Add(room);
            }
            if (availableRooms.Count == 0) break;
            // Pick a least-used room first; additional points never increase its share of the draw.
            var selectedRoom = availableRooms[Random.Range(0, availableRooms.Count)];
            var candidates = points.FindAll(point => point.GetComponentInParent<DungeonChestRoom>() == selectedRoom);
            var weights = new float[candidates.Count];
            float total = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                float distance = float.PositiveInfinity;
                foreach (var chosen in selected)
                    distance = Mathf.Min(distance, Vector2.Distance(candidates[i].position, chosen.position));
                weights[i] = selected.Count == 0 ? 1f : Mathf.Pow(distance, spreadBias);
                total += weights[i];
            }
            float roll = Random.value * total;
            int index = candidates.Count - 1;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0) { index = i; break; }
            }
            var next = candidates[index];
            roomCounts[selectedRoom] = lowestCount + 1;
            selected.Add(next);
            points.Remove(next);
            // Never force extra chests into a tight group just to reach the requested count.
            points.RemoveAll(point => Vector2.Distance(point.position, next.position) < minimumChestSpacing);
        }
        points = selected;
        count = points.Count;
        int keyChest = Random.Range(0, count);
        var container = new GameObject("Spawned Chests").transform;
        container.SetParent(transform, false);
        for (int i = 0; i < count; i++)
        {
            var chest = Instantiate(chestPrefab, points[i].position, Quaternion.identity, container);
            chest.name = "Chest " + (i + 1);
            spawnedChests.Add(chest);
            chest.Initialize(this, i == keyChest);
        }
        generated = true;
    }

    internal bool CollectBossKey(DungeonChest chest)
    {
        if (chest == null || !chest.ContainsBossKey || !spawnedChests.Contains(chest)) return false;
        return bossKeyPrefab != null && BossKeyItem.TryGive(bossKeyPrefab);
    }

    public bool TryUseBossKey() => BossKeyItem.TryConsume(bossKeyPrefab);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, .7f, .15f);
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            Gizmos.DrawWireCube(point.position + Vector3.up * .2f, new Vector3(1f, .4f, 0));
            Gizmos.DrawLine(point.position + Vector3.left * .6f, point.position + Vector3.right * .6f);
        }
    }
}

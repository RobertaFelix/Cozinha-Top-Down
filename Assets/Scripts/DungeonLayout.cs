using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DungeonLayout : MonoBehaviour
{
    [SerializeField] private string displayName = "Dungeon";
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    public string DisplayName => displayName;
    public IReadOnlyList<Transform> SpawnPoints => spawnPoints;

    public List<Transform> GetValidSpawnPoints()
    {
        var valid = new List<Transform>();
        foreach (var point in spawnPoints)
            if (point != null && point.gameObject.activeSelf && point.IsChildOf(transform) && !valid.Contains(point))
                valid.Add(point);
        return valid;
    }

    public static bool TrySelectSpawn(Scene scene, out Transform spawn)
    {
        spawn = null;
        var layouts = new List<DungeonLayout>();
        var eligible = new List<DungeonLayout>();
        foreach (var root in scene.GetRootGameObjects())
            layouts.AddRange(root.GetComponentsInChildren<DungeonLayout>(true));
        foreach (var layout in layouts)
            if (layout.enabled && layout.GetValidSpawnPoints().Count > 0)
                eligible.Add(layout);
        if (eligible.Count == 0) return false;

        var selected = eligible[Random.Range(0, eligible.Count)];
        var points = selected.GetValidSpawnPoints();
        spawn = points[Random.Range(0, points.Count)];
        foreach (var layout in layouts)
            layout.gameObject.SetActive(layout == selected);
        selected.GetComponent<DungeonChestSpawner>()?.GenerateForRun(spawn);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position, .35f);
            Gizmos.DrawLine(point.position + Vector3.left * .5f, point.position + Vector3.right * .5f);
            Gizmos.DrawLine(point.position + Vector3.down * .5f, point.position + Vector3.up * .5f);
        }
    }
}

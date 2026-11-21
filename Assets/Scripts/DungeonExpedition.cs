using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Rebuild the same floor plan and spawn indices before restoring a checkpoint.
public sealed partial class DungeonExpedition : MonoBehaviour
{
    public int Seed { get; private set; }
    public int OriginalArea { get; private set; }
    public int AddedArea { get; private set; }
    private Tile boundaryTile;
    private static readonly Vector3Int[] Directions = { Vector3Int.left, Vector3Int.right, Vector3Int.down, Vector3Int.up };

    public static void Ensure(DungeonLayout layout, int seed, int version = 2)
    {
        if (!layout.ExpeditionFloor) return;
        if (!layout.TryGetComponent<DungeonExpedition>(out var run))
            run = layout.gameObject.AddComponent<DungeonExpedition>();
        if (run.Seed != 0) return;
        run.Version = version;
        if (version == 0) run.BuildLegacy(seed);
        else run.BuildRooms(layout, seed);
    }

    public int Version { get; private set; }

    // Checkpoints from before the full-map generator retain their original geometry.
    private void BuildLegacy(int seed)
    {
        if (Seed != 0) return;
        var layout = GetComponent<DungeonLayout>();
        var floor = layout.ExpeditionFloor;
        var style = DungeonExpeditionStyle.Load();
        Seed = seed;
        var random = new System.Random(seed);
        var old = new HashSet<Vector3Int>();
        var entrances = new List<Vector3Int>();
        int lowest = int.MaxValue;
        foreach (var cell in floor.cellBounds.allPositionsWithin)
        {
            if (!floor.HasTile(cell)) continue;
            old.Add(cell);
            if (cell.y > lowest) continue;
            if (cell.y < lowest) { lowest = cell.y; entrances.Clear(); }
            entrances.Add(cell);
        }
        OriginalArea = old.Count;
        entrances.RemoveAll(c => !old.Contains(c + Vector3Int.right) || !old.Contains(c + Vector3Int.up));
        var entrance = entrances[random.Next(entrances.Count)];
        var start = entrance + Vector3Int.down * 8;
        var carved = new HashSet<Vector3Int>();
        void Carve(Vector3Int p) { if (!old.Contains(p)) carved.Add(p); }
        void Corridor(Vector3Int from, Vector3Int to)
        {
            var delta = to - from;
            var step = new Vector3Int(System.Math.Sign(delta.x), System.Math.Sign(delta.y), 0);
            var side = new Vector3Int(step.y, step.x, 0);
            for (var p = from; p != to + step; p += step)
                for (int w = -1; w <= 1; w++) Carve(p + side * w);
        }
        Corridor(entrance, start);
        var rooms = new List<Vector3Int> { Vector3Int.zero };
        var occupied = new HashSet<Vector3Int> { Vector3Int.zero };
        // A room plus its connecting hall adds at most 100 tiles.
        int target = random.Next(OriginalArea, OriginalArea * 3 - 100);
        var chestSpawner = GetComponent<DungeonChestSpawner>();
        var enemySpawner = GetComponent<DungeonEnemySpawner>();
        var atmosphere = gameObject.AddComponent<DungeonAtmosphere>();
        random.NextDouble(); // Preserve the random sequence used by saved expeditions.
        atmosphere.Initialize(style);
        int roomIndex = 0;
        while (carved.Count < target)
        {
            var center = start + rooms[roomIndex] * 12;
            int halfWidth = random.Next(3, 5), halfHeight = random.Next(2, 4);
            for (int y = -halfHeight; y <= halfHeight; y++)
                for (int x = -halfWidth; x <= halfWidth; x++) Carve(center + new Vector3Int(x, y, 0));

            var room = new GameObject("Expedition Room " + roomIndex).AddComponent<DungeonChestRoom>();
            room.transform.SetParent(transform, false);
            room.transform.position = floor.GetCellCenterWorld(center);
            chestSpawner.AddExpeditionPoint(room.transform);
            layout.AddExpeditionSpawn(room.transform);
            var enemyPoint = new GameObject("Guardian").transform;
            enemyPoint.SetParent(room.transform, false);
            enemyPoint.localPosition = new Vector3(halfWidth - 1, 1, 0);
            enemySpawner.AddExpeditionPoint(enemyPoint, random.Next());
            atmosphere.PlaceTorch(floor.GetCellCenterWorld(center + new Vector3Int(-halfWidth, halfHeight + 1, 0)));

            if (carved.Count >= target) break;
            var frontier = new List<(Vector3Int parent, Vector3Int next)>();
            foreach (var existing in rooms)
                foreach (var dir in Directions)
                {
                    var next = existing + dir;
                    if (next.y <= 0 && !occupied.Contains(next)) frontier.Add((existing, next));
                }
            var edge = frontier[random.Next(frontier.Count)];
            Corridor(start + edge.parent * 12, start + edge.next * 12);
            occupied.Add(edge.next);
            rooms.Add(edge.next);
            roomIndex++;
        }
        AddedArea = carved.Count;
        var obstacles = GetComponentsInChildren<TilemapCollider2D>();
        void OpenCell(Vector3Int cell)
        {
            var world = floor.GetCellCenterWorld(cell);
            foreach (var obstacle in obstacles)
            {
                var map = obstacle.GetComponent<Tilemap>();
                map.SetTile(map.WorldToCell(world), null);
            }
        }
        for (int y = 0; y <= 1; y++)
            for (int x = -1; x <= 1; x++) OpenCell(entrance + new Vector3Int(x, y, 0));
        var boundary = new HashSet<Vector3Int>();
        bool IsFloor(Vector3Int cell) => old.Contains(cell) || carved.Contains(cell);
        void PaintFloor(Vector3Int cell)
        {
            int column = !IsFloor(cell + Vector3Int.left) ? 0 : !IsFloor(cell + Vector3Int.right) ? 2 : 1;
            int row = !IsFloor(cell + Vector3Int.up) ? 0 : !IsFloor(cell + Vector3Int.down) ? 2 : 1;
            floor.SetTile(cell, style.floors[row * 3 + column]);
        }
        for (int x = -1; x <= 1; x++)
        {
            var join = entrance + Vector3Int.right * x;
            if (old.Contains(join)) PaintFloor(join);
        }
        foreach (var cell in carved)
        {
            OpenCell(cell);
            PaintFloor(cell);
            foreach (var dir in Directions)
            {
                var wall = cell + dir;
                if (!old.Contains(wall) && !carved.Contains(wall)) boundary.Add(wall);
            }
        }
        var rim = new GameObject("Expedition Walls", typeof(Tilemap), typeof(TilemapRenderer)).GetComponent<Tilemap>();
        rim.transform.SetParent(floor.transform.parent, false);
        rim.transform.localPosition = floor.transform.localPosition;
        var renderer = rim.GetComponent<TilemapRenderer>();
        renderer.sharedMaterial = floor.GetComponent<TilemapRenderer>().sharedMaterial;
        renderer.sortingOrder = floor.GetComponent<TilemapRenderer>().sortingOrder + 1;
        boundaryTile = Instantiate(style.wall);
        boundaryTile.colliderType = Tile.ColliderType.Grid;
        foreach (var cell in boundary) rim.SetTile(cell, boundaryTile);
        rim.gameObject.AddComponent<TilemapCollider2D>().ProcessTilemapChanges();
        foreach (var obstacle in obstacles) obstacle.ProcessTilemapChanges();
        Physics2D.SyncTransforms();
    }

    private void OnDestroy() { if (boundaryTile) Destroy(boundaryTile); }
}

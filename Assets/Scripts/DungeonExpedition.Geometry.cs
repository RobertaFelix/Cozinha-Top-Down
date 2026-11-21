using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed partial class DungeonExpedition
{
    private sealed class Chamber
    {
        public Vector3Int lattice, center;
        public int width, height, depth;
    }

    private void BuildRooms(DungeonLayout layout, int seed)
    {
        Seed = seed;
        var random = new System.Random(seed);
        var style = DungeonExpeditionStyle.Load();
        var floor = layout.ExpeditionFloor;
        foreach (var p in floor.cellBounds.allPositionsWithin)
            if (floor.HasTile(p)) OriginalArea++;
        foreach (var map in layout.GetComponentsInChildren<Tilemap>())
            map.ClearAllTiles();
        var chests = layout.GetComponent<DungeonChestSpawner>();
        var enemies = layout.GetComponent<DungeonEnemySpawner>();
        foreach (var spawn in layout.GetValidSpawnPoints()) spawn.gameObject.SetActive(false);
        foreach (var point in chests.SpawnPoints) if (point) point.gameObject.SetActive(false);
        enemies.DisableAuthoredPoints();

        // Separate the generated map from the hand-built arena, including its colliders.
        var origin = floor.WorldToCell(layout.ArenaArrival.position) + Vector3Int.right * 300;
        var cells = new HashSet<Vector3Int>();
        var rooms = new List<Chamber>();
        var occupied = new Dictionary<Vector3Int, Chamber>();
        var frontier = new List<(Chamber from, Vector3Int to)>();
        var edges = new HashSet<(Vector3Int, Vector3Int)>();
        // Reserve the terminal gate chamber and its hall within the expedition area budget.
        int gateBudget = Version >= 2 ? 170 : 0;
        int target = random.Next(2 * OriginalArea, 4 * OriginalArea - 240 - gateBudget);

        void Hall(Chamber from, Chamber to)
        {
            edges.Add((from.lattice, to.lattice));
            var p = from.center;
            var q = to.center;
            bool horizontal = from.lattice.y == to.lattice.y;
            int width = random.Next(2, 4);
            void Stroke(Vector3Int end)
            {
                var step = new Vector3Int(System.Math.Sign(end.x - p.x), System.Math.Sign(end.y - p.y), 0);
                var side = new Vector3Int(step.y, step.x, 0);
                while (p != end)
                {
                    for (int i = -1; i < width - 1; i++) cells.Add(p + side * i);
                    p += step;
                }
                for (int i = -1; i < width - 1; i++) cells.Add(p + side * i);
            }
            if (horizontal)
            {
                int midpoint = (p.x + q.x) / 2;
                Stroke(new Vector3Int(midpoint,p.y));
                Stroke(new Vector3Int(midpoint,q.y));
            }
            else
            {
                int midpoint = (p.y + q.y) / 2;
                Stroke(new Vector3Int(p.x,midpoint));
                Stroke(new Vector3Int(q.x,midpoint));
            }
            Stroke(q);
        }

        void Room(Chamber parent, Vector3Int grid)
        {
            var room = new Chamber { depth = parent == null ? 0 : parent.depth + 1,
                lattice = grid, center = origin + grid * 17 + new Vector3Int(random.Next(-2,3),random.Next(-2,3)),
                width = random.Next(3,7), height = random.Next(3,6) };
            rooms.Add(room);
            occupied.Add(grid, room);
            int cut = random.Next(0,4);
            int shape = random.Next(4);
            int cornerX = random.Next(2) == 0 ? 1 : -1;
            int cornerY = random.Next(2) == 0 ? 1 : -1;
            for (int x = -room.width; x <= room.width; x++)
                for (int y = -room.height; y <= room.height; y++)
                    {
                        // Keep both middle axes clear for passage mouths and room occupants.
                        if (room.width - Mathf.Abs(x) + room.height - Mathf.Abs(y) < cut) continue;
                        if (shape == 1 && x * cornerX >= 2 && y * cornerY >= 2) continue;
                        if (shape == 2 && Mathf.Abs(x) > room.width - 2 && Mathf.Abs(y) > room.height - 2) continue;
                        if (shape == 3 && room.width >= 5 && room.height >= 4 &&
                            Mathf.Abs(x) == 3 && Mathf.Abs(y) == 2) continue;
                        cells.Add(room.center + new Vector3Int(x,y));
                    }
            if (parent != null) Hall(parent, room);
            foreach (var dir in Directions) if (!occupied.ContainsKey(grid + dir)) frontier.Add((room,grid + dir));
        }
        Room(null, Vector3Int.zero);
        while (cells.Count < target)
        {
            int index = random.Next(frontier.Count);
            var edge = frontier[index];
            frontier.RemoveAt(index);
            if (!occupied.ContainsKey(edge.to)) Room(edge.from, edge.to);
        }
        // A few neighboring branches reconnect. Keep the gate at the end of its branch.
        Chamber gateRoom = null;
        if (Version == 1)
            foreach (var room in rooms)
                if (gateRoom == null || room.depth >= gateRoom.depth) gateRoom = room;
        foreach (var room in rooms)
            foreach (var dir in new[] { Vector3Int.right, Vector3Int.up })
                if (occupied.TryGetValue(room.lattice + dir, out var other) && room != gateRoom &&
                    other != gateRoom && !edges.Contains((room.lattice,other.lattice)) &&
                    !edges.Contains((other.lattice,room.lattice)) && cells.Count + 80 + gateBudget <= OriginalArea * 4 &&
                    random.NextDouble() < .27) Hall(room, other);

        if (Version >= 2)
        {
            // Reserve an empty lattice cell AFTER reconnecting branches. Exactly one corridor
            // can reach this room. An outward step left, right or up makes the entrance
            // arrive from the right, left or bottom and leaves the north wall for the gate.
            var options = new List<(Chamber parent, Vector3Int destination)>();
            int deepest = -1;
            foreach (var room in rooms)
            {
                if (room.depth < deepest) continue;
                foreach (var dir in Directions)
                {
                    if (dir == Vector3Int.down || occupied.ContainsKey(room.lattice + dir)) continue;
                    if (room.depth > deepest) { options.Clear(); deepest = room.depth; }
                    options.Add((room, room.lattice + dir));
                }
            }
            var choice = options[random.Next(options.Count)];
            gateRoom = new Chamber { lattice = choice.destination, center = origin + choice.destination * 17,
                width = 5, height = 4, depth = choice.parent.depth + 1 };
            rooms.Add(gateRoom);
            for (int y = -gateRoom.height; y <= gateRoom.height; y++)
                for (int x = -gateRoom.width; x <= gateRoom.width; x++)
                    if (Mathf.Abs(x) != 3 || Mathf.Abs(y) != 2)
                        cells.Add(gateRoom.center + new Vector3Int(x, y));
            Hall(choice.parent, gateRoom);
            // A bend on the approach can graze the outside corners. Keep the actual
            // threshold three cells wide and close the remainder of the perimeter.
            var entrance = choice.parent.lattice - gateRoom.lattice;
            int outerX = gateRoom.width + 1, outerY = gateRoom.height + 1;
            for (int y = -outerY; y <= outerY; y++)
                for (int x = -outerX; x <= outerX; x++)
                {
                    if (Mathf.Abs(x) != outerX && Mathf.Abs(y) != outerY) continue;
                    var cell = gateRoom.center + new Vector3Int(x,y);
                    bool threshold = entrance.x != 0
                        ? x == entrance.x * outerX && Mathf.Abs(y) <= 1
                        : y == entrance.y * outerY && Mathf.Abs(x) <= 1;
                    if (threshold) cells.Add(cell);
                    else cells.Remove(cell);
                }
        }

        AddedArea = cells.Count - OriginalArea;
        var rim = layout.TorchWalls;
        rim.transform.position = floor.transform.position;
        boundaryTile = Instantiate(style.wall);
        boundaryTile.colliderType = Tile.ColliderType.Grid;
        foreach (var cell in cells)
        {
            int col = !cells.Contains(cell + Vector3Int.left) ? 0 : !cells.Contains(cell + Vector3Int.right) ? 2 : 1;
            int row = !cells.Contains(cell + Vector3Int.up) ? 0 : !cells.Contains(cell + Vector3Int.down) ? 2 : 1;
            floor.SetTile(cell,style.floors[row * 3 + col]);
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    var wall = cell + new Vector3Int(x,y);
                    if (!cells.Contains(wall)) rim.SetTile(wall, boundaryTile);
                }
        }
        if (!rim.GetComponent<TilemapCollider2D>()) rim.gameObject.AddComponent<TilemapCollider2D>();
        foreach (var map in layout.GetComponentsInChildren<Tilemap>()) map.CompressBounds();
        foreach (var collider in layout.GetComponentsInChildren<TilemapCollider2D>()) collider.ProcessTilemapChanges();

        var atmosphere = gameObject.AddComponent<DungeonAtmosphere>();
        random.NextDouble(); // Preserve placement in existing expeditions.
        atmosphere.Initialize(style);
        foreach (var room in rooms)
        {
            var point = new GameObject("Expedition Room " + rooms.IndexOf(room)).AddComponent<DungeonChestRoom>().transform;
            point.SetParent(transform, false);
            point.position = floor.GetCellCenterWorld(room.center);
            if (room != gateRoom) layout.AddExpeditionSpawn(point);
            chests.AddExpeditionPoint(point);
            var guard = new GameObject("Guardian").transform;
            guard.SetParent(point,false);
            guard.position = floor.GetCellCenterWorld(room.center + Vector3Int.right * (room.width - 1));
            enemies.AddExpeditionPoint(guard,random.Next());
            for (int dx = -room.width; dx <= room.width; dx++)
            {
                var wall = room.center + new Vector3Int(dx, room.height + 1);
                if (!rim.HasTile(wall) || !cells.Contains(wall + Vector3Int.down)) continue;
                atmosphere.PlaceTorch(floor.GetCellCenterWorld(wall));
                break;
            }
        }
        var gate = layout.GetComponentInChildren<DungeonBossDoor>();
        gate.PositionGate(floor.GetCellCenterWorld(gateRoom.center + Vector3Int.up * gateRoom.height));
        CreateExit(transform, floor.GetCellCenterWorld(rooms[0].center + Vector3Int.up * (rooms[0].height - 1)),gate);
        Physics2D.SyncTransforms();
    }

    public static GameObject CreateExit(Transform parent, Vector3 position, DungeonBossDoor door)
    {
        // Reuse the scene's village transition (including the interaction and return spawn rules).
        var source = System.Array.Find(Object.FindObjectsByType<InteractionPromptTrigger>(FindObjectsInactive.Include),
            t => t.gameObject.scene == parent.gameObject.scene && t.name == "DungeonExit");
        var exit = Instantiate(source.gameObject, position, Quaternion.identity, parent);
        exit.name = "Village passage";
        exit.tag = "Untagged";
        var appearance = exit.AddComponent<SpriteRenderer>();
        appearance.sprite = door.GetComponent<SpriteRenderer>().sprite;
        appearance.color = new Color(.48f,1,.8f);
        appearance.sortingOrder = 5;
        exit.SetActive(true);
        return exit;
    }
}

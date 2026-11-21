using System.Collections.Generic;
using UnityEngine;

// Routes are planned at the NPC's feet. Ignore interaction triggers and the player.
public static class CustomerFootpath
{
    private const float Step = .35f;
    private static readonly Vector2Int[] Directions =
        { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    public static List<Vector3> Find(Vector3 start, Vector3 end, bool visible, Transform reservedSeat = null)
    {
        if (!visible) return new List<Vector3> { end };
        Vector2Int goal = Vector2Int.RoundToInt((end - start) / Step);
        var blocked = new HashSet<Vector2Int>();
        var parents = new Dictionary<Vector2Int, Vector2Int>();
        var costs = new Dictionary<Vector2Int, int> { [Vector2Int.zero] = 0 };
        var frontier = new SortedSet<(int score, int remaining, int serial, Vector2Int pos)>(
            Comparer<(int score, int remaining, int serial, Vector2Int pos)>.Create((a,b) =>
                (a.score, a.remaining, a.serial).CompareTo((b.score, b.remaining, b.serial))));
        var filter = new ContactFilter2D { useTriggers = false };
        var hits = new Collider2D[8];
        int serial = 0;
        frontier.Add((0, 0, serial, Vector2Int.zero));
        while (frontier.Count > 0 && costs.Count < 5000)
        {
            var current = frontier.Min;
            frontier.Remove(current);
            var cell = current.pos;
            if (cell == goal)
            {
                var path = new List<Vector3> { end };
                while (cell != Vector2Int.zero)
                {
                    path.Add(start + (Vector3)(Vector2)cell * Step);
                    cell = parents[cell];
                }
                path.Reverse();
                return path;
            }
            foreach (var direction in Directions)
            {
                var next = cell + direction;
                int cost = costs[cell] + 1;
                if (blocked.Contains(next) || (costs.TryGetValue(next, out int previous) && previous <= cost)) continue;
                Vector2 position = (Vector2)start + (Vector2)next * Step;
                if (next != goal)
                {
                    bool wall = false;
                    int count = Physics2D.OverlapCircle(position, .09f, filter, hits);
                    for (int i = 0; i < count; i++)
                    {
                        var hit = hits[i];
                        if (hit == null || (reservedSeat && hit.transform.IsChildOf(reservedSeat))) continue;
                        if (!hit.GetComponentInParent<PlayerController>()) wall = true;
                    }
                    if (wall)
                    {
                        blocked.Add(next);
                        continue;
                    }
                }
                costs[next] = cost;
                parents[next] = cell;
                var delta = next - goal;
                int remaining = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
                frontier.Add((cost + remaining, remaining, ++serial, next));
            }
        }
        return null;
    }
}

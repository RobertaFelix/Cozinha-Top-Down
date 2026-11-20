using System.Collections.Generic;
using UnityEngine;

public sealed class CameraBoundaryRegion : MonoBehaviour
{
    [SerializeField] private Rect localBounds = new Rect(-10, -10, 20, 20);
    [SerializeField] private int priority;
    private static readonly List<CameraBoundaryRegion> active = new List<CameraBoundaryRegion>();
    public Rect LocalBounds { get => localBounds; set => localBounds = value; }
    public Rect WorldBounds => new Rect((Vector2)transform.position + localBounds.position, localBounds.size);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => active.Clear();
    private void OnEnable() { if (!active.Contains(this)) active.Add(this); }
    private void OnDisable() => active.Remove(this);
    public static CameraBoundaryRegion Find(Vector2 target)
    {
        CameraBoundaryRegion best = null;
        float distance = float.PositiveInfinity;
        foreach (var region in active)
        {
            if (!region || !region.isActiveAndEnabled) continue;
            var bounds = region.WorldBounds;
            float next = (new Vector2(Mathf.Clamp(target.x, bounds.xMin, bounds.xMax), Mathf.Clamp(target.y, bounds.yMin, bounds.yMax)) - target).sqrMagnitude;
            if (best == null || region.priority > best.priority || (region.priority == best.priority && next < distance))
            { best = region; distance = next; }
        }
        return best;
    }
    public Vector3 Confine(Vector3 position, float halfHeight, float aspect)
    {
        var b = WorldBounds;
        float halfWidth = halfHeight * aspect;
        position.x = b.width <= halfWidth * 2 ? b.center.x : Mathf.Clamp(position.x, b.xMin + halfWidth, b.xMax - halfWidth);
        position.y = b.height <= halfHeight * 2 ? b.center.y : Mathf.Clamp(position.y, b.yMin + halfHeight, b.yMax - halfHeight);
        return position;
    }
    private void OnDrawGizmosSelected()
    {
        var b = WorldBounds; Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(new Vector3(b.center.x, b.center.y, transform.position.z), new Vector3(b.width, b.height, 0));
    }
}

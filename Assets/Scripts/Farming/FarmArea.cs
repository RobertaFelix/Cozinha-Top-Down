using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

// The allowed cells are authored explicitly from the existing fixed garden plots.
public sealed class FarmArea : MonoBehaviour
{
    [SerializeField] private string areaId = "village-garden";
    [SerializeField] private Tilemap ground;
    [SerializeField] private Vector3Int[] plantableCells;
    [SerializeField] private TileBase untilledTile;
    [SerializeField] private TileBase tilledTile;
    [SerializeField] private Color wateredColor = new Color(.68f, .68f, .68f, 1f);
    private readonly HashSet<Vector3Int> allowed = new HashSet<Vector3Int>();
    private readonly Dictionary<Vector3Int, SpriteRenderer> visuals = new Dictionary<Vector3Int, SpriteRenderer>();
    private static readonly List<FarmArea> areas = new List<FarmArea>();
    private FarmingSaveModule farm;
    public static bool HasActiveArea => areas.Count > 0;
    public string AreaId => areaId;
    public IReadOnlyList<Vector3Int> PlantableCells => plantableCells;
    public Tilemap Ground => ground;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { areas.Clear(); }
    private void Awake()
    {
        allowed.Clear();
        if (plantableCells != null) foreach (var cell in plantableCells) allowed.Add(new Vector3Int(cell.x, cell.y, 0));
    }
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (!areas.Contains(this)) areas.Add(this);
        Bind();
        Refresh();
    }
    private void Start() { Bind(); Refresh(); }
    private void Bind()
    {
        if (farm == null) farm = FarmingSaveModule.Instance;
        if (farm == null) return;
        farm.Changed -= Refresh;
        farm.Changed += Refresh;
    }
    private void OnDisable()
    {
        areas.Remove(this);
        if (farm != null) farm.Changed -= Refresh;
    }
    public bool CanPlantAt(Vector3Int cell) => isActiveAndEnabled && ground != null && allowed.Contains(new Vector3Int(cell.x, cell.y, 0));
    public Vector3 CellCenter(Vector3Int cell) => ground.GetCellCenterWorld(cell);
    public static bool TryFind(Vector3 world, out FarmArea area, out Vector3Int cell)
    {
        foreach (var candidate in areas)
        {
            if (candidate == null || candidate.ground == null || !candidate.isActiveAndEnabled) continue;
            var at = candidate.ground.WorldToCell(world); at.z = 0;
            if (!candidate.CanPlantAt(at)) continue;
            area = candidate; cell = at; return true;
        }
        area = null; cell = default; return false;
    }

    public void Refresh()
    {
        if (farm == null || ground == null) return;
        foreach (var cell in allowed)
        {
            var plot = farm.GetPlot(areaId, cell);
            ground.SetTile(cell, plot == null ? untilledTile : tilledTile);
            ground.SetTileFlags(cell, TileFlags.None);
            ground.SetColor(cell, plot != null && plot.wateredDay == farm.CurrentDay ? wateredColor : Color.white);
            var crop = plot != null ? farm.Catalog?.Find(plot.cropId) : null;
            if (crop == null || crop.growthStages == null || crop.growthStages.Length == 0)
            {
                if (visuals.TryGetValue(cell, out var old))
                {
                    if (old != null) { old.transform.parent.gameObject.SetActive(false); Destroy(old.transform.parent.gameObject); }
                    visuals.Remove(cell);
                }
                continue;
            }
            if (!visuals.TryGetValue(cell, out var renderer) || renderer == null)
            {
                var root = new GameObject("Crop " + cell.x + "," + cell.y, typeof(SortingGroup));
                root.transform.SetParent(transform, false);
                root.transform.position = CellCenter(cell) + Vector3.down * .2f;
                root.GetComponent<SortingGroup>().sortingOrder = 5;
                renderer = new GameObject("Plant", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                renderer.transform.SetParent(root.transform, false);
                visuals[cell] = renderer;
            }
            renderer.sprite = crop.growthStages[Mathf.Clamp(plot.stage, 0, crop.growthStages.Length - 1)];
            if (renderer.sprite != null) renderer.transform.localPosition = Vector3.up * -renderer.sprite.bounds.min.y;
            renderer.gameObject.name = crop.displayName + " - stage " + (plot.stage + 1);
        }
    }
}

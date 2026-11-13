using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FarmPlotData
{
    public string areaId;
    public int x, y;
    public string cropId = "";
    public int stage;
    public int pendingHarvestAmount;
    public int wateredDay = -1;
    public int processedDay;
    public FarmPlotData Copy() => (FarmPlotData)MemberwiseClone();
}
[Serializable] public sealed class FarmSaveState { public List<FarmPlotData> plots = new List<FarmPlotData>(); }

// Persistent module: every tilled cell, crop and watering day belongs to one stable area/cell key.
public sealed class FarmingSaveModule : SaveParticipant<FarmSaveState>
{
    [SerializeField] private CropCatalog catalog;
    [Header("Upgrade de colheita")]
    [SerializeField, Range(0f, 1f)] private float upgradedDoubleHarvestChance = .25f;
    public float CurrentDoubleHarvestChance => UpgradePurchases.Has(UpgradePurchases.HarvestCrate)
        ? Mathf.Clamp01(upgradedDoubleHarvestChance) : 0f;
    private readonly Dictionary<string, FarmPlotData> plots = new Dictionary<string, FarmPlotData>();
    public static FarmingSaveModule Instance { get; private set; }
    public event Action Changed;
    public override string SaveKey => "farming";
    public override int RestoreOrder => 40;
    public CropCatalog Catalog => catalog;
    public int CurrentDay => Math.Max(0, (int)Math.Floor((Clock != null ? Clock.TotalGameMinutes : 0) / 1440d));
    private GameClock clock;
    private GameClock Clock => clock != null ? clock : (clock = GetComponentInChildren<GameClock>(true));
    private static string Key(string area, Vector3Int cell) => area + ":" + cell.x + ":" + cell.y;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Instance = null; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
    }
    private void OnEnable() { BedDayCycle.NewDayStarted += SynchronizeDay; }
    private void OnDisable() { BedDayCycle.NewDayStarted -= SynchronizeDay; }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void Update()
    {
        var saves = GetComponent<SaveGameService>();
        if (saves != null && saves.IsReady) SynchronizeDay();
    }

    public FarmPlotData GetPlot(string area, Vector3Int cell)
    {
        plots.TryGetValue(Key(area, cell), out var plot);
        return plot?.Copy();
    }

    public void SynchronizeDay()
    {
        int day = CurrentDay;
        bool changed = false;
        foreach (var plot in plots.Values) changed |= AdvancePlot(plot, day);
        if (changed) Changed?.Invoke();
    }

    private bool AdvancePlot(FarmPlotData plot, int day)
    {
        if (day == plot.processedDay) return false;
        if (day > plot.processedDay && plot.wateredDay == plot.processedDay)
        {
            var crop = catalog != null ? catalog.Find(plot.cropId) : null;
            if (crop != null && !crop.IsMature(plot.stage)) plot.stage++;
        }
        // A jump grants only the day actually watered. Rewinding the dev clock never creates water.
        plot.processedDay = day;
        plot.wateredDay = -1;
        return true;
    }

    public bool TryTill(FarmArea area, Vector3Int cell)
    {
        SynchronizeDay();
        if (area == null || !area.CanPlantAt(cell) || plots.ContainsKey(Key(area.AreaId, cell))) return false;
        plots.Add(Key(area.AreaId, cell), new FarmPlotData { areaId = area.AreaId, x = cell.x, y = cell.y, processedDay = CurrentDay });
        Changed?.Invoke();
        return true;
    }

    public bool TryPlant(FarmArea area, Vector3Int cell, CropDefinition crop)
    {
        SynchronizeDay();
        if (area == null || !area.CanPlantAt(cell) || crop == null || crop.growthStages == null || crop.growthStages.Length == 0 ||
            crop.harvestPrefab == null || catalog == null || catalog.Find(crop.id) != crop ||
            !plots.TryGetValue(Key(area.AreaId, cell), out var plot) || !string.IsNullOrEmpty(plot.cropId)) return false;
        plot.cropId = crop.id;
        plot.stage = 0;
        plot.pendingHarvestAmount = 0;
        plot.processedDay = CurrentDay;
        Changed?.Invoke();
        return true;
    }

    public bool TryWater(FarmArea area, Vector3Int cell)
    {
        SynchronizeDay();
        if (area == null || !area.CanPlantAt(cell) || !plots.TryGetValue(Key(area.AreaId, cell), out var plot) || plot.wateredDay == CurrentDay) return false;
        plot.wateredDay = CurrentDay;
        Changed?.Invoke();
        return true;
    }

    public bool TryHarvest(FarmArea area, Vector3Int cell, InventoryController inventory)
    {
        SynchronizeDay();
        if (area == null || !area.CanPlantAt(cell) || inventory == null ||
            !plots.TryGetValue(Key(area.AreaId, cell), out var plot)) return false;
        var crop = catalog != null ? catalog.Find(plot.cropId) : null;
        if (crop == null || !crop.IsMature(plot.stage) || crop.harvestPrefab == null) return false;
        // Preserve the roll if there is no room, including across save/load; retries cannot reroll the reward.
        if (plot.pendingHarvestAmount == 0)
        {
            float chance = CurrentDoubleHarvestChance;
            plot.pendingHarvestAmount = chance >= 1f || (chance > 0f && UnityEngine.Random.value < chance) ? 2 : 1;
        }
        if (!inventory.AddItem(new InventoryItem { itemPrefab = crop.harvestPrefab, amount = plot.pendingHarvestAmount }))
        {
            MarketHoursNotice.Show("Make room in your inventory to harvest.");
            return false;
        }
        plot.cropId = "";
        plot.stage = 0;
        plot.pendingHarvestAmount = 0;
        // Keep this day's watering and the tilled plot available for another seed.
        Changed?.Invoke();
        return true;
    }

    protected override FarmSaveState Capture()
    {
        // Bed advances the clock before writing the checkpoint; include growth in that same save.
        var state = new FarmSaveState();
        foreach (var plot in plots.Values)
        {
            var saved = plot.Copy();
            AdvancePlot(saved, CurrentDay);
            state.plots.Add(saved);
        }
        state.plots.Sort((a, b) => string.CompareOrdinal(Key(a.areaId, new Vector3Int(a.x, a.y, 0)), Key(b.areaId, new Vector3Int(b.x, b.y, 0))));
        return state;
    }
    protected override void Validate(FarmSaveState state)
    {
        Require(state.plots != null && state.plots.Count <= 100000, "Invalid saved farm plots.");
        var keys = new HashSet<string>();
        foreach (var plot in state.plots)
        {
            Require(plot != null && !string.IsNullOrEmpty(plot.areaId) && plot.areaId.Length <= 128 &&
                Math.Abs((long)plot.x) <= 100000 && Math.Abs((long)plot.y) <= 100000 && plot.processedDay >= 0 && plot.wateredDay >= -1,
                "Invalid saved farm cell.");
            Require(keys.Add(Key(plot.areaId, new Vector3Int(plot.x, plot.y, 0))), "Duplicate saved farm cell.");
            var crop = catalog != null ? catalog.Find(plot.cropId) : null;
            Require(string.IsNullOrEmpty(plot.cropId) ? plot.stage == 0 : crop != null && crop.growthStages != null && plot.stage >= 0 && plot.stage < crop.growthStages.Length,
                "Unknown crop or invalid growth stage.");
            Require(plot.pendingHarvestAmount >= 0 && plot.pendingHarvestAmount <= 2 &&
                (plot.pendingHarvestAmount == 0 || (crop != null && crop.IsMature(plot.stage))),
                "Invalid pending harvest reward.");
        }
    }
    protected override void Restore(FarmSaveState state)
    {
        plots.Clear();
        foreach (var plot in state.plots) plots.Add(Key(plot.areaId, new Vector3Int(plot.x, plot.y, 0)), plot.Copy());
        Changed?.Invoke();
    }
}

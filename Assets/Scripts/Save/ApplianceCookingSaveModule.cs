using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class ApplianceCookingState
{
    [Serializable]
    public sealed class Job
    {
        public string pieceId, station, dishId;
        public float remaining, burnElapsed;
        public SavedSlot[] ingredients;
    }

    public List<Job> jobs = new List<Job>();
}

public sealed class ApplianceCookingSaveModule : SaveParticipant<ApplianceCookingState>, ISceneSaveParticipant
{
    public override string SaveKey => "appliance-cooking";
    // Purchased pieces and their stations must already exist before restoring their contents.
    public override int RestoreOrder => 45;
    public bool SceneStateAvailable => RestaurantLayout.FindLayout();

    private static HashSet<KitchenAppliance> CurrentStations()
    {
        var stations = new HashSet<KitchenAppliance>();
        var layout = RestaurantLayout.FindLayout();
        if (layout)
            foreach (var piece in layout.Pieces)
                stations.UnionWith(piece.root.GetComponentsInChildren<KitchenAppliance>(true));
        return stations;
    }

    // Older checkpoints can contain a dish in a sink. Keep the recovery in the save
    // until the player actually has room, including when loading in another scene.
    private readonly List<ApplianceCookingState.Job> retiredJobs = new List<ApplianceCookingState.Job>();
    private float nextRecovery;

    private void Update()
    {
        if (retiredJobs.Count == 0 || Time.unscaledTime < nextRecovery || !SaveGameService.Instance.IsReady) return;
        nextRecovery = Time.unscaledTime + 2;
        if (!SceneStateAvailable) return;
        var bag = FindAnyObjectByType<InventoryController>();
        var bar = FindAnyObjectByType<HotbarController>();
        if (!bag || !bar) return;
        for (int i = retiredJobs.Count - 1; i >= 0; i--)
        {
            var dish = FindRecipe(retiredJobs[i]).result;
            if (Stove.GiveItem(dish)) retiredJobs.RemoveAt(i);
        }
    }

    protected override ApplianceCookingState Capture()
    {
        var state = new ApplianceCookingState();
        state.jobs.AddRange(retiredJobs);
        foreach (var appliance in CurrentStations())
        {
            if (appliance.CanStore || appliance.Burnt) continue;
            state.jobs.Add(new ApplianceCookingState.Job {
                pieceId = appliance.PieceId, station = appliance.StationKey,
                ingredients = appliance.Ingredients, dishId = appliance.CurrentRecipe == null ? "" : RecipeSaveModule.Id(appliance.CurrentRecipe), remaining = appliance.Remaining, burnElapsed = appliance.BurnElapsed
            });
        }
        return state;
    }

    protected override void Validate(ApplianceCookingState state)
    {
        Require(state.jobs != null, "Missing appliance jobs.");
        var ids = new HashSet<string>();
        foreach (var job in state.jobs)
        {
            Require(job != null && !string.IsNullOrEmpty(job.pieceId) && ids.Add(job.pieceId) &&
                !string.IsNullOrEmpty(job.station) && float.IsFinite(job.remaining) && job.remaining >= 0 && float.IsFinite(job.burnElapsed) && job.burnElapsed >= 0,
                "Invalid appliance job.");
            if (!string.IsNullOrEmpty(job.dishId)) FindRecipe(job);
            if (job.ingredients != null) foreach (var entry in job.ingredients)
                Require(entry != null && entry.amount > 0 && SaveItemCatalog.Instance.Prefab(entry.itemId), "Invalid stored ingredient.");
        }
    }

    private static CookingRecipe FindRecipe(ApplianceCookingState.Job job)
    {
        foreach (var recipe in ApplianceRecipeCatalog.ForStation(job.station == "sink" ? "cutting board" : job.station))
            if (RecipeSaveModule.Id(recipe) == job.dishId) return recipe;
        throw new InvalidDataException("Unknown saved dish at " + job.station + ": " + job.dishId);
    }

    protected override void Restore(ApplianceCookingState state)
    {
        retiredJobs.Clear();
        var stations = new Dictionary<string, KitchenAppliance>();
        foreach (var appliance in CurrentStations())
        {
            stations.Add(appliance.PieceId, appliance);
            appliance.RestoreIngredients(null);
            appliance.SetJob(null, 0);
        }
        foreach (var job in state.jobs)
        {
            if (job.station == "sink")
            {
                retiredJobs.Add(job);
                continue;
            }
            if (!stations.TryGetValue(job.pieceId, out var station) || station.StationKey != job.station)
                throw new InvalidDataException("Saved appliance is missing: " + job.pieceId);
            station.RestoreIngredients(job.ingredients);
            if (!string.IsNullOrEmpty(job.dishId)) station.SetJob(FindRecipe(job), job.remaining - job.burnElapsed);
        }
    }
}

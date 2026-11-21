using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UnderGrub/Appliance recipes")]
public sealed class ApplianceRecipeCatalog : ScriptableObject
{
    [Serializable]
    private class Station
    {
        public string key;
        public string displayName;
        public CookingRecipe[] recipes;
    }

    [SerializeField] private Station[] stations;
    private static ApplianceRecipeCatalog instance;
    private static ApplianceRecipeCatalog Instance => instance ? instance : (instance = Resources.Load<ApplianceRecipeCatalog>("ApplianceRecipes"));

    public static IReadOnlyList<CookingRecipe> ForStation(string key)
    {
        foreach (var station in Instance.stations)
            if (station.key == key) return station.recipes;
        return Array.Empty<CookingRecipe>();
    }

    public static string StationLabel(string key)
    {
        foreach (var station in Instance.stations)
            if (station.key == key) return station.displayName;
        return "";
    }

    public static string StationFor(CookingRecipe recipe)
    {
        foreach (var station in Instance.stations)
            if (Array.IndexOf(station.recipes, recipe) >= 0) return station.key;
        return null;
    }

    public static IEnumerable<CookingRecipe> AllIncludingStove(StoveController stove)
    {
        foreach (var recipe in stove.recipes) yield return recipe;
        foreach (var station in Instance.stations)
            foreach (var recipe in station.recipes) yield return recipe;
    }
}

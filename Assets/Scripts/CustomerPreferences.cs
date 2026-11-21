using UnityEngine;

public enum DishTemperature { Neutral, Warm, Fresh }

public static class CustomerPreferences
{
    public static float Weight(CookingRecipe recipe, DishTemperature favorite, int hour, VillageWeatherKind weather)
    {
        var temperature = recipe.temperature;
        float weight = temperature == favorite ? 1.8f : 1f;
        if (temperature == DishTemperature.Fresh)
        {
            if (weather == VillageWeatherKind.Hot || weather == VillageWeatherKind.Sunny) weight *= 2f;
            if (hour < 12) weight *= 1.4f;
        }
        if (temperature == DishTemperature.Warm)
        {
            if (weather == VillageWeatherKind.Rainy || weather == VillageWeatherKind.Foggy) weight *= 2f;
            if (hour >= 17) weight *= 1.4f;
        }
        return weight;
    }
}

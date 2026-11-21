using UnityEngine;

public enum VillageWeatherKind { Sunny, Hot, Cloudy, Rainy, Foggy, AfterRain }

public static class VillageWeather
{
#if UNITY_EDITOR
    public static VillageWeatherKind? Preview { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPreview() => Preview = null;
#endif

    // The calendar is already saved. A stable forecast keeps the weather on reload
    // and lets customers use today's weather even while the exterior is unloaded.
    public static VillageWeatherKind ForDay(int zeroBasedDay)
    {
#if UNITY_EDITOR
        if (Preview.HasValue) return Preview.Value;
#endif
        if (zeroBasedDay <= 0) return VillageWeatherKind.Sunny;
        if (IsRainDay(zeroBasedDay - 1)) return VillageWeatherKind.AfterRain;
        if (IsRainDay(zeroBasedDay)) return VillageWeatherKind.Rainy;
        uint roll = Hash((uint)zeroBasedDay) % 100;
        if (roll < 24) return VillageWeatherKind.Cloudy;
        if (roll < 44) return VillageWeatherKind.Hot;
        if (roll < 61) return VillageWeatherKind.Foggy;
        return VillageWeatherKind.Sunny;
    }

    // Salt the rainbow independently of the forecast. Reloading a checkpoint or
    // re-entering the village reproduces the same post-rain sky for the day.
    public static bool HasRainbow(int day) =>
        ForDay(day) == VillageWeatherKind.AfterRain && (Hash(unchecked((uint)day) ^ 0x6b44d82du) & 3u) == 0u;

    private static bool IsRainDay(int day)
    {
        // Only the first day of a run of rainy rolls triggers a storm.
        return day > 0 && RainRoll(day) && (day == 1 || !RainRoll(day - 1));
    }

    private static bool RainRoll(int day) => Hash((uint)day + 1501u) % 100 < 27;

    internal static uint Hash(uint value)
    {
        unchecked
        {
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            return value ^ (value >> 16);
        }
    }

    public static Color LightTint(VillageWeatherKind weather)
    {
        switch (weather)
        {
            case VillageWeatherKind.Rainy: return new Color(.70f, .78f, .86f);
            case VillageWeatherKind.Foggy: return new Color(.94f, .955f, .95f);
            case VillageWeatherKind.Hot: return new Color(1, .96f, .87f);
            case VillageWeatherKind.AfterRain: return new Color(.96f, .985f, 1f);
            default: return Color.white;
        }
    }
}

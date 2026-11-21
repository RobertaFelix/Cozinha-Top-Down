using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class VillageDayNight : MonoBehaviour
{
    [SerializeField] private Light2D globalLight;
    [SerializeField] private GameObject exterior;
    [SerializeField] private Light2D[] lamps;
    [Header("Hours")]
    [SerializeField, Range(0, 24)] private float dawnStarts = 6;
    [SerializeField, Range(0, 24)] private float dawnEnds = 7;
    [SerializeField, Range(0, 24)] private float sunsetStarts = 17;
    [SerializeField, Range(0, 24)] private float sunsetEnds = 18;
    [Header("Lighting")]
    [SerializeField] private Color daylight = Color.white;
    [SerializeField] private Color nightColor = new Color(.52f, .59f, .85f);
    [SerializeField, Range(0, 1)] private float nightIntensity = .28f;
    [SerializeField, Min(0)] private float lampIntensity = 1.2f;
    [Header("Post-rain rainbow")]
    [Tooltip("Centre cell of the bow in the exterior Grid. The default crosses the river by the house.")]
    [SerializeField] private Vector2Int rainbowCell = new Vector2Int(1, -5);
    [SerializeField] private Vector2 rainbowRadii = new Vector2(7.5f, 4.6f);
    private GameClock clock;
    private VillageWeatherEffects weatherEffects;
    private VillageWaterReflection terrain;
    private int forecastDay = -1;
    public VillageWeatherKind Weather { get; private set; }
    public float Darkness { get; private set; }

    private void Start()
    {
        if (!exterior) return;
        var visuals = new GameObject("Village atmosphere");
        visuals.transform.SetParent(exterior.transform, false);
        weatherEffects = visuals.AddComponent<VillageWeatherEffects>();
        weatherEffects.Initialize(exterior.transform, rainbowCell, rainbowRadii);
        terrain = visuals.AddComponent<VillageWaterReflection>();
        terrain.Initialize(exterior.transform);
    }
    public float EvaluateDarkness(float hour)
    {
        hour = Mathf.Repeat(hour, 24);
        if (hour < dawnStarts || hour >= sunsetEnds) return 1;
        if (hour < dawnEnds) return 1 - Mathf.InverseLerp(dawnStarts, dawnEnds, hour);
        if (hour < sunsetStarts) return 0;
        return Mathf.InverseLerp(sunsetStarts, sunsetEnds, hour);
    }
    private void LateUpdate()
    {
        if (!clock) clock = FindAnyObjectByType<GameClock>();
        if (!clock || !globalLight) return;
        int day = (int)(clock.TotalGameMinutes / 1440);
        var forecast = VillageWeather.ForDay(day);
        if (day != forecastDay || Weather != forecast)
        {
            forecastDay = day;
            Weather = forecast;
            if (weatherEffects) weatherEffects.SetWeather(Weather, day);
            if (terrain) terrain.SetWeather(Weather);
        }
        bool outside = exterior && exterior.activeInHierarchy;
        Darkness = outside ? EvaluateDarkness((float)(clock.TotalGameMinutes % 1440) / 60) : 0;
        if (outside && weatherEffects) weatherEffects.SetDaylight(1 - Darkness);
        Color sky = outside ? daylight * VillageWeather.LightTint(Weather) : daylight;
        globalLight.color = Color.Lerp(sky, nightColor, Darkness);
        globalLight.intensity = Mathf.Lerp(1, nightIntensity, Darkness);
        if (lamps != null) foreach (var lamp in lamps) if (lamp) lamp.intensity = Darkness * lampIntensity;
    }
    private void OnDisable()
    {
        if (globalLight) { globalLight.color = daylight; globalLight.intensity = 1; }
    }
}

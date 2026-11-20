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
    private GameClock clock;
    public float Darkness { get; private set; }
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
        Darkness = exterior && exterior.activeInHierarchy ? EvaluateDarkness((float)(clock.TotalGameMinutes % 1440) / 60) : 0;
        globalLight.color = Color.Lerp(daylight, nightColor, Darkness);
        globalLight.intensity = Mathf.Lerp(1, nightIntensity, Darkness);
        if (lamps != null) foreach (var lamp in lamps) if (lamp) lamp.intensity = Darkness * lampIntensity;
    }
    private void OnDisable()
    {
        if (globalLight) { globalLight.color = daylight; globalLight.intensity = 1; }
    }
}

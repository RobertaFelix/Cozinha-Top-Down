using UnityEngine;

[DisallowMultipleComponent]
public sealed class MarketOpeningHours : MonoBehaviour
{
    [SerializeField, Range(0, 23)] private int openingHour = 9;
    [SerializeField, Range(1, 24)] private int closingHour = 19;
    [SerializeField, Min(0f)] private float afterHoursMarkup = 0.2f;
    private GameClock clock;

    public bool CanAccess => IsOpen || UpgradePurchases.Has(UpgradePurchases.VillageKey);
    public float AfterHoursMarkup => Mathf.Max(0f, afterHoursMarkup);

    public bool IsOpen
    {
        get
        {
            if (clock == null) clock = FindAnyObjectByType<GameClock>();
            return clock != null && IsOpenAt(clock.TotalGameMinutes);
        }
    }

    // Read the clock at the moment of entry/purchase; no cached flag can lag a frame.
    public bool IsOpenAt(double totalMinutes)
    {
        if (double.IsNaN(totalMinutes) || double.IsInfinity(totalMinutes)) return false;
        double minuteOfDay = (totalMinutes % 1440d + 1440d) % 1440d;
        return minuteOfDay >= openingHour * 60 && minuteOfDay < closingHour * 60;
    }

    public void ShowClosedNotice()
    {
        MarketHoursNotice.Show("Market closed. Open " + openingHour.ToString("00") + ":00–" + closingHour.ToString("00") + ":00.");
    }
}

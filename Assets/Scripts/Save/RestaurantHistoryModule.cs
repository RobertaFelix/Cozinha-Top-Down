using System;
using UnityEngine;

[Serializable]
public sealed class RestaurantHistory
{
    public int served;
    public int expired;
    public int seated, turnedAway;
    public int day = -1;
    public int[] lastWindow = { -1, -1, -1, -1 };
}

public sealed class RestaurantHistoryModule : SaveParticipant<RestaurantHistory>
{
    private RestaurantHistory history = new RestaurantHistory();
    public override string SaveKey => "restaurant-history";
    protected override RestaurantHistory Capture() => history;
    protected override void Validate(RestaurantHistory value) =>
        Require(value.served >= 0 && value.expired >= 0 && value.seated >= 0 && value.turnedAway >= 0 && value.lastWindow?.Length == 4, "Invalid restaurant history.");
    protected override void Restore(RestaurantHistory value) => history = value;

    public void RecordService(bool served)
    {
        if (served) history.served++;
        else history.expired++;
    }

    public void RecordArrival(bool foundSeat)
    {
        if (foundSeat) history.seated++;
        else history.turnedAway++;
    }

    public float Rating(int dishes, int chairs)
    {
        float service = (history.served + 2f) / (history.served + 4f + history.expired);
        float seatAvailability = (history.seated + 2f) / (history.seated + history.turnedAway + 2f);
        return .45f * service + .35f * Mathf.Clamp01(dishes / 4f)
            + .2f * Mathf.Clamp01(chairs / 4f) * seatAvailability;
    }

    public bool TryWindow(int day, int resident, int window)
    {
        if (history.day != day)
        {
            history.day = day;
            Array.Fill(history.lastWindow, -1);
        }
        if (history.lastWindow[resident] >= window) return false;
        history.lastWindow[resident] = window;
        return true;
    }
}

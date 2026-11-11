using System;
using UnityEngine;

[Serializable] public sealed class DayCycleSaveState { public bool awaitingNextDay; }
public sealed class BedDayCycle : SaveParticipant<DayCycleSaveState>
{
    [SerializeField, Range(0, 23)] private int morningHour = 7;
    [SerializeField] private Vector3 wakePosition;
    public bool AwaitingNextDay { get; private set; }
    public static event Action DayEnded;
    public static event Action NewDayStarted;
    public override string SaveKey => "day-cycle";
    public override int RestoreOrder => 90;
    private SaveGameService Saves => GetComponent<SaveGameService>();
    private DayEndScreen Screen => GetComponent<DayEndScreen>();
    private GameClock Clock => GetComponentInChildren<GameClock>(true);
    private float previousTimeScale = 1;
    private bool modal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { DayEnded = null; NewDayStarted = null; }

    public void Sleep()
    {
        if (modal || !Saves.IsReady || ItemTooltipView.IsDragging || Time.timeScale <= 0) return;
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        if (environment == null || environment.IsTransitioning) return;
        Pause();
        AwaitingNextDay = true;
        SaveEndOfDay();
    }

    private void SaveEndOfDay()
    {
        if (!Saves.SaveCheckpoint())
        {
            Screen.ShowError("Could not save your game. Please try again.", SaveEndOfDay);
            return;
        }
        DayEnded?.Invoke();
        ShowSummary();
    }

    public void ContinueToNextDay()
    {
        if (!modal || !AwaitingNextDay) return;
        double before = Clock.TotalGameMinutes;
        Clock.SetTotalGameMinutes((Math.Floor(before / 1440d) + 1) * 1440d + morningHour * 60);
        AwaitingNextDay = false;
        if (!Saves.SaveCheckpoint())
        {
            Clock.SetTotalGameMinutes(before);
            AwaitingNextDay = true;
            Screen.ShowError("Could not save the next day. Please try again.", ContinueToNextDay);
            return;
        }
        Screen.Hide();
        modal = false;
        Time.timeScale = previousTimeScale;
        NewDayStarted?.Invoke();
    }

    public void OnCheckpointLoaded(bool hasSave)
    {
        if (!hasSave) return;
        // Compatibility with checkpoints made before player positions were saved.
        if (!Saves.HasSavedSection("player-location")) WakeAtHome();
        if (AwaitingNextDay) { Pause(); ShowSummary(); }
        else { Screen.Hide(); modal = false; }
    }

    private void Pause()
    {
        previousTimeScale = Time.timeScale > 0 ? Time.timeScale : 1;
        Time.timeScale = 0;
        modal = true;
        ItemTooltipView.HideAll();
        InteractionPromptManager.Instance?.Hide();
    }

    private void ShowSummary() => Screen.ShowSummary((int)Math.Floor(Clock.TotalGameMinutes / 1440d) + 1, ContinueToNextDay);
    private void WakeAtHome()
    {
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        if (environment == null || !environment.RestoreHomeCheckpoint(wakePosition))
            throw new InvalidOperationException("Home checkpoint is not configured.");
    }
    protected override DayCycleSaveState Capture() => new DayCycleSaveState { awaitingNextDay = AwaitingNextDay };
    protected override void Restore(DayCycleSaveState state) { AwaitingNextDay = state.awaitingNextDay; }
    private void OnDestroy() { if (modal) Time.timeScale = previousTimeScale; }
}

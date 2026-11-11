using System;

[Serializable] public sealed class ClockSaveState { public double totalMinutes; }
public sealed class ClockSaveModule : SaveParticipant<ClockSaveState>
{
    public override string SaveKey => "clock";
    public override int RestoreOrder => 10;
    private GameClock Clock => GetComponentInChildren<GameClock>(true);
    protected override ClockSaveState Capture() => new ClockSaveState { totalMinutes = Clock.TotalGameMinutes };
    protected override void Validate(ClockSaveState state) => Require(!double.IsNaN(state.totalMinutes) && !double.IsInfinity(state.totalMinutes) && state.totalMinutes >= 0 && state.totalMinutes < int.MaxValue - 1440, "Invalid saved clock.");
    protected override void Restore(ClockSaveState state) => Clock.SetTotalGameMinutes(state.totalMinutes);
}

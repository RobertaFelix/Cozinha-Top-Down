using System;
using UnityEngine;

[Serializable]
public sealed class PlayerLocationSaveState
{
    public Vector3 position;
    public string interiorId;
}

// Bed checkpoints are made in the village scene. Keep the exact player position
// and the active interior, rather than substituting a fixed home entrance spawn.
public sealed class PlayerLocationSaveModule : SaveParticipant<PlayerLocationSaveState>
{
    public override string SaveKey => "player-location";
    public override int RestoreOrder => 80;

    protected override PlayerLocationSaveState Capture()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        if (player == null || environment == null)
            throw new InvalidOperationException("The player location cannot be saved in this scene.");
        return new PlayerLocationSaveState
        {
            position = player.transform.position,
            interiorId = environment.CurrentInteriorId
        };
    }

    protected override void Validate(PlayerLocationSaveState state)
    {
        Require(Finite(state.position.x) && Finite(state.position.y) && Finite(state.position.z), "Invalid saved player position.");
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        Require(environment != null && environment.CanRestoreCheckpoint(state.interiorId), "Unknown saved interior.");
    }

    protected override void Restore(PlayerLocationSaveState state)
    {
        if (!FindAnyObjectByType<EnvironmentTransition>().RestorePlayerCheckpoint(state.interiorId, state.position))
            throw new InvalidOperationException("Could not restore the saved player location.");
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

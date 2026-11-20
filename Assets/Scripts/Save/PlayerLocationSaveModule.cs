using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class PlayerLocationSaveState
{
    public Vector3 position;
    public string interiorId;
    public string sceneName;
    public DungeonRunState dungeon;
}

// Old checkpoints without a scene name remain village checkpoints.
public sealed class PlayerLocationSaveModule : SaveParticipant<PlayerLocationSaveState>
{
    public override string SaveKey => "player-location";
    public override int RestoreOrder => 80;

    protected override PlayerLocationSaveState Capture()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        if (player == null)
            throw new InvalidOperationException("The player location cannot be saved in this scene.");
        return new PlayerLocationSaveState
        {
            position = player.transform.position,
            interiorId = environment != null ? environment.CurrentInteriorId : "",
            sceneName = SceneManager.GetActiveScene().name,
            dungeon = SceneManager.GetActiveScene().name == FindAnyObjectByType<SceneTransitionController>().DungeonSceneName
                ? DungeonRunState.Capture(player) : null
        };
    }

    protected override void Validate(PlayerLocationSaveState state)
    {
        Require(Finite(state.position.x) && Finite(state.position.y) && Finite(state.position.z), "Invalid saved player position.");
        var environment = FindAnyObjectByType<EnvironmentTransition>();
        string scene = string.IsNullOrEmpty(state.sceneName) ? "SampleScene" : state.sceneName;
        var transitions = FindAnyObjectByType<SceneTransitionController>();
        Require(scene == "SampleScene" || scene == transitions.DungeonSceneName, "Unknown saved scene.");
        Require(Application.CanStreamedLevelBeLoaded(scene), "Saved scene is unavailable.");
        if (scene == "SampleScene" && environment != null)
            Require(environment.CanRestoreCheckpoint(state.interiorId), "Unknown saved interior.");
        if (scene == transitions.DungeonSceneName)
        {
            var run = state.dungeon;
            Require(run != null && !string.IsNullOrEmpty(run.layout) && run.health > 0 && Finite(run.stamina) && run.stamina >= 0,
                "Invalid dungeon checkpoint.");
            Require(run.chests != null && run.enemies != null && run.doors != null &&
                run.chests.Count < 1000 && run.enemies.Count < 1000 && run.doors.Count < 1000, "Invalid dungeon state.");
            foreach (var chest in run.chests) Require(chest != null && ValidPosition(chest.position), "Invalid saved chest.");
            foreach (var enemy in run.enemies) Require(enemy != null && ValidPosition(enemy.position) && enemy.health > 0 && enemy.spawnIndex >= 0, "Invalid saved enemy.");
            foreach (var door in run.doors) Require(door != null && !string.IsNullOrEmpty(door.path), "Invalid saved door.");
        }
    }

    protected override void Restore(PlayerLocationSaveState state)
    {
        string scene = string.IsNullOrEmpty(state.sceneName) ? "SampleScene" : state.sceneName;
        if (scene != SceneManager.GetActiveScene().name || scene == FindAnyObjectByType<SceneTransitionController>().DungeonSceneName)
        {
            FindAnyObjectByType<SceneTransitionController>().RestoreCheckpoint(state);
            return;
        }
        if (!FindAnyObjectByType<EnvironmentTransition>().RestorePlayerCheckpoint(state.interiorId, state.position))
            throw new InvalidOperationException("Could not restore the saved player location.");
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool ValidPosition(Vector3 p) => Finite(p.x) && Finite(p.y) && Finite(p.z);
}

// Each mechanic owns a versioned JSON section. New mechanics add a participant,
// without changing the save file format or the other mechanics.
public interface ISaveParticipant
{
    string SaveKey { get; }
    int SaveVersion { get; }
    int RestoreOrder { get; }
    string CaptureState();
    void ValidateState(string json, int version);
    void RestoreState(string json, int version);
}

// Modules whose scene-side controllers are recreated when visiting a dungeon.
// Their live progress is kept in memory between scenes, independently of bed checkpoints.
public interface ISceneSaveParticipant : ISaveParticipant
{
    bool SceneStateAvailable { get; }
}

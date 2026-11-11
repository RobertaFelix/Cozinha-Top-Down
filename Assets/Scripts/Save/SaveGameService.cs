using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveGameService : MonoBehaviour
{
    public static SaveGameService Instance { get; private set; }
    public bool IsReady { get; private set; }
    public bool LoadFailed { get; private set; }
    public string LastError { get; private set; }
    public string SaveFilePath => Store.FilePath;
    public bool HasSavedSection(string key) => current.sections.Exists(section => section.key == key);
    private SaveStore store;
    private SaveDocument current = new SaveDocument();
    private float startupTimeScale;
    private readonly Dictionary<string, SaveSection> sceneState = new Dictionary<string, SaveSection>();
#if UNITY_EDITOR
    // Allows Play Mode integration checks to use an isolated directory.
    public static string TestDirectoryOverride;
#endif
    private SaveStore Store
    {
        get
        {
            if (store == null)
            {
                string directory = Application.persistentDataPath;
#if UNITY_EDITOR
                if (!string.IsNullOrEmpty(TestDirectoryOverride)) directory = TestDirectoryOverride;
#endif
                store = new SaveStore(System.IO.Path.Combine(directory, "Saves"));
            }
            return store;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() { Instance = null; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        startupTimeScale = Time.timeScale > 0 ? Time.timeScale : 1f;
        Time.timeScale = 0;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private IEnumerator Start()
    {
        if (Instance != this) yield break;
        // Inventory slots, recipes and UI are created by other components' Start.
        yield return null;
        LoadCheckpoint();
    }

    private List<ISaveParticipant> Participants()
    {
        var participants = new List<ISaveParticipant>();
        var keys = new HashSet<string>();
        foreach (var component in GetComponents<MonoBehaviour>())
            if (component is ISaveParticipant participant)
            {
                if (!keys.Add(participant.SaveKey)) throw new InvalidOperationException("Duplicate save key: " + participant.SaveKey);
                participants.Add(participant);
            }
        participants.Sort((a, b) => a.RestoreOrder.CompareTo(b.RestoreOrder));
        return participants;
    }

    public void LoadCheckpoint()
    {
        if (Instance != this) return;
        try
        {
            var participants = Participants();
            if (Store.Exists)
            {
                var document = Store.Read();
                foreach (var participant in participants)
                {
                    var section = document.sections.Find(s => s.key == participant.SaveKey);
                    if (section != null) participant.ValidateState(section.json, section.version);
                }
                var rollback = new Dictionary<ISaveParticipant, string>();
                foreach (var participant in participants) rollback.Add(participant, participant.CaptureState());
                try
                {
                    foreach (var participant in participants)
                    {
                        var section = document.sections.Find(s => s.key == participant.SaveKey);
                        if (section != null) participant.RestoreState(section.json, section.version);
                    }
                }
                catch
                {
                    foreach (var participant in participants)
                        participant.RestoreState(rollback[participant], participant.SaveVersion);
                    throw;
                }
                current = document;
            }
            LoadFailed = false;
            LastError = null;
            IsReady = true;
            Time.timeScale = startupTimeScale;
            GetComponent<DayEndScreen>().Hide();
            GetComponent<BedDayCycle>()?.OnCheckpointLoaded(Store.Exists);
        }
        catch (Exception error)
        {
            LastError = error.Message;
            LoadFailed = true;
            IsReady = false;
            Time.timeScale = 0;
            Debug.LogError("Could not load checkpoint: " + error);
            GetComponent<DayEndScreen>().ShowError("Could not load your save. Your files have been preserved.", LoadCheckpoint);
        }
    }

    public bool SaveCheckpoint()
    {
        if (!IsReady || LoadFailed) return false;
        try
        {
            // Preserve sections owned by modules absent from this version of the game.
            var next = JsonUtility.FromJson<SaveDocument>(JsonUtility.ToJson(current));
            foreach (var participant in Participants())
            {
                string json = participant.CaptureState();
                participant.ValidateState(json, participant.SaveVersion);
                next.sections.RemoveAll(s => s.key == participant.SaveKey);
                next.sections.Add(new SaveSection { key = participant.SaveKey, version = participant.SaveVersion, json = json });
            }
            Store.Write(next);
            current = next;
            LastError = null;
            return true;
        }
        catch (Exception error)
        {
            LastError = error.Message;
            Debug.LogError("Could not save checkpoint: " + error);
            return false;
        }
    }

    public void CaptureSceneState()
    {
        if (!IsReady) return;
        foreach (var participant in Participants())
            if (participant is ISceneSaveParticipant sceneParticipant && sceneParticipant.SceneStateAvailable)
                sceneState[participant.SaveKey] = new SaveSection
                {
                    key = participant.SaveKey, version = participant.SaveVersion, json = participant.CaptureState()
                };
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsReady && sceneState.Count > 0) StartCoroutine(RestoreSceneState());
    }

    private IEnumerator RestoreSceneState()
    {
        yield return null;
        foreach (var participant in Participants())
            if (participant is ISceneSaveParticipant sceneParticipant && sceneParticipant.SceneStateAvailable &&
                sceneState.TryGetValue(participant.SaveKey, out var section))
                participant.ValidateState(section.json, section.version);
        foreach (var participant in Participants())
            if (participant is ISceneSaveParticipant sceneParticipant && sceneParticipant.SceneStateAvailable &&
                sceneState.TryGetValue(participant.SaveKey, out var section))
                participant.RestoreState(section.json, section.version);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
        Time.timeScale = startupTimeScale;
    }
}

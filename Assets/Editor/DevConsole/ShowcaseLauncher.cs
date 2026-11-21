using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Keeping this launcher in the Editor assembly also keeps its scenarios out of player builds.
[InitializeOnLoad]
public static class ShowcaseLauncher
{
    private const string Prefix = "UnderGrub.Showcase.";
    private const string Village = "Assets/Scenes/SampleScene.unity";
    private static readonly Dictionary<string, Func<IEnumerator>> scenarios = new();
    private static ShowcaseMenu menu;
    private static string observation;
    private static InputAction shortcut;
    private static double lastShortcut = -1;
    public static string Current => SessionState.GetString(Prefix + "scenario", "");
    public static string Status { get; private set; }
    public static bool IsIsolated => SessionState.GetBool(Prefix + "isolated", false);
    private static bool Restart => SessionState.GetBool(Prefix + "restart", false);

    static ShowcaseLauncher()
    {
        if (IsIsolated) SaveGameService.TestDirectoryOverride = SessionState.GetString(Prefix + "directory", "");
        EditorApplication.playModeStateChanged += OnMode;
        AssemblyReloadEvents.beforeAssemblyReload += Uninstall;
        if (EditorApplication.isPlaying) EditorApplication.delayCall += Install;
    }

    [MenuItem("UnderGrub/Playtest menu _F7")]
    public static void Open()
    {
        if (EditorApplication.timeSinceStartup - lastShortcut < .2) return;
        lastShortcut = EditorApplication.timeSinceStartup;
        if (EditorApplication.isPlaying)
        {
            if (menu != null) menu.Toggle();
        }
        else if (!EditorApplication.isPlayingOrWillChangePlaymode) Begin("");
    }

    public static void Run(string label)
    {
        if (Restart || !scenarios.ContainsKey(label)) return;
        SessionState.SetString(Prefix + "next", label);
        SessionState.SetBool(Prefix + "restart", true);
        Status = "Starting";
        menu?.SetOpen(false);
        menu?.SetGuide("Starting a fresh test session…");
        EditorApplication.delayCall += EditorApplication.ExitPlaymode;
    }

    private static void Begin(string label)
    {
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Village);
        if (!scene) { Debug.LogError("Playtest scene missing: " + Village); return; }
        SessionState.SetString(Prefix + "oldStart", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Prefix + "restoreStart", true);
        var directory = Path.GetFullPath(Path.Combine("Library", "UnderGrubShowcase", Guid.NewGuid().ToString("N")));
        SessionState.SetString(Prefix + "directory", directory);
        SessionState.SetString(Prefix + "oldSave", SaveGameService.TestDirectoryOverride ?? "");
        SessionState.SetString(Prefix + "scenario", label);
        SessionState.SetBool(Prefix + "isolated", true);
        SaveGameService.TestDirectoryOverride = directory;
        EditorSceneManager.playModeStartScene = scene;
        EditorApplication.EnterPlaymode();
    }

    private static void RestoreStartScene()
    {
        if (!SessionState.GetBool(Prefix + "restoreStart", false)) return;
        var previous = SessionState.GetString(Prefix + "oldStart", "");
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
        SessionState.EraseBool(Prefix + "restoreStart");
    }

    private static void OnMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            RestoreStartScene();
            EditorApplication.delayCall += Install;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode) Uninstall();
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            RestoreStartScene();
            if (IsIsolated)
            {
                var previous = SessionState.GetString(Prefix + "oldSave", "");
                SaveGameService.TestDirectoryOverride = previous.Length > 0 ? previous : null;
                SessionState.EraseBool(Prefix + "isolated");
            }
            if (!Restart) return;
            var next = SessionState.GetString(Prefix + "next", "");
            SessionState.EraseBool(Prefix + "restart");
            EditorApplication.delayCall += () => Begin(next);
        }
    }

    private static void Install()
    {
        if (!Application.isPlaying || menu != null) return;
        scenarios.Clear();
        menu = new ShowcaseMenu();
        void Register(string title, string instructions, Func<IEnumerator> setup)
        {
            scenarios.Add(title,setup);
            menu.Add(title,instructions);
        }
        menu.Group("Kitchen and shopping");
        KitchenShowcase.Register(Register);
        menu.Group("Restaurant and world");
        WorldShowcase.Register(Register);
        shortcut = new InputAction("Playtest menu", InputActionType.Button, "<Keyboard>/f7");
        shortcut.performed += _ => Open();
        shortcut.Enable();
        Status = IsIsolated ? "Starting" : "Ready";
        if (!IsIsolated) return;
        EditorApplication.update += WaitForGame;
        deadline = EditorApplication.timeSinceStartup + 45;
    }

    private static double deadline;
    private static void WaitForGame()
    {
        var save = SaveGameService.Instance;
        if (EditorApplication.timeSinceStartup < deadline && (!save || (!save.IsReady && !save.LoadFailed))) return;
        EditorApplication.update -= WaitForGame;
        if (!save || !save.IsReady || SceneManager.GetActiveScene().path != Village ||
            !string.Equals(Path.GetDirectoryName(Path.GetFullPath(save.SaveFilePath)),
                Path.Combine(SessionState.GetString(Prefix + "directory",""),"Saves"), StringComparison.OrdinalIgnoreCase))
        {
            Fail("The isolated village session could not be loaded.");
            return;
        }
        if (Current.Length == 0) { Status = "Ready"; menu.SetOpen(true); return; }
        if (!scenarios.TryGetValue(Current,out var scenario)) { Fail("Unknown test: " + Current); return; }
        menu.SetGuide("Preparing " + Current + "…");
        save.StartCoroutine(Play(scenario));
    }

    public static void SetStep(string message) => menu?.SetGuide(message);

    public static void Note(string text) => observation = text;

    private static IEnumerator Play(Func<IEnumerator> scenario)
    {
        observation = "";
        var steps = new Stack<IEnumerator>();
        // A nested setup failure must be visible in the Game view as well as the Console.
        try { steps.Push(scenario()); }
        catch (Exception ex) { Fail(ex.Message); yield break; }
        while (steps.Count > 0)
        {
            object next;
            try
            {
                var step = steps.Peek();
                if (!step.MoveNext()) { steps.Pop(); continue; }
                next = step.Current;
            }
            catch (Exception ex) { Fail(ex.ToString()); yield break; }
            if (next is IEnumerator nested && !(next is CustomYieldInstruction)) steps.Push(nested);
            else yield return next;
        }
        Status = "Ready";
        menu.ShowInstructions(Current, observation);
    }

    private static void Fail(string message)
    {
        Status = "Failed";
        Debug.LogError("[Playtest] " + message);
        menu?.SetGuide("Test setup failed: " + message.Split('\n')[0]);
    }

    private static void Uninstall()
    {
        EditorApplication.update -= WaitForGame;
        shortcut?.Dispose();
        shortcut = null;
        menu?.Dispose();
        menu = null;
    }
}

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DevConsoleBootstrap
{
    private static InputAction toggleAction;
    private static double lastToggle = double.NegativeInfinity;
    public static bool IsResetting { get; private set; }
    private static string resetScene;
    private static GameObject resetRoot;
    private static double resetDeadline;

    static DevConsoleBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        if (EditorApplication.isPlaying) InstallInput();
    }

    [MenuItem("UnderGrub/Developer Console _F8")]
    public static void RequestToggle()
    {
        double now = EditorApplication.timeSinceStartup;
        // The editor menu shortcut and the Game-view InputAction can see the same key press.
        if (now - lastToggle < .2) return;
        lastToggle = now;
        EditorApplication.delayCall += Toggle;
    }

    private static void Toggle()
    {
        if (!EditorApplication.isPlaying) { Debug.Log("Enter Play Mode to open the developer console in the Game view."); return; }
        var overlays = Resources.FindObjectsOfTypeAll<DevConsoleOverlay>();
        if (overlays.Length > 0) { foreach (var overlay in overlays) overlay.Close(); }
        else if (!IsResetting)
        {
            var overlay = ScriptableObject.CreateInstance<DevConsoleOverlay>();
            overlay.hideFlags = HideFlags.HideAndDontSave;
            overlay.Open();
        }
    }

    private static void InstallInput()
    {
        DisposeInput();
        toggleAction = new InputAction("EditorDevConsole", InputActionType.Button, "<Keyboard>/f8");
        toggleAction.performed += _ => RequestToggle();
        toggleAction.Enable();
    }

    private static void DisposeInput()
    {
        if (toggleAction == null) return;
        toggleAction.Disable();
        toggleAction.Dispose();
        toggleAction = null;
    }

    private static void BeforeReload()
    {
        DisposeInput();
        foreach (var window in Resources.FindObjectsOfTypeAll<DevConsoleOverlay>()) window.Close();
        EditorApplication.update -= FinishReset;
        IsResetting = false;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) EditorApplication.delayCall += () => { if (EditorApplication.isPlaying) InstallInput(); };
        if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            DisposeInput();
            foreach (var window in Resources.FindObjectsOfTypeAll<DevConsoleOverlay>()) window.Close();
            EditorApplication.update -= FinishReset;
            IsResetting = false;
        }
    }

    public static void RequestReset()
    {
        if (IsResetting) throw new InvalidOperationException("A reset is already pending.");
        IsResetting = true;
        EditorApplication.delayCall += BeginReset;
    }

    private static void BeginReset()
    {
        try
        {
            if (!Application.isPlaying) { IsResetting = false; return; }
            var saves = SaveGameService.Instance;
            if (saves == null) throw new InvalidOperationException("Save service is unavailable.");
            var scenes = UnityEngine.Object.FindAnyObjectByType<SceneTransitionController>();
            if (scenes == null) throw new InvalidOperationException("Scene transition controller is unavailable.");
            resetScene = new SerializedObject(scenes).FindProperty("firstSceneName").stringValue;
            if (!Application.CanStreamedLevelBeLoaded(resetScene)) throw new InvalidOperationException("The first scene is not in the build scene list.");
            // No game state is destroyed until every existing save file has been archived.
            string archive = DevConsoleSaveTools.Archive(saves.SaveFilePath, true);
            Debug.Log("Developer save reset. Archived files: " + archive);
            foreach (var window in Resources.FindObjectsOfTypeAll<DevConsoleOverlay>()) window.Close();
            Time.timeScale = 0;
            UpgradePurchases.RestorePurchases(Array.Empty<int>());
            resetRoot = saves.transform.root.gameObject;
            UnityEngine.Object.Destroy(resetRoot);
            foreach (var tooltip in UnityEngine.Object.FindObjectsByType<ItemTooltipView>())
                UnityEngine.Object.Destroy(tooltip.gameObject);
            resetDeadline = EditorApplication.timeSinceStartup + 10;
            EditorApplication.update += FinishReset;
        }
        catch (Exception error)
        {
            IsResetting = false;
            DevConsoleOverlay.LogGlobal("Reset aborted: " + error.Message, true);
        }
    }

    private static void FinishReset()
    {
        if (resetRoot != null && EditorApplication.timeSinceStartup < resetDeadline) return;
        EditorApplication.update -= FinishReset;
        if (!Application.isPlaying) { IsResetting = false; return; }
        if (resetRoot != null)
        {
            IsResetting = false;
            DevConsoleOverlay.LogGlobal("Reset aborted: old game objects could not be unloaded.", true);
            return;
        }
        Time.timeScale = 1;
        SceneManager.LoadScene(resetScene);
        IsResetting = false;
    }
}
#endif

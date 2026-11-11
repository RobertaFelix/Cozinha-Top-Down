using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Prevent the spawn-editing scene from being included in the initial Play setup.
[InitializeOnLoad]
public static class DungeonSpawnPlayModeGuard
{
    private static bool preparingPlay;

    static DungeonSpawnPlayModeGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode || preparingPlay ||
            !TryFindEditingScene(out Scene dungeon, out Scene source)) return;

        // Save and unload in Edit Mode, before Unity captures the Play scene setup.
        EditorApplication.isPlaying = false;
        preparingPlay = true;
        EditorApplication.delayCall += () =>
        {
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SaveAndClose(dungeon, source)) EditorApplication.EnterPlaymode();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            finally { preparingPlay = false; }
        };
    }

    private static bool TryFindEditingScene(out Scene dungeon, out Scene source)
    {
        dungeon = default;
        source = default;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var candidateSource = SceneManager.GetSceneAt(i);
            if (!candidateSource.isLoaded) continue;
            foreach (var root in candidateSource.GetRootGameObjects())
                foreach (var controller in root.GetComponentsInChildren<SceneTransitionController>(true))
                    for (int j = 0; j < SceneManager.sceneCount; j++)
                    {
                        var candidate = SceneManager.GetSceneAt(j);
                        if (candidate == candidateSource || !candidate.isLoaded ||
                            Path.GetFileNameWithoutExtension(candidate.path) != controller.DungeonSceneName) continue;
                        dungeon = candidate;
                        source = candidateSource;
                        return true;
                    }
        }
        return false;
    }

    public static bool SaveAndClose(Scene dungeon, Scene source)
    {
        if (!dungeon.IsValid() || !dungeon.isLoaded || !source.IsValid() ||
            !source.isLoaded || source == dungeon) return false;
        if (string.IsNullOrEmpty(dungeon.path) || !EditorSceneManager.SaveScene(dungeon))
        {
            Debug.LogError("Could not save the dungeon scene. Play was cancelled to preserve your spawn edits.");
            return false;
        }
        SceneManager.SetActiveScene(source);
        if (!EditorSceneManager.CloseScene(dungeon, true))
        {
            Debug.LogError("Could not close the dungeon editing scene. Play was cancelled.");
            return false;
        }
        return true;
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(SceneTransitionController))]
public sealed class SceneTransitionControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dungeon spawn points", EditorStyles.boldLabel);
        var controller = (SceneTransitionController)target;
        var scene = SceneManager.GetSceneByName(controller.DungeonSceneName);
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (GUILayout.Button("Open dungeon scene to edit spawn points"))
                {
                    foreach (var entry in EditorBuildSettings.scenes)
                        if (Path.GetFileNameWithoutExtension(entry.path) == controller.DungeonSceneName)
                        {
                            EditorSceneManager.OpenScene(entry.path, OpenSceneMode.Additive);
                            break;
                        }
                }
                return;
            }
            foreach (var root in scene.GetRootGameObjects())
                foreach (var layout in root.GetComponentsInChildren<DungeonLayout>(true))
                {
                    EditorGUILayout.LabelField(layout.DisplayName, EditorStyles.boldLabel);
                    foreach (var point in layout.SpawnPoints)
                    {
                        if (point == null) continue;
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField(point, typeof(Transform), true);
                            if (GUILayout.Button("Select", GUILayout.Width(55)))
                            {
                                Selection.activeGameObject = point.gameObject;
                                SceneView.lastActiveSceneView?.FrameSelected();
                            }
                        }
                    }
                    if (GUILayout.Button("Create spawn point — " + layout.DisplayName))
                        CreateSpawnPoint(layout);
                    var chests = layout.GetComponent<DungeonChestSpawner>();
                    if (chests != null)
                    {
                        EditorGUILayout.ObjectField("Chest spawner", chests, typeof(DungeonChestSpawner), true);
                        if (GUILayout.Button("Create chest spawn point - " + layout.DisplayName))
                            DungeonChestSpawnerEditor.CreateSpawnPoint(chests);
                    }
                }
            EditorGUILayout.HelpBox("Move each spawn point to a walkable location in the Scene view. Before Play, the dungeon scene is automatically saved and closed. Entry randomly selects a dungeon, then one of its points.", MessageType.Info);
        }
    }

    public static Transform CreateSpawnPoint(DungeonLayout layout)
    {
        int undoGroup = Undo.GetCurrentGroup();
        var point = new GameObject("Spawn " + (layout.SpawnPoints.Count + 1));
        Undo.RegisterCreatedObjectUndo(point, "Create dungeon spawn point");
        Undo.SetTransformParent(point.transform, layout.transform, "Parent dungeon spawn point");
        point.transform.localPosition = layout.SpawnPoints.Count > 0 && layout.SpawnPoints[0] != null
            ? layout.SpawnPoints[0].localPosition : Vector3.zero;
        var serialized = new SerializedObject(layout);
        var points = serialized.FindProperty("spawnPoints");
        points.InsertArrayElementAtIndex(points.arraySize);
        points.GetArrayElementAtIndex(points.arraySize - 1).objectReferenceValue = point.transform;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
        Selection.activeGameObject = point;
        SceneView.lastActiveSceneView?.FrameSelected();
        Undo.CollapseUndoOperations(undoGroup);
        return point.transform;
    }
}

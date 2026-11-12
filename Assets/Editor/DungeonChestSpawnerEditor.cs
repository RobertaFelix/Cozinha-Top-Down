using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(DungeonChestSpawner))]
public sealed class DungeonChestSpawnerEditor : Editor
{
    private DungeonChestRoom selectedRoom;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var spawner = (DungeonChestSpawner)target;
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            selectedRoom = (DungeonChestRoom)EditorGUILayout.ObjectField("Room for new points", selectedRoom, typeof(DungeonChestRoom), true);
            if (GUILayout.Button("Create chest room")) selectedRoom = CreateRoom(spawner);
            if (GUILayout.Button("Create chest spawn point")) CreateSpawnPoint(spawner, selectedRoom);
        }
        EditorGUILayout.HelpBox("Group points by their physical room. Each room receives one chest before eligible rooms can repeat, up to its Maximum Chests. Only the selected player spawn excludes nearby points. Move gold markers onto walkable floors, away from doors.", MessageType.Info);
    }

    public static DungeonChestRoom CreateRoom(DungeonChestSpawner spawner)
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create chest room");
        var room = new GameObject("Chest Room " + (spawner.GetComponentsInChildren<DungeonChestRoom>(true).Length + 1));
        Undo.RegisterCreatedObjectUndo(room, "Create chest room");
        Undo.SetTransformParent(room.transform, spawner.transform, "Parent chest room");
        room.transform.localPosition = Vector3.zero;
        var component = Undo.AddComponent<DungeonChestRoom>(room);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Selection.activeGameObject = room;
        Undo.CollapseUndoOperations(group);
        return component;
    }

    public static Transform CreateSpawnPoint(DungeonChestSpawner spawner, DungeonChestRoom room = null)
    {
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create chest spawn point");
        if (room == null || !room.transform.IsChildOf(spawner.transform))
        {
            var rooms = spawner.GetComponentsInChildren<DungeonChestRoom>(true);
            room = rooms.Length > 0 ? rooms[0] : CreateRoom(spawner);
        }
        var point = new GameObject("Chest Spawn " + (spawner.SpawnPoints.Count + 1));
        Undo.RegisterCreatedObjectUndo(point, "Create chest spawn point");
        Undo.SetTransformParent(point.transform, room.transform, "Parent chest spawn point");
        point.transform.position = room.transform.childCount > 1
            ? room.transform.GetChild(0).position + Vector3.right * 2f : room.transform.position;
        var serialized = new SerializedObject(spawner);
        var points = serialized.FindProperty("spawnPoints");
        points.InsertArrayElementAtIndex(points.arraySize);
        points.GetArrayElementAtIndex(points.arraySize - 1).objectReferenceValue = point.transform;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Selection.activeGameObject = point;
        SceneView.lastActiveSceneView?.FrameSelected();
        Undo.CollapseUndoOperations(group);
        return point.transform;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class SaveItemCatalogBuilder : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) { Rebuild(); }

    [MenuItem("UnderGrub/Save/Rebuild item catalog")]
    public static SaveItemCatalog Rebuild()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string path = "Assets/Resources/SaveItemCatalog.asset";
        var catalog = AssetDatabase.LoadAssetAtPath<SaveItemCatalog>(path);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<SaveItemCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
        var entries = new List<SaveItemCatalog.Entry>();
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        Array.Sort(guids, StringComparer.Ordinal);
        foreach (var guid in guids)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab.GetComponent<ItemDragHandler>() != null)
                entries.Add(new SaveItemCatalog.Entry { id = guid, prefab = prefab });
        }
        catalog.entries = entries;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);
        return catalog;
    }
}

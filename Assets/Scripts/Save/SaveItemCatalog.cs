using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Asset GUIDs are baked by the editor; the player never depends on AssetDatabase or item names.
public sealed class SaveItemCatalog : ScriptableObject
{
    [Serializable] public sealed class Entry { public string id; public GameObject prefab; }
    public List<Entry> entries = new List<Entry>();
    private static SaveItemCatalog instance;
    public static SaveItemCatalog Instance => instance != null ? instance : (instance = Resources.Load<SaveItemCatalog>("SaveItemCatalog"));
    public string Id(GameObject prefab)
    {
        if (prefab == null) return "";
        var entry = entries.Find(e => e.prefab == prefab);
        if (entry == null) throw new InvalidDataException("Item is missing from save catalog: " + prefab.name);
        return entry.id;
    }
    public GameObject Prefab(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var entry = entries.Find(e => e.id == id && e.prefab != null);
        if (entry == null) throw new InvalidDataException("Unknown saved item: " + id);
        return entry.prefab;
    }
}

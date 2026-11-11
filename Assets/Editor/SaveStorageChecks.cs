using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

// Run without a Play session; every check uses an isolated temporary directory.
public static class SaveStorageChecks
{
    [Serializable] private sealed class Envelope { public string format = "UnderGrub"; public string payload; public string checksum; }

    [MenuItem("UnderGrub/Save/Run storage checks")]
    public static void Run()
    {
        string directory = Path.Combine(Application.temporaryCachePath, "SaveStorageChecks-" + Guid.NewGuid().ToString("N"));
        var store = new SaveStore(directory);
        try
        {
            Require(!store.Exists, "A new game has no checkpoint.");
            store.Write(Document("first"));
            Require(store.Read().sections[0].json == "{\"value\":\"first\"}", "Checkpoint round trip.");
            store.Write(Document("second"));
            string backup = File.ReadAllText(store.FilePath + ".bak");
            File.WriteAllText(store.FilePath, File.ReadAllText(store.FilePath).Replace("second", "tampered"));
            Require(store.Read().sections[0].json.Contains("first"), "A valid JSON file with a bad checksum recovers the backup.");
            File.WriteAllText(store.FilePath, "incomplete JSON");
            Require(store.Read().sections[0].json.Contains("first"), "Corrupt primary falls back to the previous checkpoint.");
            store.Write(Document("third"));
            Require(File.ReadAllText(store.FilePath + ".bak") == backup, "Recovery does not replace a good backup with the corrupt primary.");
            File.WriteAllText(store.FilePath + ".tmp", "interrupted write");
            Require(store.Read().sections[0].json.Contains("third"), "An interrupted temporary write does not affect the checkpoint.");
            store.Write(Document("fourth"));
            Require(!File.Exists(store.FilePath + ".tmp"), "A successful write cleans its temporary file.");
            var future = Document("future");
            future.version = 2;
            string payload = JsonUtility.ToJson(future);
            string hash;
            using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "");
            File.WriteAllText(store.FilePath, JsonUtility.ToJson(new Envelope { payload = payload, checksum = hash }));
            bool rejected = false;
            try { store.Read(); } catch (NotSupportedException) { rejected = true; }
            Require(rejected, "A newer save format is not silently replaced with an older backup.");
            Debug.Log("Save storage checks: all passed.");
        }
        finally
        {
            foreach (string suffix in new[] { "", ".tmp", ".bak" })
                if (File.Exists(store.FilePath + suffix)) File.Delete(store.FilePath + suffix);
            if (Directory.Exists(directory) && Directory.GetFileSystemEntries(directory).Length == 0) Directory.Delete(directory);
        }
    }

    private static SaveDocument Document(string value)
    {
        var document = new SaveDocument();
        document.sections.Add(new SaveSection { key = "test", version = 1, json = "{\"value\":\"" + value + "\"}" });
        return document;
    }
    private static void Require(bool valid, string message)
    {
        if (!valid) throw new Exception("Save storage check failed: " + message);
    }
}

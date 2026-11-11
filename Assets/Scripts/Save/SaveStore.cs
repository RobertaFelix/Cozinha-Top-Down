using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Write to a temporary file, flush, then atomically replace the checkpoint.
// A checksum detects incomplete/corrupt JSON; the previous checkpoint is retained.
public sealed class SaveStore
{
    [Serializable] private sealed class Envelope
    {
        public string format = "UnderGrub";
        public string payload;
        public string checksum;
    }

    public string FilePath { get; }
    public bool Exists => File.Exists(FilePath) || File.Exists(FilePath + ".bak");
    private bool recoveredBackup;

    public SaveStore(string directory) { FilePath = Path.Combine(directory, "save.json"); }

    private static string Hash(string value)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
    }

    private static SaveDocument ReadFile(string path)
    {
        var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
        if (envelope == null || envelope.format != "UnderGrub" || string.IsNullOrEmpty(envelope.payload) ||
            envelope.checksum != Hash(envelope.payload)) throw new InvalidDataException("Invalid save checksum.");
        var document = JsonUtility.FromJson<SaveDocument>(envelope.payload);
        if (document != null && document.version != 1) throw new NotSupportedException("Unsupported save format.");
        if (document == null || document.sections == null) throw new InvalidDataException("Invalid save format.");
        var keys = new System.Collections.Generic.HashSet<string>();
        foreach (var section in document.sections)
            if (section == null || string.IsNullOrEmpty(section.key) || !keys.Add(section.key) ||
                string.IsNullOrEmpty(section.json)) throw new InvalidDataException("Invalid save sections.");
        return document;
    }

    public SaveDocument Read()
    {
        recoveredBackup = false;
        try { return ReadFile(FilePath); }
        catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException || error is UnauthorizedAccessException)
        {
            if (!File.Exists(FilePath + ".bak")) throw;
            var backup = ReadFile(FilePath + ".bak");
            recoveredBackup = true;
            Debug.LogWarning("The previous backup save was recovered.");
            return backup;
        }
    }

    public void Write(SaveDocument document)
    {
        document.savedAtUtc = DateTime.UtcNow.ToString("O");
        string payload = JsonUtility.ToJson(document);
        string json = JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(payload) }, true);
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        string temp = FilePath + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(FilePath)) File.Replace(temp, FilePath, recoveredBackup ? null : FilePath + ".bak");
            else File.Move(temp, FilePath);
            recoveredBackup = false;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

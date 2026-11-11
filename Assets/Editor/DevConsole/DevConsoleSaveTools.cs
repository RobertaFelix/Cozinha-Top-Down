#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

public static class DevConsoleSaveTools
{
    // Known checkpoint files only. Reset moves them out of the load path; no recursive deletion.
    public static string Archive(string savePath, bool reset)
    {
        string directory = Path.GetDirectoryName(savePath);
        var files = new List<string>();
        foreach (string suffix in new[] { "", ".bak", ".tmp" })
            if (File.Exists(savePath + suffix)) files.Add(savePath + suffix);
        if (files.Count == 0)
        {
            if (reset) return "No previous save files.";
            throw new ArgumentException("There is no save file to back up yet.");
        }
        string archive = Path.Combine(directory, reset ? "ResetBackups" : "ManualBackups",
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(archive);
        var moved = new List<string>();
        try
        {
            foreach (string path in files)
            {
                string destination = Path.Combine(archive, Path.GetFileName(path));
                if (reset) { File.Move(path, destination); moved.Add(path); }
                else File.Copy(path, destination);
            }
            return archive;
        }
        catch
        {
            // Roll back a partially completed move before cancelling the reset.
            foreach (string original in moved)
                File.Move(Path.Combine(archive, Path.GetFileName(original)), original);
            throw;
        }
    }
}
#endif

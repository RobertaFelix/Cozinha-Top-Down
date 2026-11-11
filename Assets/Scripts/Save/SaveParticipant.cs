using System;
using System.IO;
using UnityEngine;

public abstract class SaveParticipant<T> : MonoBehaviour, ISaveParticipant where T : class, new()
{
    public abstract string SaveKey { get; }
    public virtual int SaveVersion => 1;
    public virtual int RestoreOrder => 0;
    public string CaptureState() => JsonUtility.ToJson(Capture());
    public void ValidateState(string json, int version)
    {
        if (version != SaveVersion) throw new InvalidDataException("Unsupported section version: " + SaveKey);
        var state = JsonUtility.FromJson<T>(json);
        if (state == null) throw new InvalidDataException("Invalid state: " + SaveKey);
        Validate(state);
    }
    public void RestoreState(string json, int version)
    {
        ValidateState(json, version);
        Restore(JsonUtility.FromJson<T>(json));
    }
    protected abstract T Capture();
    protected abstract void Restore(T state);
    protected virtual void Validate(T state) { }
    protected static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidDataException(message);
    }
}

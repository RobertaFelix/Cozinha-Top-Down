using System;
using System.Collections.Generic;

[Serializable]
public sealed class SaveDocument
{
    public int version = 1;
    public string savedAtUtc;
    public List<SaveSection> sections = new List<SaveSection>();
}

[Serializable]
public sealed class SaveSection
{
    public string key;
    public int version;
    public string json;
}

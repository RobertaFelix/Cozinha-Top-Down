using UnityEngine;

[CreateAssetMenu(menuName = "UnderGrub/Crop Catalog")]
public sealed class CropCatalog : ScriptableObject
{
    public CropDefinition[] crops;
    public CropDefinition Find(string id)
    {
        if (string.IsNullOrEmpty(id) || crops == null) return null;
        foreach (var crop in crops) if (crop != null && crop.id == id) return crop;
        return null;
    }
}

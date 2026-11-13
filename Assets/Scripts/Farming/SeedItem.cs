using UnityEngine;

public sealed class SeedItem : MonoBehaviour
{
    [SerializeField] private CropDefinition crop;
    public CropDefinition Crop => crop;
}

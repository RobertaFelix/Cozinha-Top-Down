using UnityEngine;

[CreateAssetMenu(menuName = "UnderGrub/Crop")]
public sealed class CropDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite[] growthStages;
    public GameObject harvestPrefab;
    public GameObject seedPrefab;
    public bool IsMature(int stage) => growthStages != null && growthStages.Length > 0 && stage >= growthStages.Length - 1;
}

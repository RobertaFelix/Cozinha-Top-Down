using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "UnderGrub/Dungeon expedition")]
public sealed class DungeonExpeditionStyle : ScriptableObject
{
    public TileBase[] floors;
    public Tile wall;
    public Sprite[] torch, fairyIdle, fairyCharge, fairyCast, meteor, fire, ice;
    [Range(0, 1)] public float ambientIntensity = .23f;
    [Min(1)] public float lanternRadius = 4.5f;
    [Min(1)] public int fairyHealth = 460;
    [Min(1)] public int fairyTreasure = 150;
    [Min(1)] public int fireDamage = 16, iceDamage = 12;
    public static DungeonExpeditionStyle Load() => Resources.Load<DungeonExpeditionStyle>("DungeonExpeditionStyle");
}

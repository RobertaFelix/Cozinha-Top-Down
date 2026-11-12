using UnityEngine;

// Authored room grouping: keep all possible chest points in a physical room under this object.
[DisallowMultipleComponent]
public sealed class DungeonChestRoom : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumChests = 2;
    public int MaximumChests => Mathf.Max(1, maximumChests);
}

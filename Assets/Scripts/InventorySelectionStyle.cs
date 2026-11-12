using UnityEngine;

// Direct sprite references keep the original UI sheet in its artist-owned folder.
public sealed class InventorySelectionStyle : ScriptableObject
{
    public Sprite topLeft;
    public Sprite topRight;
    public Sprite bottomLeft;
    public Sprite bottomRight;

    [Tooltip("Outward spacing in pixels of the original 32px slot.")]
    [Range(0f, 8f)] public float framePadding = 4f;

    [Header("Hover audio")]
    public AudioClip hoverSound;
    [Range(0f, 1f)] public float hoverVolume = 0.25f;
}

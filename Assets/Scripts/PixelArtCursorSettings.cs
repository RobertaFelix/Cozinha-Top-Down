using UnityEngine;

public sealed class PixelArtCursorSettings : ScriptableObject
{
    public Texture2D texture;
    [Range(1, 8)] public int pixelScale = 3;
    [Tooltip("Click point in original texture pixels, measured from the top-left corner.")]
    public Vector2Int hotspot = Vector2Int.zero;
}

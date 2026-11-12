using UnityEngine;

public sealed class ItemTooltipStyle : ScriptableObject
{
    public TMPro.TMP_FontAsset font;
    public Sprite headerBackground;
    public Sprite bodyBackground;
    [Min(1f)] public float pixelScale = 6.25f;
    [Min(100f)] public float width = 360f;
    [Min(100f)] public float minimumWidth = 220f;
    [Min(0f)] public float minimumHeight = 128f;
    [Min(1f)] public float titleFontSize = 56f;
    [Min(1f)] public float bodyFontSize = 28f;
    public Color titleColor = new Color32(69, 43, 29, 255);
    public Color descriptionColor = Color.black;
    public Color valueColor = Color.black;
    public Color effectColor = Color.black;
    public Color durationColor = Color.black;
    public Color quantityColor = Color.black;
    public Color ingredientsColor = Color.black;
}

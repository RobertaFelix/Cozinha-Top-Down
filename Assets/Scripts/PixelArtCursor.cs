using UnityEngine;

// Hardware cursor: no UI image following the pointer or frame-dependent movement.
public sealed class PixelArtCursor : MonoBehaviour
{
    private static PixelArtCursor instance;
    private Texture2D cursorTexture;
    private Vector2 hotspot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        var settings = Resources.Load<PixelArtCursorSettings>("PixelArtCursorSettings");
        if (settings == null || settings.texture == null) return;
        var root = new GameObject("PixelArtCursor");
        instance = root.AddComponent<PixelArtCursor>();
        DontDestroyOnLoad(root);
        instance.Configure(settings);
    }

    private void Configure(PixelArtCursorSettings settings)
    {
        var source = settings.texture;
        int scale = Mathf.Clamp(settings.pixelScale, 1, 8);
        int width = source.width * scale;
        int height = source.height * scale;
        var pixels = source.GetPixels32();
        var scaled = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                scaled[y * width + x] = pixels[(y / scale) * source.width + x / scale];
        cursorTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "UnderGrub Pixel Cursor",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        cursorTexture.SetPixels32(scaled);
        // Cursor.SetCursor requires a readable texture. Keep the scaled texture readable.
        cursorTexture.Apply(false, false);
        hotspot = new Vector2(Mathf.Clamp(settings.hotspot.x, 0, source.width - 1) * scale,
            Mathf.Clamp(settings.hotspot.y, 0, source.height - 1) * scale);
        ApplyCursor();
    }

    private void ApplyCursor()
    {
        if (cursorTexture != null) Cursor.SetCursor(cursorTexture, hotspot, CursorMode.Auto);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused) ApplyCursor();
        else Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void OnDisable() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

    private void OnDestroy()
    {
        if (instance == this)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            instance = null;
        }
        if (cursorTexture != null) Destroy(cursorTexture);
    }
}

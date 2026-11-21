using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

// Owned by the scene so the lantern and mist disappear even when the player is persistent.
public sealed class DungeonAtmosphere : MonoBehaviour
{
    private DungeonExpeditionStyle style;
    private Transform lantern, player;
    private SpriteRenderer mist;
    private Camera dungeonCamera;
    private Color oldBackground;
    private CameraClearFlags oldClear;
    private Sprite mistSprite;
    private Texture2D mistTexture;
    private readonly System.Collections.Generic.List<SpriteRenderer> torches = new();

    public void Initialize(DungeonExpeditionStyle settings)
    {
        style = settings;
        var camera = Camera.main;
        if (camera)
        {
            dungeonCamera = camera;
            oldBackground = camera.backgroundColor;
            oldClear = camera.clearFlags;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.043f, .036f, .065f);
        }
        foreach (var light in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (light.gameObject.scene == gameObject.scene && light.lightType == Light2D.LightType.Global)
            {
                light.color = new Color(.55f, .64f, .86f);
                light.intensity = style.ambientIntensity;
            }
        lantern = AddLight("Player lantern", transform.position, new Color(1f, .86f, .68f), style.lanternRadius).transform;
        var layout = GetComponent<DungeonLayout>();
        if (layout.TorchWalls)
            foreach (var spawn in layout.GetValidSpawnPoints())
                PlaceOnNearestWall(layout.TorchWalls, spawn.position);
        if (layout.ArenaWalls && layout.ArenaArrival)
            PlaceOnNearestWall(layout.ArenaWalls, layout.ArenaArrival.position);
        CreateMist();
    }

    private Light2D AddLight(string name, Vector3 position, Color color, float radius)
    {
        var light = new GameObject(name).AddComponent<Light2D>();
        light.transform.SetParent(transform, false);
        light.transform.position = position;
        light.lightType = Light2D.LightType.Point;
        light.pointLightInnerRadius = radius * .18f;
        light.pointLightOuterRadius = radius;
        light.color = color;
        light.intensity = 1.45f;
        return light;
    }

    private void PlaceOnNearestWall(Tilemap walls, Vector3 near)
    {
        float best = float.PositiveInfinity;
        Vector3 position = default;
        foreach (var cell in walls.cellBounds.allPositionsWithin)
        {
            if (!walls.HasTile(cell)) continue;
            var center = walls.GetCellCenterWorld(cell);
            float distance = (center - near).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            position = center;
        }
        if (!float.IsPositiveInfinity(best) && !torches.Exists(torch => (torch.transform.position - position).sqrMagnitude < 1))
            PlaceTorch(position);
    }

    public void PlaceTorch(Vector3 position)
    {
        var torch = AddLight("Wall torch", position, new Color(1f, .61f, .26f), 3.2f);
        var renderer = torch.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = style.torch[0];
        renderer.sortingOrder = 4;
        torches.Add(renderer);
    }

    private void CreateMist()
    {
        const int size = 128;
        mistTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Periodic noise avoids visible seams when the cloud band scrolls.
                float u = x / (float)size, v = y / (float)size;
                float Noise(float a, float b) => Mathf.PerlinNoise(23 + a * 5, 10 + b * 12);
                float n = Mathf.Lerp(Mathf.Lerp(Noise(u, v), Noise(u - 1, v), u),
                    Mathf.Lerp(Noise(u, v - 1), Noise(u - 1, v - 1), u), v);
                pixels[y * size + x] = new Color32(166, 182, 210, (byte)(Mathf.Clamp01((n - .34f) * 2) * 62));
            }
        mistTexture.SetPixels32(pixels); mistTexture.Apply();
        mistSprite = Sprite.Create(mistTexture, new Rect(0, 0, size, size), Vector2.one * .5f, 8, 0, SpriteMeshType.FullRect);
        mist = new GameObject("Low mist").AddComponent<SpriteRenderer>();
        mist.transform.SetParent(transform, false);
        mist.sprite = mistSprite;
        mist.drawMode = SpriteDrawMode.Tiled;
        mist.size = new Vector2(80, 64);
        mist.sortingOrder = 50;
    }

    private void LateUpdate()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!player) return;
        lantern.position = player.position;
        if (mist)
        {
            float drift = Time.time * .35f;
            mist.transform.position = new Vector3(Mathf.Floor((player.position.x - drift) / 16) * 16 + drift,
                Mathf.Floor(player.position.y / 16) * 16, 0);
        }
        for (int i = 0; i < torches.Count; i++)
            torches[i].sprite = style.torch[((int)(Time.time * 8) + i) % style.torch.Length];
    }

    private void OnDestroy()
    {
        if (dungeonCamera)
        {
            dungeonCamera.backgroundColor = oldBackground;
            dungeonCamera.clearFlags = oldClear;
        }
        if (mistSprite) Destroy(mistSprite);
        if (mistTexture) Destroy(mistTexture);
    }
}

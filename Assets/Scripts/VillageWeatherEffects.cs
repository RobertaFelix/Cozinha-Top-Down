using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// This component lives under Exterior so interiors, menus and dungeon transitions
// inherit the same visibility as the village geometry.
public sealed class VillageWeatherEffects : MonoBehaviour
{
    private const int FogCount = 12;
    private Material atmosphere;
    private SpriteRenderer sky;
    private Sprite skySprite;
    private readonly List<SpriteRenderer> wisps = new();
    private GameObject puddles;
    private Camera gameCamera;

    public void Initialize(Transform exterior, Vector2Int rainbowCell, Vector2 rainbowRadii)
    {
        BuildSky(exterior);
        var villageGrid = exterior.GetComponentInChildren<Grid>(true);
        Vector3 bow = villageGrid.GetCellCenterWorld(new Vector3Int(rainbowCell.x, rainbowCell.y));
        atmosphere.SetVector("_RainbowArc", new Vector4(bow.x, bow.y,
            Mathf.Max(.1f, rainbowRadii.x), Mathf.Max(.1f, rainbowRadii.y)));
        BuildParticles(wisps, "Mist", FogCount);
        puddles = new GameObject("Wet ground");
        puddles.transform.SetParent(transform, false);
        Sprite puddle = Resources.Load<Sprite>("Weather/Puddle");
        foreach (var map in exterior.GetComponentsInChildren<Tilemap>(true))
        {
            var floor = map.GetComponent<TilemapRenderer>();
            if (!floor || !floor.enabled || map.GetComponent<Collider2D>()) continue;
            foreach (var cell in map.cellBounds.allPositionsWithin)
            {
                // Place a sparse, repeatable scattering on outdoor grass and paths.
                uint hash = VillageWeather.Hash(unchecked((uint)(cell.x * 31337 + cell.y * 101)));
                if (hash % 23 != 0) continue;
                var tile = map.GetTile(cell);
                if (!tile || !(tile.name.StartsWith("chaoGrama_") || tile.name.StartsWith("Estrada_") ||
                               tile.name.StartsWith("grama_"))) continue;
                Vector3 center = map.GetCellCenterWorld(cell);
                if (Physics2D.OverlapPoint(center)) continue;
                var image = CreateSprite(puddles.transform, puddle, floor.sortingOrder + 1);
                image.sortingLayerID = floor.sortingLayerID;
                image.transform.position = center;
                image.flipX = (hash & 2) == 0;
            }
        }
        SetWeather(VillageWeatherKind.Sunny, 0);
    }

    private void BuildSky(Transform exterior)
    {
        // The quad covers map geometry, even when the camera is stationary or zooms.
        Bounds area = new Bounds(exterior.position, Vector3.zero);
        foreach (var map in exterior.GetComponentsInChildren<TilemapRenderer>(true))
            if (map.enabled) area.Encapsulate(map.bounds);
        area.Expand(new Vector3(40, 40, 0));
        var texture = Texture2D.whiteTexture;
        skySprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            Vector2.one * .5f, texture.width, 0, SpriteMeshType.FullRect);
        skySprite.name = "Village sky";
        sky = CreateSprite(transform, skySprite, 80);
        sky.transform.position = new Vector3(area.center.x, area.center.y, 0);
        var localSize = transform.InverseTransformVector(area.size);
        sky.transform.localScale = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), 1);
        atmosphere = new Material(Resources.Load<Shader>("Weather/VillageAtmosphere"));
        sky.sharedMaterial = atmosphere;
    }

    public void SetDaylight(float daylight) => atmosphere.SetFloat("_Daylight", daylight);

    private void OnDestroy()
    {
        if (atmosphere) Destroy(atmosphere);
        if (skySprite) Destroy(skySprite);
    }

    private void BuildParticles(List<SpriteRenderer> pool, string spriteName, int count)
    {
        var sprite = Resources.Load<Sprite>("Weather/" + spriteName);
        for (int i = 0; i < count; i++) pool.Add(CreateSprite(transform, sprite, 40));
    }

    private static SpriteRenderer CreateSprite(Transform root, Sprite sprite, int order)
    {
        var obj = new GameObject(sprite.name);
        obj.transform.SetParent(root, false);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    public void SetWeather(VillageWeatherKind kind, int day)
    {
        atmosphere.SetFloat("_Weather", (int)kind);
        atmosphere.SetFloat("_Rainbow", kind == VillageWeatherKind.AfterRain && VillageWeather.HasRainbow(day) ? 1f : 0f);
        sky.enabled = kind == VillageWeatherKind.Foggy || kind == VillageWeatherKind.Rainy ||
                      kind == VillageWeatherKind.AfterRain;
        foreach (var wisp in wisps) wisp.enabled = kind == VillageWeatherKind.Foggy;
        puddles.SetActive(kind == VillageWeatherKind.AfterRain || kind == VillageWeatherKind.Rainy);
    }

    private void LateUpdate()
    {
        bool fog = wisps.Count > 0 && wisps[0].enabled;
        if (!fog) return;
        if (!gameCamera || !gameCamera.isActiveAndEnabled) gameCamera = Camera.main;
        if (!gameCamera) return;
        float height = gameCamera.orthographicSize * 2 + 3;
        float width = gameCamera.orthographicSize * gameCamera.aspect * 2 + 7;
        Vector3 cameraPosition = gameCamera.transform.position;
        float minX = cameraPosition.x - width * .5f;
        float minY = cameraPosition.y - height * .5f;
        float now = Time.time;
        for (int i = 0; i < wisps.Count; i++)
        {
            uint seed = VillageWeather.Hash((uint)(i + 17));
            float speed = .75f + (seed & 255) / 512f;
            float x = ((seed >> 8) & 4095) * width / 4096f + now * .65f * speed;
            float y = (seed >> 20) * height / 4096f + .13f * Mathf.Sin(now * .38f + i);
            // Distant wisps drift through the thicker veil near the camera edges.
            x = minX + Mathf.Repeat(x - minX, width);
            y = minY + Mathf.Repeat(y - minY, height);
            var position = new Vector3(Mathf.Round(x * 16) / 16, Mathf.Round(y * 16) / 16, 0);
            wisps[i].transform.position = position;
            Vector3 viewport = gameCamera.WorldToViewportPoint(position);
            float radius = new Vector2((viewport.x - .5f) * 1.78f, (viewport.y - .5f) * 2).magnitude;
            wisps[i].color = new Color(1, 1, 1, Mathf.SmoothStep(.07f, 1, Mathf.InverseLerp(.40f, .96f, radius)));
        }
    }
}

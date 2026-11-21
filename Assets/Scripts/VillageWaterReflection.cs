using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class VillageWaterReflection : MonoBehaviour
{
    [Tooltip("Vertical distance from the walking surface to the water, in terrain pixels.")]
    [SerializeField, Range(0, 16)] private int bankHeightPixels = 6;
    private struct Frame
    {
        public Vector4 uvRect;
        public Vector4 uvX;
        public Vector4 uvY;
    }

    private readonly Dictionary<Sprite, Frame> frames = new();
    private Material waterMaterial;
    private Material terrainMaterial;
    private readonly Dictionary<TilemapRenderer, Material> originalMaterials = new();
    private SpriteRenderer player;
    private Sprite lastSprite;
    private static readonly int Pose = Shader.PropertyToID("_ReflectionToPlayer");
    private static readonly int PlayerTexture = Shader.PropertyToID("_PlayerTex");
    private static readonly int UVRect = Shader.PropertyToID("_PlayerRect");
    private static readonly int UVX = Shader.PropertyToID("_PlayerUVX");
    private static readonly int UVY = Shader.PropertyToID("_PlayerUVY");
    private static readonly int Flip = Shader.PropertyToID("_ReflectionFlip");

    public void Initialize(Transform exterior)
    {
        var shader = Resources.Load<Shader>("Weather/ShoreReflection");
        waterMaterial = new Material(shader) { name = "Village shoreline" };
        terrainMaterial = new Material(shader) { name = "Village terrain weather" };
        waterMaterial.SetFloat("_HasWater", 1);
        foreach (var map in exterior.GetComponentsInChildren<Tilemap>(true))
        {
            var renderer = map.GetComponent<TilemapRenderer>();
            if (!renderer || !renderer.enabled || !renderer.sharedMaterial ||
                renderer.sharedMaterial.shader.name != "Universal Render Pipeline/2D/Sprite-Lit-Default") continue;
            originalMaterials.Add(renderer, renderer.sharedMaterial);
            renderer.sharedMaterial = terrainMaterial;
            var tiles = new TileBase[map.GetUsedTilesCount()];
            map.GetUsedTilesNonAlloc(tiles);
            foreach (var tile in tiles)
            {
                if (!tile || !tile.name.StartsWith("chaoElevadoAgua_")) continue;
                renderer.sharedMaterial = waterMaterial;
                break;
            }
        }
    }

    public void SetWeather(VillageWeatherKind kind)
    {
        terrainMaterial.SetFloat("_Weather", (int)kind);
        waterMaterial.SetFloat("_Weather", (int)kind);
    }

    private void LateUpdate()
    {
        if (!waterMaterial) return;
        if (!player)
        {
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller) player = controller.GetComponent<SpriteRenderer>();
        }
        if (!player || !player.sprite || !player.enabled)
        {
            waterMaterial.SetVector(UVRect, Vector4.zero);
            lastSprite = null;
            return;
        }
        Sprite sprite = player.sprite;
        if (sprite != lastSprite)
        {
            if (!frames.TryGetValue(sprite, out Frame frame))
                frames.Add(sprite, frame = GetFrame(sprite));
            waterMaterial.SetTexture(PlayerTexture, sprite.texture);
            waterMaterial.SetVector(UVRect, frame.uvRect);
            waterMaterial.SetVector(UVX, frame.uvX);
            waterMaterial.SetVector(UVY, frame.uvY);
            lastSprite = sprite;
        }
        waterMaterial.SetMatrix(Pose, player.transform.worldToLocalMatrix);
        waterMaterial.SetVector(Flip, new Vector4(player.flipX ? -1 : 1, player.flipY ? -1 : 1,
            1f / sprite.pixelsPerUnit, bankHeightPixels / 16f));
    }

    // Derive the local-position to UV transform from the actual sprite mesh.
    // Different pivots, tight meshes and rotated atlas entries share the same path.
    private static Frame GetFrame(Sprite sprite)
    {
        var positions = sprite.vertices;
        var uvs = sprite.uv;
        var triangles = sprite.triangles;
        if (triangles.Length < 3) return default;
        int a = triangles[0], b = triangles[1], c = triangles[2];
        Vector2 p = positions[b] - positions[a], q = positions[c] - positions[a];
        Vector2 du = uvs[b] - uvs[a], dv = uvs[c] - uvs[a];
        float det = p.x * q.y - p.y * q.x;
        if (Mathf.Approximately(det, 0)) return default;
        Vector2 dx = (du * q.y - dv * p.y) / det;
        Vector2 dy = (dv * p.x - du * q.x) / det;
        Vector2 origin = uvs[a] - dx * positions[a].x - dy * positions[a].y;
        Vector2 min = uvs[0], max = uvs[0];
        foreach (var uv in uvs) { min = Vector2.Min(min, uv); max = Vector2.Max(max, uv); }
        return new Frame
        {
            uvRect = new Vector4(min.x, min.y, max.x, max.y),
            uvX = new Vector4(dx.x, dy.x, origin.x, 0),
            uvY = new Vector4(dx.y, dy.y, origin.y, 0)
        };
    }

    private void OnDestroy()
    {
        foreach (var pair in originalMaterials)
            if (pair.Key) pair.Key.sharedMaterial = pair.Value;
        if (waterMaterial) Destroy(waterMaterial);
        if (terrainMaterial) Destroy(terrainMaterial);
    }
}

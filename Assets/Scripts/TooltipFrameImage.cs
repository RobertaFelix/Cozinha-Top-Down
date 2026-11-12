using UnityEngine;
using UnityEngine.UI;

// Corrects the three inset-colored pixels in each top corner of tooltipV2
// in UI geometry, preserving the original artist-owned texture.
public sealed class TooltipFrameImage : Image
{
    public bool repairTopCorners;

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        base.OnPopulateMesh(mesh);
        if (!repairTopCorners || sprite == null) return;
        float scale = 1f / multipliedPixelsPerUnit;
        var rect = GetPixelAdjustedRect();
        for (int row = 1; row <= 3; row++)
        {
            float inset = (4 - row) * scale;
            float top = rect.yMax - row * scale;
            AddPixel(mesh, rect.xMin + inset, top - scale, scale);
            AddPixel(mesh, rect.xMax - inset - scale, top - scale, scale);
        }
    }

    private void AddPixel(VertexHelper mesh, float x, float y, float size)
    {
        int start = mesh.currentVertCount;
        // A white texel is unnecessary: use the existing highlight texel in the top border.
        var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
        Vector2 sample = new Vector2((uv.x + uv.z) * .5f, uv.w - 1.5f / sprite.texture.height);
        Color tint = color;
        var vertex = UIVertex.simpleVert;
        vertex.color = tint;
        vertex.uv0 = sample;
        foreach (var point in new[] { new Vector2(x, y), new Vector2(x, y + size), new Vector2(x + size, y + size), new Vector2(x + size, y) })
        {
            vertex.position = point;
            mesh.AddVert(vertex);
        }
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start + 2, start + 3, start);
    }
}

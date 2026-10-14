using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

// One continuous feather per panel: no stretched slice boundaries to leave seams.
internal static class CharmMenuBackdrop
{
    private sealed class Surface
    {
        internal int Width, Height, Edge;
        internal Texture2D Texture;
    }
    private static readonly List<Surface> surfaces = new List<Surface>(4);

    private static Texture2D GetSurface(Rect rect, float edge)
    {
        int w = Mathf.Max(1, Mathf.RoundToInt(rect.width));
        int h = Mathf.Max(1, Mathf.RoundToInt(rect.height));
        int e = Mathf.Max(1, Mathf.RoundToInt(edge));
        foreach (var item in surfaces)
            if (item.Width == w && item.Height == h && item.Edge == e && item.Texture) return item.Texture;
        float paddedW = w + e * 2f, paddedH = h + e * 2f;
        int tw = Mathf.Min(1024, Mathf.CeilToInt(paddedW)), th = Mathf.Min(1024, Mathf.CeilToInt(paddedH));
        Color[] pixels = new Color[tw * th];
        for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
            {
                float px = (x + .5f) * paddedW / tw, py = (y + .5f) * paddedH / th;
                float dx = Mathf.Max(0f, Mathf.Max(e - px, px - (e + w)));
                float dy = Mathf.Max(0f, Mathf.Max(e - py, py - (e + h)));
                float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / e);
                float alpha = x == 0 || y == 0 || x == tw - 1 || y == th - 1 ? 0f : t * t * (3f - 2f * t);
                pixels[y * tw + x] = new Color(1f, 1f, 1f, alpha);
            }
        var texture = new Texture2D(tw, th, TextureFormat.RGBA32, false)
        {
            name = "Local8.CharmMenuBackdrop", filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        if (surfaces.Count == 4)
        {
            if (surfaces[0].Texture) Object.Destroy(surfaces[0].Texture);
            surfaces.RemoveAt(0);
        }
        surfaces.Add(new Surface { Width = w, Height = h, Edge = e, Texture = texture });
        return texture;
    }

    internal static void Draw(Rect rect, float height, Color tint)
    {
        if (rect.width <= 0f || rect.height <= 0f) return;
        float edge = 12f * height / 900f;
        Texture2D texture = GetSurface(rect, edge);
        Color previous = GUI.color;
        try
        {
            tint.a = 1f;
            GUI.color = tint;
            // This separate opaque core also covers subpixel sampling at its boundary.
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.DrawTexture(CharmMenuLayout.Feather(rect, height), texture, ScaleMode.StretchToFill, true);
        }
        finally { GUI.color = previous; }
    }

    internal static void Release()
    {
        foreach (var item in surfaces) if (item.Texture) Object.Destroy(item.Texture);
        surfaces.Clear();
    }
}

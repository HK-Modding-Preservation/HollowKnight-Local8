using UnityEngine;

namespace KO.HollowKnight8;

// One tiny neutral surface for every player/color/fill. Use matching full-bar
// coordinates for adjacent spans so partial fills never stretch the corners.
internal static class SoulReserveBar
{
    private static Texture2D surface;

    private static Texture2D Surface()
    {
        if (surface) return surface;
        const int width = 224, height = 12;
        Color[] pixels = new Color[width * height];
        const float radius = 4f;
        for (int y = 0; y < height; y++)
        {
            float v = (y + .5f) / height;
            float shade = v < .65f ? Mathf.Lerp(.84f, 1f, v / .65f)
                : Mathf.Lerp(1f, .95f, (v - .65f) / .35f);
            for (int x = 0; x < width; x++)
            {
                float px = x + .5f, py = y + .5f;
                float cx = Mathf.Clamp(px, radius, width - radius);
                float cy = Mathf.Clamp(py, radius, height - radius);
                float distance = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                float alpha = Mathf.Clamp01((radius - distance) / .8f + .5f);
                pixels[y * width + x] = new Color(shade, shade, shade, alpha);
            }
        }
        surface = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Local8.SoulReserveBar",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        surface.SetPixels(pixels);
        surface.Apply(false, true);
        return surface;
    }

    internal static void Draw(Rect rect, float fraction, float capacity, Color color)
    {
        if (rect.width <= 0f || rect.height <= 0f) return;
        fraction = Mathf.Clamp01(fraction);
        capacity = Mathf.Clamp01(capacity);
        float filled = Mathf.Min(fraction, capacity);
        Texture2D texture = Surface();
        Color previous = GUI.color;
        try
        {
            Color full = Color.Lerp(color, Color.white, .08f);
            full.a = color.a;
            Color empty = Color.Lerp(color, new Color(.12f, .14f, .19f, color.a), .58f);
            Color locked = Color.Lerp(empty, new Color(.08f, .10f, .14f, color.a), .38f);
            // Disjoint spans avoid additive brightness and dark slice seams.
            Span(texture, rect, 0f, filled, full);
            Span(texture, rect, filled, capacity, empty);
            Span(texture, rect, capacity, 1f, locked);
        }
        finally { GUI.color = previous; }
    }

    private static void Span(Texture2D texture, Rect rect, float from, float to, Color tint)
    {
        if (to <= from) return;
        float left = rect.x + rect.width * from;
        float right = rect.x + rect.width * to;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(new Rect(left, rect.y, right - left, rect.height),
            texture, new Rect(from, 0f, to - from, 1f), true);
    }

    internal static void Release()
    {
        if (surface) Object.Destroy(surface);
        surface = null;
    }
}

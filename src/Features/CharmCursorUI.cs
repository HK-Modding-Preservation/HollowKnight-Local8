using UnityEngine;

namespace KO.HollowKnight8;

internal static class CharmCursorUI
{
    // The same four square corners used by the existing native grid cursors.
    internal static void Draw(Rect rect, Color color)
    {
        float size = Mathf.Min(rect.width, rect.height);
        if (size <= 0f) return;
        float thickness = Mathf.Min(2.5f, Mathf.Max(1f, size * .045f));
        float arm = Mathf.Min(11f, size * .28f);
        Color old = GUI.color;
        try
        {
            GUI.color = color;
            for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++)
            {
                float px = x == 0 ? rect.x : rect.xMax - arm;
                float py = y == 0 ? rect.y : rect.yMax - thickness;
                GUI.DrawTexture(new Rect(px, py, arm, thickness), Texture2D.whiteTexture);
                px = x == 0 ? rect.x : rect.xMax - thickness;
                py = y == 0 ? rect.y : rect.yMax - arm;
                GUI.DrawTexture(new Rect(px, py, thickness, arm), Texture2D.whiteTexture);
            }
        }
        finally { GUI.color = old; }
    }
}

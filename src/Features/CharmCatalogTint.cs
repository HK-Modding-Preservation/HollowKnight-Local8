using System;
using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class CharmCatalogTint
{
    private static readonly Dictionary<SpriteRenderer, Color> before = new Dictionary<SpriteRenderer, Color>();
    private static readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
    private static Camera rendering;
    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        Camera.onPreCull += BeforeCamera;
        Camera.onPostRender += AfterCamera;
    }

    internal static void BeforeCamera(Camera camera)
    {
        // A missed post-render callback must not leave altered scene colors.
        Restore();
        var gc = GameCameras.instance;
        var session = Plugin.Self == null ? null : Plugin.Self.Session;
        if (!gc || camera != gc.hudCamera || session == null || session.Primary == null || !Charms.NativeMenuOpen) return;
        rendering = camera;
        try { foreach (var board in Charms.NativeBoards)
        {
            if (!board || !board.charmObject || !session.Primary.Charms.IsEquipped(board.charmNum)
                || !CharmExtensions.Owned(session.Data, board.charmNum)) continue;
            var display = board.charmObject.GetComponentInChildren<CharmDisplay>(true);
            SpriteRenderer main = display && display.spriteRenderer ? display.spriteRenderer : board.charmObject.GetComponent<SpriteRenderer>();
            if (main) { Brighten(main, true); continue; }
            board.charmObject.GetComponentsInChildren<SpriteRenderer>(true, sprites);
            foreach (var sprite in sprites)
                Brighten(sprite, false);
        } }
        catch (Exception ex) { Restore(); Diagnostics.Throttled("CHARM CATALOG TINT", ex); }
    }

    private static void Brighten(SpriteRenderer sprite, bool main)
    {
        if (!sprite || before.ContainsKey(sprite)) return;
        Color color = sprite.color;
        before[sprite] = color;
        // The explicit CharmDisplay link may point to a sibling renderer.
        // Preserve invisible states and alpha of unknown composite layers.
        float alpha = main && color.a > 0f ? 1f : color.a;
        sprite.color = new Color(1f, 1f, 1f, alpha);
    }

    internal static void AfterCamera(Camera camera) { if (camera == rendering) Restore(); }

    internal static void Restore()
    {
        foreach (var entry in before) if (entry.Key) entry.Key.color = entry.Value;
        before.Clear(); sprites.Clear(); rendering = null;
    }

    internal static void Uninstall()
    {
        if (installed) { Camera.onPreCull -= BeforeCamera; Camera.onPostRender -= AfterCamera; }
        installed = false; Restore();
    }
}

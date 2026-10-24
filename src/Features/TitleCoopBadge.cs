using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

// Append to the real DLC group. Never move or replace a vanilla/foreign icon.
internal static class TitleCoopBadge
{
    private static GameObject badge;
    private static SpriteRenderer original, image;
    private static Sprite sprite;
    private static Transform screen, group;
    private static float nextProbe;
    private static ICollection registered;
    private static float registryProbe;
    private static readonly List<Renderer> renderers = new List<Renderer>();
    private static readonly List<Rect> occupied = new List<Rect>();

    internal static void Tick(Local8Runtime runtime)
    {
        if (!TitleLobby.Visible || !runtime.Enabled.Value)
        {
            if (badge) badge.SetActive(false);
            return;
        }
        if (!original || !badge)
        {
            if (Time.unscaledTime < nextProbe) return;
            nextProbe = Time.unscaledTime + .5f;
            var ui = UIManager.instance;
            var source = ui ? ui.transform.Find("UICanvas/MainMenuScreen/TeamCherryLogo/Hidden_Dreams_Logo") : null;
            if (!source) return;
            var native = source.GetComponent<SpriteRenderer>();
            var texture = MenuArt.Texture(MenuArt.Players);
            if (!native || !native.sprite || !texture) return;
            Release();
            original = native;
            screen = ui.mainMenuScreen ? ui.mainMenuScreen.transform : source.parent;
            group = source.parent;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            sprite.name = ReleaseInfo.Label;
            badge = Object.Instantiate(source.gameObject, source.parent);
            badge.SetActive(false);
            badge.name = "Local8 Co-op DLC Icon " + ReleaseInfo.Version;
            image = badge.GetComponent<SpriteRenderer>();
            image.sprite = sprite;
            image.enabled = true;
            // Match the original cell while retaining the existing icon's shape.
            Vector3 size = original.sprite.bounds.size, own = sprite.bounds.size;
            float scale = Mathf.Min(size.x / Mathf.Max(.001f, own.x), size.y / Mathf.Max(.001f, own.y));
            badge.transform.localScale = source.localScale * scale;
            float extent = Mathf.Max(image.bounds.size.x, image.bounds.size.y);
            if (extent > .7f) badge.transform.localScale *= .7f / extent;
            Diagnostics.Write("MENU co-op DLC icon ready version=" + ReleaseInfo.Version + " artwork=icon_players");
        }
        if (!screen || !screen.gameObject.activeInHierarchy) return;
        // UIManager.SetupRefs uses the canvas camera (falling back to main),
        // which can differ from the in-game HUD camera even in the title scene.
        var manager = UIManager.instance;
        var camera = manager && manager.UICanvas ? manager.UICanvas.worldCamera : null;
        if (!camera && GameCameras.instance) camera = GameCameras.instance.mainCamera;
        if (!camera) { badge.SetActive(false); return; }
        float depth = camera.WorldToViewportPoint(original.bounds.center).z;
        Vector3 low = camera.ViewportToWorldPoint(new Vector3(0, 0, depth)), high = camera.ViewportToWorldPoint(new Vector3(1, 1, depth));
        Rect viewport = Rect.MinMaxRect(Mathf.Min(low.x, high.x), Mathf.Min(low.y, high.y), Mathf.Max(low.x, high.x), Mathf.Max(low.y, high.y));
        Vector3 origin = original.transform.position, cell = image.bounds.size;
        Rect anchor = new Rect(origin.x - cell.x * .5f, origin.y - cell.y * .5f, cell.x, cell.y);
        occupied.Clear(); renderers.Clear();
        // Only the real DLC group reserves cells. The parent Team Cherry logo
        // and unrelated menu text/background bounds must not push us far away.
        group.GetComponentsInChildren(includeInactive: true, renderers);
        foreach (var renderer in renderers)
            if (renderer && renderer.transform != group && !renderer.transform.IsChildOf(badge.transform))
            {
                Vector3 delta = renderer.transform.position - origin;
                if (!renderer.gameObject.name.StartsWith("CUSTOM DLC ICON ", System.StringComparison.Ordinal) &&
                    !DlcBadgePlacement.OnCustomGrid(new Vector2(delta.x, delta.y))) continue;
                Rect area = BoundsRect(renderer.bounds);
                if (area.width > 0 && area.height > 0) occupied.Add(area);
            }
        Rect place;
        bool placed = DlcBadgePlacement.Find(anchor, viewport, occupied, out place, RegisteredCount());
        if (placed)
        {
            Vector3 point = origin;
            point.x = place.center.x;
            point.y = place.center.y;
            badge.transform.position = point;
        }
        badge.SetActive(placed);
    }
    private static Rect BoundsRect(Bounds bounds) => Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
    private static int RegisteredCount()
    {
        // Read SFCore's shared reservations without adding a required dependency
        // or changing/removing anyone else's sprites, including Pale Court's.
        try
        {
            if (registered == null && Time.unscaledTime >= registryProbe)
            {
                registryProbe = Time.unscaledTime + .5f;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType("SFCore.DlcIconHelper", false);
                    if (type == null) continue;
                    var field = type.GetField("CustomSprites", BindingFlags.Static | BindingFlags.NonPublic);
                    registered = field?.GetValue(null) as ICollection;
                    break;
                }
            }
            return registered?.Count ?? 0;
        }
        catch (System.Exception error)
        {
            registered = null;
            registryProbe = Time.unscaledTime + 10f;
            Diagnostics.Throttled("MENU DLC icon registry", error);
            return 0; // Existing sibling icons still reserve their visible cells.
        }
    }
    internal static void Release()
    {
        if (badge) Object.Destroy(badge);
        if (sprite) Object.Destroy(sprite);
        badge = null; sprite = null; original = image = null; screen = group = null;
        registered = null; registryProbe = 0f;
        occupied.Clear(); renderers.Clear();
    }
}

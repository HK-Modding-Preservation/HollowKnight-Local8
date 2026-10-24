using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KO.HollowKnight8;

// IMGUI keeps working while the native menu behind the panel ignores input.
internal static class LobbyMenuInput
{
    private static EventSystem owner;
    private static float releaseAt;
    private static readonly Dictionary<BaseInputModule, bool> previous = new Dictionary<BaseInputModule, bool>();
    private static readonly List<BaseInputModule> modules = new List<BaseInputModule>();

    internal static void Sync(bool block)
    {
        EventSystem current = EventSystem.current;
        block = block || owner && current == owner && Time.unscaledTime < releaseAt;
        if (!block || current != owner) Restore();
        if (!block || !current) return;
        owner = current;
        current.GetComponents(modules);
        foreach (var module in modules)
        {
            if (!module) continue;
            if (!previous.ContainsKey(module)) previous[module] = module.enabled;
            module.enabled = false;
        }
    }
    internal static void Close()
    {
        // Let the close click/Escape edge expire before native buttons resume.
        if (owner) releaseAt = Time.unscaledTime + .1f;
    }
    internal static void Restore()
    {
        foreach (var item in previous) if (item.Key) item.Key.enabled = item.Value;
        previous.Clear(); modules.Clear(); owner = null; releaseAt = 0f;
    }
}

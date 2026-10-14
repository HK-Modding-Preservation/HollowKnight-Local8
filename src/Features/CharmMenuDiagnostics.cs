using UnityEngine;

namespace KO.HollowKnight8;

internal static class CharmMenuDiagnostics
{
    private static bool seen, lastOpen;
    private static string lastReason;
    private static float nextLog;

    internal static bool Report(bool open, string reason)
    {
        bool candidate = Charms.NativePane && Charms.NativePane.activeInHierarchy;
        bool relevant = seen || open || candidate;
        bool changed = !seen || open != lastOpen || reason != lastReason;
        if (relevant && changed && Time.unscaledTime >= nextLog)
        {
            var gm = GameManager.instance;
            var inventory = gm ? gm.inventoryFSM : null;
            var events = UnityEngine.EventSystems.EventSystem.current;
            var selected = events == null ? null : events.currentSelectedGameObject;
            Diagnostics.Write("CHARM MENU open=" + open + " reason=" + reason
                + " inventory=" + (inventory ? inventory.ActiveStateName : "missing")
                + " pane=" + (Charms.NativePane ? Charms.NativePane.name : "missing")
                + " anchor=" + Charms.NativeAnchor
                + " selected=" + (selected ? selected.name : "none")
                + " benchInteraction=" + InteractionRouter.ActiveIsBench
				+ " interaction=" + InteractionRouter.ActiveSummary
                + " context=" + (PlayerContext.Current == null ? "none" : "P" + (PlayerContext.Current.Index + 1)));
            seen = true; lastOpen = open; lastReason = reason;
            nextLog = Time.unscaledTime + .25f;
        }
        return open;
    }

    internal static void Reset() { seen = lastOpen = false; lastReason = null; nextLog = 0f; }
}

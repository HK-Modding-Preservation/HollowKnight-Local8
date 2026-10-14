using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

// Native Weaversong uses a scene-wide list and CHARM EQUIP CHECK broadcast.
// Track only actual spawned spiders, separately per actor, including pool reuse.
// Unequipping removes that actor's spiders immediately; another actor's three
// spiders and unrelated familiar projectiles are never touched.
internal static class WeaverlingLifetime
{
    private sealed class Spawn { internal GameObject Object; internal PlayerSlot Owner; internal HeroController Hero; }
    private static readonly List<Spawn> live = new List<Spawn>(24);
    private static CoopSession Session => Plugin.Self ? Plugin.Self.Session : null;

    internal static void Spawned(GameObject obj, PlayerSlot owner)
    {
        var s = Session;
        if (!obj || s == null || !s.Active || obj.name.IndexOf("weaverling", System.StringComparison.OrdinalIgnoreCase) < 0 ||
            obj.GetComponent<HeroController>() || obj.GetComponent<HealthManager>() ||
            !obj.GetComponent<tk2dSpriteAnimator>() || obj.GetComponents<PlayMakerFSM>().Length == 0) return;
        // A pooled spider can still carry its last borrower's OwnerTag. The
        // spawning actor wins; a world enemy context is never a familiar owner.
        if (owner == null) owner = s.Resolve(obj);
        if (owner == null || !owner.Hero || owner.Retiring || PlayerContext.TargetingEnemy) return;
        live.RemoveAll(x => !x.Object || x.Object == obj || !x.Object.activeInHierarchy);
        (obj.GetComponent<OwnerTag>() ?? obj.AddComponent<OwnerTag>()).Player = owner;
        s.RefreshOwnership(obj, owner);
        Hooks.RebindReferences(obj, owner);
        live.Add(new Spawn { Object = obj, Owner = owner, Hero = owner.Hero });
        Trim(owner);
    }

    private static bool Equipped(PlayerSlot p) => p != null && p.Charms != null && p.Charms.Equipped[38];

    internal static void Trim(PlayerSlot owner)
    {
        int keep = Equipped(owner) && owner.Hero && !owner.Down && !owner.Retiring ? 3 : 0;
        // Keep the latest native trio: the summoner FSM holds references to the
        // new objects. Recycling those and retaining the old trio breaks exits.
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var entry = live[i];
            if (!entry.Object || !entry.Object.activeInHierarchy) { live.RemoveAt(i); continue; }
            if (entry.Owner != owner) continue;
            if (entry.Hero == owner.Hero && keep-- > 0) continue;
            live.RemoveAt(i);
            // Do not recycle an object already reassigned by a different spawn.
            var tag = entry.Object.GetComponent<OwnerTag>();
            if (tag && tag.Player == owner) ObjectPool.Recycle(entry.Object);
        }
    }

    internal static void Reset()
    {
        var s = Session;
        foreach (var entry in live)
        {
            if (!entry.Object || !entry.Object.activeInHierarchy) continue;
            var tag = entry.Object.GetComponent<OwnerTag>();
            if (tag && tag.Player == entry.Owner && (s == null || s.Resolve(entry.Object) == entry.Owner))
                ObjectPool.Recycle(entry.Object);
        }
        live.Clear();
    }
}

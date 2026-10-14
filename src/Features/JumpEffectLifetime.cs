using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

// A pooled native JumpEffects can outlive the guest/body cached by GetHero.
// Recycle only effects positively associated with this session; leave foreign
// effects and errors in unrelated native/mod code on their original path.
internal static class JumpEffectLifetime
{
    private struct Lease { internal CoopSession Session; internal PlayerSlot Player; internal HeroController Hero; }
    private static readonly Dictionary<JumpEffects, Lease> owned = new Dictionary<JumpEffects, Lease>();
    private static readonly ReferenceField Hero = ReferenceField.For(Reflect.Field(typeof(JumpEffects), "heroObject"));
    private static readonly ReferenceField Body = ReferenceField.For(Reflect.Field(typeof(JumpEffects), "heroRigidBody"));
    private static bool installed;
    private static float nextLog;

    internal static void Install()
    {
        if (installed || Hero == null || Body == null) return;
        installed = true;
        On.JumpEffects.GetHero += Capture;
        On.JumpEffects.Update += Update;
    }
    internal static void Uninstall()
    {
        if (installed) { On.JumpEffects.GetHero -= Capture; On.JumpEffects.Update -= Update; }
        installed = false; Reset();
    }
    internal static void Reset()
    {
        // Pool roots can survive scene changes. Finish known session effects
        // before dropping their leases, rather than leaving stale native refs.
        foreach (var effect in new List<JumpEffects>(owned.Keys))
            if (effect && effect.gameObject.activeInHierarchy) effect.gameObject.Recycle();
        owned.Clear(); nextLog = 0;
    }
    private static void Capture(On.JumpEffects.orig_GetHero orig, JumpEffects effect)
    {
        orig(effect);
        var s = Plugin.Self == null ? null : Plugin.Self.Session;
        var obj = Hero.GetValue(effect) as GameObject;
        var p = s != null && s.Active && obj ? s.Resolve(obj) : null;
        if (p != null && p.Hero && p.Hero.gameObject == obj)
            owned[effect] = new Lease { Session = s, Player = p, Hero = p.Hero };
        else owned.Remove(effect);
    }
    private static void Update(On.JumpEffects.orig_Update orig, JumpEffects effect)
    {
        Lease lease;
        if (owned.TryGetValue(effect, out lease) &&
            (!lease.Session.Active || Plugin.Self == null || Plugin.Self.Session != lease.Session ||
             !lease.Hero || lease.Player.Hero != lease.Hero ||
             !(Hero.GetValue(effect) as GameObject) || !(Body.GetValue(effect) as Rigidbody2D)))
        {
            owned.Remove(effect);
            effect.gameObject.Recycle();
            if (Time.unscaledTime >= nextLog)
            {
                nextLog = Time.unscaledTime + 1f;
                Diagnostics.Write("JUMP FX orphan recycled P" + (lease.Player.Index + 1));
            }
            return;
        }
        orig(effect);
        if (!effect || !effect.gameObject.activeInHierarchy) owned.Remove(effect);
    }
}

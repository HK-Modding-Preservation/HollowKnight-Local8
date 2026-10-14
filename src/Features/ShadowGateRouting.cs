using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

// Keep the gate solid globally. Only a dashing hero gets a temporary collision
// exemption; another hero cannot walk through because a teammate is dashing.
internal static class ShadowGateRouting
{
    private sealed class Gate
    {
        internal ShadowGateColliderControl Control;
        internal Collider2D Collider;
        internal bool Reenabled;
        internal readonly HashSet<Collider2D> Owned = new HashSet<Collider2D>();
        internal readonly HashSet<Collider2D> Wanted = new HashSet<Collider2D>();
    }
    private static readonly FieldInfo Unlocked = Reflect.Field(typeof(ShadowGateColliderControl), "unlocked");
    private static readonly ReferenceField Collider = ReferenceField.For(Reflect.Field(typeof(ShadowGateColliderControl), "disableCollider"));
    private static readonly Dictionary<ShadowGateColliderControl, Gate> gates = new Dictionary<ShadowGateColliderControl, Gate>();
    private static readonly Dictionary<HeroController, Collider2D[]> bodies = new Dictionary<HeroController, Collider2D[]>();
    private static readonly List<Collider2D> release = new List<Collider2D>();
    private static readonly List<ShadowGateColliderControl> remove = new List<ShadowGateColliderControl>();
    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.ShadowGateColliderControl.FixedUpdate += Fixed;
        On.ShadowGateColliderControl.OnDestroy += Destroy;
    }
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.ShadowGateColliderControl.FixedUpdate -= Fixed;
        On.ShadowGateColliderControl.OnDestroy -= Destroy;
        Reset();
    }

    private static void Release(Gate gate, Collider2D body)
    {
        // Pairs already ignored before us were never added to Owned.
        if (body && gate.Collider && Physics2D.GetIgnoreCollision(gate.Collider, body))
            Physics2D.IgnoreCollision(gate.Collider, body, false);
    }
    private static void Release(Gate gate)
    {
        ReleasePairs(gate);
        if (gate.Reenabled && gate.Collider && gate.Collider.enabled) gate.Collider.enabled = false;
        gate.Reenabled = false;
    }
    private static void ReleasePairs(Gate gate)
    {
        foreach (var body in gate.Owned) Release(gate, body);
        gate.Owned.Clear(); gate.Wanted.Clear();
    }
    internal static void Reset()
    {
        foreach (var gate in gates.Values) Release(gate);
        gates.Clear(); bodies.Clear(); remove.Clear(); release.Clear();
    }

    internal static void Tick()
    {
        remove.Clear();
        foreach (var entry in gates)
        {
            var gate = entry.Value;
            if (!gate.Control || !gate.Collider)
            { Release(gate); remove.Add(entry.Key); }
            else if (!gate.Control.enabled || !gate.Control.gameObject.activeInHierarchy) ReleasePairs(gate);
            else Refresh(gate, (bool)Unlocked.GetValue(gate.Control));
        }
        foreach (var key in remove) gates.Remove(key);
        // Respawns replace hero instances. Retain only the current party roots.
        if (bodies.Count > 8) bodies.Clear();
    }

    private static void Refresh(Gate gate, bool unlocked)
    {
        gate.Wanted.Clear();
        var session = WorldContactRouting.Session;
        if (unlocked && session != null && session.Active && gate.Collider.enabled && gate.Collider.gameObject.activeInHierarchy)
        {
            foreach (var p in session.Players)
            {
                if (!WorldContactRouting.Eligible(p) || !p.Hero.cState.dashing) continue;
                Collider2D[] colliders;
                if (!bodies.TryGetValue(p.Hero, out colliders)) bodies[p.Hero] = colliders = p.Hero.GetComponentsInChildren<Collider2D>(true);
                foreach (var body in colliders)
                {
                    if (!body || !body.enabled || !body.gameObject.activeInHierarchy || body.GetComponentInParent<HeroController>() != p.Hero
                        || !(body.gameObject == p.Hero.gameObject || body.CompareTag("HeroBox")
                            || (body.gameObject.layer == 9 && !body.isTrigger))) continue;
                    gate.Wanted.Add(body);
                    if (!Physics2D.GetIgnoreCollision(gate.Collider, body))
                    {
                        Physics2D.IgnoreCollision(gate.Collider, body, true);
                        gate.Owned.Add(body);
                    }
                }
            }
        }
        release.Clear();
        foreach (var body in gate.Owned) if (!gate.Wanted.Contains(body)) release.Add(body);
        foreach (var body in release) { Release(gate, body); gate.Owned.Remove(body); }
    }

    private static void Fixed(On.ShadowGateColliderControl.orig_FixedUpdate orig, ShadowGateColliderControl self)
    {
        var s = WorldContactRouting.Session;
        if (s == null || !s.Active || Unlocked == null || Collider == null)
        { Reset(); orig(self); return; }
        var collider = Collider.GetValue(self) as Collider2D;
        if (!collider) { orig(self); return; }
        Gate gate;
        if (gates.TryGetValue(self, out gate) && gate.Collider != collider) { Release(gate); gates.Remove(self); gate = null; }
        bool unlocked = (bool)Unlocked.GetValue(self);
        if (gate == null)
        {
            gate = new Gate { Control = self, Collider = collider };
            gates[self] = gate;
            // Coop may start while vanilla has the shared collider open for P1.
            if (unlocked && !collider.enabled && HeroController.instance && HeroController.instance.cState.dashing)
            { collider.enabled = true; gate.Reenabled = true; }
        }
        // Native FixedUpdate only toggles this one shared collider. Mask that
        // branch while still invoking the hook chain, then restore its live flag.
        try { if (unlocked) Unlocked.SetValue(self, false); orig(self); }
        finally
        {
            if (self && unlocked && !(bool)Unlocked.GetValue(self)) Unlocked.SetValue(self, true);
            if (self && collider) Refresh(gate, (bool)Unlocked.GetValue(self));
        }
    }

    private static void Destroy(On.ShadowGateColliderControl.orig_OnDestroy orig, ShadowGateColliderControl self)
    {
        Gate gate;
        if (gates.TryGetValue(self, out gate)) { Release(gate); gates.Remove(self); }
        orig(self);
    }
}

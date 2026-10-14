using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

// Walker's cached hero and its line-of-sight query must refer to the same live
// actor. Keep a one-unit switching margin to avoid turning on near-equal targets.
internal static class WalkerTargeting
{
    private sealed class Binding
    {
        internal Walker Walker;
        internal LineOfSightDetector Detector;
        internal PlayerSlot Target, SightTarget;
        internal HeroController SightHero;
        internal int Frame = -1;
    }
    private static readonly ReferenceField Hero = ReferenceField.For(Reflect.Field(typeof(Walker), "hero"));
    private static readonly ReferenceField Detector = ReferenceField.For(Reflect.Field(typeof(Walker), "lineOfSightDetector"));
    private static readonly Dictionary<Walker, Binding> walkers = new Dictionary<Walker, Binding>();
    private static readonly Dictionary<LineOfSightDetector, Binding> detectors = new Dictionary<LineOfSightDetector, Binding>();
    private static readonly List<Walker> remove = new List<Walker>();
    private static bool installed;
    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.Walker.UpdateWaitingForConditions += Waiting;
        On.Walker.UpdateWalking += Walking;
    }
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.Walker.UpdateWaitingForConditions -= Waiting;
        On.Walker.UpdateWalking -= Walking;
        Reset();
    }
    internal static void Reset() { walkers.Clear(); detectors.Clear(); remove.Clear(); }
    internal static void Tick()
    {
        remove.Clear();
        foreach (var entry in walkers) if (!entry.Key) remove.Add(entry.Key);
        foreach (var key in remove)
        {
            var b = walkers[key];
            if (!ReferenceEquals(b.Detector, null)) detectors.Remove(b.Detector);
            walkers.Remove(key);
        }
    }
    private static Binding Get(Walker self)
    {
        Binding binding;
        if (!walkers.TryGetValue(self, out binding)) walkers[self] = binding = new Binding { Walker = self };
        var detector = Detector == null ? null : Detector.GetValue(self) as LineOfSightDetector;
        if (detector != binding.Detector)
        {
            if (!ReferenceEquals(binding.Detector, null)) detectors.Remove(binding.Detector);
            binding.Detector = detector;
            binding.SightTarget = null;
            if (detector) detectors[detector] = binding;
        }
        return binding;
    }
    private static PlayerSlot Target(Binding binding, bool waiting)
    {
        if (binding.Frame == Time.frameCount && WorldContactRouting.Eligible(binding.Target)) return binding.Target;
        var s = WorldContactRouting.Session;
        var origin = binding.Walker.transform.position;
        bool threshold = waiting && Reflect.Get(binding.Walker, "waitForHeroX", false);
        float x = threshold ? Reflect.Get(binding.Walker, "waitHeroX", origin.x) : origin.x;
        PlayerSlot best = null;
        float distance = float.MaxValue;
        foreach (var p in s.Players)
        {
            if (!WorldContactRouting.Eligible(p)) continue;
            float d = threshold ? Mathf.Abs(p.Hero.transform.position.x - x) : Vector3.Distance(origin, p.Hero.transform.position);
            if (d < distance) { best = p; distance = d; }
        }
        if (!threshold && best != binding.Target && WorldContactRouting.Eligible(binding.Target)
            && Vector3.Distance(origin, binding.Target.Hero.transform.position) <= distance + 1f) best = binding.Target;
        binding.Target = best; binding.Frame = Time.frameCount;
        return best;
    }

    // Called from the existing LOS hook. Return false for unrelated enemy types.
    internal static bool Sight(On.LineOfSightDetector.orig_Update orig, LineOfSightDetector self)
    {
        var s = WorldContactRouting.Session;
        if (s == null || !s.Active) return false;
        Binding binding;
        if (!detectors.TryGetValue(self, out binding))
        {
            var walker = self.GetComponentInParent<Walker>();
            if (!walker) return false;
            binding = Get(walker);
            if (binding.Detector != self) return false;
        }
        var p = Target(binding, false);
        if (p == null) Reflect.Set(self, "canSeeHero", false);
        else using (PlayerContext.Enter(p, enemy: true)) orig(self);
        binding.SightTarget = p;
        binding.SightHero = p == null ? null : p.Hero;
        return true;
    }

    private static void Waiting(On.Walker.orig_UpdateWaitingForConditions orig, Walker self)
    {
        var s = WorldContactRouting.Session;
        if (s == null || !s.Active || Hero == null) { orig(self); return; }
        var binding = Get(self);
        // Each activation condition is native; any eligible hero may satisfy X.
        binding.Frame = -1;
        var p = Target(binding, true);
        var previous = Hero.GetValue(self);
        var hero = p == null ? null : p.Hero;
        try { Hero.SetValue(self, hero); using (PlayerContext.Enter(p, enemy: true)) orig(self); }
        finally { if (self && ReferenceEquals(Hero.GetValue(self), hero)) Hero.SetValue(self, previous); }
    }
    private static void Walking(On.Walker.orig_UpdateWalking orig, Walker self)
    {
        var s = WorldContactRouting.Session;
        if (s == null || !s.Active || Hero == null) { orig(self); return; }
        var binding = Get(self);
        var p = Target(binding, false);
        // A sensor result produced for the previous hero must not authorize a
        // turn towards a new hero behind a wall. Refresh only upon retargeting.
        if (binding.Detector && (binding.SightTarget != p || binding.SightHero != (p == null ? null : p.Hero)))
            Reflect.Call(binding.Detector, "Update");
        var previous = Hero.GetValue(self);
        var hero = p == null ? null : p.Hero;
        try { Hero.SetValue(self, hero); using (PlayerContext.Enter(p, enemy: true)) orig(self); }
        finally { if (self && ReferenceEquals(Hero.GetValue(self), hero)) Hero.SetValue(self, previous); }
    }
}

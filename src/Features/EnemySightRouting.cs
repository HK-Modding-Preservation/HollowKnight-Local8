using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

// Keep native wall/range checks, but do not let an occluded nearest hero hide
// a second visible hero. The AI's hero references use the same sight target.
internal static class EnemySightRouting
{
    private sealed class Sight
    {
        internal PlayerSlot Target;
        internal HeroController Hero;
        internal AlertRange[] Ranges;
        internal int Frame = -1;
        internal bool Visible;
    }
    private static readonly Dictionary<LineOfSightDetector, Sight> sights = new Dictionary<LineOfSightDetector, Sight>();
    private static readonly Dictionary<Fsm, LineOfSightDetector> fsms = new Dictionary<Fsm, LineOfSightDetector>();
    private static readonly Dictionary<AlertRange, List<Sight>> ranges = new Dictionary<AlertRange, List<Sight>>();
    internal static void Reset() { sights.Clear(); fsms.Clear(); ranges.Clear(); }
    internal static void ContactChanged(AlertRange range)
    {
        // Native physics callbacks and FSM reads can share a frame. A real
        // enter/exit invalidates only detectors using this range, not every AI.
        if (!range || !ranges.TryGetValue(range, out var listeners)) return;
        foreach (var sight in listeners) sight.Frame = -1;
    }
    private static bool Eligible(PlayerSlot p) => WorldContactRouting.Eligible(p) && !TransitionVote.Holding(p);

    private static Sight Get(LineOfSightDetector self)
    {
        if (!sights.TryGetValue(self, out var value))
        {
            value = new Sight { Ranges = Reflect.Get<AlertRange[]>(self, "alertRanges") ?? new AlertRange[0] };
            sights[self] = value;
            foreach (var range in value.Ranges)
            {
                if (!range) continue;
                if (!ranges.TryGetValue(range, out var listeners)) ranges[range] = listeners = new List<Sight>();
                if (!listeners.Contains(value)) listeners.Add(value);
            }
        }
        return value;
    }
    private static bool InRange(Sight sight, PlayerSlot p)
    {
        if (sight.Ranges.Length == 0) return true;
        foreach (var range in sight.Ranges) if (PartySensorOccupancy.Contains(range, p)) return true;
        return false;
    }
    internal static PlayerSlot Update(On.LineOfSightDetector.orig_Update orig, LineOfSightDetector self, PlayerSlot preferred = null)
    {
        var s = WorldContactRouting.Session;
        if (s == null || !s.Active) { orig(self); return null; }
        var sight = Get(self);
        if (sight.Frame == Time.frameCount && (sight.Target == null || (Eligible(sight.Target) && sight.Target.Hero == sight.Hero)))
        { Reflect.Set(self, "canSeeHero", sight.Visible); return sight.Target; }
        foreach (var range in sight.Ranges) PartySensorOccupancy.Refresh(range);
        var at = self.transform.position;
        PlayerSlot nearest = null;
        float distance = float.MaxValue;
        foreach (var p in s.Players)
        {
            if (!Eligible(p) || !InRange(sight, p)) continue;
            float d = Vector3.Distance(at, p.Hero.transform.position);
            if (d < distance) { nearest = p; distance = d; }
        }
        var first = Eligible(preferred) ? preferred : sight.Target;
        if (!Eligible(first) || !InRange(sight, first)
            || Vector3.Distance(at, first.Hero.transform.position) > distance + 1f) first = nearest;
        PlayerSlot visible = null;
        if (first != null)
        {
            using (PlayerContext.Enter(first, enemy: true)) orig(self);
            if (self.CanSeeHero) visible = first;
        }
        if (visible == null)
        {
            float best = float.MaxValue;
            foreach (var p in s.Players)
            {
                if (p == first || !Eligible(p) || !InRange(sight, p)) continue;
                float d = Vector3.Distance(at, p.Hero.transform.position);
                if (d >= best) continue;
                using (PlayerContext.Enter(p, enemy: true)) orig(self);
                if (self.CanSeeHero) { visible = p; best = d; }
            }
        }
        sight.Target = visible ?? nearest;
        sight.Hero = sight.Target == null ? null : sight.Target.Hero;
        sight.Visible = visible != null;
        sight.Frame = Time.frameCount;
        Reflect.Set(self, "canSeeHero", sight.Visible);
        return sight.Target;
    }
    internal static PlayerSlot Target(Fsm fsm)
    {
        if (fsm == null || !fsm.GameObject) return null;
        if (!fsms.TryGetValue(fsm, out var detector))
        {
            // This action's native detector is on its owner/ancestor. Do not
            // borrow an unrelated child enemy's sensor or scan the scene.
            detector = fsm.GameObject.GetComponentInParent<LineOfSightDetector>();
            fsms[fsm] = detector;
        }
        if (!detector || !detector.enabled || !detector.gameObject.activeInHierarchy) return null;
        var sight = Get(detector);
        if (sight.Frame != Time.frameCount || !Eligible(sight.Target) || sight.Target.Hero != sight.Hero)
            Reflect.Call(detector, "Update");
        return Eligible(sight.Target) ? sight.Target : null;
    }
}

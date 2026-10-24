using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

// A trigger exit belongs to one collider, not the whole party. Query only the
// known sensor's colliders and up to eight hero bodies; never scan the scene.
internal static class PartySensorOccupancy
{
    private sealed class Sensor
    {
        internal Behaviour Owner;
        internal Collider2D[] Shapes;
        internal readonly HashSet<Collider2D> Contacts = new HashSet<Collider2D>();
        internal bool Occupied, Wrote;
        internal int Frame = -1, Actors;
    }
    private struct Notice { internal EnemyWakeRegion Region; internal bool Occupied; }
    private static readonly Dictionary<Behaviour, Sensor> sensors = new Dictionary<Behaviour, Sensor>();
    private static readonly Dictionary<HeroController, Collider2D[]> bodies = new Dictionary<HeroController, Collider2D[]>();
    private static readonly List<Collider2D> expired = new List<Collider2D>();
    private static readonly List<Behaviour> removed = new List<Behaviour>();
    private static readonly List<Notice> notices = new List<Notice>();
    private static bool installed;
    private static float nextSweep;
    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.AlertRange.OnTriggerEnter2D += AlertEnter;
        On.AlertRange.OnTriggerExit2D += AlertExit;
        On.EnemyWakeRegion.OnTriggerEnter2D += WakeEnter;
        On.EnemyWakeRegion.OnTriggerExit2D += WakeExit;
        On.CheckAlertRange.Apply += Alert;
        On.CheckAlertRangeByName.Apply += NamedAlert;
    }
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.AlertRange.OnTriggerEnter2D -= AlertEnter;
        On.AlertRange.OnTriggerExit2D -= AlertExit;
        On.EnemyWakeRegion.OnTriggerEnter2D -= WakeEnter;
        On.EnemyWakeRegion.OnTriggerExit2D -= WakeExit;
        On.CheckAlertRange.Apply -= Alert;
        On.CheckAlertRangeByName.Apply -= NamedAlert;
        Reset();
    }
    private static void Restore(Sensor state)
    {
        if (state.Owner is AlertRange range && range && state.Wrote && Reflect.Get(range, "isHeroInRange", false) == state.Occupied)
        {
            // On leaving coop, a primary hero still inside must remain visible
            // to vanilla. Restoring the value from before Enter would lose it.
            bool occupied = false;
            if (range.gameObject.activeInHierarchy)
                foreach (var body in state.Contacts)
                {
                    var hero = body ? body.GetComponentInParent<HeroController>() : null;
                    var session = WorldContactRouting.Session;
                    var p = hero && session != null ? session.Resolve(hero) : null;
                    if ((!hero || (p != null && p.Index == 0 && p.Hero == hero && WorldContactRouting.Eligible(p))) && Touches(state, body))
                    { occupied = true; break; }
                }
            Reflect.Set(range, "isHeroInRange", occupied);
        }
    }
    internal static void Reset()
    {
        foreach (var state in sensors.Values) Restore(state);
        sensors.Clear(); bodies.Clear(); expired.Clear(); removed.Clear(); notices.Clear(); nextSweep = 0;
    }
    private static bool Active => WorldContactRouting.Session != null && WorldContactRouting.Session.Active;
    private static Collider2D[] Shapes(Behaviour owner)
    {
        var shapes = owner.GetComponents<Collider2D>();
        var body = owner is EnemyWakeRegion ? owner.GetComponent<Rigidbody2D>() : null;
        if (body)
        {
            // Unity also delivers a child trigger's callback to its attached
            // rigidbody. Include those shapes, excluding independent bodies.
            var attached = new List<Collider2D>();
            foreach (var shape in owner.GetComponentsInChildren<Collider2D>(true))
                if (shape && shape.attachedRigidbody == body) attached.Add(shape);
            return attached.ToArray();
        }
        return shapes.Length == 0 ? owner.GetComponentsInChildren<Collider2D>(true) : shapes;
    }
    private static Sensor Get(Behaviour owner)
    {
        Sensor state;
        if (!sensors.TryGetValue(owner, out state))
        {
            state = new Sensor { Owner = owner, Shapes = Shapes(owner) };
            sensors[owner] = state;
            Seed(state);
        }
        return state;
    }
    private static void Seed(Sensor state)
    {
        state.Actors = 0;
        foreach (var p in WorldContactRouting.Session.Players)
        {
            if (!WorldContactRouting.Eligible(p) || TransitionVote.Holding(p)) continue;
            foreach (var body in BodyShapes(p)) if (Touches(state, body))
            {
                state.Contacts.Add(body);
                state.Actors |= 1 << p.Index;
            }
        }
    }
    private static Collider2D[] BodyShapes(PlayerSlot p)
    {
        if (!bodies.TryGetValue(p.Hero, out var colliders)) bodies[p.Hero] = colliders = ColliderGeometry.HeroBodies(p.Hero);
        return colliders;
    }
    // Scene entry/pooling can enable a range around an already-overlapping
    // hero without a new Enter. Refresh when native AI actually queries it.
    internal static void Refresh(AlertRange range)
    {
        if (!Active || !range) return;
        var state = Get(range);
        if (state.Frame == Time.frameCount) return;
        Seed(state);
        Write(state, Occupied(state), nativeWrote: true);
        state.Frame = Time.frameCount;
    }
    internal static bool Contains(AlertRange range, PlayerSlot p)
    {
        if (!range || !range.gameObject.activeInHierarchy
            || !WorldContactRouting.Eligible(p) || TransitionVote.Holding(p)) return false;
        Refresh(range);
        return (Get(range).Actors & (1 << p.Index)) != 0;
    }
    private static void Alert(On.CheckAlertRange.orig_Apply orig, CheckAlertRange action)
    {
        Refresh(action.alertRange == null ? null : action.alertRange.Value as AlertRange);
        orig(action);
    }
    private static void NamedAlert(On.CheckAlertRangeByName.orig_Apply orig, CheckAlertRangeByName action)
    {
        if (Active) Refresh(AlertRange.Find(action.Owner, action.childName));
        orig(action);
    }
    private static bool Touches(Sensor state, Collider2D body)
    {
        if (!body || !body.enabled || !body.gameObject.activeInHierarchy) return false;
        foreach (var shape in state.Shapes)
            if (shape && shape.enabled && shape.gameObject.activeInHierarchy
                && ColliderGeometry.Overlaps(shape, body)) return true;
        return false;
    }
    private static bool Occupied(Sensor state)
    {
        // Unity delivers trigger callbacks to disabled MonoBehaviours too.
        // The active object and enabled collider define a live sensor.
        if (!state.Owner || !state.Owner.gameObject.activeInHierarchy) return false;
        expired.Clear();
        bool occupied = false;
        foreach (var body in state.Contacts)
        {
            var p = WorldContactRouting.Body(body);
            var hero = body ? body.GetComponentInParent<HeroController>() : null;
            if (!Touches(state, body) || (hero && p == null)) expired.Add(body);
            else if (!hero || (WorldContactRouting.Eligible(p) && !TransitionVote.Holding(p))) occupied = true;
            // Keep an inactive slot's still-overlapping body as a dormant
            // contact: reviving in place may not produce a new Enter callback.
        }
        foreach (var body in expired) state.Contacts.Remove(body);
        // Union geometry retains a body still inside another shape of this range.
        return occupied;
    }
    private static void Write(Sensor state, bool value, bool nativeWrote = false)
    {
        if (state.Wrote && state.Occupied == value && !nativeWrote) return;
        state.Occupied = value;
        if (state.Owner is AlertRange range && range) { Reflect.Set(range, "isHeroInRange", value); state.Wrote = true; }
    }
    private static void Notify(EnemyWakeRegion region, bool occupied)
    {
        var fsm = Reflect.Get<PlayMakerFSM>(region, "fsm");
        if (fsm) fsm.SendEvent(occupied ? Reflect.Get(region, "enterEvent", "WAKE") : Reflect.Get(region, "exitEvent", "SLEEP"));
    }
    internal static void Tick()
    {
        // Contacts update synchronously. The bounded ten-Hz sweep only repairs
        // missing exits (death/disable/retirement), not ordinary enemy decisions.
        if (Time.unscaledTime < nextSweep) return;
        nextSweep = Time.unscaledTime + .1f;
        removed.Clear(); notices.Clear();
        foreach (var entry in sensors)
        {
            var state = entry.Value;
            bool was = state.Occupied, now = Occupied(state);
            Write(state, now);
            if (was != now && state.Owner is EnemyWakeRegion wake && wake && wake.gameObject.activeInHierarchy)
                notices.Add(new Notice { Region = wake, Occupied = now });
            if (!state.Owner || !state.Owner.gameObject.activeInHierarchy) removed.Add(entry.Key);
        }
        foreach (var key in removed) { Restore(sensors[key]); sensors.Remove(key); }
        if (bodies.Count > 8) bodies.Clear();
        // FSM events can synchronously clear scene caches. Send them after the
        // dictionary traversal; Reset may safely clear this pending list too.
        for (int i = 0; i < notices.Count; i++)
        { var notice = notices[i]; if (notice.Region) Notify(notice.Region, notice.Occupied); }
        notices.Clear();
    }

    private static void AlertEnter(On.AlertRange.orig_OnTriggerEnter2D orig, AlertRange self, Collider2D other)
    {
        if (!Active) { orig(self, other); return; }
        var state = Get(self);
        state.Frame = -1;
        EnemySightRouting.ContactChanged(self);
        if (other) state.Contacts.Add(other);
        orig(self, other);
        Write(state, Occupied(state), nativeWrote: true);
    }
    private static void AlertExit(On.AlertRange.orig_OnTriggerExit2D orig, AlertRange self, Collider2D other)
    {
        if (!Active) { orig(self, other); return; }
        var state = Get(self);
        state.Frame = -1;
        EnemySightRouting.ContactChanged(self);
        orig(self, other);
        Write(state, Occupied(state), nativeWrote: true);
    }
    private static void WakeEnter(On.EnemyWakeRegion.orig_OnTriggerEnter2D orig, EnemyWakeRegion self, Collider2D other)
    {
        if (!Active) { orig(self, other); return; }
        var state = Get(self);
        if (other) state.Contacts.Add(other);
        bool now = Occupied(state);
        if (now) orig(self, other);
        Write(state, now);
    }
    private static void WakeExit(On.EnemyWakeRegion.orig_OnTriggerExit2D orig, EnemyWakeRegion self, Collider2D other)
    {
        if (!Active) { orig(self, other); return; }
        var state = Get(self);
        bool now = Occupied(state);
        if (!now && state.Occupied) orig(self, other);
        Write(state, now);
    }
}

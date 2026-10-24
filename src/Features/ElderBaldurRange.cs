using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace KO.HollowKnight8;

/// <summary>
/// An Elder Baldur's proximity queries concern the whole living party, rather
/// than whichever hero the outer FSM/last contact happened to select.
/// Native thresholds, state transitions, armour and attack actions remain native.
/// </summary>
internal static class ElderBaldurRange
{
    private static bool installed;
    private static readonly Dictionary<Fsm, HealthManager> blockers = new Dictionary<Fsm, HealthManager>();
    private static readonly Dictionary<GameObject, Collider2D[]> sensors = new Dictionary<GameObject, Collider2D[]>();
    private static readonly Dictionary<HeroController, Collider2D[]> bodies = new Dictionary<HeroController, Collider2D[]>();
    private static CoopSession Session => Plugin.Self == null ? null : Plugin.Self.Session;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.HutongGames.PlayMaker.Actions.GetDistance.DoGetDistance += Distance;
        On.HutongGames.PlayMaker.Actions.GetXDistance.DoGetDistance += XDistance;
        On.HutongGames.PlayMaker.Actions.FloatCompare.DoCompare += Compare;
        On.HutongGames.PlayMaker.Actions.BoolTest.OnEnter += BoolEnter;
        On.HutongGames.PlayMaker.Actions.BoolTest.OnUpdate += BoolUpdate;
        On.CheckAlertRange.Apply += Alert;
        On.CheckAlertRangeByName.Apply += NamedAlert;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.HutongGames.PlayMaker.Actions.GetDistance.DoGetDistance -= Distance;
        On.HutongGames.PlayMaker.Actions.GetXDistance.DoGetDistance -= XDistance;
        On.HutongGames.PlayMaker.Actions.FloatCompare.DoCompare -= Compare;
        On.HutongGames.PlayMaker.Actions.BoolTest.OnEnter -= BoolEnter;
        On.HutongGames.PlayMaker.Actions.BoolTest.OnUpdate -= BoolUpdate;
        On.CheckAlertRange.Apply -= Alert;
        On.CheckAlertRangeByName.Apply -= NamedAlert;
        Reset();
    }

    internal static void Reset() { blockers.Clear(); sensors.Clear(); bodies.Clear(); ColliderGeometry.Reset(); }

    private static bool Blocker(Fsm fsm)
    {
        var session = Session;
        if (session == null || !session.Active || fsm == null || fsm.Name != "Blocker Control" || !fsm.GameObject) return false;
        HealthManager health;
        if (!blockers.TryGetValue(fsm, out health))
        {
            health = fsm.GameObject.GetComponent<HealthManager>();
            blockers[fsm] = health;
            if (health) Diagnostics.Write("BALDUR party proximity enabled object=" + fsm.GameObject.name);
        }
        return health && !health.GetIsDead();
    }

    private static bool Eligible(PlayerSlot player)
    {
        return player != null && player.Ready && player.Alive && (player.Index == 0 || player.Connected)
            && player.Vitals.Health > 0 && !player.ArenaTransfer && !EmergencyWarp.Active(player) && !TransitionVote.Holding(player);
    }

    private static bool Minimum(Fsm fsm, FsmOwnerDefault source, FsmGameObject target, FsmFloat result, bool horizontal)
    {
        if (!Blocker(fsm) || source == null || target == null || result == null) return false;
        var session = Session;
        var from = fsm.GetOwnerDefaultTarget(source);
        var to = target.Value;
        if (!from || !to) return false;
        // Only replace a real hero-distance query. Other distances in this same
        // enemy FSM (shots/rollers/etc.) keep their original meaning.
        bool fromHero = IsHero(from, session), toHero = IsHero(to, session);
        if (fromHero == toHero) return false;
        var anchor = (fromHero ? to : from).transform.position;
        float nearest = float.MaxValue;
        foreach (var player in session.Players)
        {
            if (!Eligible(player)) continue;
            var position = player.Hero.transform.position;
            // Use the native action's metric, including X-only checks. A jumping
            // hero being nearer in Euclidean distance must not hide a close hero.
            float distance = horizontal ? Mathf.Abs(anchor.x - position.x) : Vector3.Distance(anchor, position);
            if (distance < nearest) nearest = distance;
        }
        result.Value = nearest;
        return true;
    }

    private static bool IsHero(GameObject value, CoopSession session)
    {
        // OwnerTag also identifies spells and summons; they must not be treated
        // as hero bodies when this FSM measures its own projectile distances.
        var hero = value.GetComponent<HeroController>();
        var player = hero ? session.Resolve(hero) : null;
        return player != null && player.Hero == hero;
    }

    private static void Distance(On.HutongGames.PlayMaker.Actions.GetDistance.orig_DoGetDistance orig, GetDistance action)
    {
        if (!Minimum(action.Fsm, action.gameObject, action.target, action.storeResult, false)) orig(action);
    }

    private static void XDistance(On.HutongGames.PlayMaker.Actions.GetXDistance.orig_DoGetDistance orig, GetXDistance action)
    {
        if (!Minimum(action.Fsm, action.gameObject, action.target, action.storeResult, true)) orig(action);
    }

    private static bool SameValue(FsmFloat left, FsmFloat right)
    {
        return left != null && right != null && (ReferenceEquals(left, right)
            || (left.UseVariable && right.UseVariable && !string.IsNullOrEmpty(left.Name) && left.Name == right.Name));
    }

    private static bool Refresh(FloatCompare comparison)
    {
        if (!Blocker(comparison.Fsm) || comparison.Fsm.ActiveState == null || comparison.Fsm.ActiveState.Actions == null) return false;
        bool refreshed = false;
        foreach (var action in comparison.Fsm.ActiveState.Actions)
        {
            if (action == null || !action.Enabled) continue;
            var distance = action as GetDistance;
            if (distance != null && (SameValue(distance.storeResult, comparison.float1) || SameValue(distance.storeResult, comparison.float2)))
                refreshed |= Minimum(distance.Fsm, distance.gameObject, distance.target, distance.storeResult, false);
            var xDistance = action as GetXDistance;
            if (xDistance != null && (SameValue(xDistance.storeResult, comparison.float1) || SameValue(xDistance.storeResult, comparison.float2)))
                refreshed |= Minimum(xDistance.Fsm, xDistance.gameObject, xDistance.target, xDistance.storeResult, true);
        }
        return refreshed;
    }

    private static void Compare(On.HutongGames.PlayMaker.Actions.FloatCompare.orig_DoCompare orig, FloatCompare action)
    {
        // Refresh immediately before the native comparison as well as in the
        // distance action. A one-shot query/outer context cannot leave a stale
        // far-player value in the opening decision.
        Refresh(action);
        orig(action);
    }

    private static bool RefreshBool(BoolTest action)
    {
        if (!Blocker(action.Fsm) || action.boolVariable == null || !action.boolVariable.UseVariable) return false;
        string child;
        switch (action.boolVariable.Name)
        {
            case "In Alert Range": child = "Alert Range New"; break;
            case "In Attack Range": child = "Attack Range"; break;
            case "Unalert Range": child = "Unalert Range"; break;
            default: return false;
        }
        // The separate hero_detect_bool FSM writes Unalert Range on each
        // contact, including another player's Exit. Refresh at the decision,
        // retaining the original sensor geometry and native bool/event logic.
        var sensor = action.Owner.transform.Find(child);
        bool occupied;
        if (!sensor || !Occupied(sensor.gameObject, out occupied)) return false;
        action.boolVariable.Value = occupied;
        return true;
    }

    private static void BoolEnter(On.HutongGames.PlayMaker.Actions.BoolTest.orig_OnEnter orig, BoolTest action)
    {
        RefreshBool(action);
        orig(action);
    }

    private static void BoolUpdate(On.HutongGames.PlayMaker.Actions.BoolTest.orig_OnUpdate orig, BoolTest action)
    {
        RefreshBool(action);
        orig(action);
    }

    internal static bool BlockFar(Fsm fsm, FsmEvent evt)
    {
        if (evt == null || evt.Name != "FAR" || !Blocker(fsm) || fsm.ActiveState == null || fsm.ActiveState.Name != "Closed" || fsm.ActiveState.Actions == null) return false;
        foreach (var action in fsm.ActiveState.Actions)
        {
            var boolean = action as BoolTest;
            if (boolean != null && boolean.Enabled && (IsFar(boolean.isTrue) || IsFar(boolean.isFalse)) && RefreshBool(boolean))
                return !IsFar(boolean.boolVariable.Value ? boolean.isTrue : boolean.isFalse);
            var comparison = action as FloatCompare;
            if (comparison == null || !comparison.Enabled || !Refresh(comparison)) continue;
            if (!IsFar(comparison.equal) && !IsFar(comparison.lessThan) && !IsFar(comparison.greaterThan)) continue;
            float a = comparison.float1.Value, b = comparison.float2.Value;
            var selected = Mathf.Abs(a - b) <= comparison.tolerance.Value ? comparison.equal
                : a < b ? comparison.lessThan : a > b ? comparison.greaterThan : null;
            // A contact from another hero may arrive between native comparisons.
            // Reject only this enemy's opening event if its own current query
            // says someone is still close. Do not intercept damage/death/attacks.
            return !IsFar(selected);
        }
        return false;
    }

    private static bool IsFar(FsmEvent evt) { return evt != null && evt.Name == "FAR"; }

    private static bool Occupied(GameObject range, out bool occupied)
    {
        occupied = false;
        if (!range) return false;
        Collider2D[] colliders;
        if (!sensors.TryGetValue(range, out colliders)) sensors[range] = colliders = range.GetComponents<Collider2D>();
        if (colliders.Length == 0) return false;
        foreach (var player in Session.Players)
        {
            if (!Eligible(player)) continue;
            Collider2D[] body;
            // Native HeroBox also participates in detection. Owned attacks and
            // summons are not body colliders, even underneath the hero root.
            if (!bodies.TryGetValue(player.Hero, out body)) bodies[player.Hero] = body = ColliderGeometry.HeroBodies(player.Hero);
            foreach (var sensor in colliders)
            {
                if (!sensor || !sensor.enabled || !sensor.gameObject.activeInHierarchy) continue;
                foreach (var collider in body)
                {
                    if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                    if (ColliderGeometry.Overlaps(sensor, collider)) { occupied = true; return true; }
                }
            }
        }
        return true;
    }

    private static void Alert(On.CheckAlertRange.orig_Apply orig, CheckAlertRange action)
    {
        bool occupied;
        var range = action.alertRange == null ? null : action.alertRange.Value as AlertRange;
        if (Blocker(action.Fsm) && action.storeResult != null && action.alertRange != null
            && range && Occupied(range.gameObject, out occupied)) action.storeResult.Value = occupied;
        else orig(action);
    }

    private static void NamedAlert(On.CheckAlertRangeByName.orig_Apply orig, CheckAlertRangeByName action)
    {
        bool occupied;
        var range = Blocker(action.Fsm) ? AlertRange.Find(action.Owner, action.childName) : null;
        if (Blocker(action.Fsm) && action.storeResult != null
            && range && Occupied(range.gameObject, out occupied)) action.storeResult.Value = occupied;
        else orig(action);
    }
}

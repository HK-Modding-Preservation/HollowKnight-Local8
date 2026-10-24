using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace KO.HollowKnight8;

// The first Greenpath conversation is an automatic encounter, not an Up
// prompt. Keep its native control handoff on the actor that started it.
// The corpse's later position choice must finish before HORNET LEAVE can
// activate the cloak and release the native gates.
internal static class HornetEncounterFlow
{
    private static Fsm intro, corpse;
    private static PlayerSlot owner;
    private static HeroController hero;
    private static FsmState choice;
    private static float choiceAt;

    private static bool Room(CoopSession s) => s != null && s.Active && GameManager.instance
        && GameManager.instance.sceneName == "Fungus1_04";

    private static bool Named(Fsm f, string name)
    {
        if (f == null || !f.GameObject) return false;
        string value = f.GameObject.name;
        if (value != null && value.EndsWith("(Clone)", System.StringComparison.Ordinal))
            value = value.Substring(0, value.Length - 7).TrimEnd();
        return value == name;
    }

    private static bool ValidOwner() => owner != null && owner.Hero == hero && hero && owner.Ready
        && owner.Alive && !owner.Retiring && (owner.Index == 0 || owner.Connected)
        && intro != null && intro.GameObject && intro.GameObject.activeInHierarchy;

    internal static PlayerSlot Resolve(Fsm f)
    {
        if (!Room(Plugin.Self ? Plugin.Self.Session : null) || !ValidOwner()) return null;
        return f == intro ? owner : null;
    }

    internal static bool Holds(PlayerSlot p)
    {
        if (p != owner || Resolve(intro) == null) return false;
        string state = intro.ActiveStateName;
        return state == "Point" || state == "Box Up" || state == "Dialogue"
            || state == "Box Down" || state == "Set Hero Active";
    }

    internal static void ObserveState(FsmState state, PlayerSlot routed)
    {
        var s = Plugin.Self ? Plugin.Self.Session : null;
        if (!Room(s) || state?.Fsm == null) return;
        var f = state.Fsm;
        if (f.Name == "Encounter" && Named(f, "Hornet Infected Knight Encounter")
            && f.GetState("Point") != null && f.GetState("Set Hero Active") != null && f.GetState("Start Fight") != null)
        {
            if (state.Name == "Point" && routed != null && routed.Hero && routed.Ready && routed.Alive
                && (intro != f || !ValidOwner()))
            {
                intro = f; owner = routed; hero = routed.Hero;
                Diagnostics.Write("HORNET dialogue owner=P" + (owner.Index + 1));
                ArenaGather.Encounter(f, owner);
            }
            if (state.Name == "Leap Antic" || state.Name == "Attacked")
                ArenaGather.Encounter(f, ValidOwner() ? owner : routed);
            if (state.Name == "Start Fight" || state.Name == "Destroy")
            { intro = null; owner = null; hero = null; }
        }
        if (f.Name != "Control" || !Named(f, "Corpse Hornet 1")) return;
        Diagnostics.Write("HORNET corpse state=" + state.Name);
        if (state.Name == "Check Pos")
        { corpse = f; choice = state; choiceAt = Time.time; }
        else if (f == corpse) { corpse = null; choice = null; }
    }

    private static bool Branch(FsmState state, string name)
    {
        if (state?.Transitions == null) return false;
        foreach (var transition in state.Transitions)
            if (transition != null && transition.EventName == name && transition.ToState == name
                && state.Fsm.GetState(name) != null) return true;
        return false;
    }

    internal static void Tick(CoopSession s)
    {
        if (!Room(s)) { Reset(); return; }
        if (intro != null && !ValidOwner()) { intro = null; owner = null; hero = null; }
        var gm = GameManager.instance;
        if (corpse == null || !corpse.GameObject || !corpse.GameObject.activeInHierarchy
            || corpse.ActiveState != choice)
        { corpse = null; choice = null; return; }
        if (!s.Gameplay || gm.isPaused || gm.IsLoadingSceneTransition || s.TeamWipe
            || Time.time - choiceAt < .5f || s.Data == null || !s.Data.hornet1Defeated) return;
        // This state contains no native wait or animation. If the normal
        // FloatCompare already chose a side, the identity check above drops
        // the watch. Recover only the verified native L/R decision, without
        // awarding an ability, writing progression, or opening gates here.
        if (choice.Actions == null || !Branch(choice, "L") || !Branch(choice, "R")) return;
        int positions = 0, comparisons = 0;
        foreach (var action in choice.Actions)
        {
            if (action == null || !action.Enabled) continue;
            if (action is GetPosition) positions++;
            else if (action is FloatCompare) comparisons++;
            else return; // Another mod added a wait/animation/other decision.
        }
        if (positions != 1 || comparisons != 1) return;
        foreach (var action in choice.Actions)
        {
            var compare = action as FloatCompare;
            if (compare == null || !compare.Enabled || compare.float1 == null || compare.float2 == null
                || compare.float1.Name != "X Pos" || compare.float2.UseVariable
                || Mathf.Abs(compare.float2.Value - 27.3f) > .01f) continue;
            float x = corpse.GameObject.transform.position.x;
            if (float.IsNaN(x) || float.IsInfinity(x)) return;
            string side = x <= compare.float2.Value ? "L" : "R";
            var f = corpse;
            corpse = null; choice = null;
            Diagnostics.Write("HORNET resumed stalled corpse position choice side=" + side + " x=" + x);
            f.Event(FsmEvent.GetFsmEvent(side));
            return;
        }
    }

    internal static void Reset()
    { intro = corpse = null; owner = null; hero = null; choice = null; choiceAt = 0; }
}

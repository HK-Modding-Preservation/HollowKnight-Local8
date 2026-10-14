using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Text;

namespace KO.HollowKnight8;

// A shared roar is an animation/control lock, not damage recoil. Guest FSMs
// can keep applying its -5 horizontal push while their native movement is
// blocked. Bound that push to the lock and finish cleanup AFTER its actions.
internal static class RoarMotionRecovery
{
    private static bool installed;
    private struct Entry
    {
        internal PlayerSlot Player;
        internal CoopSession Session;
        internal HeroController Hero;
        internal Fsm Source;
        internal Rigidbody2D Body;
        internal float Gravity;
        internal bool Released, PushLogged;
        internal int Corrections;
    }
    private static readonly Entry[] entries = new Entry[8];
    private static readonly int[] scopes = new int[8];
    private static int active;
    private static bool described;
    internal static bool Active => active != 0;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.HeroController.RelinquishControl += Relinquish;
        On.HeroController.RegainControl += Regain;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.HeroController.RelinquishControl -= Relinquish;
        On.HeroController.RegainControl -= Regain;
        Reset();
    }

    private static PlayerSlot Owner(HeroController hero)
    {
        var f = WorldRouting.Current;
        var s = Plugin.Self ? Plugin.Self.Session : null;
        if (!hero || s == null || !s.Active || f == null || f.Name != "Roar Lock") return null;
        var p = s.Resolve(f);
        // Require the actual hero's root FSM, not a same-named boss/mod FSM
        // that happens to route to the nearest player.
        return p != null && p.Hero == hero && f.GameObject == hero.gameObject &&
            p.Index >= 0 && p.Index < 8 ? p : null;
    }

    private static void Relinquish(On.HeroController.orig_RelinquishControl orig, HeroController hero)
    {
        var p = Owner(hero);
        bool wasHeld = hero.controlReqlinquished;
        float saved = 0;
        if (p != null)
        {
            var body = hero.GetComponent<Rigidbody2D>();
            saved = body ? body.gravityScale : 0;
            if (saved <= .01f) saved = Reflect.Get(hero, "prevGravityScale", 0f);
        }
        orig(hero);
        if (p == null || !installed) return;
        // Repeated ROAR ENTER can re-run the native lock while its gravity is
        // already zero. Do not replace the original positive backup with it.
        if (wasHeld && (active & (1 << p.Index)) != 0 && entries[p.Index].Hero == hero &&
            entries[p.Index].Player == p && entries[p.Index].Source == WorldRouting.Current) return;
        entries[p.Index] = new Entry
        {
            Player = p, Session = Plugin.Self.Session,
            Hero = hero, Source = WorldRouting.Current, Body = hero.GetComponent<Rigidbody2D>(),
            Gravity = saved > .01f ? saved : hero.DEFAULT_GRAVITY
        };
        active |= 1 << p.Index;
        Describe(WorldRouting.Current);
        Log(p, "enter");
    }

    private static void Regain(On.HeroController.orig_RegainControl orig, HeroController hero)
    {
        var p = Owner(hero);
        orig(hero);
        if (p == null || !installed) return;
        // RegainControl can precede SetVelocity/SetGravity actions in the
        // same state. Do not discard our backup before those actions finish.
        ref Entry e = ref entries[p.Index];
        if ((active & (1 << p.Index)) == 0 || e.Hero != hero || e.Player != p)
        {
            e = new Entry { Player = p, Session = Plugin.Self.Session, Hero = hero, Source = WorldRouting.Current,
                Body = hero.GetComponent<Rigidbody2D>(), Gravity = hero.DEFAULT_GRAVITY };
            active |= 1 << p.Index;
        }
        e.Released = true;
        Log(p, "native-exit");
    }

    private static bool OwnsBody(PlayerSlot p, ref Entry e)
    {
        var hero = p.Hero;
        if (e.Player != p || !hero || e.Hero != hero || e.Source == null ||
            e.Source.GameObject != hero.gameObject || !p.Ready || !p.Alive || p.Down || p.Hazard || p.Retiring ||
            p.SpawnPending || p.ArenaTransfer || p.Reviving || Time.unscaledTime < p.WakeUntil) return false;
        var gm = GameManager.instance;
        var c = hero.cState;
        return !c.dead && !c.transitioning && !c.recoilFrozen && !c.recoiling &&
            !c.dashing && !c.shadowDashing && !c.superDashing && !c.casting && !c.freezeCharge &&
            !c.bouncing && !c.doubleJumping && !c.swimming && !hero.inAcid &&
            !p.AcidAssistActive && gm && !gm.IsLoadingSceneTransition &&
            !EmergencyWarp.Active(p) && !ScriptedParty.Holds(p) && !CoopEnding.HoldsActor(p) &&
            !ChallengeSequence.Holds(p) && !BenchSeats.Seated(p) && !TransitionVote.Holding(p) &&
            !RoleSystem.BlockHero(hero);
    }

    private static void Finish(PlayerSlot p, ref Entry e)
    {
        var hero = p.Hero;
        var body = e.Body;
        if (body.gravityScale <= .01f && e.Gravity > .01f)
        {
            body.gravityScale = e.Gravity;
            Reflect.Set(hero, "prevGravityScale", e.Gravity);
            Diagnostics.Write("ROAR restored gravity P" + (p.Index + 1));
        }
        // Preserve vertical motion and all damage/dash/spell owners. Only
        // guests have the extra push observed in the shared intro.
        if (p.Index > 0)
        {
            var velocity = body.velocity;
            if (velocity.x != 0) { body.velocity = new Vector2(0, velocity.y); e.Corrections++; }
        }
        Diagnostics.Write("ROAR complete P" + (p.Index + 1) + " drift_corrections=" + e.Corrections);
        Log(p, "complete");
        Clear(p.Index);
    }

    private static void Apply(PlayerSlot p)
    {
        if (p == null || p.Index < 0 || p.Index >= 8 || (active & (1 << p.Index)) == 0) return;
        var s = Plugin.Self ? Plugin.Self.Session : null;
        if (s == null || !s.Active) { Clear(p.Index); return; }
        ref Entry e = ref entries[p.Index];
        if (e.Session != s || !OwnsBody(p, ref e)) { Cancel(p, "motion-owner-changed"); return; }
        if (!e.Body) e.Body = p.Hero.GetComponent<Rigidbody2D>();
        if (!e.Body || e.Body.gameObject != p.Hero.gameObject || !e.Body.simulated || e.Body.isKinematic)
        { Cancel(p, "body-disabled-or-replaced"); return; }
        if (!p.Hero.controlReqlinquished)
        {
            if (e.Released && p.Hero.acceptingInput) Finish(p, ref e);
            else Cancel(p, "control-moved-without-roar-release");
            return;
        }
        if (e.Released || p.Index == 0) return;
        var velocity = e.Body.velocity;
        if (velocity.x == 0) return;
        if (!e.PushLogged)
        {
            e.PushLogged = true;
            Diagnostics.Write("ROAR guest drift P" + (p.Index + 1) + " state=" + e.Source.ActiveStateName +
                " native_velocity=" + velocity + " input_blocked=" + p.InputBlocked);
        }
        e.Body.velocity = new Vector2(0, velocity.y);
        e.Corrections++;
    }

    internal static int BeforeFsm(Fsm fsm, PlayerSlot player)
    {
        if (!installed || fsm == null || fsm.Name != "Roar Lock" || player == null || player.Index < 0 || player.Index >= 8 || !player.Hero ||
            fsm.GameObject != player.Hero.gameObject) return 0;
        scopes[player.Index]++;
        return player.Index + 1;
    }

    internal static void AfterFsm(Fsm fsm, PlayerSlot player, int scope = 0)
    {
        if (scope > 0 && scope <= 8 && scopes[scope - 1] > 0) scopes[scope - 1]--;
        if (active == 0 || player == null || player.Index < 0 || player.Index >= 8) return;
        if ((active & (1 << player.Index)) == 0 || entries[player.Index].Source != fsm || scopes[player.Index] != 0) return;
        // Runs after OnEnter/ProcessEvent/FixedUpdate, before Unity's physics
        // step. A LateUpdate-only fix lets every fixed step drift first.
        Apply(player);
    }

    internal static void LateTick(CoopSession s)
    {
        if (active == 0) return;
        if (s == null || !s.Active) { Reset(); return; }
        // Input-blocked guests can have their FSM frame suspended. Clear the
        // already-issued push too, without allowing their input or FSMs.
        int present = 0;
        foreach (var p in s.Players)
        {
            if (p == null || p.Index < 0 || p.Index >= 8) continue;
            present |= 1 << p.Index;
            if ((active & (1 << p.Index)) != 0 && scopes[p.Index] == 0) Apply(p);
        }
        // A slot can leave during the lock. Do not retain its hero or keep
        // the extra callback check alive for the rest of the session.
        for (int i = 0; i < entries.Length; i++)
            if ((active & ~present & (1 << i)) != 0) Clear(i);
    }

    private static void Describe(Fsm fsm)
    {
        if (described || fsm == null || fsm.States == null) return;
        described = true;
        int count = 0;
        foreach (var state in fsm.States)
        {
            if (count >= 24) break;
            if (state == null || state.Actions == null) continue;
            count++;
            var text = new StringBuilder("ROAR FSM state=").Append(state.Name).Append(" actions=");
            foreach (var action in state.Actions)
            {
                if (action == null) continue;
                text.Append(action.GetType().Name);
                var velocity = action as SetVelocity2d;
                if (velocity != null)
                    text.Append("[x=").Append(velocity.x == null || velocity.x.IsNone ? "keep" : velocity.x.Value.ToString())
                        .Append(",y=").Append(velocity.y == null || velocity.y.IsNone ? "keep" : velocity.y.Value.ToString())
                        .Append(",fixed=").Append(velocity.everyFrame).Append(']');
                var gravity = action as SetGravity2dScale;
                if (gravity != null && gravity.gravityScale != null) text.Append("[gravity=").Append(gravity.gravityScale.Value).Append(']');
                text.Append(';');
                if (text.Length > 1600) break;
            }
            Diagnostics.Write(text.ToString());
        }
    }

    private static void Clear(int index)
    {
        entries[index] = default(Entry); active &= ~(1 << index);
    }

    private static void Cancel(PlayerSlot p, string reason)
    {
        Diagnostics.Write("ROAR cancelled P" + (p.Index + 1) + " reason=" + reason);
        if (p.Hero) Log(p, "superseded");
        Clear(p.Index);
    }

    private static void Log(PlayerSlot p, string phase)
    {
        var h = p.Hero;
        var body = h.GetComponent<Rigidbody2D>();
        var f = entries[p.Index].Source;
        Diagnostics.Write("ROAR " + phase + " P" + (p.Index + 1) + " state=" + h.hero_state + " fsm_state=" + (f == null ? "none" : f.ActiveStateName) +
            " control=" + h.controlReqlinquished + " accepting_input=" + h.acceptingInput + " recoil=" + h.cState.recoiling +
            " frozen=" + h.cState.recoilFrozen + " gravity=" + (body ? body.gravityScale : -1) +
            " velocity=" + (body ? body.velocity.ToString() : "none") + " position=" + h.transform.position +
            " input_blocked=" + p.InputBlocked + " focused=" + Application.isFocused +
            " ready=" + p.Ready + " down=" + p.Down + " hazard=" + p.Hazard + " spawn_pending=" + p.SpawnPending +
            " reviving=" + p.Reviving + " arena_transfer=" + p.ArenaTransfer + " wake_pending=" + (Time.unscaledTime < p.WakeUntil) +
            " dash=" + h.cState.dashing + " shadow_dash=" + h.cState.shadowDashing + " superdash=" + h.cState.superDashing +
            " casting=" + h.cState.casting + " charge=" + h.cState.freezeCharge + " bounce=" + h.cState.bouncing +
            " double_jump=" + h.cState.doubleJumping + " transition=" + h.cState.transitioning);
    }

    internal static void Reset()
    {
        for (int i = 0; i < entries.Length; i++) { entries[i] = default(Entry); scopes[i] = 0; }
        active = 0; described = false;
    }
}

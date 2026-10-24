using System;
using UnityEngine;

namespace KO.HollowKnight8;

// An entry can restore zero from native prevGravityScale after a dash/jump.
// Repair only the short completed-entry window, never an active dash or spell.
internal static class EntryGravityRecovery
{
    private static readonly HeroController[] heroes = new HeroController[8];
    private static readonly float[] until = new float[8];

    internal static void Reset() { Array.Clear(heroes, 0, 8); Array.Clear(until, 0, 8); }

    internal static void Entered(CoopSession session, HeroController hero)
    {
        var p = session == null ? null : session.Resolve(hero);
        if (p == null || p.Index < 0 || p.Index >= 8) return;
        heroes[p.Index] = hero;
        until[p.Index] = Time.unscaledTime + 2f;
        Repair(session, p);
    }

    internal static void Tick(CoopSession session)
    {
        if (session == null || !session.Active) { Reset(); return; }
        foreach (var p in session.Players)
            if (p.Index >= 0 && p.Index < 8 && heroes[p.Index] == p.Hero) Repair(session, p);
    }

    private static void Repair(CoopSession s, PlayerSlot p)
    {
        int i = p.Index;
        if (!p.Hero || Time.unscaledTime > until[i] || p.Down || p.Hazard || p.Retiring)
        { heroes[i] = null; return; }
        var h = p.Hero;
        var gm = GameManager.instance;
        if (!s.Gameplay || !p.Ready || p.SpawnPending || p.InputBlocked || p.ArenaTransfer
            || !gm || gm.isPaused || h.controlReqlinquished || !h.acceptingInput
            || h.cState.transitioning || h.cState.dashing || h.cState.shadowDashing
            || h.cState.superDashing || h.cState.recoiling || h.cState.recoilFrozen
            || h.cState.swimming || p.AcidAssistActive || h.cState.spellQuake
            || h.cState.casting || h.cState.freezeCharge || h.cState.onConveyorV || h.cState.superDashOnWall
            || ScriptedParty.Active || CoopEnding.Active || EmergencyWarp.Active(p)
            || TransitionVote.Holding(p) || BenchSeats.Seated(p)) return;
        var body = h.GetComponent<Rigidbody2D>();
        if (!body || !body.simulated || body.isKinematic) return;
        if (Mathf.Abs(body.gravityScale) > .01f || float.IsNaN(body.gravityScale)
            || float.IsInfinity(body.gravityScale) || !(h.DEFAULT_GRAVITY > .01f))
        { heroes[i] = null; return; }
        body.gravityScale = h.DEFAULT_GRAVITY;
        Reflect.Set(h, "prevGravityScale", 0f);
        heroes[i] = null;
        Diagnostics.Write("ENTRY gravity restored P" + (i + 1));
    }
}

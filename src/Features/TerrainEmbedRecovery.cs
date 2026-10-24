using System;
using UnityEngine;

namespace KO.HollowKnight8;

// A falling solid can push a knight deeply into other terrain without a
// native hazard callback. Recover persistent penetration, not ordinary contact.
internal static class TerrainEmbedRecovery
{
    private static readonly Collider2D[] hits = new Collider2D[64];
    private static readonly HeroController[] heroes = new HeroController[8];
    private static readonly float[] since = new float[8];
    private static float next;

    internal static void Reset() { Array.Clear(heroes, 0, 8); Array.Clear(since, 0, 8); next = 0; }

    internal static void Tick(CoopSession s)
    {
        if (s == null || !s.Active) { Reset(); return; }
        var gm = GameManager.instance;
        if (!s.Gameplay || !gm || gm.isPaused || s.TeamWipe || ScriptedParty.Active
            || CoopEnding.Active || ArenaGather.TransferActive)
        { Array.Clear(heroes, 0, 8); return; }
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + .1f;
        Physics2D.SyncTransforms();
        foreach (var p in s.Players)
        {
            if (p.Index < 0 || p.Index >= 8) continue;
            int i = p.Index;
            Collider2D obstruction;
            if (!Eligible(p) || !Embedded(p, out obstruction)) { heroes[i] = null; continue; }
            if (heroes[i] != p.Hero) { heroes[i] = p.Hero; since[i] = Time.unscaledTime; continue; }
            if (Time.unscaledTime - since[i] < .25f) continue;
            Vector3 at;
            if (p.HasSafePoint && ArenaGather.Allows(p.SafePoint) && s.IsSafe(p.SafePoint, p)) at = p.SafePoint;
            else if (p.HasPreviousSafePoint && ArenaGather.Allows(p.PreviousSafePoint) && s.IsSafe(p.PreviousSafePoint, p)) at = p.PreviousSafePoint;
            else if (!SpawnSafety.Living(s, p, out at)) continue;
            s.Recover(p, respawn: false, reviveAt: at);
            if (!Embedded(p, out var remaining))
            {
                heroes[i] = null;
                Diagnostics.Write("TERRAIN embedded actor recovered P" + (i + 1) + " collider=" + obstruction.name + " layer=" + obstruction.gameObject.layer);
            }
        }
    }

    private static bool Eligible(PlayerSlot p)
    {
        if (p == null || !p.Hero || !p.Alive || !p.Ready || p.SpawnPending || p.InputBlocked
            || p.ArenaTransfer || p.Hero.controlReqlinquished || !p.Hero.acceptingInput
            || Time.time < p.ProtectionUntil || EmergencyWarp.Active(p) || TransitionVote.Holding(p) || BenchSeats.Seated(p)
            || HornetEncounterFlow.Holds(p)) return false;
        var c = p.Hero.cState;
        return !c.transitioning && !c.dashing && !c.shadowDashing && !c.superDashing && !c.spellQuake
            && !c.casting && !c.freezeCharge && !c.onConveyorV && !c.superDashOnWall
            && !c.recoiling && !c.recoilFrozen && !c.swimming && !p.AcidAssistActive;
    }

    private static bool Embedded(PlayerSlot p, out Collider2D obstruction)
    {
        obstruction = null;
        if (!p.Hero) return false;
        var body = p.Hero.GetComponent<Collider2D>();
        var rigidbody = p.Hero.GetComponent<Rigidbody2D>();
        if (!body || !body.enabled || body.isTrigger || !rigidbody || !rigidbody.simulated || rigidbody.isKinematic) return false;
        var bounds = DuelGround.Body(p);
        int count = Physics2D.OverlapBoxNonAlloc(bounds.center,
            new Vector2(Mathf.Max(.05f, bounds.size.x - .12f), Mathf.Max(.05f, bounds.size.y - .12f)),
            0, hits, (1 << 8) | (1 << 25));
        for (int i = 0; i < count; i++)
            if (SpawnSafety.Solid(hits[i]) && !hits[i].usedByEffector
                && !Physics2D.GetIgnoreLayerCollision(body.gameObject.layer, hits[i].gameObject.layer)
                && !Physics2D.GetIgnoreCollision(body, hits[i])
                && hits[i].OverlapPoint(bounds.center))
            {
                var distance = body.Distance(hits[i]);
                // A foot/edge overlap or one-way/ignored surface is not a
                // trapped body. Require the body's center inside blocking
                // terrain and substantial penetration relative to its height.
                if (distance.isValid && distance.isOverlapped && distance.distance < -Mathf.Max(.2f, bounds.size.y * .25f))
                { obstruction = hits[i]; return true; }
            }
        return false;
    }
}

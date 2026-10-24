using System.Collections.Generic;
using UnityEngine;
using GlobalEnums;

namespace KO.HollowKnight8;

// Track native cut/land lifecycle, not arbitrary moving scenery. Sweeping the
// underside catches a head hit even if physics already pushed the hero down.
internal static class FallingPlatformHazard
{
    private sealed class Fall
    {
        internal VinePlatform Platform;
        internal Rigidbody2D Body;
        internal Collider2D Shape;
        internal Bounds Before;
        internal readonly HeroController[] Hit = new HeroController[8];
    }
    private static readonly Dictionary<VinePlatform, Fall> falling = new Dictionary<VinePlatform, Fall>();
    private static readonly List<Fall> work = new List<Fall>();
    private static readonly PlayerSlot[] pending = new PlayerSlot[8];
    private static readonly HeroController[] recovering = new HeroController[8];
    private static bool installed;
    private static int epoch;
    private static CoopSession Session => Plugin.Self == null ? null : Plugin.Self.Session;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.VinePlatformCut.Cut += Cut;
        On.VinePlatform.Land += Land;
    }
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.VinePlatformCut.Cut -= Cut;
        On.VinePlatform.Land -= Land;
        Reset();
    }
    internal static void Reset()
    {
        epoch++;
        falling.Clear(); work.Clear();
        System.Array.Clear(pending, 0, 8); System.Array.Clear(recovering, 0, 8);
    }
    private static bool Active(CoopSession s)
    {
        var gm = GameManager.instance;
        return s != null && s.Active && s.Gameplay && !s.TeamWipe && gm && !gm.isPaused
            && !ScriptedParty.Active && !CoopEnding.Active && !ArenaGather.TransferActive;
    }
    private static void Cut(On.VinePlatformCut.orig_Cut orig, VinePlatformCut self)
    {
        orig(self);
        var platform = self ? self.GetComponentInParent<VinePlatform>() : null;
        if (!Active(Session) || !platform || !self.body || self.body.isKinematic || !platform.collider || falling.ContainsKey(platform)) return;
        falling[platform] = new Fall { Platform = platform, Body = self.body, Shape = platform.collider, Before = platform.collider.bounds };
    }
    private static void Land(On.VinePlatform.orig_Land orig, VinePlatform self)
    {
        // Native landing can precede the next session update. Check the last
        // descent before native Land stops the rigidbody and deactivates FX.
        try { if (Active(Session) && falling.TryGetValue(self, out var fall)) Sweep(Session, fall); }
        catch (System.Exception e) { Diagnostics.Throttled("PLATFORM landing", e); }
        try { orig(self); }
        finally { falling.Remove(self); }
    }
    private static bool Eligible(PlayerSlot p)
    {
        return p != null && p.Index >= 0 && p.Index < 8 && p.Hero && p.Ready && p.Alive
            && (p.Index == 0 || p.Connected) && !p.SpawnPending && !p.InputBlocked && !p.ArenaTransfer
            && !p.Hero.cState.transitioning && !p.Hero.controlReqlinquished
            && !EmergencyWarp.Active(p) && !TransitionVote.Holding(p) && !BenchSeats.Seated(p);
    }
    private static void Sweep(CoopSession s, Fall fall)
    {
        if (!fall.Shape || !fall.Shape.enabled || !fall.Shape.gameObject.activeInHierarchy || !fall.Body || fall.Body.isKinematic) return;
        var now = fall.Shape.bounds;
        float descent = fall.Before.min.y - now.min.y;
        if (descent > .005f)
        {
            foreach (var p in s.Players)
            {
                if (!Eligible(p) || fall.Hit[p.Index] == p.Hero || Time.time < p.ProtectionUntil) continue;
                var body = DuelGround.Body(p);
                float head = body.max.y;
                if (fall.Before.min.y < head - .12f || now.min.y > head + .12f) continue;
                float t = Mathf.Clamp((fall.Before.min.y - head) / descent, 0f, 1f);
                float x = fall.Before.center.x + (now.center.x - fall.Before.center.x) * t;
                float half = Mathf.Max(fall.Before.size.x, now.size.x) * .5f;
                if (x + half <= body.min.x + .05f || x - half >= body.max.x - .05f) continue;
                fall.Hit[p.Index] = p.Hero;
                var hero = p.Hero;
                int generation = epoch;
                using (PlayerContext.Enter(p)) hero.TakeDamage(fall.Platform.gameObject, CollisionSide.top, 1, 2);
                if (epoch != generation) return;
                // Native damage owns blue health, charms, immunity and fatal
                // hits. A fatal hit remains a normal down, never a free revive.
                if (!hero || p.Hero != hero || p.Down || p.Retiring || p.Vitals.Health <= 0) continue;
                pending[p.Index] = p; recovering[p.Index] = hero;
                if (!p.Hazard) s.Hazard(p);
                ActorRecovery.Freeze(p);
                Diagnostics.Write("PLATFORM head hit P" + (p.Index + 1));
            }
        }
        fall.Before = now;
    }
    private static void Recover(CoopSession s)
    {
        for (int i = 0; i < pending.Length; i++)
        {
            var p = pending[i];
            if (p == null) continue;
            if (!p.Hero || p.Hero != recovering[i] || p.Down || p.Retiring || !p.Ready || !p.Hazard || p.Vitals.Health <= 0)
            { pending[i] = null; recovering[i] = null; continue; }
            if (Time.time < p.HazardUntil) continue;
            Vector3 at;
            bool companion = false;
            foreach (var ally in s.Players) if (ally != p && Eligible(ally)) { companion = true; break; }
            if (!SpawnSafety.Living(s, p, out at))
            {
                if (!companion && p.HasSafePoint && ArenaGather.Allows(p.SafePoint) && s.IsSafe(p.SafePoint, p)) at = p.SafePoint;
                else if (!companion && p.HasPreviousSafePoint && ArenaGather.Allows(p.PreviousSafePoint) && s.IsSafe(p.PreviousSafePoint, p)) at = p.PreviousSafePoint;
                else { p.HazardUntil = Time.time + .3f; continue; }
            }
            s.Recover(p, respawn: false, reviveAt: at);
            if (!p.Hazard) { pending[i] = null; recovering[i] = null; }
            else p.HazardUntil = Time.time + .3f;
        }
    }
    internal static void Tick(CoopSession s)
    {
        if (s == null || !s.Active) { Reset(); return; }
        if (!Active(s)) return;
        work.Clear(); foreach (var fall in falling.Values) work.Add(fall);
        for (int i = 0; i < work.Count; i++)
        {
            var fall = work[i];
            if (!fall.Platform || !fall.Body || fall.Body.isKinematic || !fall.Shape || !fall.Shape.gameObject.activeInHierarchy)
            { falling.Remove(fall.Platform); continue; }
            Sweep(s, fall);
        }
        work.Clear(); Recover(s);
    }
}

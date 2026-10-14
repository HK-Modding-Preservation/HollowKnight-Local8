using UnityEngine;

namespace KO.HollowKnight8;

internal static class PhysicsGuard
{
    private static readonly float[] since = new float[8];
    internal static void Reset() { for (int i = 0; i < since.Length; i++) since[i] = 0; }

    internal static void Tick(CoopSession s)
    {
        var gm = GameManager.instance;
        if (s == null || !s.Gameplay || !s.Active || !gm || gm.isPaused || gm.TimeSlowed ||
            gm.IsLoadingSceneTransition || (Plugin.Self && Plugin.Self.Panel) || TransitionVote.Pending ||
            ScriptedParty.Active || CoopEnding.Active || Charms.NativeMenuOpen ||
            PickupCard.Owner != null || InteractionRouter.ActivePlayer != null)
        {
            // Time spent in menus/cutscenes is not evidence of a stuck body.
            Reset(); return;
        }
        foreach (var p in s.Players)
        {
            if (p.Index < 0 || p.Index >= since.Length) continue;
            var h = p.Hero;
            if (!p.Ready || !p.Alive || p.InputBlocked || p.SpawnPending || p.ArenaTransfer || !h ||
                h.cState.transitioning || h.cState.swimming || p.AcidAssistActive || h.inAcid ||
                h.controlReqlinquished || !h.acceptingInput || EmergencyWarp.Active(p) || BenchSeats.Seated(p) ||
                h.cState.dashing || h.cState.superDashing || h.cState.casting || h.cState.freezeCharge ||
                h.cState.recoilFrozen || h.cState.recoiling || h.cState.bouncing || h.cState.doubleJumping)
            {
                since[p.Index] = 0; continue;
            }
            var body = h.GetComponent<Rigidbody2D>();
            if (!body || (!body.isKinematic && body.simulated && body.gravityScale > .01f))
            {
                since[p.Index] = 0; continue;
            }
            // A vertical conveyor deliberately suspends gravity while the hero
            // clings to its wall. Exclude only a live native gravity lease, not
            // a stale onConveyorV flag which could hide an actual stuck body.
            if (ConveyorOwnsGravity(h)) { since[p.Index] = 0; continue; }
            if (since[p.Index] == 0) { since[p.Index] = Time.unscaledTime; continue; }
            if (Time.unscaledTime - since[p.Index] < 1.4f) continue;
            since[p.Index] = 0;
            float old = body.gravityScale;
            float gravity = Reflect.Get(h, "prevGravityScale", 0f);
            if (gravity <= .01f) gravity = h.DEFAULT_GRAVITY > 0 ? h.DEFAULT_GRAVITY : 1f;
            body.simulated = true; body.isKinematic = false; body.gravityScale = gravity;
            Reflect.Set(h, "prevGravityScale", gravity);
            Diagnostics.Write("PHYSICS restored gravity P" + (p.Index + 1) + " at " + h.transform.position +
                " old=" + old + " new=" + gravity + " state=" + h.hero_state + " velocity=" + body.velocity);
        }
    }

    private static bool ConveyorOwnsGravity(HeroController hero)
    {
        if (!hero.cState.wallSliding && !hero.cState.superDashOnWall) return false;
        var conveyor = hero.GetComponent<ConveyorMovementHero>();
        return conveyor && conveyor.enabled && conveyor.gameObject.activeInHierarchy
            && Reflect.Get<HeroController>(conveyor, "heroCon") == hero
            && Reflect.Get(conveyor, "onConveyor", false) && Reflect.Get(conveyor, "gravityOff", false)
            && Reflect.Get(conveyor, "ySpeed", 0f) != 0f;
    }
}

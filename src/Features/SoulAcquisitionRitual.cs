using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using Scenes = UnityEngine.SceneManagement.SceneManager;

namespace KO.HollowKnight8;

// Bounded replicas of the shadow ritual, in white. The one native collector
// owns progression, callbacks and UI; followers absorb soul where they stand.
internal static class SoulAcquisitionRitual
{
    private sealed class Actor
    {
        internal PlayerSlot Player; internal HeroController Hero; internal Vector3 At;
        internal Rigidbody2D Body; internal bool Simulated;
        internal HeroAnimationController Animation; internal bool AnimationEnabled;
        internal tk2dSpriteAnimator Animator; internal tk2dSpriteAnimationClip Pose;
        internal Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip> Completed;
        internal Action<tk2dSpriteAnimator, tk2dSpriteAnimationClip, int> Triggered;
        internal bool Taken, Released;
        internal DarkEssenceFx Effect;
    }
    internal const float Seconds = 4.2f;
    private static readonly List<Actor> actors = new List<Actor>();
    private static CoopSession session;
    private static PlayerSlot collector;
    private static HeroController nativeHero;
    private static float started;
    private static int scene;
    internal static bool Active => session != null;
    internal static bool White(string key, int value)
        => value == 1 && (AcquisitionRules.Level(key) || key == "hasAcidArmour");

    internal static bool Holds(PlayerSlot p)
        => Active && p != null && actors.Exists(a => !a.Released && a.Taken && a.Player == p && p.Hero == a.Hero && p.Alive && p.Ready);
    internal static bool HoldsFsm(PlayerSlot p, Fsm f)
        => Holds(p) && f != null && f.GameObject && f.GameObject.GetComponentInParent<HeroController>() == p.Hero;

    internal static void Begin(CoopSession s, PlayerSlot owner, string key, int value)
    {
        if (!White(key, value) || s == null || !s.Active || s.Players.Count < 2 || s.TeamWipe ||
            PvpMatch.Running || CoopEnding.Active || !Valid(owner) || ShadeCloakRitual.Combined(false)) return;
        Release(); SharedAcquisition.ReleasePoses();
        session = s; collector = owner; nativeHero = owner.Hero;
        scene = Scenes.GetActiveScene().handle; started = Time.unscaledTime;
        try
        {
            foreach (var p in s.Players)
            {
                if (p == owner || !Valid(p)) continue;
                var a = new Actor { Player = p, Hero = p.Hero, At = p.Hero.transform.position,
                    Body = p.Hero.GetComponent<Rigidbody2D>(), Animation = p.Hero.GetComponent<HeroAnimationController>(),
                    Animator = p.Hero.GetComponent<tk2dSpriteAnimator>() };
                actors.Add(a);
                // Do not steal an NPC conversation, rescue, swim, bench or a
                // concurrent native pickup. Those recipients still get essence.
                bool free = !p.InputBlocked && !p.Reviving && !p.Hero.controlReqlinquished && p.Hero.acceptingInput &&
                    !p.Hero.cState.swimming && !p.Hero.cState.transitioning && !p.Hero.cState.dashing && !p.Hero.cState.superDashing &&
                    !p.Hero.cState.focusing && !p.Hero.cState.attacking && !p.Hero.cState.recoiling && (!a.Body || a.Body.simulated) && !EmergencyWarp.Active(p) &&
                    !ScriptedParty.Holds(p) && !ChallengeSequence.Holds(p) && !BenchSeats.Seated(p) &&
                    !Charms.NativeMenuOpen && InteractionRouter.ActivePlayer != p && (!a.Animation || a.Animation.controlEnabled);
                a.Effect = DarkEssenceFx.CreateSoul(p);
                a.Effect?.Prime(a.At);
                if (!free || !a.Animator) continue;
                foreach (var name in new[] { "Focus", "Focus Loop", "Focus Get", "Idle" })
                {
                    var clip = a.Animator.GetClipByName(name);
                    if (clip != null && clip.frames != null && clip.frames.Length > 0 && clip.fps > 0) { a.Pose = clip; break; }
                }
                if (a.Pose == null) continue;
                a.Simulated = !a.Body || a.Body.simulated; a.AnimationEnabled = !a.Animation || a.Animation.controlEnabled;
                a.Completed = a.Animator.AnimationCompleted; a.Triggered = a.Animator.AnimationEventTriggered;
                a.Taken = true; // Cleanup also covers an exception halfway through taking control.
                a.Animator.AnimationCompleted = null; a.Animator.AnimationEventTriggered = null;
                bool replaying = SharedAcquisition.Replaying;
                try
                {
                    SharedAcquisition.Replaying = true;
                    using (PlayerContext.Enter(p))
                    {
                        CrystalDashCoop.StopPlayer(p);
                        p.Hero.RelinquishControl(); p.Hero.StopAnimationControl();
                        a.Animator.Play(a.Pose);
                    }
                }
                finally { SharedAcquisition.Replaying = replaying; }
                if (a.Body) { a.Body.velocity = Vector2.zero; a.Body.simulated = false; }
                p.ProtectionUntil = Mathf.Max(p.ProtectionUntil, Time.time + .3f);
            }
            DarkEssenceFx.Cue(nativeHero, false);
            Diagnostics.Write("SOUL ritual " + key + ":" + value + " collector=P" + (owner.Index + 1) + " recipients=" + actors.Count + "; white essence, no gathering during absorption");
        }
        catch (Exception ex) { Release(); Diagnostics.Throttled("SOUL ritual setup", ex); }
    }

    private static bool Valid(PlayerSlot p)
        => p != null && p.Hero && p.Alive && p.Ready && (p.Index == 0 || p.Connected) && !p.Hazard && !p.Retiring && !p.ArenaTransfer && !TransitionVote.Holding(p);

    // Called independently of Session.Tick, including modal native item UI.
    internal static void Tick(CoopSession s)
    {
        if (!Active) return;
        var gm = GameManager.instance;
        if (s != session || !s.Active || s.TeamWipe || !Valid(collector) || collector.Hero != nativeHero ||
            Scenes.GetActiveScene().handle != scene || !gm || gm.IsLoadingSceneTransition || Time.unscaledTime - started >= Seconds)
        { Release(); return; }
        if (gm.isPaused) return;
        try
        {
            float elapsed = Time.unscaledTime - started;
            float strength = Mathf.Clamp01(elapsed / .6f) * Mathf.Clamp01((Seconds - elapsed) / .65f);
            foreach (var a in actors)
            {
                if (!Valid(a.Player) || a.Player.Hero != a.Hero) { Restore(a); continue; }
                if (a.Taken && !a.Released)
                {
                    a.Player.ProtectionUntil = Mathf.Max(a.Player.ProtectionUntil, Time.time + .3f);
                    a.Hero.transform.position = a.At;
                    if (a.Body) a.Body.velocity = Vector2.zero;
                    bool replaying = SharedAcquisition.Replaying;
                    try
                    {
                        SharedAcquisition.Replaying = true;
                        if (a.Animator && a.Animator.CurrentClip != a.Pose) using (PlayerContext.Enter(a.Player)) a.Animator.Play(a.Pose);
                    }
                    finally { SharedAcquisition.Replaying = replaying; }
                }
                if (!a.Released) a.Effect?.Tick(a.Hero.transform.position, elapsed, strength);
            }
        }
        catch (Exception ex) { Release(); Diagnostics.Throttled("SOUL ritual recovery", ex); }
    }

    private static void Restore(Actor a)
    {
        if (a.Released) return; a.Released = true;
        try { a.Effect?.Dispose(); }
        finally
        {
            a.Effect = null;
            if (a.Taken)
            {
                bool replaying = SharedAcquisition.Replaying;
                try
                {
                    SharedAcquisition.Replaying = true;
                    // Stop replica BEFORE returning callbacks: it may contain
                    // vanilla frame/completion events belonging only to the collector.
                    if (a.Animator && a.Animator.CurrentClip == a.Pose && a.Animator.AnimationCompleted == null) a.Animator.Stop();
                }
                finally
                {
                    SharedAcquisition.Replaying = replaying;
                    if (a.Animator)
                    {
                        if (a.Animator.AnimationCompleted == null) a.Animator.AnimationCompleted = a.Completed;
                        if (a.Animator.AnimationEventTriggered == null) a.Animator.AnimationEventTriggered = a.Triggered;
                    }
                    if (a.Body) { a.Body.simulated = a.Simulated; a.Body.velocity = Vector2.zero; }
                    var p = a.Player;
                    if (p.Hero == a.Hero && p.Alive && p.Ready && !p.Hazard && !p.Retiring && !p.ArenaTransfer)
                    {
                        using (PlayerContext.Enter(p)) { a.Hero.RegainControl(); a.Hero.StartAnimationControl(); }
                        if (a.Animation) Reflect.Set(a.Animation, "<controlEnabled>k__BackingField", a.AnimationEnabled);
                    }
                }
            }
        }
    }
    internal static void Release()
    {
        // Clear Active first, so native input and recovery hooks cannot treat
        // a restoring follower as still suspended.
        session = null;
        foreach (var a in actors) try { Restore(a); } catch (Exception ex) { Diagnostics.Throttled("SOUL ritual release", ex); }
        actors.Clear(); collector = null; nativeHero = null;
    }
}

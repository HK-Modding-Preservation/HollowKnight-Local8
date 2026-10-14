using System;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

/// <summary>
/// A local knockout is not the game's global player death. Only Wipe can
/// authorise the original death sequence, after a snapshot of the whole party.
/// Hazards, dream returns, enemy DEATH and scripted endings keep their own paths.
/// </summary>
internal static class PartyDeath
{
    private sealed class Defeat
    {
        internal CoopSession Session;
        internal PlayerSlot Primary;
        internal HeroController Hero;
        internal bool HeroStarted, ManagerStarted;
    }

    private static Defeat defeat;
    private static readonly HashSet<string> reported = new HashSet<string>();
    private static CoopSession Session => Plugin.Self == null ? null : Plugin.Self.Session;

    internal static void Reset()
    {
        defeat = null;
        reported.Clear();
    }

    internal static bool TryBegin(CoopSession session)
    {
        if (session == null || !session.Active || session.TeamWipe || PvpMatch.Running
            || session.Primary == null || !session.Primary.Hero || session.Players.Count == 0) return false;
        bool hasPrimary = false;
        foreach (var player in session.Players)
        {
            if (player == null) continue;
            // An abandoned slot is not a combatant. Connected/recovering slots
            // still count, even with no active body: missing is not dead.
            if (player != session.Primary && player.Retiring && !player.Connected && !player.SpawnPending) continue;
            if (player == session.Primary) hasPrimary = true;
            if (!player.Down || player.Vitals.Health > 0 || player.Vitals.Blue > 0)
            {
                if (player.Down) Report("snapshot", "PARTY DEATH refused inconsistent knockout P" + (player.Index + 1));
                return false;
            }
        }
        if (!hasPrimary) return false;
        defeat = new Defeat { Session = session, Primary = session.Primary, Hero = session.Primary.Hero };
        Diagnostics.Write("PARTY DEATH confirmed all participants down");
        return true;
    }

    private static bool Valid(Defeat proof)
    {
        return proof != null && ReferenceEquals(defeat, proof) && ReferenceEquals(Session, proof.Session)
            && proof.Session.Active && proof.Session.TeamWipe && !PvpMatch.Running
            && ReferenceEquals(proof.Session.Primary, proof.Primary) && !proof.Primary.Retiring
            && proof.Hero && proof.Primary.Hero == proof.Hero;
    }

    internal static bool NativeHero(PlayerSlot player, HeroController hero)
    {
        return Valid(defeat) && ReferenceEquals(player, defeat.Primary) && hero == defeat.Hero;
    }

    internal static IEnumerator HeroRoutine(Func<IEnumerator> original)
    {
        return new NativeRoutine(defeat, original, false);
    }

    internal static IEnumerator ManagerRoutine(Func<IEnumerator> original)
    {
        var proof = defeat;
        if (!Valid(proof) || !proof.HeroStarted
            || (PlayerContext.Current != null && PlayerContext.Current != proof.Primary))
        {
            Report("manager", "PARTY DEATH ignored unconfirmed GameManager.PlayerDead");
            return Empty();
        }
        return new NativeRoutine(proof, original, true);
    }

    private static IEnumerator Empty() { yield break; }

    internal static bool IsDeath(FsmEvent evt)
    {
        if (evt == null) return false;
        // Exact notifications only. In particular DEATH belongs to enemies,
        // and HAZARD DEATH/HAZARD RELOAD are nonfatal recovery notifications.
        return evt.Name == "HERO DEATH" || evt.Name == "HERO DEAD"
            || evt.Name == "PLAYER DEATH" || evt.Name == "PLAYER DEAD";
    }

    private static PlayerSlot HeroBody(GameObject gameObject, CoopSession session)
    {
        if (!gameObject) return null;
        var hero = gameObject.GetComponentInParent<HeroController>();
        if (hero)
        {
            var player = session.Resolve(hero);
            if (player != null && player.Hero == hero) return player;
        }
        foreach (var player in session.Players)
        {
            if (player == null || !player.Hero || !player.Hero.heroDeathPrefab) continue;
            var death = player.Hero.heroDeathPrefab;
            if (gameObject == death || gameObject.transform.IsChildOf(death.transform)) return player;
        }
        // OwnerTag also marks summons, spells and local shades. They are not
        // hero bodies and must not consume an owner's death as a world defeat.
        return null;
    }

    internal static bool BlockEvent(Fsm receiver, FsmEvent evt, FsmEventData data)
    {
        var session = Session;
        if (!IsDeath(evt) || session == null || !session.Active) return false;
        if (Valid(defeat)) return false;
        var recipient = receiver == null ? null : HeroBody(receiver.GameObject, session);
        var source = data == null ? null : HeroBody(data.SentByGameObject, session);
        if (source == null && data != null && data.SentByFsm != null)
            source = HeroBody(data.SentByFsm.GameObject, session);
        if (source == null) source = PlayerContext.Current;

        // These known story sequences can use death-style events without a
        // fatal hit. Do not exempt dream arenas or Godhome combat as a whole.
        bool narrative = CoopEnding.Active || (DreamSequence.InStoryDream && session.Data != null && !session.Data.hasDreamNail);
        if (narrative && (source == null || (!source.Down && source.Vitals.Health > 0))) return false;

        if (recipient != null && recipient.Down && recipient.Vitals.Health <= 0 && recipient.Vitals.Blue <= 0
            && (source == null || source == recipient)) return false;

        string name = receiver == null ? "unknown" : receiver.Name;
        string key = evt.Name + ":" + name + ":" + (receiver != null && receiver.GameObject ? receiver.GameObject.GetInstanceID().ToString() : "none");
        Report(key, "PARTY DEATH isolated " + evt.Name + " receiver=" + name);
        return true;
    }

    private static void Report(string key, string message)
    {
        if (reported.Add(key)) Diagnostics.Write(message);
    }

    /// <summary>
    /// Claim at first execution, not at iterator creation. Capture the proof so
    /// delayed coroutines cannot become authorised by a later party defeat.
    /// The API's PlayerDead iterator wraps its native iterator; after the native
    /// child completes it must get its last step, even if respawning reset the
    /// scene, so the original AfterPlayerDeadHook can finish exactly once.
    /// </summary>
    private sealed class NativeRoutine : IEnumerator, IDisposable
    {
        private readonly Defeat proof;
        private readonly Func<IEnumerator> factory;
        private readonly bool manager, root;
        private IEnumerator routine;
        private object current;
        private NativeRoutine child;
        private bool started, disposed, naturalCompletion;

        internal NativeRoutine(Defeat proof, Func<IEnumerator> factory, bool manager)
        {
            this.proof = proof;
            this.factory = factory;
            this.manager = manager;
            root = true;
        }

        private NativeRoutine(Defeat proof, IEnumerator routine, bool manager)
        {
            this.proof = proof;
            this.routine = routine;
            this.manager = manager;
        }

        public object Current => current;

        public bool MoveNext()
        {
            if (disposed) return false;
            bool valid = Valid(proof);
            // A cancelled child is not completion. Only a child that actually
            // reached its end may allow the enclosing API/mod completion step.
            bool completing = manager && started && child != null && child.naturalCompletion;
            if (!valid && !completing) { Dispose(); return false; }
            try
            {
                using (valid ? PlayerContext.Enter(proof.Primary) : null)
                {
                    if (!started)
                    {
                        if (root)
                        {
                            if (manager ? proof.ManagerStarted : proof.HeroStarted) { Dispose(); return false; }
                            if (manager) proof.ManagerStarted = true;
                            else proof.HeroStarted = true;
                            routine = factory();
                        }
                        started = true;
                    }
                    if (!Valid(proof) && !completing) { Dispose(); return false; }
                    if (routine == null || !routine.MoveNext())
                    {
                        naturalCompletion = true;
                        Dispose();
                        return false;
                    }
                    object yielded = routine.Current;
                    if (yielded is IEnumerator nested)
                    {
                        child = new NativeRoutine(proof, nested, manager);
                        current = child;
                    }
                    else
                    {
                        child = null;
                        current = yielded;
                    }
                    return true;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Reset() { throw new NotSupportedException(); }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { child?.Dispose(); }
            finally
            {
                using (Valid(proof) ? PlayerContext.Enter(proof.Primary) : null)
                {
                    (routine as IDisposable)?.Dispose();
                }
                current = null;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scenes = UnityEngine.SceneManagement.SceneManager;

namespace KO.HollowKnight8;

// Modern lifeseeds use ScuttlerControl, not HealthManager or a pickup FSM.
// Its accepted Hit starts Heal, whose delayed global event otherwise awards
// P1. Share at this exact death boundary, before the party can leave the room.
internal static class LifebloodScuttlers
{
    private static bool installed;
    private sealed class Life
    {
        internal CoopSession Session;
        internal int Scene;
        internal bool Shared;
    }
    private static readonly Dictionary<ScuttlerControl, Life> pending = new Dictionary<ScuttlerControl, Life>();

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.ScuttlerControl.Hit += Hit;
        On.ScuttlerControl.Heal += Heal;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.ScuttlerControl.Hit -= Hit;
        On.ScuttlerControl.Heal -= Heal;
        Reset();
    }

    private static void Hit(On.ScuttlerControl.orig_Hit orig, ScuttlerControl self, HitInstance damage)
    {
        // A living/rearmed bug starts a new lifetime. Keep old visual iterators
        // from consuming the reward or disabling this reused object.
        if (self && self.healthScuttler && Reflect.Get(self, "alive", false))
        {
            pending.Remove(self);
            var s = Plugin.Self ? Plugin.Self.Session : null;
            if (s != null && s.Active) pending[self] = new Life { Session = s, Scene = Scenes.GetActiveScene().handle };
        }
        orig(self, damage);
    }

    private static IEnumerator Heal(On.ScuttlerControl.orig_Heal orig, ScuttlerControl self)
    {
        var s = Plugin.Self ? Plugin.Self.Session : null;
        if (s == null || !s.Active || !self || !self.healthScuttler || Reflect.Get(self, "alive", true) ||
            !pending.TryGetValue(self, out var life) || life.Session != s || life.Scene != Scenes.GetActiveScene().handle) return orig(self);
        // Native Hit alone decides activation delay, living state, accepted
        // attack types, journal updates and the initial particle burst.
        // Heal can be entered by a coroutine/hook after Hit has returned. The
        // captured lifetime/session, rather than a thread-local caller, is proof.
        if (!life.Shared) { life.Shared = true; Lifeblood.NativeBug(s, self.gameObject); }
        return Finish(self, life);
    }

    private static IEnumerator Finish(ScuttlerControl self, Life token)
    {
        yield return new WaitForSeconds(1.2f);
        if (!pending.TryGetValue(self, out var current) || !ReferenceEquals(current, token)) yield break;
        pending.Remove(self);
        // Another mod can rearm a pooled scuttler before this old visual ends.
        if (!self || Reflect.Get(self, "alive", true)) yield break;
        if (self.screenFlash)
        {
            var effect = self.screenFlash.Spawn();
            effect.GetComponent<Renderer>().material.SetColor("_Color", new Color(0f, .7f, 1f));
            var flash = FSMUtility.LocateFSM(effect, "Fade Away");
            if (flash) FSMUtility.SetFloat(flash, "Alpha", .75f);
        }
        self.gameObject.SetActive(false);
    }

    internal static void Reset()
    {
        var old = new List<ScuttlerControl>(pending.Keys);
        pending.Clear();
        foreach (var seed in old)
            if (seed && !Reflect.Get(seed, "alive", true)) seed.gameObject.SetActive(false);
    }
}

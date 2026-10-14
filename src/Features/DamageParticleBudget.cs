using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace KO.HollowKnight8;

// Filter cosmetic emitter/debris actions in the actual native hurt FSM only.
// Keep its events, other actions, invulnerability and all charm/boss FSMs.
internal static class DamageParticleBudget
{
    private static bool installed;
    private static int bursts, emitters;
    private static float nextLog;

    internal static bool Enabled => Plugin.Self && Plugin.Self.Session != null &&
        Plugin.Self.Session.Active && (CombatEffects.Enabled || PerformanceTuning.ReduceDamage);

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.OnEnter += Fling;
        On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolVel.OnEnter += FlingVelocity;
        On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime.OnUpdate += FlingTimed;
        On.HutongGames.PlayMaker.Actions.PlayParticleEmitter.OnEnter += Play;
        On.HutongGames.PlayMaker.Actions.PlayParticleEmitterInState.OnEnter += PlayInState;
        On.HutongGames.PlayMaker.Actions.SetParticleEmission.OnEnter += Emission;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.OnEnter -= Fling;
        On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolVel.OnEnter -= FlingVelocity;
        On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime.OnUpdate -= FlingTimed;
        On.HutongGames.PlayMaker.Actions.PlayParticleEmitter.OnEnter -= Play;
        On.HutongGames.PlayMaker.Actions.PlayParticleEmitterInState.OnEnter -= PlayInState;
        On.HutongGames.PlayMaker.Actions.SetParticleEmission.OnEnter -= Emission;
        ResetWindow(); nextLog = 0;
    }

    private static PlayerSlot Owner(Fsm fsm)
    {
        if (!Enabled || fsm == null) return null;
        var p = Plugin.Self.Session.Resolve(fsm);
        return p != null && p.Hero && p.Hero.damageEffectFSM &&
            p.Hero.damageEffectFSM.Fsm == fsm ? p : null;
    }

    private static bool Cosmetic(GameObject obj)
    {
        // A mod may add an offensive spawn to this FSM. Do not remove it even
        // if its artwork is black or it also contains a particle system.
        return obj && !obj.GetComponentInChildren<HeroController>(true) &&
            !obj.GetComponentInChildren<HealthManager>(true) &&
            !obj.GetComponentInChildren<DamageHero>(true) &&
            !obj.GetComponentInChildren<DamageEnemies>(true) &&
            !obj.GetComponentInChildren<KnightHatchling>(true);
    }

    private static void Recorded(PlayerSlot p, GameObject obj, bool emitter)
    {
        if (emitter) emitters++; else bursts++;
        if (Time.unscaledTime < nextLog) return;
        nextLog = Time.unscaledTime + 5f;
        Diagnostics.Write("REDUCED PARTICLES P" + (p.Index + 1) + " object=" + obj.name +
            " source=" + p.Hero.damageEffectFSM.FsmName + " emitter=" + emitter);
    }

    private static bool Reject(Fsm fsm, GameObject prefab)
    {
        var p = Owner(fsm);
        if (p == null || !Cosmetic(prefab)) return false;
        Recorded(p, prefab, false);
        return true;
    }

    private static bool RejectEmitter(Fsm fsm, FsmOwnerDefault target)
    {
        var p = Owner(fsm);
        if (p == null || target == null) return false;
        var obj = fsm.GetOwnerDefaultTarget(target);
        if (!obj || obj == p.Hero.gameObject || !Cosmetic(obj)) return false;
        var ps = obj.GetComponent<ParticleSystem>();
        if (!ps) return false;
        // Clear just this emitter. Never recurse into healing/attack children.
        ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        Recorded(p, obj, true);
        return true;
    }

    private static void Fling(On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.orig_OnEnter orig, FlingObjectsFromGlobalPool action)
    {
        if (Reject(action.Fsm, action.gameObject?.Value)) action.Finish();
        else orig(action);
    }

    private static void FlingVelocity(On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolVel.orig_OnEnter orig, FlingObjectsFromGlobalPoolVel action)
    {
        if (Reject(action.Fsm, action.gameObject?.Value)) action.Finish();
        else orig(action);
    }

    private static void FlingTimed(On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime.orig_OnUpdate orig, FlingObjectsFromGlobalPoolTime action)
    {
        // This native action deliberately stays unfinished while the state is
        // active. Preserve that lifetime; only omit its cosmetic spawns.
        if (!Reject(action.Fsm, action.gameObject?.Value)) orig(action);
    }

    private static void Play(On.HutongGames.PlayMaker.Actions.PlayParticleEmitter.orig_OnEnter orig, PlayParticleEmitter action)
    {
        if (RejectEmitter(action.Fsm, action.gameObject)) action.Finish();
        else orig(action);
    }

    private static void PlayInState(On.HutongGames.PlayMaker.Actions.PlayParticleEmitterInState.orig_OnEnter orig, PlayParticleEmitterInState action)
    {
        // Native PlayParticleEmitterInState does not call Finish on entry.
        if (!RejectEmitter(action.Fsm, action.gameObject)) orig(action);
    }

    private static void Emission(On.HutongGames.PlayMaker.Actions.SetParticleEmission.orig_OnEnter orig, SetParticleEmission action)
    {
        // Let the native OFF action run, so toggling this setting cannot leave
        // a formerly active emitter latched on.
        if (action.emission != null && action.emission.Value && RejectEmitter(action.Fsm, action.gameObject)) action.Finish();
        else orig(action);
    }

    internal static string Report() => "hit_particle_bursts_skipped=" + bursts + " hit_emitters_skipped=" + emitters;
    internal static void ResetWindow() { bursts = emitters = 0; }
}

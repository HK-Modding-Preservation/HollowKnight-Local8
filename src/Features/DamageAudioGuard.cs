using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.Audio;

namespace KO.HollowKnight8;

// Damage snapshots are global in vanilla, including the cloned heroes' FSMs.
// Skip only their muffling transitions; keep audio settings, pauses, music cues,
// death/cutscene mixes and the FSM's normal recovery path intact.
internal static class DamageAudioGuard
{
    private static bool installed;
    private static int damageDepth;
    private static float nextLog;
    internal static bool Enabled => CombatEffects.Enabled && Plugin.Self.Session.Players.Count>1 && !Plugin.Self.Session.TeamWipe;
    internal static void Install()
    {
        if(installed)return;installed=true;
        On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter+=SnapshotAction;
        On.AudioManager.ApplyMusicSnapshot+=MusicSnapshot;
    }
    internal static void Uninstall()
    {
        if(!installed)return;installed=false;
        On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.OnEnter-=SnapshotAction;
        On.AudioManager.ApplyMusicSnapshot-=MusicSnapshot;damageDepth=0;nextLog=0;
    }
    private sealed class Scope : IDisposable
    {
        private bool active=true;
        internal Scope(){damageDepth++;}
        public void Dispose(){if(active){active=false;damageDepth--;}}
    }
    internal static IDisposable Damage() => Enabled ? new Scope() : null;
    internal static bool DamageName(string value)
    {
        if(string.IsNullOrEmpty(value))return false;
        return value.IndexOf("damage",StringComparison.OrdinalIgnoreCase)>=0 ||
            value.IndexOf("hurt",StringComparison.OrdinalIgnoreCase)>=0 ||
            value.IndexOf("injur",StringComparison.OrdinalIgnoreCase)>=0 ||
            value.IndexOf("lowpass",StringComparison.OrdinalIgnoreCase)>=0 ||
            value.IndexOf("low pass",StringComparison.OrdinalIgnoreCase)>=0 ||
            string.Equals(value,"Hit",StringComparison.OrdinalIgnoreCase);
    }
    private static bool OwnedDamage(Fsm fsm)
    {
        if(fsm==null||!Enabled)return false;
        foreach(var p in Plugin.Self.Session.Players)
        {
            if(!p.Hero)continue;
            if(p.Hero.damageEffectFSM && p.Hero.damageEffectFSM.Fsm==fsm)return true;
            if(p.Hero.proxyFSM && p.Hero.proxyFSM.Fsm==fsm && DamageName(fsm.ActiveStateName))return true;
        }
        return false;
    }
    private static bool Reject(AudioMixerSnapshot snapshot,Fsm fsm)
    {
        var game=GameManager.instance;
        if(!Enabled||!snapshot||!game||game.isPaused||game.IsLoadingSceneTransition||CoopEnding.Active||ScriptedParty.Active)return false;
        // Recovery snapshots can be emitted while the FSM still reports "Damage".
        // Never suppress the normal or pause mix just because of that state name.
        if(snapshot==game.actorSnapshotUnpaused||snapshot==game.actorSnapshotPaused||
            string.Equals(snapshot.name,"Normal",StringComparison.OrdinalIgnoreCase)||
            string.Equals(snapshot.name,"None",StringComparison.OrdinalIgnoreCase)||
            string.Equals(snapshot.name,"Off",StringComparison.OrdinalIgnoreCase))return false;
        bool own=OwnedDamage(fsm),named=DamageName(snapshot.name);
        // The normal/default/return transition in a damage FSM must still run.
        if(!(named&&(own||damageDepth>0)) && !(own&&fsm!=null&&DamageName(fsm.ActiveStateName)))return false;
        if(Time.unscaledTime>=nextLog)
        {
            nextLog=Time.unscaledTime+5f;
            Diagnostics.Write("REDUCED EFFECTS skipped damage audio mix="+snapshot.name+" fsm="+(fsm==null?"native damage":fsm.Name)+" state="+(fsm==null?"scoped":fsm.ActiveStateName));
        }
        return true;
    }
    private static void SnapshotAction(On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.orig_OnEnter orig,TransitionToAudioSnapshot action)
    {
        var snapshot=action.snapshot?.Value as AudioMixerSnapshot;
        if(Reject(snapshot,action.Fsm)){action.Finish();return;}
        orig(action);
    }
    private static void MusicSnapshot(On.AudioManager.orig_ApplyMusicSnapshot orig,AudioManager manager,AudioMixerSnapshot snapshot,float delay,float transition)
    {
        if(!Reject(snapshot,null))orig(manager,snapshot,delay,transition);
    }
}

using System;
using System.Collections.Generic;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
namespace KO.HollowKnight8 {
// Native trail renderers can survive a cancelled dash when a death/transition
// skips their own FSM exit. Hide just those renderers and clear their particles;
// leave the knight, physics, screen flashes and unrelated effects untouched.
internal static class NativeDashEffects {
 sealed class Fx {
  internal PlayerSlot Player;internal HeroController Hero;internal bool Active,Flight;
  internal readonly Dictionary<Renderer,bool> Hidden=new Dictionary<Renderer,bool>();
  internal readonly Dictionary<ParticleSystem,bool> Stopped=new Dictionary<ParticleSystem,bool>();
  internal readonly List<GameObject> External=new List<GameObject>();
  internal readonly List<Renderer> ResumedRenderers=new List<Renderer>();
  internal readonly List<ParticleSystem> ResumedParticles=new List<ParticleSystem>();
  internal readonly List<GameObject> ChargeArt=new List<GameObject>();
 }
 static readonly Dictionary<HeroController,Fx> effects=new Dictionary<HeroController,Fx>();
 static readonly List<HeroController> expired=new List<HeroController>();
 static bool Visual(Fx fx,Transform t){
  bool visual=false;
  for(;t&&t!=fx.Hero.transform;t=t.parent){string n=CrystalDashRules.Name(t.name);if(n.Contains("light")||n.Contains("flash")||n.Contains("screen")||n.Contains("mask")||n.Contains("fade"))return false;if(CrystalDashRules.Trail(t.name)||CrystalDashRules.CrystalArt(t.name)||fx.ChargeArt.Contains(t.gameObject))visual=true;}
  return visual;
 }
 static Fx Get(PlayerSlot p){Fx fx;if(!effects.TryGetValue(p.Hero,out fx)){
  effects[p.Hero]=fx=new Fx{Player=p,Hero=p.Hero};
  // The circular charge artwork is not always named SD Crystals (skins can
  // rename it). Identify the real effect from native charge actions instead.
  if(p.Hero.superDash)foreach(var state in p.Hero.superDash.FsmStates){
   if(!CrystalDashRules.Charging(state.Name)||state.Actions==null)continue;
   foreach(var action in state.Actions){var mesh=action as SetMeshRenderer;if(mesh==null)continue;
    var obj=p.Hero.superDash.Fsm.GetOwnerDefaultTarget(mesh.gameObject);
    if(obj&&obj!=p.Hero.gameObject&&obj.transform.IsChildOf(p.Hero.transform)&&!fx.ChargeArt.Contains(obj))fx.ChargeArt.Add(obj);
   }
  }
 }return fx;}
 internal static void Spawned(GameObject obj){
  var s=Plugin.Self?Plugin.Self.Session:null;if(!obj||s==null||!s.Active||!(CrystalDashRules.Trail(obj.name)||CrystalDashRules.CrystalArt(obj.name)))return;
  var p=s.Resolve(obj);if(p==null||!p.Hero||obj==p.Hero.gameObject)return;
  var fx=Get(p);if(!fx.External.Contains(obj))fx.External.Add(obj);
  // A delayed native action may spawn its pooled trail after death/travel
  // already cancelled the flight. Do not let that late spawn restore it.
  if(!p.Alive||!p.Ready||p.Hero.cState.dead||p.ArenaTransfer||TransitionVote.Holding(p))Stop(p);
 }
 internal static void Stop(PlayerSlot p){
  if(p==null||!p.Hero)return;
  var fx=Get(p);fx.Active=fx.Flight=false;
  Hide(fx,p.Hero.gameObject);
  var s=Plugin.Self?Plugin.Self.Session:null;
  foreach(var obj in fx.External)if(obj&&s!=null&&s.Resolve(obj)==p)Hide(fx,obj);
 }
 static void Hide(Fx fx,GameObject obj){
  foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))if(renderer&&renderer.gameObject!=fx.Hero.gameObject&&Visual(fx,renderer.transform)){
   // Never remember another subsystem's temporary invisibility as this FX's
   // default. Room voting hides renderers before cleanup runs.
   bool restore;fx.Hidden.TryGetValue(renderer,out restore);
   fx.Hidden[renderer]=restore||renderer.enabled;
   renderer.enabled=false;
  }
  foreach(var ps in obj.GetComponentsInChildren<ParticleSystem>(true))if(ps&&Visual(fx,ps.transform)){
   if(!fx.Stopped.ContainsKey(ps))fx.Stopped[ps]=ps.isPlaying;
   // Stop each identified emitter only. Its unrelated child effects retain
   // their own FSM and particles, including excluded lights and screen flashes.
   ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
  }
 }
 static bool Phase(Fx fx,Transform t,bool flight,bool particle){
  for(;t&&t!=fx.Hero.transform;t=t.parent){
   string n=CrystalDashRules.Name(t.name);
   // Bursts are one-shot effects driven by their own native actions. Cleanup
   // must not replay them, especially on every room entry.
   if(n.Contains("burst"))return !particle&&flight;
   if(CrystalDashRules.CrystalArt(t.name)||fx.ChargeArt.Contains(t.gameObject))return !flight;
   if(CrystalDashRules.Trail(t.name))return flight;
  }return false;
 }
 static void Rearm(PlayerSlot p,bool flight){
  if(p==null||!p.Hero||!p.Alive||!p.Ready||p.Hero.cState.dead||p.ArenaTransfer||TransitionVote.Holding(p))return;var fx=Get(p);fx.Active=true;fx.Flight=flight;
  var remove=fx.ResumedRenderers;remove.Clear();
  foreach(var pair in fx.Hidden){if(!pair.Key){remove.Add(pair.Key);continue;}
   var s=Plugin.Self?Plugin.Self.Session:null;if(s!=null&&s.Resolve(pair.Key.gameObject)!=p){remove.Add(pair.Key);continue;}
   // Track initially hidden effects for late native activation too, but never
   // restore a saved false value over another subsystem's valid activation.
   if(pair.Value&&Phase(fx,pair.Key.transform,flight,false)&&pair.Key.gameObject.activeInHierarchy){pair.Key.enabled=true;remove.Add(pair.Key);}
  }
  foreach(var renderer in remove)fx.Hidden.Remove(renderer);
  remove.Clear();var resumed=fx.ResumedParticles;resumed.Clear();var session=Plugin.Self?Plugin.Self.Session:null;
  foreach(var pair in fx.Stopped){var ps=pair.Key;
   if(!ps||session!=null&&session.Resolve(ps.gameObject)!=p){resumed.Add(ps);continue;}
   if(!Phase(fx,ps.transform,flight,true)||!ps.gameObject.activeInHierarchy)continue;
   // Enabling native emission does not restart a system stopped by cleanup.
   // Resume just that emitter, preserving its native emission/color/timing.
   // Inactive prefabs remain pending until the native activation action runs.
   if(!ps.isPlaying&&pair.Value&&ps.main.loop&&ps.emission.enabled){ps.Clear(false);ps.Play(false);}
   if(ps.isPlaying)resumed.Add(ps);
  }
  foreach(var ps in resumed)fx.Stopped.Remove(ps);
  resumed.Clear();
 }
 internal static void Charge(PlayerSlot p)=>Rearm(p,false);
 internal static void Flight(PlayerSlot p)=>Rearm(p,true);
 internal static void Tick(CoopSession s){
  foreach(var pair in effects){var fx=pair.Value;
   if(!fx.Hero||fx.Player==null||fx.Player.Hero!=fx.Hero||s==null||!s.Players.Contains(fx.Player)){expired.Add(pair.Key);continue;}
   bool invalid=!fx.Player.Alive||!fx.Player.Ready||fx.Player.Hero.cState.dead||fx.Player.ArenaTransfer||TransitionVote.Holding(fx.Player);
   string state=fx.Hero.superDash?fx.Hero.superDash.ActiveStateName:"";
   bool idle=!fx.Hero.cState.superDashing&&CrystalDashRules.Stopped(state);
   if(invalid||idle){bool resumed=fx.Active;foreach(var r in fx.Hidden.Keys)if(r&&r.enabled&&s.Resolve(r.gameObject)==fx.Player){resumed=true;break;}if(!resumed)foreach(var ps in fx.Stopped.Keys)if(ps&&ps.isPlaying&&s.Resolve(ps.gameObject)==fx.Player){resumed=true;break;}if(resumed)Stop(fx.Player);}
   else if(fx.Active&&fx.Stopped.Count>0)Rearm(fx.Player,fx.Flight);
  }
  foreach(var h in expired)effects.Remove(h);expired.Clear();
 }
 internal static void Clear(){
  foreach(var fx in effects.Values)if(fx.Hero&&fx.Player!=null&&fx.Player.Hero==fx.Hero){
   Stop(fx.Player);
   if(fx.Player.Alive&&!fx.Hero.cState.dead)Rearm(fx.Player,true);
  }
  effects.Clear();expired.Clear();
 }
}
}

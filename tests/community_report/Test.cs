using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using GlobalEnums;
using KO.HollowKnight8;

class Test {
 static int checks;
 static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
 static (PlayerSlot,PlayMakerFSM,SpriteFlash) Player(CoopSession s,int index,int health=1){
  var root=new GameObject("Knight P"+(index+1));var hero=root.Add(new HeroController());
  var effects=new GameObject("Charm Effects");root.Children.Add(effects);
  var fsm=effects.Add(new PlayMakerFSM());var flash=root.Add(new SpriteFlash{repeatFlash=true,flashColour=new(.71f,.18f,.18f)});
  var p=new PlayerSlot{Index=index,Hero=hero};p.Vitals.Health=health;p.Charms.Equipped[6]=true;s.Players.Add(p);
  fsm.OnDeactivate=()=>Check(PlayerContext.Current==p,"Fury cleanup used another player's context");
  return(p,fsm,flash);
 }
 static void Fury(){
  var s=new CoopSession();Plugin.Self=new(){Session=s};Time.unscaledTime=10;
  var (p1,f1,x1)=Player(s,0);var (p2,f2,x2)=Player(s,1);
  FuryRecovery.Tick(s);Check(f1.Deactivated==0&&f2.Deactivated==0,"valid one-mask Fury cancelled");
  p2.Down=true;p2.Vitals.Health=0;Time.unscaledTime++;
  FuryRecovery.Tick(s);Check(f2.Deactivated==1&&x2.Cancelled==1,"dead player's Fury did not end");
  Check(f1.Deactivated==0&&x1.Cancelled==0,"P2 death cancelled P1 Fury");
  p2.Down=false;p2.Vitals.Health=3;Time.unscaledTime++;
  FuryRecovery.Tick(s);Check(f2.Deactivated==1,"cleanup repeats native Deactivate every tick");
  // Native state can already be idle after a recovery, with an orphaned flash.
  f2.ActiveStateName="Pause";x2.repeatFlash=true;Time.unscaledTime++;
  FuryRecovery.Tick(s);Check(f2.Deactivated==1&&x2.Cancelled==2,"orphan Fury flash survives an idle FSM");
  f2.ActiveStateName="Stay Furied";x2.repeatFlash=true;x2.flashColour=new(1,1,1);Time.unscaledTime++;
  FuryRecovery.Tick(s);Check(f2.Deactivated==2&&x2.Cancelled==2,"non-Fury flash was cancelled");
  p1.Vitals.Health=1;p1.Vitals.Blue=2;Time.unscaledTime++;
  FuryRecovery.Tick(s);Check(f1.Deactivated==1,"blue masks incorrectly keep Fury active");
  var hero=p2.Hero;var (replacement,rf,rx)=Player(s,2,4);p2.Hero=replacement.Hero;s.Players.Remove(replacement);Time.unscaledTime++;
  rf.OnDeactivate=()=>Check(PlayerContext.Current==p2,"replacement hero uses old Fury owner");
  FuryRecovery.Tick(s);Check(rf.Deactivated==1&&rx.Cancelled==1,"replacement hero inherits cached Fury state");
  Check(PlayerContext.Current==null,"Fury cleanup leaked context");
  s.Active=false;f1.ActiveStateName="Stay Furied";Time.unscaledTime++;
  FuryRecovery.Tick(s);Check(f1.Deactivated==1,"inactive mod altered native Fury");
 }
 static IEnumerator Original(HeroController hero,HazardType type,float angle){throw new Exception("native whole-party hazard routine should stay bypassed");}
 static void Hazards(){
  var s=new CoopSession();Plugin.Self=new(){Session=s};Time.time=30;
  for(int index=0;index<8;index++){
   var(p,_,_)=Player(s,index,4);p.Hero.transform.position=new(index+1,20);p.Hero.transform.localScale=new(-1,1,1);
   var audio=p.Hero.gameObject.Add(new HeroAudioController());var flash=p.Hero.GetComponent<SpriteFlash>();
   int spawned=0;GameObject effect=null;
   p.Hero.spikeDeathPrefab=new GameObject("Spikes"){Factory=at=>throw new Exception("animated spike body must never spawn")};
   p.Hero.acidDeathPrefab=new GameObject("Acid"){Factory=at=>throw new Exception("animated acid body must never spawn")};
   var streaks=p.Hero.takeHitPrefab=new GameObject("Take Hit streaks"){Factory=at=>{
    Check(PlayerContext.Current==p,"brief feedback owner is wrong");spawned++;effect=new GameObject();effect.transform.position=at;return effect;
   }};
   var routine=ReportHooks.Run(Original,p.Hero,HazardType.SPIKES,.25f);
   Check(!routine.MoveNext()&&p.Hazard&&spawned==1,"spike hit lacks feedback or starts global death");
   Check(effect.transform.position.x==p.Hero.transform.position.x&&effect.transform.position.y==20,"spike effect at the wrong player");
   Check(effect.GetComponent<PlayMakerFSM>()==null,"hazard feedback adds an animated Knight FSM");
   s.Hazard(p);Check(spawned==1,"repeated hazard duplicated feedback");
   p.Hazard=false;ReportHooks.Run(Original,p.Hero,HazardType.ACID,0);
   Check(spawned==2&&effect.transform.position.x==p.Hero.transform.position.x,"acid lacks the same brief actor-local streaks");
   p.Hazard=false;s.Data.hasAcidArmour=true;ReportHooks.Run(Original,p.Hero,HazardType.ACID,0);
   Check(spawned==2&&!p.Hazard,"acid immunity produced damage feedback");s.Data.hasAcidArmour=false;
   p.Hero.takeHitPrefab=null;
   ReportHooks.Run(Original,p.Hero,HazardType.PIT,0);Check(p.Hazard&&audio.Hits==1&&flash.White==1,"missing hit prefab fallback lacks feedback");
   p.Hero.takeHitPrefab=streaks;
   p.Hazard=false;p.Down=true;HazardFeedback.Play(p,HazardType.SPIKES);Check(spawned==2,"downed player spawned another hazard effect");
   p.Down=false;p.Vitals.Health=0;ReportHooks.Run(Original,p.Hero,HazardType.SPIKES,0);
   Check(p.Down&&spawned==3,"fatal direct hazard lost its native effect");
   p.Down=false;p.Hazard=false;p.Vitals.Health=3;Plugin.Self.Performance.HazardEffects=false;
   ReportHooks.Run(Original,p.Hero,HazardType.SPIKES,0);
   Check(p.Hazard&&spawned==3,"disabled feedback either spawned effects or disabled hazard recovery");
   p.Hazard=false;p.Vitals.Health=0;ReportHooks.Run(Original,p.Hero,HazardType.SPIKES,0);
   Check(p.Down&&spawned==3,"disabled feedback changed fatal hazard behavior");
   Plugin.Self.Performance.HazardEffects=true;
   Check(PlayerContext.Current==null,"hazard feedback leaked context");
   Check(!Diagnostics.Messages.Any(m=>m.Contains("animated")),"forbidden death prefab was attempted then swallowed");
  }
 }
 sealed class AsyncRoute:GameManager.SceneLoadInfo {}
 static void Travel(){
  var s=new CoopSession();var p=new PlayerSlot();var gm=new GameManager();
  UnityEngine.SceneManagement.SceneManager.Name="Town";VoteProbe.Reset();
  var start=new GameManager.SceneLoadInfo{SceneName=StagTravelRules.Cinematic,EntryGateName="door1"};
  Check(VoteProbe.LegacyDirect(s,p,gm,start)&&VoteProbe.Queued==1&&gm.Starts==0,"negative control did not defer native stag departure");
  // Native departure can suspend the roster and clear that deferred load.
  VoteProbe.Reset();Check(gm.Starts==0,"negative control unexpectedly loaded a scene");
  for(int trip=0;trip<20;trip++){
   foreach(var route in new GameManager.SceneLoadInfo[]{start,new(){SceneName="Town",EntryGateName=StagTravelRules.ArrivalGate},new AsyncRoute{SceneName="Crossroads_47",EntryGateName=StagTravelRules.ArrivalGate}}){
    UnityEngine.SceneManagement.SceneManager.Name=route==start?"Crossroads_47":StagTravelRules.Cinematic;
    route.PreventCameraFadeOut=true;route.EntryDelay=.3f;
    if(!VoteProbe.Direct(s,p,gm,route))gm.BeginSceneTransition(route);
    Check(VoteProbe.Queued==0&&ReferenceEquals(gm.Received,route)&&route.PreventCameraFadeOut&&route.EntryDelay==.3f,"shared stag route was voted, cloned or modified");
   }
  }
  Check(gm.Starts==60,"repeated round trips lost a native transition");
  UnityEngine.SceneManagement.SceneManager.Name="Town";VoteProbe.Reset();
  var normal=new GameManager.SceneLoadInfo{SceneName="Crossroads_01",EntryGateName="left1"};
  Check(VoteProbe.Direct(s,p,gm,normal)&&VoteProbe.Queued==1,"ordinary doorway no longer waits for the party");
  VoteProbe.Reset();var gate=new TransitionPoint{targetScene=StagTravelRules.Cinematic,entryPoint="door1"};var body=new GameObject().Add(new Collider2D());
  Check(!VoteProbe.Gate(s,p,gate,body,()=>{})&&VoteProbe.Queued==0,"stag trigger queued a room vote");
  gate.targetScene="Crossroads_01";Check(VoteProbe.Gate(s,p,gate,body,()=>{})&&VoteProbe.Queued==1,"ordinary trigger no longer queues a vote");
  Check(!StagTravelRules.SharedRoute("Town","Crossroads_01","door_stagExit_extra"),"stag exception matches unrelated doors");
 }
 static void Main(){Fury();Hazards();Travel();Console.WriteLine("PASS "+checks+" production Fury cleanup, brief hazard feedback without animated bodies and stag vote/lifecycle checks. Effects, health IO and scenes use controlled doubles; no native Unity playthrough.");}
}

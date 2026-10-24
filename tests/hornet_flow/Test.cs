using System;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using KO.HollowKnight8;

class Test {
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static CoopSession Setup(){HornetEncounterFlow.Reset();Diagnostics.Lines.Clear();GameManager.instance=new();Time.time=Time.unscaledTime=10;var s=new CoopSession();for(int i=0;i<8;i++)s.Players.Add(new(){Index=i});Plugin.Self=new(){Session=s};return s;}
 static Fsm Fsm(string name,string go,params string[] states){var f=new Fsm{Name=name,GameObject=new(go)};foreach(string state in states)f.States.Add(new(){Name=state,Fsm=f});return f;}
 static void Enter(Fsm f,string name,PlayerSlot p=null){f.ActiveState=f.GetState(name);HornetEncounterFlow.ObserveState(f.ActiveState,p);}
 static Fsm Intro()=>Fsm("Encounter","Hornet Infected Knight Encounter","Idle","Point","Box Up","Dialogue","Box Down","Set Hero Active","Leap Antic","Start Fight","Destroy");
 static Fsm Corpse(float x,bool nativeEvents=false){var f=Fsm("Control","Corpse Hornet 1(Clone)","Set PD","Blow","Land","Check Pos","L","R","Jump","Throw Start","Throw","Yank","End");f.GameObject.transform.position=new(x);var c=new FloatCompare{Fsm=f};c.float1.Value=x;
  if(nativeEvents){c.lessThan=c.equal=FsmEvent.GetFsmEvent("L");c.greaterThan=FsmEvent.GetFsmEvent("R");}
  var choice=f.GetState("Check Pos");choice.Actions=new FsmStateAction[]{c,new GetPosition()};choice.Transitions=new[]{new FsmTransition{EventName="L",ToState="L"},new FsmTransition{EventName="R",ToState="R"}};return f;
 }
 static void Step(CoopSession s,float dt=.6f){Time.time+=dt;Time.unscaledTime+=dt;HornetEncounterFlow.Tick(s);}
 static void Main(){
  for(int actor=0;actor<8;actor++){
   var s=Setup();var p=s.Players[actor];var f=Intro();Enter(f,"Point",p);p.Hero.controlReqlinquished=true;
   foreach(string phase in new[]{"Point","Box Up","Dialogue","Box Down","Set Hero Active"}){
    Enter(f,phase,s.Players[(actor+1)%8]);Step(s,4);
    Check(HornetEncounterFlow.Resolve(f)==p&&HornetEncounterFlow.Holds(p),"native conversation changed owner or timed out at "+phase);
    for(int other=0;other<8;other++)if(other!=actor)Check(!HornetEncounterFlow.Holds(s.Players[other]),"conversation protected another actor");
   }
   Enter(f,"Leap Antic");Check(!HornetEncounterFlow.Holds(p)&&HornetEncounterFlow.Resolve(f)==p,"native return should end only the control hold");
   Enter(f,"Start Fight");Check(HornetEncounterFlow.Resolve(f)==null,"fight retained automatic conversation ownership");
  }
  foreach(string mode in new[]{"replacement","death","retire","disconnect","source-disabled","room","inactive","reset"}){
   var s=Setup();var p=s.Players[1];var f=Intro();Enter(f,"Point",p);
   switch(mode){case "replacement":p.Hero=new();break;case "death":p.Alive=false;break;case "retire":p.Retiring=true;break;case "disconnect":p.Connected=false;break;case "source-disabled":f.GameObject.activeInHierarchy=false;break;case "room":GameManager.instance.sceneName="Town";break;case "inactive":s.Active=false;break;case "reset":HornetEncounterFlow.Reset();break;}
   Check(HornetEncounterFlow.Resolve(f)==null&&!HornetEncounterFlow.Holds(p),"invalid conversation claim survived "+mode);
  }
  foreach(float x in new[]{16f,27.3f,37f}){
   var s=Setup();s.Data.hornet1Defeated=true;var f=Corpse(x);Enter(f,"Check Pos");var comparison=(FloatCompare)f.ActiveState.Actions[0];comparison.OnEnter();
   Check(comparison.Finished&&f.ActiveStateName=="Check Pos"&&f.Events.Count==0,"published missing-event native FloatCompare did not reproduce the stalled decision");
   Step(s,.49f);Check(f.ActiveStateName=="Check Pos","fallback interrupted native decision before grace");Step(s,.02f);
   Check(f.ActiveStateName==(x<=27.3f?"L":"R")&&f.Events.Count==1,"corpse decision did not choose native side");
   Check(!s.Data.hasDash&&s.Data.hornet1Defeated,"position repair directly changed progression");Step(s,30);Check(f.Events.Count==1,"position repair repeated an event");
   // Continue the published native outgoing graph. Event/render/animation
   // delivery is controlled: the repair itself must not emit HORNET LEAVE.
   Check(!f.Events.Contains("HORNET LEAVE"),"repair bypassed native departure");
  }
  foreach(float x in new[]{16f,27.3f,37f}){
   var s=Setup();s.Data.hornet1Defeated=true;var f=Corpse(x,true);Enter(f,"Check Pos");((FloatCompare)f.ActiveState.Actions[0]).OnEnter();string state=f.ActiveStateName;int events=f.Events.Count;Step(s,5);
   Check(state==(x<=27.3f?"L":"R")&&f.ActiveStateName==state&&f.Events.Count==events,"working native choice changed or repeated");
  }
  foreach(string guard in new[]{"pause","loading","gameplay","wipe","not-defeated","room","inactive","source-disabled","changed-state","changed-threshold","variable-threshold","wrong-variable","disabled-compare","missing-branch","nan","reset","other-corpse","other-fsm","modded-wait"}){
   var s=Setup();s.Data.hornet1Defeated=true;var f=Corpse(20);var c=(FloatCompare)f.GetState("Check Pos").Actions[0];
   if(guard=="other-corpse")f.GameObject.name="Corpse Hornet GG";if(guard=="other-fsm")f.Name="Other Control";
   Enter(f,"Check Pos");
   if(guard=="modded-wait")f.ActiveState.Actions=new FsmStateAction[]{c,new GetPosition(),new FsmStateAction()};
   switch(guard){case "pause":GameManager.instance.isPaused=true;break;case "loading":GameManager.instance.IsLoadingSceneTransition=true;break;case "gameplay":s.Gameplay=false;break;case "wipe":s.TeamWipe=true;break;case "not-defeated":s.Data.hornet1Defeated=false;break;case "room":GameManager.instance.sceneName="GG_Hornet_1";break;case "inactive":s.Active=false;break;case "source-disabled":f.GameObject.activeInHierarchy=false;break;case "changed-state":f.ActiveState=f.GetState("Land");break;case "changed-threshold":c.float2.Value=18;break;case "variable-threshold":c.float2.UseVariable=true;break;case "wrong-variable":c.float1.Name="Range";break;case "disabled-compare":c.Enabled=false;break;case "missing-branch":f.GetState("Check Pos").Transitions=Array.Empty<FsmTransition>();break;case "nan":f.GameObject.transform.position=new(float.NaN);break;case "reset":HornetEncounterFlow.Reset();break;}
   Step(s,10);Check(f.Events.Count==0,"corpse guard ignored "+guard);
  }
  Console.WriteLine("PASS "+checks+" Hornet flow checks: all eight intro owners; exact native FloatCompare IL reproduces the missing-event decision; both native branches, grace, filtering, lifecycle and no direct progression writes. Full Unity fight/animations/gates remain untested.");
 }
}

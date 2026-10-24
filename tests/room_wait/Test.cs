using System;using System.Linq;using UnityEngine;using UnityEngine.SceneManagement;using HutongGames.PlayMaker;using HutongGames.PlayMaker.Actions;using GlobalEnums;using KO.HollowKnight8;

static class Test {
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static CoopSession Start(int count=2){
  TransitionVote.Reset();RoomWaitVisibility.Uninstall();RoomWaitVisibility.Install();RoomWaitVisibility.Install();
  Check(On.HutongGames.PlayMaker.Fsm.Count==1&&On.CameraController.Count==1,"duplicate/missing fade hooks");
  Doorways.Reset();InteractionRouter.Reset();TransitionPoint.TransitionPoints.Clear();Diagnostics.Lines.Clear();
  SceneManager.Name="Town";Time.unscaledTime=10;Time.unscaledDeltaTime=.02f;
  GameManager.instance=new();GameCameras.instance=new();GameCameras.instance.cameraFadeFSM.SetState("Normal");
  BossSequenceController.IsInSequence=false;ScriptedParty.Active=false;CoopEnding.Active=false;ArenaGather.TransferActive=false;PvpMatch.Running=false;Charms.NativeMenuOpen=false;StagMenuRouting.HasOwner=false;DreamSequence.InStoryDream=false;
  var s=new CoopSession();Plugin.Self=new(){Session=s};
  for(int i=0;i<count;i++){var p=new PlayerSlot{Index=i};p.Hero.Body.velocity=new(i+1,2);p.Hero.Body.gravityScale=1+i*.1f;s.Players.Add(p);}
  return s;
 }
 static PlayMakerFSM Door(string scene="Room_Town_Stag_Station",string gate="door1",string state="Change Scene"){
  var f=new PlayMakerFSM();f.Fsm.GameObject.name="door_station";
  f.Fsm.ActiveState=new(){Name=state};f.Fsm.States=new[]{new FsmState{Name="Change Scene",Actions=new FsmStateAction[]{new BeginSceneTransition{sceneName=scene,entryGateName=gate}}}};
  Doorways.Seed(f);return f;
 }
 static GameManager.SceneLoadInfo Info(string scene="Room_Town_Stag_Station",string gate="door1")=>new(){SceneName=scene,EntryGateName=gate,PreventCameraFadeOut=true,WaitForSceneTransitionCameraFade=false,EntryDelay=.75f,Visualization=GameManager.SceneLoadVisualizations.Custom};
 static void NativeEntry(CoopSession s,PlayerSlot p,PlayMakerFSM door,GameManager.SceneLoadInfo info){
  InteractionRouter.Seed(door.Fsm,p,true);p.Hero.controlReqlinquished=true;
  GameCameras.instance.cameraFadeFSM.SetState("Black");GameManager.instance.cameraCtrl.SetMode(CameraController.CameraMode.FROZEN);
  Check(TransitionVote.Direct(s,p,GameManager.instance,info),"ordinary native door was not deferred");
 }
 static void NegativeControl(){
  var s=Start();RoomWaitVisibility.Uninstall();var door=Door();var p=s.Players[0];InteractionRouter.Seed(door.Fsm,p,true);p.Hero.controlReqlinquished=true;GameCameras.instance.cameraFadeFSM.SetState("Black");
  Check(LegacyVote.Direct(s,p,GameManager.instance,Info()),"test6 negative control did not queue");
  Time.unscaledTime=14.99f;LegacyVote.Tick(s);s.Players[1].Actions.up.IsPressed=true;
  Check(GameCameras.instance.cameraFadeFSM.ActiveStateName=="Black"&&InteractionRouter.Busy(door.Fsm)&&!Doorways.Tick(s),"negative control did not reproduce black screen/retained door ownership");
  Check(GameManager.instance.Loads==0,"negative control did not preserve five-second wait");LegacyVote.Reset();
 }
 static void PartyDoors(){
  foreach(int count in new[]{2,4,8})foreach(int first in Enumerable.Range(0,count)){
   var s=Start(count);var door=Door();var info=Info();var selected=s.Players[first];
   var velocity=selected.Hero.Body.velocity;float gravity=selected.Hero.Body.gravityScale;
   NativeEntry(s,selected,door,info);
   Check(TransitionVote.Pending&&TransitionVote.Holding(selected)&&GameManager.instance.Loads==0,"door loaded before party was ready");
   Check(GameCameras.instance.cameraFadeFSM.ActiveStateName=="Normal","pre-existing black fade remained during countdown");
   Check(GameManager.instance.cameraCtrl.mode==CameraController.CameraMode.LOCKED,"native door left party camera frozen");
   Check(!InteractionRouter.Busy(door.Fsm)&&InteractionRouter.ActivePlayer==null,"voter retained exclusive door interaction");
   Check(selected.InputBlocked&&selected.Hero.Body.isKinematic&&selected.Hero.Body.gravityScale==0,"voter was not held");
   Check(selected.Hero.Renderers.All(r=>!r.enabled),"voter was not hidden");
   Check(info.PreventCameraFadeOut&&!info.WaitForSceneTransitionCameraFade,"queued native info was mutated before commit");
   Check(TransitionVote.WaitingDoor(door.gameObject,info.SceneName,info.EntryGateName),"exact door was not reserved for joining");
   Check(!TransitionVote.WaitingDoor(new GameObject(),info.SceneName,info.EntryGateName)&&!TransitionVote.WaitingDoor(door.gameObject,info.SceneName,"other"),"waiting vote unlocked another door/entry");
   var fade=GameCameras.instance.cameraFadeFSM;int events=fade.Fsm.Events;
   foreach(string evt in new[]{"FADE OUT","FADE OUT INSTANT","FADE SCENE OUT","FADE SCENE OUT INSTANT"})fade.Fsm.Event(evt);
   GameManager.instance.cameraCtrl.FadeOut(CameraFadeType.LEVEL_TRANSITION);
   Check(fade.Fsm.Events==events&&fade.ActiveStateName=="Normal"&&GameManager.instance.cameraCtrl.Fades==0,"late transition fade blacked out waiting room");
   foreach(var p in s.Players.Where(p=>p!=selected)){
    Check(!p.InputBlocked&&!p.Hero.Body.isKinematic&&p.Hero.Renderers[0].enabled,"unvoted player was blocked/hidden");
    p.Hero.transform.position=new(.25f,0);p.Actions.up.IsPressed=true;
    Check(Doorways.Tick(s)&&TransitionVote.Holding(p),"player could not join native door with its prompt already hidden");
    TransitionVote.Tick(s);Time.unscaledTime+=.1f;
   }
   Check(GameManager.instance.Loads==1&&!TransitionVote.Pending&&Time.unscaledTime<15,"all players did not finish wait early");
   Check(GameManager.instance.Entrant==selected&&PlayerContext.Current==null,"commit lost the entering actor or leaked player context");
   Check(ReferenceEquals(GameManager.instance.Last,info)&&!info.PreventCameraFadeOut&&info.WaitForSceneTransitionCameraFade,"commit did not restore ordinary loading fade on exact native info");
   Check(info.EntryDelay==.75f&&info.Visualization==GameManager.SceneLoadVisualizations.Custom,"commit lost native entry metadata");
   Check(GameManager.instance.cameraCtrl.Fades==1&&fade.Fsm.LastEvent=="FADE OUT","actual transition fade stayed suppressed after commit");
   Check(!selected.InputBlocked&&!selected.Hero.Body.isKinematic&&selected.Hero.Body.gravityScale==gravity&&selected.Hero.Body.velocity.x==velocity.x,"commit failed to restore voter body/input");
   Check(selected.Hero.Renderers[0].enabled&&!selected.Hero.Renderers[1].enabled,"commit changed renderer flags that were already hidden");
  }
 }
 static void GateAndLocks(){
  var s=Start();var door=Door();var native=Info();
  var gate=new TransitionPoint();TransitionPoint.TransitionPoints.Add(gate);
  using(PlayerContext.Enter(s.Players[0]))InteractionMotion.Door(GameManager.instance,native);
  Check(TransitionVote.WaitingDoor(gate.gameObject,native.SceneName,native.EntryGateName),"custom entry lost nearby gate identity");
  s.Players[1].Actions.up.IsPressed=true;
  Check(Doorways.Tick(s)&&TransitionVote.Holding(s.Players[1]),"gate did not admit other player without the consumed native prompt");
  TransitionVote.Tick(s);Check(GameManager.instance.Loads==1,"custom gate did not commit early");

  foreach(string state in new[]{"Locked","Closed","Bloqueado"}){
   s=Start();door=Door();NativeEntry(s,s.Players[0],door,Info());door.Fsm.ActiveState.Name=state;
   s.Players[1].Actions.up.IsPressed=true;
   Check(!Doorways.Tick(s)&&!TransitionVote.Holding(s.Players[1]),"waiting route bypassed locked door state "+state);
  }
  s=Start();door=Door();NativeEntry(s,s.Players[0],door,Info());var other=Door();door.transform.position=new(30,0);s.Players[1].Actions.up.IsPressed=true;
  Check(!Doorways.Tick(s)&&!TransitionVote.Holding(s.Players[1]),"same destination unlocked a different nearby door");
  s=Start();door=Door();NativeEntry(s,s.Players[0],door,Info());s.Players[1].Hero.transform.position=new(30,0);s.Players[1].Actions.up.IsPressed=true;
  Check(!Doorways.Tick(s),"pending door admitted distant player");
  s=Start();door=Door("Room_Ouiji");s.Data.jijiDoorUnlocked=true;NativeEntry(s,s.Players[0],door,Info("Room_Ouiji"));s.Data.jijiDoorUnlocked=false;s.Players[1].Actions.up.IsPressed=true;
  Check(!Doorways.Tick(s),"pending route bypassed native prerequisite");
  s=Start();door=Door();NativeEntry(s,s.Players[0],door,Info());s.Players[1].Hero.Interact=false;s.Players[1].Actions.up.IsPressed=true;
  Check(!Doorways.Tick(s),"door bypassed actor interaction eligibility");
 }
 static void FadesAndScope(){
  var s=Start();var door=Door();NativeEntry(s,s.Players[0],door,Info());
  var other=new Fsm();other.Event("FADE OUT");Check(other.Events==1,"unrelated FSM fade was suppressed");
  var foreign=new CameraController();foreign.FadeOut(CameraFadeType.LEVEL_TRANSITION);Check(foreign.Fades==1,"foreign camera controller was suppressed");
  foreach(var type in new[]{CameraFadeType.HERO_DEATH,CameraFadeType.HERO_HAZARD_DEATH,CameraFadeType.JUST_FADE,CameraFadeType.START_FADE}){
   var camera=GameManager.instance.cameraCtrl;int before=camera.Fades;camera.FadeOut(type);string state=GameCameras.instance.cameraFadeFSM.ActiveStateName;
   RoomWaitVisibility.Tick(s);Check(camera.Fades==before+1&&GameCameras.instance.cameraFadeFSM.ActiveStateName==state,"native hazard/death/story fade was overridden");
   GameCameras.instance.cameraFadeFSM.SetState("Normal");RoomWaitVisibility.Tick(s);
  }
  TransitionVote.Reset();var fade=GameCameras.instance.cameraFadeFSM;fade.SetState("Black");RoomWaitVisibility.Tick(s);Check(fade.ActiveStateName=="Black","visibility guard runs without a vote");
  s=Start();fade=GameCameras.instance.cameraFadeFSM;fade.Fsm.HasNormal=false;fade.SetState("Black");
  Check(TransitionVote.Direct(s,s.Players[0],GameManager.instance,Info()),"fallback vote failed");
  Check(GameManager.instance.Ins==1&&fade.ActiveStateName=="Normal","missing Normal state did not use native fade-in fallback");
  fade.SetState("Black");RoomWaitVisibility.Tick(s);RoomWaitVisibility.Tick(s);Check(GameManager.instance.Ins==1,"fallback repeatedly restarts native fade-in");
  foreach(string mode in new[]{"hazard","down","loading","scripted","ending","arena","boss","gameplay","inactive","wipe","stagmenu","stagscene","dream"}){
   s=Start();Check(TransitionVote.Direct(s,s.Players[0],GameManager.instance,Info()),"scope fixture did not vote");fade=GameCameras.instance.cameraFadeFSM;fade.SetState("Black");
   switch(mode){case "hazard":s.Players[1].Hazard=true;break;case "down":s.Players[1].Down=true;break;case "loading":GameManager.instance.IsLoadingSceneTransition=true;break;case "scripted":ScriptedParty.Active=true;break;case "ending":CoopEnding.Active=true;break;case "arena":ArenaGather.TransferActive=true;break;case "boss":BossSequenceController.IsInSequence=true;break;case "gameplay":s.Gameplay=false;break;case "inactive":s.Active=false;break;case "wipe":s.TeamWipe=true;break;case "stagmenu":StagMenuRouting.HasOwner=true;break;case "stagscene":SceneManager.Name=StagTravelRules.Cinematic;break;case "dream":DreamSequence.InStoryDream=true;break;}
   RoomWaitVisibility.Tick(s);fade.Fsm.Event("FADE OUT");Check(fade.ActiveStateName=="FADE OUT","fade was overridden outside ordinary wait: "+mode);
  }
  RoomWaitVisibility.Uninstall();RoomWaitVisibility.Uninstall();Check(On.CameraController.Count==0&&On.HutongGames.PlayMaker.Fsm.Count==0,"fade hooks survived uninstall");
 }
 static void TimingAndRelease(){
  var s=Start(4);TransitionVote.Direct(s,s.Players[0],GameManager.instance,Info());
  Time.unscaledTime=14.99f;TransitionVote.Tick(s);Check(GameManager.instance.Loads==0&&TransitionVote.Progress>.99f,"timeout fired before five seconds");
  Time.unscaledTime=15.001f;TransitionVote.Tick(s);Check(GameManager.instance.Loads==1,"timeout did not commit");
  s=Start(4);TransitionVote.Direct(s,s.Players[0],GameManager.instance,Info("A"));TransitionVote.Direct(s,s.Players[1],GameManager.instance,Info("B"));TransitionVote.Direct(s,s.Players[2],GameManager.instance,Info("B"));Time.unscaledTime=15;TransitionVote.Tick(s);Check(GameManager.instance.Last.SceneName=="B","majority route was not selected");
  s=Start(4);TransitionVote.Direct(s,s.Players[0],GameManager.instance,Info("A"));TransitionVote.Direct(s,s.Players[1],GameManager.instance,Info("B"));Time.unscaledTime=15;TransitionVote.Tick(s);Check(GameManager.instance.Last.SceneName=="A","tie did not preserve first entry");
  foreach(string invalid in new[]{"dead","unready","disconnect","removed","reset","wipe"}){
   s=Start();var p=s.Players[0];TransitionVote.Direct(s,p,GameManager.instance,Info());
   switch(invalid){case "dead":p.Alive=false;break;case "unready":p.Ready=false;break;case "disconnect":p.Connected=false;break;case "removed":s.Players.Remove(p);break;case "reset":TransitionVote.Reset();break;case "wipe":s.TeamWipe=true;break;}
   TransitionVote.Tick(s);Check(!TransitionVote.Pending&&!p.InputBlocked&&!p.Hero.Body.isKinematic&&p.Hero.Body.gravityScale==1,"invalid/reset vote left body or input held: "+invalid);Check(GameManager.instance.Loads==0,"invalidated vote still loaded a scene");
  }
  s=Start(4);TransitionVote.Direct(s,s.Players[0],GameManager.instance,Info());
  GameManager.instance.isPaused=true;for(int i=0;i<400;i++){Time.unscaledTime+=.02f;TransitionVote.Tick(s);}GameManager.instance.isPaused=false;
  Check(GameManager.instance.Loads==0&&TransitionVote.Progress<.001f,"pause consumed countdown");
  Plugin.Self.Panel=true;for(int i=0;i<400;i++){Time.unscaledTime+=.02f;TransitionVote.Tick(s);}Plugin.Self.Panel=false;
  Check(GameManager.instance.Loads==0&&TransitionVote.Progress<.001f,"settings panel consumed countdown");
  Time.unscaledTime+=5.01f;TransitionVote.Tick(s);Check(GameManager.instance.Loads==1,"paused vote did not resume");
  s=Start(4);TransitionVote.Direct(s,s.Players[0],GameManager.instance,Info());Time.unscaledTime+=.2f;TransitionVote.Direct(s,s.Players[1],GameManager.instance,Info());
  GameManager.instance.isPaused=true;for(int i=0;i<400;i++){Time.unscaledTime+=.02f;TransitionVote.Tick(s);}GameManager.instance.isPaused=false;s.Players[0].Connected=false;
  TransitionVote.Tick(s);Check(GameManager.instance.Loads==0&&TransitionVote.Pending&&TransitionVote.Progress<.001f,"removing a voter after pause restarted the clock from unadjusted timestamps");
  s=Start();var actor=s.Players[0];var f=Door();var npc=new Fsm{Name="npc_control"};InteractionRouter.Seed(f.Fsm,actor,true);InteractionRouter.Seed(npc,s.Players[1],false);
  TransitionVote.Direct(s,actor,GameManager.instance,Info());Check(!InteractionRouter.Busy(f.Fsm)&&InteractionRouter.Busy(npc)&&InteractionRouter.ActivePlayer==s.Players[1],"completed door closed another actor's dialogue");
 }
 static void Exclusions(){
  foreach(string target in new[]{"Cinematic_Stag_travel","GG_Atrium","Dream_01"}){
   var s=Start();GameCameras.instance.cameraFadeFSM.SetState("Black");
   Check(!TransitionVote.Direct(s,s.Players[0],GameManager.instance,Info(target)),"scripted/stag route entered ordinary vote: "+target);
   Check(!TransitionVote.Pending&&GameCameras.instance.cameraFadeFSM.ActiveStateName=="Black","excluded route had its fade cleared");
  }
  var party=Start();SceneManager.Name="Cinematic_Stag_travel";Check(!TransitionVote.Direct(party,party.Players[0],GameManager.instance,Info("Town","door_stagExit")),"stag arrival entered party vote");
  party=Start();var collider=new Collider2D();var edge=new TransitionPoint{isADoor=false,targetScene="Next",entryPoint="left1"};int commits=0;
  Check(TransitionVote.Gate(party,party.Players[0],edge,collider,()=>commits++),"ordinary edge did not defer");Check(TransitionVote.Gate(party,party.Players[1],edge,collider,()=>commits++),"second edge vote failed");TransitionVote.Tick(party);Check(commits==1&&!TransitionVote.Pending,"edge committed more than once");
 }
 static void Main(){NegativeControl();PartyDoors();GateAndLocks();FadesAndScope();TimingAndRelease();Exclusions();TransitionVote.Reset();RoomWaitVisibility.Uninstall();Console.WriteLine("PASS "+checks+" room-wait checks: test6 negative control, complete production vote, P1-P8 entry order, late/pre-existing fades, early join, exact door identity, locks, native fade commit, safety scopes, pause/reset/invalid voters and scripted/stag exclusions. Unity IO/loading are controlled doubles.");}
}

using System;using System.Linq;using UnityEngine;using HutongGames.PlayMaker;using HutongGames.PlayMaker.Actions;using KO.HollowKnight8;
class Test {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static CoopSession Setup(int count=4){HornetEncounterFlow.Reset();WorldRouting.Routed=null;InteractionRouter.Clear();LegacyRouter.Clear();BenchSeats.Seats.Clear();BenchSeats.CustomSeats.Clear();ActorRecovery.Resets.Clear();Diagnostics.Messages.Clear();InteractionControlLifecycle.Source=WorldRouting.Current=null;Time.frameCount=100;Time.unscaledTime=Time.time=10;var s=new CoopSession();for(int i=0;i<count;i++){var go=new GameObject("Knight P"+(i+1));var h=go.Add<HeroController>();go.Add<Rigidbody2D>();go.Add<HeroAnimationController>();h.transform.position=new(i+1,0);s.Players.Add(new(){Index=i,Hero=h});}Plugin.Self=new(){Session=s};GameManager.instance=new();UIManager.instance=new();ShopMenuRouting.Buyer=null;ShopMenuRouting.MenuVisible=false;PickupCard.Owner=null;StagMenuRouting.HasOwner=false;ScriptedParty.Active=CoopEnding.Active=PvpMatch.Running=DreamSequence.InStoryDream=false;EmergencyWarp.Held.Clear();TransitionVote.Held.Clear();Charms.ReleaseNative();return s;}
 static Fsm Npc(PlayerSlot p,bool bench=false){var f=new Fsm{Name=bench?"Bench Control":"npc_control",GameObject=new GameObject(bench?"RestBench":"Leg Eater")};f.ActiveState=new(){Fsm=f,Name="In Range",Actions=new FsmStateAction[]{new ListenForUp()}};f.Range.Add(p);InteractionRouter.Seed(f,p,bench);LegacyRouter.Seed(f,p,bench);return f;}
 static void Talking(Fsm f){f.ActiveState=new(){Fsm=f,Name="Conversation"};}
 static void Take(Fsm f,PlayerSlot p,bool velocity=false){var previous=InteractionControlLifecycle.Source;InteractionControlLifecycle.Source=f;try{On.HeroController.Take(p.Hero,velocity);}finally{InteractionControlLifecycle.Source=previous;}}
 static void Next(float dt=.3f){Time.frameCount++;Time.unscaledTime+=dt;Time.time+=dt;}
 static GameObject Inventory(){var gm=GameManager.instance;var go=new GameObject("Inventory");gm.inventoryFSM=go.Add<PlayMakerFSM>();gm.inventoryFSM.ActiveStateName="Opened";var pane=new GameObject("Charms");pane.transform.parent=go.transform;Charms.Visible(pane);return pane;}
 static void Main(){
  InteractionControlLifecycle.Install();InteractionControlLifecycle.Install();Check(On.HeroController.TakeCount==1&&On.HeroController.TakeVelocityCount==1&&On.HeroController.ReturnCount==1,"control hooks install once");
  // Regression observed in the new log: a live inventory is incorrectly
  // hidden as 'dialogue' because a nearby NPC borrows the bench/control flag.
  var s=Setup();var p=s.Players[1];var f=Npc(p);p.Vitals.AtBench=true;p.Hero.controlReqlinquished=true;LegacyRouter.Select(f);InteractionRouter.Select(f);
  Check(LegacyRouter.ActivePlayer==p,"alpha226 reproduces borrowed bench ownership");
  Check(InteractionRouter.ActivePlayer==null,"nearby NPC cannot inherit a bench hold");
  Inventory();Check(CharmMenuExit.NativeActive(true),"bench hold leaves charm panel visible");
  p.Vitals.AtBench=false;Check(LegacyRouter.ActivePlayer==p,"alpha226 inventory hold remains false NPC ownership");
  Check(InteractionRouter.ActivePlayer==null&&Charms.NativeMenuOpen,"inventory hold is not NPC ownership");
  var other=s.Players[2];f.Range.Add(other);other.Actions.up.IsPressed=true;InteractionRouter.Select(f);Check(InteractionRouter.Owner(f)==p,"inventory does not route another player's up into NPC");
  Charms.ReleaseNative();GameManager.instance.inventoryFSM.ActiveStateName="Closed";On.HeroController.Return(p.Hero);Next();
  // The real native take pins its exact owner, including nested callbacks
  // after RegainControl and while another player holds interaction input.
  Talking(f);Take(f,p);Check(InteractionRouter.ActivePlayer==p&&InteractionRouter.Holds(p),"native take owns NPC");
  Check(!InteractionRouter.Holds(other),"NPC only holds its actor");
  Inventory();Check(!CharmMenuExit.NativeActive(true),"real NPC dialogue still blocks charm overlay");Charms.ReleaseNative();GameManager.instance.inventoryFSM.ActiveStateName="Closed";
  On.HeroController.Return(p.Hero);InteractionRouter.Select(f);Check(InteractionRouter.Owner(f)==p&&InteractionRouter.ActivePlayer==p,"same-frame return cannot switch owner to held up");
  Next();Check(InteractionRouter.ActivePlayer==null,"native return ends claim next frame");Next();InteractionRouter.Select(f);Check(InteractionRouter.Owner(f)==other,"next request can select next actor after completion");
  // Source specificity, zero native behavior changes, both take variants.
  s=Setup();p=s.Players[1];f=Npc(p);Talking(f);Take(f,p,true);Check(InteractionRouter.Holds(p),"no-velocity control path owns actor");On.HeroController.Return(p.Hero);Next();Check(!InteractionRouter.Holds(p),"no-velocity hold releases");
  var ui=new Fsm{Name="Inventory",GameObject=new GameObject("UI"),Ui=true};Take(ui,p);Check(InteractionRouter.ActivePlayer==null,"UI relinquish cannot claim world NPC");On.HeroController.Return(p.Hero);
  var heroFsm=new Fsm{Name="Roar Lock",GameObject=p.Hero.gameObject,Actor=true};Take(heroFsm,p);Check(InteractionRouter.ActivePlayer==null,"hero/boss relinquish cannot claim world NPC");On.HeroController.Return(p.Hero);
  InteractionControlLifecycle.Source=f;On.HeroController.Take(p.Hero,cancel:true);Check(InteractionRouter.ActivePlayer==null,"no native handoff means no claim");
  // Exact action/state scopes restore their source even when nested original
  // calls throw. The pin survives a nested different-world callback.
  var outer=new Fsm{Name="Outer",GameObject=new GameObject("Outer")};InteractionControlLifecycle.Source=outer;
  Hooks.Call(action=>{Check(InteractionControlLifecycle.Source==f,"CallMethod source");On.HeroController.Take(p.Hero);},new(){Fsm=f});Check(InteractionControlLifecycle.Source==outer,"CallMethod source restores");
  var inner=Npc(s.Players[2]);Talking(inner);
  try{Hooks.Call(action=>{Hooks.Call(a=>throw new InvalidOperationException(),new(){Fsm=inner});},new(){Fsm=f});}catch(InvalidOperationException){}
  Check(InteractionControlLifecycle.Source==outer&&InteractionRouter.Owner(f)==p,"nested failed actions restore scope and owner");
  try{Hooks.State(state=>throw new InvalidOperationException(),new(){Fsm=f,Name="Conversation"});}catch(InvalidOperationException){}
  Check(InteractionControlLifecycle.Source==outer,"failed StateEnter restores exact source");InteractionControlLifecycle.Source=null;
  // A returned NPC prompt drops a stale claim, so the normal movement guard
  // can repair its actor. It must not restart a real ongoing conversation.
  s=Setup();p=s.Players[1];f=Npc(p);Talking(f);Take(f,p);p.Actions.moveVector.X=1;s.Check(p);Next(3);s.Check(p);Check(ActorRecovery.Resets.Count==0,"real dialogue not reset by movement input");
  f.ActiveState=new(){Fsm=f,Name="In Range",Actions=new FsmStateAction[]{new ListenForUp()}};Check(InteractionRouter.ActivePlayer==null,"idle NPC stops protecting stale control");
  s.Check(p);Next(3);s.Check(p);Check(ActorRecovery.Resets.SequenceEqual(new[]{p})&&!p.Hero.controlReqlinquished,"stale actor recovered in place");
  // A different player's live NPC should not immunize an orphan control lock.
  s=Setup();p=s.Players[1];other=s.Players[2];f=Npc(other);Talking(f);Take(f,other);p.Hero.controlReqlinquished=true;p.Actions.moveVector.X=1;s.Check(p);Next(3);s.Check(p);Check(ActorRecovery.Resets.SequenceEqual(new[]{p})&&InteractionRouter.Holds(other),"unrelated dialogue does not disable recovery");
  // Replaced/dead/disconnected actors and removed NPC roots cannot retain the
  // control ownership of a newly spawned body in the same slot.
  foreach(var mode in new[]{"replacement","disconnected","dead","root-disabled"}){s=Setup();p=s.Players[1];f=Npc(p);Talking(f);Take(f,p);if(mode=="replacement"){p.Hero.Exists=false;p.Hero=new GameObject("New body").Add<HeroController>();}if(mode=="disconnected")p.Connected=false;if(mode=="dead")p.Alive=false;if(mode=="root-disabled")f.GameObject.activeInHierarchy=false;Check(InteractionRouter.ActivePlayer==null&&!InteractionRouter.Holds(p),"lifecycle cleanup "+mode);}
  // Frozen guest bodies must regain their original kinematic state on every
  // inventory exit. Repeating panels and retaining a legitimate custom seat
  // are separate, and the renderer gate still recognizes a real shop.
  for(int count=2;count<=8;count++){s=Setup(count);Inventory();for(int cycle=0;cycle<20;cycle++){Charms.Freeze(s);Charms.Freeze(s);for(int i=1;i<count;i++)Check(s.Players[i].Hero.GetComponent<Rigidbody2D>().isKinematic,"inventory freezes guest exactly once");Charms.ReleaseNative();for(int i=1;i<count;i++)Check(!s.Players[i].Hero.GetComponent<Rigidbody2D>().isKinematic,"repeated inventory restores guest body");Inventory();}var body=s.Players[1].Hero.GetComponent<Rigidbody2D>();body.isKinematic=true;Charms.Freeze(s);Charms.ReleaseNative();Check(body.isKinematic,"inventory preserves legitimate seat snapshot");}
  s=Setup();p=s.Players[1];f=Npc(p,true);p.Vitals.AtBench=true;BenchSeats.Seats.Add(p);InteractionRouter.Select(f);Check(InteractionRouter.ActivePlayer==p&&InteractionRouter.ActiveIsBench,"native already seated join preserved");Inventory();Check(CharmMenuExit.NativeActive(true),"native bench charm overlay preserved");ShopMenuRouting.Buyer=p;ShopMenuRouting.MenuVisible=true;Check(!CharmMenuExit.NativeActive(true),"real shop retains menu input");
  s=Setup();p=s.Players[1];f=Npc(p,true);Talking(f);Take(f,p);p.Vitals.AtBench=true;BenchSeats.Seats.Add(p);On.HeroController.Return(p.Hero);Next();Check(InteractionRouter.ActivePlayer==p&&InteractionRouter.ActiveIsBench,"native bench remains seated after returning movement control");Next(5);Check(InteractionRouter.Holds(p),"native seat does not expire before leave");p.Vitals.AtBench=false;BenchSeats.Seats.Remove(p);Check(InteractionRouter.ActivePlayer==null,"native seat claim ends on real leave");
  Hornet();InteractionControlLifecycle.Uninstall();InteractionControlLifecycle.Uninstall();Check(On.HeroController.TakeCount==0&&On.HeroController.TakeVelocityCount==0&&On.HeroController.ReturnCount==0,"control hooks uninstall balanced");
  Console.WriteLine("PASS "+checks+" production interaction/control/charm/recovery checks; alpha226 false-ownership cases reproduced. Unity gameplay remains untested.");
 }
 static void Hornet(){
  for(int index=0;index<8;index++){
   var s=Setup(8);var p=s.Players[index];var other=s.Players[(index+1)%8];
   var f=new Fsm{Name="Encounter",GameObject=new GameObject("Hornet Infected Knight Encounter")};
   foreach(string name in new[]{"Point","Box Up","Dialogue","Box Down","Set Hero Active","Leap Antic","Start Fight"})f.States.Add(new(){Name=name,Fsm=f});
   f.ActiveState=f.GetState("Point");WorldRouting.Routed=p;
   Hooks.State(state=>{Check(PlayerContext.Current==p,"automatic intro initial context");On.HeroController.Take(p.Hero);},f.ActiveState);
   Check(HornetEncounterFlow.Holds(p)&&InteractionRouter.ActivePlayer==null,"automatic encounter was not held independently of NPC prompts");
   f.ActiveState=f.GetState("Dialogue");WorldRouting.Routed=other;p.Actions.moveVector.X=1;
   s.Check(p);Next(4);s.Check(p);Check(ActorRecovery.Resets.Count==0,"movement input broke Hornet's native dialogue control");
   other.Hero.controlReqlinquished=true;other.Actions.moveVector.X=1;s.Check(other);Next(4);s.Check(other);
   Check(ActorRecovery.Resets.SequenceEqual(new[]{other})&&p.Hero.controlReqlinquished,"Hornet dialogue immunized or reset another actor");
   f.ActiveState=f.GetState("Set Hero Active");Hooks.Call(action=>{
    Check(PlayerContext.Current==p,"Hornet RegainControl switched to the other touching actor");On.HeroController.Return(p.Hero);
   },new(){Fsm=f,methodName=new(){Value="RegainControl"}});
   Check(!p.Hero.controlReqlinquished&&!HornetEncounterFlow.Holds(other),"native intro returned the wrong hero");
   f.ActiveState=f.GetState("Leap Antic");Check(!HornetEncounterFlow.Holds(p),"intro hold outlived native control return");
   f.ActiveState=f.GetState("Start Fight");Hooks.State(state=>{},f.ActiveState);Check(HornetEncounterFlow.Resolve(f)==null,"native fight retained dialogue owner");
  }
 }

}

using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using KO.HollowKnight8;using UnityEngine;using HutongGames.PlayMaker;using MonoMod.Cil;using Mono.Cecil;using Mono.Cecil.Cil;
class Test {
 static int checks;static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static bool Guard(string name,params object[] args)=>(bool)typeof(PaleCourtInterop).GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
 static PlayerSlot Player(int index){var go=new GameObject();var hero=go.Add(new HeroController());hero.spellControl=go.Add(new PlayMakerFSM());return new(){Index=index,Hero=hero};}
 static void Main(string[] args){if(args.Length>0){PatchedRuntime();Console.WriteLine("PASS execution of emitted clone/start, ownership and uninitialized-state IL guards: "+checks);return;}UiOwnership();Interop();IL();Console.WriteLine("PASS UI ownership, clone lifecycle, spell callback and IL regression checks: "+checks);}
 static void UiOwnership(){
  UIManager.instance=new(){uiState=GlobalEnums.UIState.PLAYING};GameManager.instance=new(){inventoryFSM=new GameObject().Add(new PlayMakerFSM())};
  var p=Player(0);p.Vitals.AtBench=true;InteractionRouter.ActivePlayer=p;InteractionRouter.ActiveIsBench=true;Charms.NativePane=new GameObject();
  var inventory=new Fsm{Name="ui_list_getinput",GameObject=new GameObject()};inventory.GameObject.transform.name="Inventory";
  ShopMenuRouting.Reset();Check(!ShopMenuRouting.IsShopList(inventory),"A native input-list name/bench owner is not a shop");
  for(int frame=0;frame<100;frame++)ShopMenuRouting.Observe(inventory,p);
  Check(ShopMenuRouting.Buyer==null&&ShopMenuRouting.Binds==0,"Inventory cannot acquire shop input or block the custom panel");
  Check(CharmMenuExit.NativeActive(true),"Bench inventory keeps the custom panel");
  var shop=new Fsm{Name="ui_list_getinput",GameObject=new GameObject()};shop.GameObject.transform.name="UI List";shop.GameObject.transform.parent=new(){name="Shop Menu"};
  ShopMenuRouting.Observe(shop,p);Check(ShopMenuRouting.Buyer==p&&ShopMenuRouting.Binds==1,"Real shop lists still acquire their player's controls");Check(!CharmMenuExit.NativeActive(true),"Shop suppresses stale charm-pane visibility");
  var confirmation=new Fsm{Name="ui_list",GameObject=new GameObject()};ShopMenuRouting.Observe(confirmation,p);Check(ShopMenuRouting.confirmation==confirmation,"Shop yes/no confirmation preserves owner");
  ShopMenuRouting.Reset();var stock=new Fsm{Name="ui_list_getinput",GameObject=new GameObject()};stock.GameObject.Add(new ShopMenuStock());Check(ShopMenuRouting.IsShopList(stock),"ShopMenuStock recognizes shops with custom hierarchy names");
  ShopMenuRouting.owner=p;ShopMenuRouting.list=shop;shop.GameObject.activeInHierarchy=false;Check(CharmMenuExit.NativeActive(true),"A hidden stale shop does not block inventory");ShopMenuRouting.Reset();
  p.Vitals.AtBench=false;Check(CharmMenuExit.NativeActive(true),"Real bench ownership survives a stale atBench flag");InteractionRouter.ActiveIsBench=false;Check(!CharmMenuExit.NativeActive(true),"Active NPC dialogue remains modal");InteractionRouter.ActivePlayer=null;
  StagMenuRouting.HasOwner=true;Check(!CharmMenuExit.NativeActive(true),"Stag routes remain modal");StagMenuRouting.HasOwner=false;
  PickupCard.Owner=p;Check(!CharmMenuExit.NativeActive(true),"Pickup cards remain modal");PickupCard.Owner=null;
  GameManager.instance.inventoryFSM.ActiveStateName="Closed";Check(!CharmMenuExit.NativeActive(true),"Closed inventory cannot hijack input");GameManager.instance.inventoryFSM.ActiveStateName="Open";
  GameManager.instance.isPaused=true;Check(!CharmMenuExit.NativeActive(true),"Pause UI still wins");GameManager.instance.isPaused=false;
  GameManager.instance.IsLoadingSceneTransition=true;Check(!CharmMenuExit.NativeActive(true),"Scene transitions still win");GameManager.instance.IsLoadingSceneTransition=false;
  UnityEngine.EventSystems.EventSystem.current=new(){currentSelectedGameObject=new GameObject()};Check(!CharmMenuExit.NativeActive(true),"Unrelated selected UI is protected");UnityEngine.EventSystems.EventSystem.current=null;
 }
 static void Interop(){
  var p1=Player(0);var p2=Player(1);var session=new CoopSession{Primary=p1,Players=new(){p1,p2}};Plugin.Self=new(){Session=session};HeroController.instance=p1.Hero;
  Check(!Guard("ExtraHero",p1.Hero)&&Guard("ExtraHero",p2.Hero),"Pale Court Start skips clones only, never primary native initialization");
  var unregistered=Player(2);session.Cloning=unregistered;Check(Guard("ExtraHero",unregistered.Hero),"Awake-time clone without a registered hierarchy is recognized");session.Cloning=null;
  Check(!Guard("ExtraHero",unregistered.Hero),"Unrelated heroes are not skipped");
  var a=p1.Hero.gameObject.Add(new FiveKnights.CheckBloomStage());var b=p2.Hero.gameObject.Add(new FiveKnights.CheckBloomStage());
  Check(!Guard("WrongActor",a)&&Guard("WrongActor",b),"An extra Bloom hook cannot act on the primary's health/bench update");
  using(PlayerContext.Enter(p2))Check(Guard("WrongActor",a)&&!Guard("WrongActor",b),"Nested contexts activate only their own Bloom stage");
  Check(Guard("UninitializedBloom",b,0,null),"Disabled cloned Bloom does not dereference an uninitialized slash list");
  using(PlayerContext.Enter(p2)){Check(Guard("UninitializedBloom",b,0,null),"Null level-zero reset is skipped even for the correct clone");Check(!Guard("UninitializedBloom",b,1,new object()),"Initialized active Bloom is preserved");}
  session.Active=false;Check(!Guard("ExtraHero",p2.Hero)&&!Guard("UninitializedBloom",b,0,null),"Solo/inactive sessions retain provider behavior");session.Active=true;
  typeof(PaleCourtInterop).GetField("boonType",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,typeof(FiveKnights.BoonSpells));var boon=p2.Hero.gameObject.Add(new FiveKnights.BoonSpells());
  string[] names={"Fireball 1 SmallShots","Fireball 2 SmallShots","Q1 Land Plumes","Q2 Land Plumes","Scream Burst 1 Blasts","Scream Burst 2 Blasts"};
  p1.Hero.spellControl.FsmStates=names.Select((n,i)=>new FsmState{Name=n,Actions=new FsmStateAction[]{i%2==0?new FiveKnights.InvokeMethod():new FiveKnights.MethodAction(),new FsmStateAction()}}).ToArray();
  p2.Hero.spellControl.FsmStates=names.Select((n,i)=>new FsmState{Name=n,Actions=new FsmStateAction[]{i%2==0?null:new FiveKnights.MissingAction(),new FsmStateAction()}}).ToArray();
  PaleCourtInterop.RepairClone(session,p2);
  foreach(var state in p2.Hero.spellControl.FsmStates){Check(state.Actions[0]!=null&&state.Actions[0].State==state,"Missing constructor-only callback restored and bound to clone state");Check(state.Actions[1].GetType()==typeof(FsmStateAction),"Native serialized actions are untouched");state.Actions[0].OnEnter();Check(state.Actions[0].Finished,"Restored callback always finishes");}
  Check(boon.Casts.Count==6&&boon.Casts.All(c=>c.hero==p2.Hero),"All six restored spells execute against their caster, not P1");Check(boon.Casts.Select(c=>c.upgrade).SequenceEqual(new[]{false,true,false,true,false,true}),"Both spell upgrade levels are preserved");
  var existing=p2.Hero.spellControl.FsmStates[0].Actions[0];PaleCourtInterop.RepairClone(session,p2);Check(ReferenceEquals(existing,p2.Hero.spellControl.FsmStates[0].Actions[0]),"Valid provider actions are never replaced on repeated repair");
  boon.Throws=true;existing.Finished=false;existing.OnEnter();Check(existing.Finished&&Diagnostics.Errors==1&&HeroController.instance==p1.Hero,"Callback failure finishes the FSM and restores hero context");
  Check(!PaleCourtInterop.BoonState("Focus",out _,out _),"Unknown/custom focus states are not fabricated or changed");
 }
 static void IL(){
  using var assembly=AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);var module=assembly.MainModule;
  foreach(var patch in new (string name,ILContext.Manipulator body,string type,string method)[]{("Start",Patches.Start,"Amulets","On_HeroController_Start"),("Stage",Patches.Stage,"CheckBloomStage","CheckStage"),("Bloom",Patches.Bloom,"AbyssalBloom","SetLevel")}){
   var type=module.Types.Single(t=>t.FullName=="FiveKnights."+patch.type);
   var method=type.Methods.Single(m=>m.Name==patch.method);
   var original=method.Body.Instructions[0];int returns=method.Body.Instructions.Count(i=>i.OpCode==OpCodes.Ret);
   using var context=new ILContext(method);context.Invoke(patch.body);
   var branch=method.Body.Instructions.First(i=>i.OpCode==OpCodes.Brfalse||i.OpCode==OpCodes.Brfalse_S);
   Check(ReferenceEquals(branch.Operand,original),patch.name+" guard leaves the original provider body reachable");
   Check(method.Body.Instructions.Count(i=>i.OpCode==OpCodes.Ret)==returns+1,patch.name+" guard has an isolated early return");
   if(patch.name=="Start")Check(method.Body.Instructions.Any(i=>i.OpCode==OpCodes.Callvirt&&i.Operand is MethodReference r&&r.Name=="Invoke"),"Clone Start forwards the real original Start delegate exactly once");
  }
  var boonEnable=module.Types.Single(t=>t.FullName=="FiveKnights.BoonSpells").Methods.Single(m=>m.Name=="OnEnable");
  using(var context=new ILContext(boonEnable))context.Invoke(Patches.Boon);
  Check(boonEnable.Body.Instructions.Count(i=>i.OpCode==OpCodes.Dup)==1,"Boon template patch preserves the instantiated FSM on the stack");
  Check(boonEnable.Body.Instructions.Any(i=>i.OpCode==OpCodes.Call&&i.Operand is MethodReference r&&r.Name=="DiscardBoonTemplate"),"Boon template cleanup uses the public cross-assembly guard");
  assembly.Write(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"PatchedTest.dll"));
 }
 static void PatchedRuntime(){
  var p1=Player(0);var p2=Player(1);Plugin.Self=new(){Session=new(){Primary=p1,Players=new(){p1,p2}}};HeroController.instance=p1.Hero;
  var amulets=new FiveKnights.Amulets();int starts=0;amulets.On_HeroController_Start(_=>starts++,p2.Hero);Check(starts==1,"Patched clone forwards native Start once and skips repeated provider mutation");
  bool primary=false;try{amulets.On_HeroController_Start(_=>starts++,p1.Hero);}catch(Exception e){primary=e.Message=="provider body sentinel";}Check(primary&&starts==2,"P1 original native/provider Start is preserved");
  var stage=p2.Hero.gameObject.Add(new FiveKnights.CheckBloomStage());var check=stage.GetType().GetMethod("CheckStage",BindingFlags.Instance|BindingFlags.NonPublic);check.Invoke(stage,null);Check(true,"Patched duplicate stage hook cannot act on P1");
  bool own=false;using(PlayerContext.Enter(p2)){try{check.Invoke(stage,null);}catch(TargetInvocationException e){own=e.InnerException.Message=="provider stage sentinel";}}Check(own,"Correct actor still enters the provider stage body");
  var bloom=p2.Hero.gameObject.Add(new FiveKnights.AbyssalBloom());using(PlayerContext.Enter(p2)){bloom.SetLevel(0);Check(true,"Null cloned Bloom level-zero reset cannot throw");bool positive=false;try{bloom.SetLevel(1);}catch(Exception e){positive=e.Message=="provider bloom sentinel";}Check(positive,"Positive Bloom activation is never silently discarded");}
  var boon=p2.Hero.gameObject.Add(new FiveKnights.BoonSpells());
  var template=new GameObject{name="PV temporary"}.Add(new PlayMakerFSM{ActiveStateName="Clips ready"});boon.Template=template;
  using(PlayerContext.Enter(p2))boon.GetType().GetMethod("OnEnable",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(boon,null);
  Check(!template.gameObject.activeInHierarchy&&template.gameObject.Destroyed,"Copied temporary boss is disabled and scheduled for cleanup");
  Check(boon.ReadState=="Clips ready"&&boon.Last==template,"Patched provider still reads its copied actions and clips");
  var world=new GameObject{name="Real PV"}.Add(new PlayMakerFSM());PaleCourtInterop.DiscardBoonTemplate(world);
  Check(!world.gameObject.Destroyed&&world.gameObject.activeInHierarchy,"Real arena bosses are never touched");
  Plugin.Self.Session.Active=false;var solo=new GameObject{name="Solo template"}.Add(new PlayMakerFSM());solo.transform.parent=p1.Hero.transform;
  PaleCourtInterop.DiscardBoonTemplate(solo);Check(!solo.gameObject.Destroyed,"Inactive co-op does not modify the provider's solo behavior");
 }
}

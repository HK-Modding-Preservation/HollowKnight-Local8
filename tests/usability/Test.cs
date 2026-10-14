using System;using System.Linq;using System.Collections;using UnityEngine;using InControl;using KO.HollowKnight8;
static class Test {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static bool Same(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;
 static CoopSession World(int count=2){
  PvpMatch.Stop(false);ArenaUnderTest.Reset();MenuInputRecovery.Reset();JumpEffectLifetime.Uninstall();
  UnityEngine.Object.All.Clear();Time.unscaledTime+=100;Time.time=Time.unscaledTime;Time.frameCount+=10;
  var s=new CoopSession();Plugin.Self=new Runtime{Session=s};GameManager.instance=new();UIManager.instance=new();Local8Mod.Settings=new();
  for(int i=0;i<count;i++){var p=new PlayerSlot{Index=i,Hero=new GameObject("P"+(i+1)).Add<HeroController>(),SafePoint=new(i*3,0),PreviousSafePoint=new(i*3-1,0)};p.Hero.transform.position=p.SafePoint;p.Vitals.Health=4+i;p.Vitals.MaxHealth=12;s.Players.Add(p);}
  GameManager.instance.inputHandler.inputActions=s.Primary.Actions;ShopMenuRouting.Buyer=null;ShopMenuRouting.MenuVisible=false;StagMenuRouting.HasOwner=false;PickupCard.Owner=null;Charms.NativeMenuOpen=false;RoleSystem.JoiningMenu=false;InteractionRouter.ActivePlayer=null;InteractionRouter.ActiveIsBench=false;InteractionRouter.Closed=0;BenchSeats.Sitting.Clear();BenchSeats.Leaves=0;ActorRecovery.Resets=0;EmergencyWarp.Warping.Clear();ScriptedParty.Active=CoopEnding.Active=TransitionVote.Pending=false;BossSequenceController.IsInSequence=ArenaGather.EngagedBossAlive=false;BossSceneController.Instance=null;Application.Available=true;PvpSpellOnly.Issue="";PvpArena.Options=new();PvpArena.Travels=0;ActionKeys.Capturing=0;MenuInput.Waiting=false;KeypadInput.Down.Clear();PlayerContext.Current=null;Diagnostics.Messages.Clear();return s;
 }
 static void Air(PlayerSlot p,float height){p.Hero.transform.position=new(p.SafePoint.x,height);p.Hero.cState.onGround=false;}
 static void Bench(PlayerSlot p,bool active=false){BenchSeats.Sitting.Add(p);p.Vitals.AtBench=p.Vitals.DisablePause=true;p.Hero.cState.nearBench=true;p.Hero.controlReqlinquished=true;if(active){InteractionRouter.ActivePlayer=p;InteractionRouter.ActiveIsBench=true;}}
 static void Duel(){
  foreach(int n in new[]{2,4,5,8}){
   var s=World(n);Bench(s.Primary,true);for(int i=1;i<n;i++)Air(s.Players[i],i%2==0?-4:6);
   if(n>4){var p=s.Players[4];s.InvalidFloors.Add(p.SafePoint.x);}
   if(n>5){var p=s.Players[5];p.HasSafePoint=p.HasPreviousSafePoint=false;}
   var vitals=s.Players.Select(p=>(p.Vitals.Health,p.Vitals.Blue,p.Vitals.Soul,p.Vitals.Reserve)).ToArray();
   Check(PvpMatch.TestBegin(s),$"{n} airborne/bench players start duel");Check(PvpMatch.Running&&PvpMatch.round==1,"countdown created");Check(BenchSeats.Leaves==1&&InteractionRouter.ActivePlayer==null,"bench released after all validation");
   var points=s.Players.Select(p=>PvpMatch.Saved(p).Position).ToArray();
   for(int i=0;i<n;i++){var p=s.Players[i];Check(s.IsSafe(p.Hero.transform.position,p),"duel placed on validated floor");Check(!p.Vitals.AtBench&&!p.Hero.controlReqlinquished,"duel actor free");Check(p.Vitals.Health==p.CurrentMaxHealth&&p.Vitals.Soul==99,"duel resource setup unchanged");}
   PvpMatch.Stop(true);Check(!PvpMatch.Running,"duel stops");
   for(int i=0;i<n;i++){var p=s.Players[i];Check(Same(p.Hero.transform.position,points[i]),"duel returns to safe position");Check((p.Vitals.Health,p.Vitals.Blue,p.Vitals.Soul,p.Vitals.Reserve)==vitals[i],"duel restores resources");}
   Check(s.Data.geo==777&&s.Data.equippedCharms.SequenceEqual(new[]{1,6}),"adventure progression and charms retained");
  }
  foreach(string bad in new[]{"disconnected","loading","dead","transition","control","dream","hazard","modal","scripted","vote","teams","floor"}){
   var s=World(3);Bench(s.Primary,true);var p=s.Players[2];
   switch(bad){case "disconnected":p.Connected=false;break;case "loading":p.SpawnPending=true;break;case "dead":p.Down=true;break;case "transition":p.Hero.cState.transitioning=true;break;case "control":p.Hero.controlReqlinquished=true;break;case "dream":p.Hero.IsDreamReturning=true;break;case "hazard":p.Hazard=true;break;case "modal":ShopMenuRouting.MenuVisible=true;break;case "scripted":ScriptedParty.Active=true;break;case "vote":TransitionVote.Pending=true;break;case "teams":foreach(var x in s.Players)x.Team=1;break;case "floor":s.FloorAvailable=false;break;}
   Check(!PvpMatch.TestBegin(s),bad+" still safely blocks PvP");Check(BenchSeats.Leaves==0&&s.Recoveries==0&&s.Primary.Vitals.AtBench,"failed request is transactional: "+bad);
  }
  Console.WriteLine("PASS duel 2/4/5/8 players, jumping/falling/bench, fallback ground, resources, transactional rejection");
 }
 static void Arena(){
  foreach(int n in new[]{2,4,5,8}){var s=World(n);Bench(s.Primary,true);foreach(var p in s.Players.Skip(1))Air(p,5);var health=s.Players.Select(p=>p.Vitals.Health).ToArray();var points=s.Players.Select(p=>p.SafePoint).ToArray();
   Check(ArenaUnderTest.StartRequest()&&ArenaUnderTest.Active,"arena accepts airborne roster");Check(PvpArena.Travels==1&&GameManager.instance.Saved==1,"arena saves adventure before travel");Check(BenchSeats.Leaves==1,"arena frees native bench");
   foreach(var p in s.Players){Check(Same(ArenaUnderTest.Saved(p),points[p.Index]),"arena captures validated return floor");p.Vitals.Health=1;p.Vitals.Soul=0;p.Vitals.AtBench=true;p.Hero.transform.position=new(50,0);}
   s.Data.geo=0;s.Data.equippedCharms.Clear();ArenaUnderTest.TestRestore();
   foreach(var p in s.Players){Check(p.Vitals.Health==health[p.Index]&&p.Vitals.Soul==63,"arena restores actual pre-duel resources");Check(!p.Vitals.AtBench&&!p.Vitals.DisablePause,"arena returns standing, no orphan bench pause flag");Check(Same(p.Hero.transform.position,points[p.Index]),"arena safe return");}
   Check(s.Data.geo==777&&s.Data.equippedCharms.SequenceEqual(new[]{1,6}),"arena world snapshot restored");
  }
  var mixed=World();Check(PvpMatch.TestBegin(mixed),"normal duel starts before arena switch");mixed.Players[1].Hero.transform.position=new(75,0);Check(ArenaUnderTest.StartRequest(),"switch duel to arena");Check(Same(ArenaUnderTest.Saved(mixed.Players[1]),new(3,0)),"arena switch retains original adventure destination");Check(mixed.Players[1].Vitals.Health==5,"arena captures restored adventure health");
  foreach(string bad in new[]{"floor","unavailable","boss","modal","teams"}){var s=World();Bench(s.Primary,true);if(bad=="floor")s.FloorAvailable=false;if(bad=="unavailable")Application.Available=false;if(bad=="boss")BossSceneController.Instance=new();if(bad=="modal")PickupCard.Owner=s.Primary;if(bad=="teams")foreach(var p in s.Players)p.Team=1;Check(ArenaUnderTest.StartRequest()&&!ArenaUnderTest.Active,"arena rejects "+bad);Check(BenchSeats.Leaves==0&&GameManager.instance.Saved==0&&PvpArena.Travels==0,"arena failed request has no side effects: "+bad);}
  Console.WriteLine("PASS arena entry/return 2/4/5/8 players, safe snapshot, world copy, normal-duel switch, rejection before save");
 }
 static void Escape(){
  var s=World();var gm=GameManager.instance;var input=gm.inputHandler;Plugin.Self.Panel=true;KeypadInput.Down.Add(KeyCode.Escape);
  MenuInput.Waiting=true;Check(!MenuInput.CloseOnEscape(true),"Escape first cancels menu key capture");MenuInput.Poll();Check(!MenuInput.Waiting&&Plugin.Self.Panel,"menu capture cancellation keeps F8");ActionKeys.Capturing=1;Check(!MenuInput.CloseOnEscape(true),"action capture retains Escape");ActionKeys.Capturing=0;
  Check(MenuInput.CloseOnEscape(true)&&MenuInput.EscapeConsumed,"F8 closes on physical Escape");Plugin.Self.SetPanel(false);var e=Hooks.TestToggle(x=>x.PauseGameToggle(),gm);while(e.MoveNext()){}Check(!gm.isPaused,"same Escape does not open pause");Hooks.TestInput(_=>{},input);Check(gm.Toggled==0,"same Escape fallback suppressed");Time.frameCount++;Check(!MenuInput.EscapeConsumed,"Escape consumption lasts one frame");
  Bench(s.Players[1]);Check(Hooks.TestFree(s,gm,input),"seated guest does not block primary pause");Bench(s.Primary);Check(!Hooks.TestFree(s,gm,input),"primary native bench remains protected");BenchSeats.Sitting.Remove(s.Primary);s.Primary.Vitals.AtBench=false;s.Primary.Hero.controlReqlinquished=false;Time.unscaledTime+=1;Check(Hooks.TestFree(s,gm,input),"pause resumes after modal settles");
  ShopMenuRouting.Buyer=s.Players[1];ShopMenuRouting.MenuVisible=false;Check(Hooks.TestFree(s,gm,input),"hidden stale shop owner does not block pause");ShopMenuRouting.MenuVisible=true;s.Data.disablePause=true;Check(!Hooks.TestFree(s,gm,input),"visible shop owns pause");ShopMenuRouting.Buyer=null;ShopMenuRouting.MenuVisible=false;Time.unscaledTime+=1;Hooks.TestInput(_=>{},input);Check(!s.Data.disablePause&&gm.Toggled==1&&gm.isPaused,"stale interaction flag repaired then keyboard fallback pauses");
  gm.isPaused=false;foreach(string gate in new[]{"dialogue","scripted","warp","loading","panel","charm","input","pauseAllowed"}){s=World();gm=GameManager.instance;input=gm.inputHandler;switch(gate){case "dialogue":InteractionRouter.ActivePlayer=s.Players[1];break;case "scripted":ScriptedParty.Active=true;break;case "warp":EmergencyWarp.Warping.Add(s.Primary);break;case "loading":s.Gameplay=false;break;case "panel":Plugin.Self.Panel=true;break;case "charm":Charms.NativeMenuOpen=true;break;case "input":input.acceptingInput=false;break;case "pauseAllowed":input.pauseAllowed=false;break;}Check(!Hooks.TestFree(s,gm,input),"pause still protects "+gate);}
  Console.WriteLine("PASS Escape capture/F8/same-frame handling, guest bench, stale shop/disablePause, true modal protections");
 }
 static void Neutral(){
  var s=World();var p=s.Primary;var view=new HeroActions();var privateCancel=view.menuCancel;var privateSubmit=view.menuSubmit;MenuInputRecovery.CaptureCreated(view);CharmNativeUi.view=view;view.menuCancel=p.Actions.menuCancel;view.jump=p.Actions.jump;view.attack=p.Actions.attack;view.cast=p.Actions.cast;view.pause=p.Actions.pause;
  var module=new GameObject().Add<HollowKnightInputModule>();module.MoveAction=view.moveVector;module.CancelAction=privateCancel;module.SubmitAction=privateSubmit;module.JumpAction=view.jump;module.AttackAction=view.attack;module.CastAction=view.cast;module.enabled=false;
  var foreign=new GameObject().Add<InControlInputModule>();foreign.CancelAction=new();var foreignAction=foreign.CancelAction;GameManager.instance.inputHandler.inputActions=view;Charms.NativeMenuOpen=true;MenuInputRecovery.Flush();Check(module.CancelAction==privateCancel,"neutral view retained while charm menu open");
  Charms.NativeMenuOpen=false;CharmNativeUi.view=null;Bench(s.Players[1],true);Time.unscaledTime+=1;MenuInputRecovery.Flush();Check(module.CancelAction==p.Actions.menuCancel&&module.SubmitAction==p.Actions.menuSubmit&&module.MoveAction==p.Actions.moveVector,"all captured private UI actions restored including disabled module");Check(module.JumpAction==p.Actions.jump&&foreign.CancelAction==foreignAction,"shared primary and foreign actions kept");Check(GameManager.instance.inputHandler.inputActions==p.Actions,"InputHandler neutral view restored");
  var late=new GameObject().Add<InControlInputModule>();late.CancelAction=privateCancel;Time.unscaledTime+=1;InteractionRouter.ActiveIsBench=false;MenuInputRecovery.Tick();Check(late.CancelAction==privateCancel,"real dialogue defers neutral repair");InteractionRouter.ActivePlayer=null;Time.unscaledTime+=1;MenuInputRecovery.Tick();Check(late.CancelAction==p.Actions.menuCancel,"late-added module discovered after modal exit");
  Console.WriteLine("PASS neutral private cancel/submit capture before aliases, disabled/late UI modules, foreign/shared inputs");
 }
 static void Jump(){
  var s=World();JumpEffectLifetime.Install();JumpEffectLifetime.Install();Check(On.JumpEffects.CaptureCount==1&&On.JumpEffects.UpdateCount==1,"JumpEffects hook installation idempotent");
  JumpEffects Fx(PlayerSlot p){var fx=new GameObject().Add<JumpEffects>();var body=p.Hero.gameObject.Add<Rigidbody2D>();fx.Bind(p.Hero,body);On.JumpEffects.Capture(fx);return fx;}
  var live=Fx(s.Primary);On.JumpEffects.Tick(live);Check(live.Updated==1&&live.gameObject.Recycled==0,"live owned FX unchanged");
  var orphan=Fx(s.Players[1]);s.Players[1].Hero.Exists=false;On.JumpEffects.Tick(orphan);Check(orphan.Updated==0&&orphan.gameObject.Recycled==1,"destroyed guest FX recycled without native exception");
  var bodyless=Fx(s.Primary);bodyless.InvalidateBody();On.JumpEffects.Tick(bodyless);Check(bodyless.gameObject.Recycled==1,"stale body recycled");
  var replaced=Fx(s.Primary);s.Primary.Hero=new GameObject().Add<HeroController>();On.JumpEffects.Tick(replaced);Check(replaced.gameObject.Recycled==1,"replaced actor FX recycled");
  var other=new PlayerSlot{Hero=new GameObject().Add<HeroController>()};var foreign=Fx(other);foreign.InvalidateBody();bool threw=false;try{On.JumpEffects.Tick(foreign);}catch(NullReferenceException){threw=true;}Check(threw&&foreign.gameObject.Recycled==0,"foreign native error remains visible");
  var reuse=Fx(s.Primary);reuse.gameObject.activeInHierarchy=false;On.JumpEffects.Tick(reuse);reuse.gameObject.activeInHierarchy=true;reuse.Bind(other.Hero,other.Hero.gameObject.Add<Rigidbody2D>());On.JumpEffects.Capture(reuse);s.Primary.Hero.Exists=false;On.JumpEffects.Tick(reuse);Check(reuse.gameObject.Recycled==0,"pooled FX rebound to foreign owner not suppressed");
  s.Primary.Hero=new GameObject().Add<HeroController>();var boundary=Fx(s.Primary);JumpEffectLifetime.Reset();Check(boundary.gameObject.Recycled==1,"scene reset finishes known pooled FX before dropping ownership");
  JumpEffectLifetime.Uninstall();Check(On.JumpEffects.CaptureCount==0&&On.JumpEffects.UpdateCount==0,"JumpEffects hook removal balanced");Console.WriteLine("PASS owned JumpEffects lifecycle, body/owner loss, pooling, foreign FX, scene reset, balanced hooks");
 }
 static void Main(){Duel();Arena();Escape();Neutral();Jump();Console.WriteLine("PASS alpha225 usability checks="+checks);}
}

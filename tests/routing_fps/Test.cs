using System;using System.Linq;using System.Collections.Generic;using System.Diagnostics;
using UnityEngine;using UnityEngine.EventSystems;using UnityEngine.SceneManagement;using HutongGames.PlayMaker;using HutongGames.PlayMaker.Actions;using InControl;using KO.HollowKnight8;
using Object=UnityEngine.Object;
static class Test {
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static void Fixture(){
  Recorder.Reset();Guards.Reset();Object.All.Clear();var s=new CoopSession();
  for(int i=0;i<8;i++)s.Players.Add(new(){Index=i,Hero=new GameObject("Knight"+i).Add<HeroController>()});
  Plugin.Self=new(){Session=s};Charms.NativeMenuOpen=Charms.Editing=StagMenuRouting.HasOwner=false;
  ShopMenuRouting.Buyer=InteractionRouter.ActivePlayer=null;GameManager.instance=new(){inputHandler=new(){inputActions=s.Primary.Actions}};
 }
 static string[] EventRun(bool legacy,int n,string failure=null){
  Fixture();Recorder.Failure=failure;var s=Plugin.Self.Session;
  var f=new Fsm{Name="Receiver",GameObject=new("receiver"),Player=n%5==0?null:s.Players[n%8],Enemy=n%3==0,Hud=n%7==0};
  var nested=new Fsm{Name="Nested",GameObject=new("nested"),Player=s.Players[(n+3)%8],Enemy=n%2==0};
  var e=new FsmEvent{Name=n%23==0?null:n%11==0?"HERO DAMAGED":n%13==0?"MP ADD":n%17==0?"HERO DEATH":"UNMATCHED"};var d=new FsmEventData{SentByFsm=f};
  if(n%4==0)PlayerContext.Current=s.Players[(n+1)%8];PlayerContext.TargetingEnemy=n%6==0;
  var before=PlayerContext.Current;bool beforeEnemy=PlayerContext.TargetingEnemy;
  switch(n%41){case 1:Guards.Death=true;break;case 2:Guards.Baldur=true;break;case 3:Guards.Challenge=true;break;case 4:Guards.Interaction=true;break;case 5:Guards.Denied=true;break;case 6:Guards.Life=true;break;}
  On.HutongGames.PlayMaker.Fsm.orig_ProcessEvent original=(self,evt,data)=>{
   Check(ReferenceEquals(self,f)&&ReferenceEquals(evt,e)&&ReferenceEquals(data,d),"original event arguments changed");Recorder.Add("native");Recorder.ThrowAt("native");
   if(n%9==0){if(legacy)LegacyHooks.Event((a,b,c)=>Recorder.Add("nested.native"),nested,new(){Name="CUSTOM"},null);else Hooks.Event((a,b,c)=>Recorder.Add("nested.native"),nested,new(){Name="CUSTOM"},null);}
  };
  try{if(legacy)LegacyHooks.Event(original,f,e,d);else Hooks.Event(original,f,e,d);}catch(Exception ex){Recorder.Add("exception:"+ex.GetType().Name+":"+ex.Message);}
  Check(PlayerContext.Current==before&&PlayerContext.TargetingEnemy==beforeEnemy,"event exception leaked context");
  return Recorder.Steps.ToArray();
 }
 static void Events(){
  for(int i=0;i<12000;i++){var a=EventRun(true,i);var b=EventRun(false,i);Check(a.SequenceEqual(b),"event trace differs at "+i);}
  foreach(string stage in new[]{"route","context","ui","life","bind","world.begin","native","world.end"})for(int i=0;i<100;i++)Check(EventRun(true,i*41,stage).SequenceEqual(EventRun(false,i*41,stage)),"unwinding differs at "+stage);
  foreach(bool legacy in new[]{true,false})for(int i=0;i<8;i++){
   Fixture();var f=new Fsm{Name="Initialize",GameObject=new("clone"),Player=Plugin.Self.Session.Players[i]};
   if(legacy){LegacyHooks.Awake(s=>Recorder.Add("orig.awake"),f);LegacyHooks.Enable(s=>Recorder.Add("orig.enable"),f);LegacyHooks.Start(s=>Recorder.Add("orig.start"),f);}
   else{Hooks.Awake(s=>Recorder.Add("orig.awake"),f);Hooks.Enable(s=>Recorder.Add("orig.enable"),f);Hooks.Start(s=>Recorder.Add("orig.start"),f);}
   string signature=string.Join("|",Recorder.Steps);if(legacy)initialization[i]=signature;else Check(signature==initialization[i],"clone initialization order differs");
  }
 }
 static readonly string[] initialization=new string[8];
 static Fsm Source(){
  return new(){Name="World",GameObject=new("pickup"),ActiveState=new(){Name="Idle",Transitions=new[]{new FsmTransition{EventName="GET",ToState="Acquire"}}},States=new[]{new FsmState{Name="Acquire",Actions=Array.Empty<FsmStateAction>()}}};
 }
 static string Probe(bool legacy,int n,out int queries){
  Fixture();AcquisitionUnderTest.Reset();LegacyAcquisition.Reset();Time.unscaledTime=1;SceneManager.Name="GG_Soul_Tyrant";
  var f=Source();if(legacy)LegacyAcquisition.Prime(f);else AcquisitionUnderTest.Prime(f);
  switch(n%10){
   case 0:f.GameObject.name="custom renamed";break;
   case 1:f.States=new[]{new FsmState{Name="Acquire",Actions=new[]{new SetPlayerDataInt{intName=new(){Value="fireballLevel"},value=new(){Value=2}}}}};break;
   case 2:f.GameObject.Add<HeroController>();break;
   case 3:f.GameObject.Add<HealthManager>();break;
   case 4:f.GameObject.Alive=false;break;
   case 5:SceneManager.Name="Crossroads_ShamanTemple";f.GameObject.name="Vengeful Spirit(Clone)";break;
   case 6:SceneManager.Name="Waterways_13";f.GameObject=new("Shiny Item Acid");break;
   case 7:SceneManager.Name="Ruins1_24";f.GameObject=new("Quake Item",new GameObject("new parent").transform);break;
   case 8:Time.unscaledTime=2;f.States[0].Actions=new[]{new SetPlayerDataBool{boolName=new(){Value="hasDash"},value=new(){Value=true}}};break;
   case 9:SceneManager.Name="Room_Fungus_Shaman";f.GameObject=new("child",new GameObject("Scream Item").transform);break;
  }
  GameObject.ParentQueries=0;
  if(legacy)LegacyAcquisition.BeforeEvent(f,new(){Name="GET"},new(){SentByFsm=f});else AcquisitionUnderTest.BeforeEvent(f,new(){Name="GET"},new(){SentByFsm=f});
  queries=GameObject.ParentQueries;return string.Join("|",Recorder.Steps);
 }
 static void Probes(){
  long oldQueries=0,newQueries=0;
  for(int i=0;i<10000;i++){var old=Probe(true,i,out int a);var next=Probe(false,i,out int b);oldQueries+=a;newQueries+=b;Check(old==next,"source miss/acquisition behavior differs at "+i);}
  Check(newQueries<oldQueries,"valid negative probes still repeat all ancestry work");
  for(int n=0;n<10000;n++){
   LegacyShade.Reset();ShadeUnderTest.Reset();Time.unscaledTime=1;SceneManager.Name="GG_Soul_Tyrant";var f=Source();
   Check(LegacyShade.Describe(f)==ShadeUnderTest.Describe(f),"initial shade classification differs");
   if(n%3==0)f.GameObject.Add<HeroController>();if(n%3==1){f.States=new[]{new FsmState{Actions=new[]{new SetPlayerDataInt{intName=new(){Value="quakeLevel"},value=new(){Value=2}}}}};}if(n%3==2)Time.unscaledTime=2;
   Check(LegacyShade.Describe(f)==ShadeUnderTest.Describe(f),"shade probe changed hero/reparent/new-state result");
  }
  Console.WriteLine("Controlled source probe parent queries: alpha220="+oldQueries+", alpha221="+newQueries+". Not Unity FPS.");
 }
 static string InputRun(bool legacy,int n,out int scans){
  Fixture();MenuInputRecovery.Reset();LegacyMenuInputRecovery.Reset();Time.unscaledTime=100;
  var view=CharmNativeUi.view=new HeroActions();if(legacy)LegacyMenuInputRecovery.Capture();else MenuInputRecovery.Capture();
  var foreign=new HeroActions();var s=Plugin.Self.Session;
  var generic=new GameObject("generic").Add<InControlInputModule>();var hk=new GameObject("hk").Add<HollowKnightInputModule>();
  generic.MoveAction=view.moveVector;generic.SubmitAction=view.menuSubmit;generic.CancelAction=foreign.menuCancel;
  hk.MoveAction=view.moveVector;hk.SubmitAction=view.menuSubmit;hk.CancelAction=foreign.menuCancel;hk.JumpAction=foreign.jump;hk.AttackAction=view.menuSubmit;hk.CastAction=view.menuSubmit;
  new GameObject("unrelated mod").Add<BaseInputModule>();generic.enabled=false;
  if(n%5==0)generic.gameObject.activeInHierarchy=false;if(n%7==0)hk.Alive=false;
  if(n%11==0)Charms.NativeMenuOpen=true;if(n%13==0)ShopMenuRouting.Buyer=s.Primary;if(n%17==0)StagMenuRouting.HasOwner=true;if(n%19==0)InteractionRouter.ActivePlayer=s.Primary;
  GameManager.instance.inputHandler.inputActions=view;Object.Searches=0;
  if(legacy)LegacyMenuInputRecovery.Tick();else MenuInputRecovery.Tick();scans=Object.Searches;
  string Ref(object o,object real,object neutral,object other)=>ReferenceEquals(o,real)?"real":ReferenceEquals(o,neutral)?"neutral":ReferenceEquals(o,other)?"foreign":"unexpected";
  string value=Ref(generic.MoveAction,s.Primary.Actions.moveVector,view.moveVector,foreign.moveVector)+Ref(generic.SubmitAction,s.Primary.Actions.menuSubmit,view.menuSubmit,foreign.menuSubmit)+Ref(generic.CancelAction,s.Primary.Actions.menuCancel,view.menuCancel,foreign.menuCancel)+Ref(hk.MoveAction,s.Primary.Actions.moveVector,view.moveVector,foreign.moveVector)+Ref(hk.SubmitAction,s.Primary.Actions.menuSubmit,view.menuSubmit,foreign.menuSubmit)+Ref(hk.CancelAction,s.Primary.Actions.menuCancel,view.menuCancel,foreign.menuCancel)+Ref(hk.JumpAction,s.Primary.Actions.jump,view.menuSubmit,foreign.jump)+Ref(hk.AttackAction,s.Primary.Actions.attack,view.menuSubmit,foreign.attack)+Ref(hk.CastAction,s.Primary.Actions.cast,view.menuSubmit,foreign.cast)+Ref(GameManager.instance.inputHandler.inputActions,s.Primary.Actions,view,foreign);
  // Fresh module after capture; it must be repaired on the very next scan.
  var added=new GameObject("added").Add<HollowKnightInputModule>();added.SubmitAction=view.menuSubmit;Time.unscaledTime+=1;
  Charms.NativeMenuOpen=StagMenuRouting.HasOwner=false;ShopMenuRouting.Buyer=InteractionRouter.ActivePlayer=null;
  if(legacy)LegacyMenuInputRecovery.Tick();else MenuInputRecovery.Tick();Check(added.SubmitAction==s.Primary.Actions.menuSubmit,"newly created module was missed");
  return value;
 }
 static void Inputs(){int old=0,next=0;for(int i=0;i<10000;i++){Check(InputRun(true,i,out int a)==InputRun(false,i,out int b),"menu input/foreign binding behavior changed");old+=a;next+=b;}Check(next*2==old,"input guard still scans world twice");Console.WriteLine("Controlled input recovery world scans: "+old+" -> "+next+"; bindings preserved.");}
 static Dictionary<string,string> Values(string value)=>value.Split(' ').Where(v=>v.Contains('=')).ToDictionary(v=>v.Split('=')[0],v=>v.Substring(v.IndexOf('=')+1));
 static void Metrics(){
  Fixture();ImpactMetrics.ResetWindow();DamageEventMetrics.Begin(null).Dispose();
  for(int i=0;i<5000;i++)DamageEventMetrics.Begin(null).Dispose();long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)DamageEventMetrics.Begin(null).Dispose();long idle=GC.GetAllocatedBytesForCurrentThread()-allocated;Console.WriteLine("Idle tracing allocation after warmup: "+idle+" bytes.");Check(idle==0,"idle tracing allocates");
  var f=Source();Recorder.Record=false;
  using(ImpactMetrics.Damage(Plugin.Self.Session.Primary))for(int i=0;i<20000;i++){
   var t=DamageEventMetrics.Begin(f);foreach(DamageEventMetrics.Phase phase in Enum.GetValues<DamageEventMetrics.Phase>())t.Mark(phase);t.Dispose();
  }
  var report=Values(DamageEventMetrics.Report());Check(report["event_calls"]=="20000"&&report["event_samples"]=="256","sample budget/counters wrong");
  double sum=new[]{"guard","route","context","bind","native","world","restore"}.Sum(p=>double.Parse(report["event_"+p+"_ms"],System.Globalization.CultureInfo.InvariantCulture));Check(Math.Abs(sum-double.Parse(report["event_sample_ms"],System.Globalization.CultureInfo.InvariantCulture))<.01,"phase sum is inconsistent");
  using(ImpactMetrics.Damage(Plugin.Self.Session.Primary)){for(int i=0;i<500;i++){var t=DamageEventMetrics.Begin(f);t.Mark(DamageEventMetrics.Phase.Guard);DamageEventMetrics.ResetWindow();t.Dispose();}}
  Check(Values(DamageEventMetrics.Report())["event_samples"]=="0","in-flight trace leaked into new window");
  using(ImpactMetrics.Damage(Plugin.Self.Session.Primary))for(int i=0;i<20000;i++){var t=DamageEventMetrics.Begin(f);t.Dispose();}Check(Values(DamageEventMetrics.Report())["event_samples"]=="256","reset/exception leaked sampling depth");
  DamageEventMetrics.ResetWindow();
  for(int i=0;i<256;i++){
   var receiver=new Fsm{Name=i==255?"LateExpensiveReceiver":"receiver"+i,GameObject=new("owner"+i)};
   var trace=new DamageEventMetrics.Trace(receiver);
   if(i==255){trace.Mark(DamageEventMetrics.Phase.Guard);trace.Mark(DamageEventMetrics.Phase.Route);trace.Mark(DamageEventMetrics.Phase.Context);trace.Mark(DamageEventMetrics.Phase.Bind);long until=Stopwatch.GetTimestamp()+Stopwatch.Frequency/100;while(Stopwatch.GetTimestamp()<until){}trace.Mark(DamageEventMetrics.Phase.Native);}
   trace.Dispose();
  }
  var late=Values(DamageEventMetrics.Report());Check(late["event_samples"]=="256"&&late["event_top1"].Contains("LateExpensiveReceiver"),"costly late receiver omitted from ranking");Check(late["event_slowest"].Contains("owner255/LateExpensiveReceiver")&&double.Parse(late["event_slowest_native_ms"],System.Globalization.CultureInfo.InvariantCulture)>0,"slowest sampled callback lacks owner/native attribution");
  var spawn=new SpawnMetrics(Plugin.Self.Session.Players[7],false);spawn.Mark("instantiate");spawn.Mark("rebind");spawn.Dispose();int logs=Diagnostics.Messages.Count;spawn.Dispose();Check(Diagnostics.Messages.Count==logs&&Diagnostics.Messages.Last().Contains("SPAWN PERF P8"),"spawn timer repeats or wrong player");
 }
 static void Texts(){for(int i=0;i<10000;i++){string value=i%3==0?"İ Ş SOUL ſ ß Σ ς K":"Knight P"+i;Check(InvariantText.Lower(value)==value.ToLowerInvariant(),"invariant Unicode matching changed");}string s=InvariantText.Lower("KNOWN SOUL");for(int i=0;i<10000;i++)Check(ReferenceEquals(s,InvariantText.Lower("KNOWN SOUL")),"steady lowercase conversion allocates");InvariantText.Clear();Check(InvariantText.Lower("SOUL")=="soul","reset poisons names");}
 static void Allocations(){
  Fixture();Recorder.Record=false;var f=new Fsm{Name="steady",GameObject=new("steady")};var evt=new FsmEvent{Name="CUSTOM"};On.HutongGames.PlayMaker.Fsm.orig_ProcessEvent orig=(a,b,c)=>{};
  for(int i=0;i<1000;i++){LegacyHooks.Event(orig,f,evt,null);Hooks.Event(orig,f,evt,null);}
  long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)LegacyHooks.Event(orig,f,evt,null);long old=GC.GetAllocatedBytesForCurrentThread()-before;
  before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)Hooks.Event(orig,f,evt,null);long next=GC.GetAllocatedBytesForCurrentThread()-before;
  Check(next<old,"event call still allocates the extra closure/delegate");Console.WriteLine("Controlled 10,000 event calls: allocated "+old+" -> "+next+" bytes (includes unchanged fixture scopes). Not Unity FPS.");
 }
 static string SpellRun(bool legacy,int n,out int positions){
  Fixture();Recorder.Record=false;var s=Plugin.Self.Session;Time.unscaledTime=5;
  for(int i=0;i<8;i++){var p=s.Players[i];p.Hero.transform.position=new(){x=i*.6f};p.Hero.cState.facingRight=(i+n)%2==0;p.Ready=(i+n)%3!=0;p.Alive=(i+n)%5!=0;PvpCombat.lastCast[i]=(i+n)%4==0?5:4;}
  var root=new GameObject(n%2==0?"Fireball(Clone)":"other effect");root.transform.position=new(){x=n%6*.5f};var body=root.Add<Rigidbody2D>();body.velocity=new(-12,7);
  var child=new GameObject("hitbox",root.transform);child.transform.position=root.transform.position;
  var a=new PvpCombat.Attack{Kind=(PvpKind)(n%4),Owner=s.Players[n%8],Collider=child.Add<Collider2D>()};a.Swing.touched=n%7==0?0x20000000:0;
  Transform.PositionReads=0;PvpCombat.Correct(s,a,legacy);positions=Transform.PositionReads;
  var tag=child.GetComponent<OwnerTag>();return a.Owner.Index+":"+a.Swing.touched+":"+body.velocity.x+":"+body.velocity.y+":"+root.transform.localScale.x+":"+tag?.Player.Index;
 }
 static void Spells(){long old=0,next=0;for(int i=0;i<10000;i++){Check(SpellRun(true,i,out int a)==SpellRun(false,i,out int b),"spell owner/velocity/touched flags differ");old+=a;next+=b;}Check(next<old,"non-spell attacks still scan cast ownership");Console.WriteLine("Controlled mixed attack hero/projectile position reads: "+old+" -> "+next+"; spell attribution/velocity unchanged.");}
 static void Main(){Events();Probes();Inputs();Metrics();Texts();Allocations();Spells();Console.WriteLine("PASS "+checks+" event-order/exception/nesting/acquisition/input/metrics/spell-owner checks against frozen alpha220. Controlled .NET fixtures, not native Unity FPS.");}
}

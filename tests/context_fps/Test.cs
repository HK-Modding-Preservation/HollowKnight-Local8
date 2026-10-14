using System;using System.Collections.Generic;using System.Diagnostics;using System.Reflection;using System.Linq;
using UnityEngine;using HutongGames.PlayMaker;using KO.HollowKnight8;
static class Test {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
 class RefBase{private GameObject hidden;internal GameObject Hidden=>hidden;public volatile GameObject Volatile;public readonly GameObject ReadOnly=new("readonly");public const string Literal="literal";}
 class RefOwner:RefBase{private static GameObject staticHidden;[ThreadStatic]private static GameObject threadHidden;internal static GameObject Static=>staticHidden;internal static GameObject Thread=>threadHidden;public GameObject Value;public int Number;}
 struct ValueOwner{public GameObject Value;}
 static Type Error(Action call){try{call();return null;}catch(Exception e){return e.GetType();}}
 static void Fields(){
  var flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  foreach(var field in new[]{typeof(RefBase).GetField("hidden",flags),typeof(RefBase).GetField("Volatile",flags),typeof(RefOwner).GetField("staticHidden",flags),typeof(RefOwner).GetField("threadHidden",flags),typeof(RefOwner).GetField("Value",flags)}){
   var fast=ReferenceField.For(field);var slow=new ReferenceField(field,false);var target=new RefOwner();Check(fast.FastGet&&fast.FastSet,"reference accessor not executable");
   for(int n=0;n<1000;n++){var value=n%3==0?null:new GameObject("v"+n);fast.SetValue(target,value);Check(ReferenceEquals(field.GetValue(target),value)&&ReferenceEquals(fast.GetValue(target),value),"fast field is stale or wrong");slow.SetValue(target,null);Check(fast.GetValue(target)==null,"fallback edit not visible immediately");if(value!=null){value.Alive=false;fast.SetValue(target,value);Check(ReferenceEquals(fast.GetValue(target),value),"destroyed Unity managed reference lost");}}
   Check(ReferenceEquals(fast,ReferenceField.For(field)),"field not reused");
   if(!field.IsStatic){Check(Error(()=>fast.GetValue(null))==Error(()=>field.GetValue(null)),"null-target exception differs");Check(Error(()=>fast.SetValue(new object(),null))==Error(()=>field.SetValue(new object(),null)),"wrong-target exception differs");}
   Check(Error(()=>fast.SetValue(target,new object()))==Error(()=>field.SetValue(target,new object())),"wrong value accepted or exception differs");
  }
  foreach(var field in new[]{typeof(RefOwner).GetField("Number",flags),typeof(RefBase).GetField("Literal",flags),typeof(ValueOwner).GetField("Value",flags)})Check(!ReferenceField.For(field).FastGet,"unsupported field didn't preserve reflection fallback");
  var readOnly=ReferenceField.For(typeof(RefBase).GetField("ReadOnly",flags));Check(readOnly.FastGet&&!readOnly.FastSet,"readonly field emits a setter");
  var tls=ReferenceField.For(typeof(RefOwner).GetField("threadHidden",flags));var mine=new GameObject("main");tls.SetValue(null,mine);object remote=null;
  var t=new System.Threading.Thread(()=>{Check(tls.GetValue(null)==null,"TLS crossed threads");tls.SetValue(null,new GameObject("worker"));remote=tls.GetValue(null);});t.Start();t.Join();Check(ReferenceEquals(tls.GetValue(null),mine)&&!ReferenceEquals(remote,mine),"thread-static field semantics lost");
 }
 class NativeRefs:FsmStateAction {
  private GameObject hidden;public HeroControllerStates State;public PlayerData Data;public HeroActions Actions;public GameObject Direct;public Component Component;
  public FsmGameObject Wrapped=new();public FsmObject Object=new();public FsmOwnerDefault Owner=new(){GameObject=new()};public FsmEventTarget Target=new(){gameObject=new(){GameObject=new()}};
  public readonly GameObject ReadOnly;public int Ignore=7;internal GameObject Hidden=>hidden;
  internal NativeRefs(GameObject root){hidden=Direct=root;ReadOnly=root;Component=root.transform;Wrapped.Value=root;Object.Value=root.transform;Owner.GameObject.Value=root;Target.gameObject.GameObject.Value=root;}
  internal string Signature(CoopSession s){return string.Join(";",new[]{hidden,Direct,Wrapped.Value,Owner.GameObject.Value,Target.gameObject.GameObject.Value}.Select(x=>s.Resolve(x)?.Index.ToString()??"x"))+";"+s.Resolve(Component)?.Index+";"+s.Resolve(Object.Value)?.Index+";"+ReferenceEquals(State,s.Players.Last().Hero.cState)+";"+Ignore;}
 }
 static CoopSession Fixture(){
  CharmExtensions.Reset();CharmIconList.Instance=new(){spriteList=new Sprite[44]};CharmIconList.Instance.spriteList[43]=new();CharmExtensions.Discover();CharmExtensions.Install();Time.frameCount++;
  var s=new CoopSession();for(int i=0;i<8;i++){var h=new GameObject("Hero"+i).Add<HeroController>();var p=new PlayerSlot{Index=i,Hero=h};s.Players.Add(p);p.Vitals.Health=9-i;p.Vitals.Soul=i*7;p.Charms.Initialize(s.Data,new[]{i+1,43});p.Charms.Extra.Clear();if(i%2==1)p.Charms.Extra.Add(43);p.Charms.Order.RemoveAll(id=>id>40);if(i%2==1)p.Charms.Order.Add(43);p.Actions.right.Value=i*.1f;new GameObject("FX",h.transform).Add<PlayMakerFSM>().FsmName="Native";}
  s.Primary.Apply(s.Data);Plugin.Self=new(){Session=s};HeroController.NativeAssign(s.Primary.Hero);var input=new InputHandler{inputActions=s.Primary.Actions,inputX=.8f,inputY=.2f};input.NativeAssign(s.Primary.Hero);GameManager.instance=new(){inputHandler=input};GameManager.instance.NativeAssign(s.Primary.Hero);HeroBox.inactive=false;
  FsmVariables.GlobalVariables=new(){GameObjectVariables=new[]{new FsmGameObject{Value=s.Primary.Hero.gameObject},new FsmGameObject{Value=s.Primary.Hero.transform.Find("FX").gameObject}},ObjectVariables=new[]{new FsmObject{Value=s.Primary.Hero.transform.Find("FX").gameObject.GetComponent<PlayMakerFSM>()}}};return s;
 }
 static void Binding(){
  var s=Fixture();for(int n=0;n<1000;n++){var source=s.Players[n%8];var to=s.Players[(n+3)%8];var a=new NativeRefs(source.Hero.gameObject);var b=new NativeRefs(source.Hero.gameObject);Hooks.RebindObject(a,to);LegacyHooks.RebindObject(b,to);
   Check(a.Signature(s)==b.Signature(s),"native action binding differs from alpha218");Check(ReferenceEquals(a.Hidden,to.Hero.gameObject)&&ReferenceEquals(a.State,to.Hero.cState)&&ReferenceEquals(a.Data,s.Data)&&ReferenceEquals(a.Actions,to.Actions),"private or per-player field was not rebound");Check(ReferenceEquals(a.ReadOnly,source.Hero.gameObject),"readonly reference rebound");
   var world=new GameObject("world");a.Wrapped.Value=b.Wrapped.Value=world;Hooks.RebindObject(a,source);LegacyHooks.RebindObject(b,source);Check(ReferenceEquals(a.Wrapped.Value,world)&&a.Signature(s)==b.Signature(s),"same-frame external edit overwritten");
  }
 }
 static string Snapshot(CoopSession s, bool legacy){var gm=GameManager.instance;var inp=gm.inputHandler;return (legacy?LegacyPlayerContext.Current?.Index:PlayerContext.Current?.Index)+"|"+(legacy?LegacyPlayerContext.TargetingEnemy:PlayerContext.TargetingEnemy)+"|"+s.Resolve(HeroController.instance)?.Index+"|"+s.Resolve(gm.hero_ctrl)?.Index+"|"+s.Resolve(inp.Hero)?.Index+"|"+ReferenceEquals(inp.State,inp.Hero.cState)+"|"+s.Data.health+"|"+s.Data.MPCharge+"|"+s.Data.Provider.GetValueOrDefault(43)+"|"+string.Join(",",s.Data.equippedCharms)+"|"+HeroBox.inactive+"|"+inp.inputX+"|"+inp.inputY+"|"+string.Join(",",FsmVariables.GlobalVariables.GameObjectVariables.Select(v=>s.Resolve(v.Value)?.Index));}
 static List<string> ContextRun(bool legacy){
  var s=Fixture();var random=new Random(9087);var log=new List<string>();
  IDisposable Enter(PlayerSlot p,bool enemy)=>legacy?LegacyPlayerContext.Enter(p,enemy):PlayerContext.Enter(p,enemy);
  void Scope(int level){var p=s.Players[random.Next(8)];bool enemy=random.Next(3)==0;using(Enter(p,enemy)){log.Add(Snapshot(s,legacy));s.Data.health=Math.Max(1,s.Data.health-1);s.Data.MPCharge+=2;s.Data.equippedCharm_12=!s.Data.equippedCharm_12;s.Data.SetBool("equippedCharm_43",random.Next(2)==0);HeroBox.inactive=!HeroBox.inactive;if(level>0)Scope(level-1);log.Add(Snapshot(s,legacy));}log.Add(Snapshot(s,legacy));}
  for(int i=0;i<1000;i++){Scope(i%5);Time.frameCount++;}
  for(int i=0;i<8;i++){s.Players[i].Retiring=true;Check(Enter(s.Players[i],false)==null,"retiring hero admitted");s.Players[i].Retiring=false;}
  try{using(Enter(s.Players[3],false))throw new InvalidOperationException("inside damage");}catch(InvalidOperationException){}
  log.Add(Snapshot(s,legacy));Check(HeroController.instance==s.Primary.Hero&&gmPrimary(s)&&PlayerContext.Current==null&&LegacyPlayerContext.Current==null,"exception leaked a hero context");
  return log;
 }
 static bool gmPrimary(CoopSession s)=>GameManager.instance.hero_ctrl==s.Primary.Hero&&GameManager.instance.inputHandler.Hero==s.Primary.Hero;
 static void Contexts(){var legacy=ContextRun(true);var fast=ContextRun(false);Check(legacy.Count==fast.Count,"context trace length differs");for(int i=0;i<legacy.Count;i++)Check(legacy[i]==fast[i],"nested health/controls/custom charm/global context differs at "+i);}
 static void Names(){
  var cache=new FsmTextCache();var f=new Fsm{Name="NPC_Control",GameObject=new GameObject("Inspect")};string first=cache.Kind(f);for(int i=0;i<10000;i++)Check(ReferenceEquals(first,cache.Kind(f)),"steady classification allocates");
  var random=new Random(621);for(int i=0;i<10000;i++){f.Name=i%7==0?null:"F"+random.Next(800)+" İ SOUL ſTAG";f.GameObject.name=i%5==0?null:"Object"+random.Next(800);f.GameObject.Alive=i%3!=0;Check(cache.Kind(f)==(f.Name+" "+(f.GameObject?f.GameObject.name:"")).ToLowerInvariant(),"rename/destroyed/Unicode classification differs");if(i%50==0)f.GameObject=new GameObject("pooled");}
  cache.Clear();Check(cache.Kind(f)==(f.Name+" "+(f.GameObject?f.GameObject.name:"")).ToLowerInvariant(),"reset changes names");
 }
 static Dictionary<string,string> Values(string text)=>text.Split(' ').Where(x=>x.Contains('=')).ToDictionary(x=>x.Split('=')[0],x=>x.Split('=')[1]);
 static void Metrics(){
  ImpactMetrics.ResetWindow();for(int i=0;i<100;i++)using(ImpactMetrics.PooledEffect()){}Check(Values(ImpactMetrics.Report())["damage_pool_calls"]=="0","idle effects unnecessarily measured");
  var s=Fixture();for(int i=0;i<8;i++)using(ImpactMetrics.Damage(s.Players[i]))using(ImpactMetrics.NativeDamage())for(int j=0;j<i;j++)using(ImpactMetrics.PooledEffect()){}
  var values=Values(ImpactMetrics.Report());Check(values["damage_calls"]=="8"&&values["damage_native_calls"]=="8"&&values["damage_pool_calls"]=="28","per-player effect counters wrong");Check(Values(ImpactMetrics.Report())["damage_calls"]=="0","window counters not reset");
  try{using(ImpactMetrics.Damage(s.Primary))using(ImpactMetrics.NativeDamage())throw new Exception();}catch(Exception){}using(ImpactMetrics.PooledEffect()){}values=Values(ImpactMetrics.Report());Check(values["damage_calls"]=="1"&&values["damage_native_calls"]=="1"&&values["damage_pool_calls"]=="0","exception leaked measured damage scope");
  long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)using(ImpactMetrics.PooledEffect()){}Check(GC.GetAllocatedBytesForCurrentThread()==before,"idle timer allocates");
 }
 static void Bench(){
  var owner=new RefOwner();var f=typeof(RefOwner).GetField("Value");var fast=ReferenceField.For(f);var value=new GameObject("value");for(int i=0;i<1000;i++){fast.SetValue(owner,value);fast.GetValue(owner);f.SetValue(owner,value);f.GetValue(owner);}
  var watch=Stopwatch.StartNew();for(int i=0;i<1000000;i++){f.SetValue(owner,value);f.GetValue(owner);}double slow=watch.Elapsed.TotalMilliseconds;watch.Restart();for(int i=0;i<1000000;i++){fast.SetValue(owner,value);fast.GetValue(owner);}double quick=watch.Elapsed.TotalMilliseconds;
  Console.WriteLine("Controlled .NET 8 field benchmark, 1,000,000 read/write pairs: reflection="+slow.ToString("0.0")+" ms; executable access="+quick.ToString("0.0")+" ms. Not Unity/Mono FPS evidence.");
 }
 static void Main(){Fields();Binding();Contexts();Names();Metrics();Bench();Console.WriteLine("PASS "+checks+" production reference/context/binding/custom charm/classification/impact checks. Unity frame performance requires the user test.");}
}

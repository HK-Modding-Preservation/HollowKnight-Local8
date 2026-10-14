using System;using System.Collections.Generic;using System.Diagnostics;using System.Linq;
using UnityEngine;using HutongGames.PlayMaker;using KO.HollowKnight8;using Mono.Cecil;using MonoMod.Cil;
static class Test {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
 static PlayMakerFSM Add(List<PlayMakerFSM> list,GameObject g,string name,Action<Fsm,FsmEvent,FsmEventData> receive=null){var p=new PlayMakerFSM{gameObject=g,Fsm=new(){Name=name,Receive=receive}};list.Add(p);return p;}
 // This is the control flow of the supplied native IL, including taking the
 // per-object Fsm snapshot before callbacks and looking up children live.
 static void Broadcast(Fsm sender,GameObject target,FsmEvent evt,FsmEventData data,bool children,bool exclude,List<PlayMakerFSM> registry,FsmRegistryIndex index){
  if(!target)return;
  var receivers=new List<Fsm>();var scan=index==null?registry:index.ForObject(registry,target);
  foreach(var component in scan)if(component&&component.gameObject==target)receivers.Add(component.Fsm);
  foreach(var receiver in receivers)if(!exclude||receiver!=sender)receiver.ProcessEvent(evt,data);
  if(children)for(int child=0;child<target.transform.childCount;child++)Broadcast(sender,target.transform.GetChild(child).gameObject,evt,data,true,exclude,registry,index);
 }
 static void Membership(){
  Check(FsmRegistryIndex.Supported,"version reader unavailable on test runtime");
  var random=new Random(857);var registry=new List<PlayMakerFSM>();var roots=Enumerable.Range(0,100).Select(i=>new GameObject("g"+i)).ToArray();var idx=new FsmRegistryIndex();
  for(int i=0;i<500;i++)Add(registry,roots[random.Next(roots.Length)],"f"+i);registry.Add(null);registry.Add(registry[37]);registry[11].Alive=false;
  for(int step=0;step<1000;step++){
   foreach(var root in roots){var native=registry.Where(p=>p&&p.gameObject==root).ToArray();var indexed=idx.ForObject(registry,root).Where(p=>p&&p.gameObject==root).ToArray();Check(native.SequenceEqual(indexed),"registration order/membership differs at "+step);}
   switch(step%6){case 0:Add(registry,roots[random.Next(roots.Length)],"new"+step);break;case 1:registry.RemoveAt(random.Next(registry.Count));break;case 2:int a=random.Next(registry.Count),b=random.Next(registry.Count);var old=registry[a];registry[a]=registry[b];registry[b]=old;break;case 3:registry.Reverse();break;case 4:registry.Clear();for(int i=0;i<30;i++)Add(registry,roots[random.Next(roots.Length)],"fresh"+i);break;case 5:var replacement=new List<PlayMakerFSM>(registry);registry=replacement;break;}
  }
  Check(ReferenceEquals(idx.ForObject(registry,null),registry),"null target fallback differs");roots[0].Alive=false;Check(ReferenceEquals(idx.ForObject(registry,roots[0]),registry),"destroyed target fallback differs");idx.Clear();Check(idx.Queries==0&&idx.Rebuilds==0&&idx.EntriesRead==0,"scene clear retained stats");
 }
 static List<string> Scenario(bool indexed,int variant){
  var reg=new List<PlayMakerFSM>();var idx=indexed?new FsmRegistryIndex():null;var root=new GameObject("root");var child=new GameObject("child",root.transform);var later=new GameObject("later",root.transform);var trace=new List<string>();bool mutated=false,nested=false;Fsm sender=null;
  void Record(Fsm f,FsmEvent e,FsmEventData d){trace.Add(f.Name+"/"+e.Name+"/"+d.SentByFsm.Name+"/"+f.Owner.gameObject.name);if(mutated||e.Name!="damage"||f.Name!="parent0")return;mutated=true;
   if(variant==0){Add(reg,child,"modded",Record);reg.RemoveAt(reg.FindIndex(x=>x?.Fsm?.Name=="child0"));} // Same count, new member.
   if(variant==1)reg.Reverse();
   if(variant==2){Add(reg,later,"late",Record);reg.RemoveAt(reg.FindIndex(x=>x?.Fsm?.Name=="parent1"));}
   if(variant==3){var added=new GameObject("added",root.transform);Add(reg,added,"new_child",Record);root.transform.Children.Reverse();}
   if(variant==4){var c=reg.Find(x=>x?.Fsm?.Name=="child0");c.Fsm=new(){Name="replacedFSM",Receive=Record};}
   if(variant==5){var c=reg.Find(x=>x?.Fsm?.Name=="child0");c.Alive=false;}
   if(variant==6){reg.Clear();Add(reg,later,"registered_after_clear",Record);}
   if(variant==7&&!nested){nested=true;Add(reg,child,"nested_new",Record);Broadcast(sender,child,new(){Name="nested"},d,true,false,reg,idx);}
  }
  sender=Add(reg,root,"parent0",Record).Fsm;Add(reg,root,"parent1",Record);Add(reg,child,"child0",Record);Add(reg,later,"later0",Record);Add(reg,new GameObject("foreign"),"unrelated",Record);reg.Add(null);reg.Add(reg[2]);
  var data=new FsmEventData{SentByFsm=sender,SentByGameObject=root};
  Broadcast(sender,root,new(){Name="damage"},data,true,false,reg,idx);
  Broadcast(sender,root,new(){Name="heal"},data,true,true,reg,idx);
  Broadcast(sender,root,new(){Name="only-root"},data,false,false,reg,idx);
  return trace;
 }
 static void Events(){for(int scenario=0;scenario<8;scenario++){var original=Scenario(false,scenario);var optimized=Scenario(true,scenario);Check(original.SequenceEqual(optimized),"native recipient/order/data/nesting/live child behavior differs: "+scenario);Check(optimized.All(x=>!x.StartsWith("unrelated/")),"world recipient added");}}
 static void Patch(string path){
  using var assembly=AssemblyDefinition.ReadAssembly(path);var f=assembly.MainModule.Types.Single(t=>t.FullName=="HutongGames.PlayMaker.Fsm");
  var method=f.Methods.Single(m=>m.Name=="BroadcastEventToGameObject"&&m.Parameters.Count==5&&m.Parameters[1].ParameterType.FullName=="HutongGames.PlayMaker.FsmEvent");
  var original=method.Body.Instructions.ToArray();using(var il=new ILContext(method))NativeEventSearch.Patch(il);
  var now=method.Body.Instructions.ToArray();Check(now.Length==original.Length+2,"patch inserts more than selector/target");int oldIndex=0;
  foreach(var ins in now)if(oldIndex<original.Length&&ReferenceEquals(ins,original[oldIndex]))oldIndex++;
  Check(oldIndex==original.Length,"original native instructions/handlers removed/reordered");int scan=Array.FindIndex(now,i=>i.Operand is MethodReference m&&m.Name=="get_FsmList");
  Check(now[scan+1].OpCode==Mono.Cecil.Cil.OpCodes.Ldarg_1,"wrong target argument/stack");var selector=(MethodReference)now[scan+2].Operand;
  Check(selector.Name=="Select"&&selector.Parameters.Count==2&&selector.ReturnType.FullName==method.Module.ImportReference(typeof(List<PlayMakerFSM>)).FullName,"wrong selector signature/stack result");
  // An incompatible prior IL change must leave the method unmodified.
  using var fresh=AssemblyDefinition.ReadAssembly(path);var mismatch=fresh.MainModule.Types.Single(t=>t.FullName==f.FullName).Methods.Single(m=>m.FullName==method.FullName);
  int before=mismatch.Body.Instructions.Count;var listRead=mismatch.Body.Instructions.Single(i=>i.Operand is MethodReference m&&m.Name=="get_FsmList");mismatch.Body.GetILProcessor().InsertAfter(listRead,Mono.Cecil.Cil.Instruction.Create(Mono.Cecil.Cil.OpCodes.Nop));
  using(var il=new ILContext(mismatch))NativeEventSearch.Patch(il);Check(mismatch.Body.Instructions.Count==before+1,"incompatible IL was guessed/modified");
  Console.WriteLine("Native PlayMaker IL: original "+original.Length+" instructions retained in order; only target + collection selector inserted. Incompatible pattern preserved.");
 }
 static void Timers(){
  ImpactMetrics.ResetWindow();using(ImpactMetrics.Broadcast()){}var p=new PlayerSlot();using(ImpactMetrics.Damage(p))using(ImpactMetrics.NativeDamage())using(ImpactMetrics.Broadcast()){using(ImpactMetrics.Broadcast())using(ImpactMetrics.PooledEffect()){}using(ImpactMetrics.Broadcast()){}}
  string report=ImpactMetrics.Report();Check(report.Contains("damage_broadcast_calls=1"),"nested broadcasts double-counted");Check(report.Contains("damage_pool_calls=1"),"pool timing not preserved");
  try{using(ImpactMetrics.Damage(p))using(ImpactMetrics.Broadcast())throw new Exception();}catch(Exception){}using(ImpactMetrics.Damage(p))using(ImpactMetrics.Broadcast()){}
  Check(ImpactMetrics.Report().Contains("damage_broadcast_calls=2"),"throw leaked broadcast depth");
 }
 static void Cost(){
  var reg=new List<PlayMakerFSM>();var parent=new GameObject("player");for(int i=0;i<400;i++){var g=new GameObject("child"+i,parent.transform);if(i%4==0)Add(reg,g,"native"+i);}for(int i=0;i<1000;i++)Add(reg,new GameObject("scene"+i),"scene"+i);
  var idx=new FsmRegistryIndex();var sender=new Fsm();var data=new FsmEventData{SentByFsm=sender};var evt=new FsmEvent{Name="damage"};PlayMakerFSM.ObjectReads=0;var watch=Stopwatch.StartNew();Broadcast(sender,parent,evt,data,true,false,reg,null);double oldMs=watch.Elapsed.TotalMilliseconds;long oldReads=PlayMakerFSM.ObjectReads;
  PlayMakerFSM.ObjectReads=0;watch.Restart();Broadcast(sender,parent,evt,data,true,false,reg,idx);double newMs=watch.Elapsed.TotalMilliseconds;long newReads=PlayMakerFSM.ObjectReads;Check(newReads<oldReads/100,"per-child scene scan not removed");
  Console.WriteLine("Controlled 400-child/1100-controller broadcast: gameObject reads "+oldReads+" -> "+newReads+"; elapsed "+oldMs.ToString("0.0")+" -> "+newMs.ToString("0.0")+" ms. Not measured Unity/Mono FPS.");
 }
 static void Main(string[] args){Membership();Events();Patch(args[0]);Timers();Cost();Console.WriteLine("PASS "+checks+" production event-index, native IL, registration/order/live hierarchy/nesting/timer checks. Native Unity runtime still requires user testing.");}
}

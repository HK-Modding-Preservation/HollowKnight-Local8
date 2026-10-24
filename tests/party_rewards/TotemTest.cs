using System;
using Random = UnityEngine.Random;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using KO.HollowKnight8;

partial class NativeProbe
{
 static FlingObjectsFromGlobalPool Drop(CoopSession s,string fsmName="soul_totem",bool soul=true)
 {
  var source=new GameObject {name="Statue variant"};
  var prefab=new GameObject();if(soul)prefab.Add<SoulOrb>();
  var action=new FlingObjectsFromGlobalPool {Fsm=new(){GameObject=source,Name=fsmName}};
  action.gameObject.Value=prefab;action.spawnPoint.Value=source;
  return action;
 }
 static List<SoulOrb> Orbs(CoopSession s,PlayerSlot factoryOwner)
 {
  var result=new List<SoulOrb>();
  ObjectPoolExtensions.Factory=(prefab,at,rotation)=>{
   var orb=Orb(factoryOwner);orb.transform.position=at;result.Add(orb);return orb.gameObject;
  };
  return result;
 }
 static void Outward(SoulOrb orb)
 {
  orb.Update();var routine=orb.zoomRoutine?.Routine;Check(routine!=null&&routine.MoveNext(),"shared radial flourish ended before its first visible frame");
  Check(orb.target==null&&orb.body.velocity.magnitude<=2.01f,"shared orb still targets P1 or retains its high native launch speed");
  Check(orb.getParticles.Plays==0,"shared orb played a vanilla collection effect at P1");
  Check(routine.MoveNext()&&orb.sprite.color.a<1,"outward soul effect did not fade");
  int guard=20;while(routine.MoveNext()&&guard-->0){}
  Check(guard>0&&!orb.gameObject.activeInHierarchy&&orb.sprite.color.a==1,"radial soul effect did not recycle and restore its pooled color");
 }
 static void Totems()
 {
  Check(On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.Count_OnEnter==1,"totem sharing hook missing or duplicated");
  foreach(int count in new[]{2,4,8})for(int hitter=0;hitter<count;hitter++)foreach(bool maximum in new[]{false,true})
  {
   var s=Party(count);foreach(var p in s.Players)p.Hero.transform.position=new(p.Index,0,-.01f*p.Index);
   var action=Drop(s);var spawned=Orbs(s,s.Players[hitter]);Random.Maximum=maximum;
   using(PlayerContext.Enter(s.Players[hitter]))action.OnEnter();
   int nativeCount=maximum?9:8;
   Check(action.Finished&&spawned.Count==nativeCount,"native random totem orb count/Finish was replaced");
   Check(s.Players.All(p=>p.Vitals.Soul==nativeCount*2),"totem did not grant native SOUL units equally to every nearby player");
   Check(spawned.All(o=>o.awardSoul&&o.target==null&&o.body.velocity.magnitude<=2.01f),"shared effect mutates prefab award flags or homes toward a player");
   Check(action.speedMin.Value==10&&action.speedMax.Value==20&&action.spawnMin.Value==8&&action.spawnMax.Value==9,"native fling fields were left changed");
   foreach(var orb in spawned)Outward(orb);
   Check(s.Players.All(p=>p.Vitals.Soul==nativeCount*2&&p.Hero.GetComponent<SpriteFlash>().SoulFlashes==0),"radial visual doubled rewards or flashed only P1");
   Context(s);
  }
  foreach(string reason in new[]{"far","down","hazard","retired","unready","disconnected","spawn","transfer","warp","waiting","zerohealth","inactive"})
  {
   var s=Party();foreach(var p in s.Players)p.Hero.transform.position=new();var excluded=s.Players[6];
   switch(reason){case "far":excluded.Hero.transform.position=new(12.01f,0);break;case "down":excluded.Down=true;break;case "hazard":excluded.Hazard=true;break;case "retired":excluded.Retiring=true;break;case "unready":excluded.Ready=false;break;case "disconnected":excluded.Connected=false;break;case "spawn":excluded.SpawnPending=true;break;case "transfer":excluded.ArenaTransfer=true;break;case "warp":excluded.Warp=true;break;case "waiting":excluded.Holding=true;break;case "zerohealth":excluded.Vitals.Health=0;break;case "inactive":excluded.Hero.gameObject.SetActive(false);break;}
   var action=Drop(s);var spawned=Orbs(s,s.Primary);Random.Maximum=false;action.OnEnter();
   Check(excluded.Vitals.Soul==0&&s.Players.Where(p=>p!=excluded).All(p=>p.Vitals.Soul==16),"totem sharing eligibility mismatch: "+reason);
   SoulTotemSharing.Reset();Check(spawned.All(o=>!o.gameObject.activeInHierarchy),"reset left shared orbs available for native P1 collection");Context(s);
  }
  {
   var s=Party(2);s.Primary.Hero.transform.position=new(100,0);s.Players[1].Hero.transform.position=new(12,0);
   var action=Drop(s,"SOUL_TOTEM");var spawned=Orbs(s,s.Primary);action.OnEnter();
   Check(s.Primary.Vitals.Soul==0&&s.Players[1].Vitals.Soul==16,"out-of-range P1 got SOUL or exact-radius guest did not");
   var cancel=spawned[0].Zoom(false);Check(!cancel.MoveNext()&&s.Primary.Vitals.Soul==0&&s.Players[1].Vitals.Soul==16,"scene cancellation ran the native reward branch");
   var reused=spawned[1];var old=reused.Zoom();Check(old.MoveNext(),"missing old shared flight");
   reused.gameObject.SetActive(false);reused.gameObject.SetActive(true);using(PlayerContext.Enter(s.Primary))reused.OnEnable();reused.Start();
   Check(!old.MoveNext()&&reused.gameObject.activeInHierarchy,"old radial iterator recycled a newly reused native orb");
   SoulTotemSharing.Reset();Check(reused.gameObject.activeInHierarchy,"shared reset recycled an unrelated reused native orb");
  }
  {
   var s=Party(2);var action=Drop(s,"Unrelated Control");var spawned=Orbs(s,s.Players[1]);action.OnEnter();
   Check(spawned.Count==8&&s.Players.All(p=>p.Vitals.Soul==0)&&spawned.All(o=>o.target==s.Players[1].Hero.transform),"non-totem soul orb behavior changed");
   var normal=Drop(s,soul:false);int calls=0;ObjectPoolExtensions.Factory=(prefab,at,rotation)=>{calls++;var g=new GameObject();g.Add<Rigidbody2D>();return g;};normal.OnEnter();
   Check(calls==8&&s.Players.All(p=>p.Vitals.Soul==0),"non-soul fling in a totem granted SOUL or was suppressed");
  }
  Console.WriteLine("PASS totem sharing: exact native Fling spawn count/Finish plus SoulOrb IL; every hitter/nearby slot, radius/eligibility, outward fade without P1 homing/collection, pool/reset/scene cleanup and unchanged non-totem branches. Unity rendering and native AddMPCharge IO are controlled.");
 }
}

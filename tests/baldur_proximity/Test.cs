using System;using System.Linq;using UnityEngine;using HutongGames.PlayMaker;using HutongGames.PlayMaker.Actions;using KO.HollowKnight8;

static partial class Test {
 static int checks;
 static readonly FsmEvent Far=new("FAR");
 static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static CoopSession Party(int count){ElderBaldurRange.Reset();var s=new CoopSession();Plugin.Self=new(){Session=s};for(int i=0;i<count;i++){var go=new GameObject();go.transform.position=new(1+i*.1f,0);go.Add<Collider2D>();go.transform.position=new(1+i*.1f,0,-.01f*(i+1));s.Players.Add(new(){Index=i,Hero=go.Add<HeroController>()});}return s;}
 static Fsm Blocker(){var go=new GameObject{name="Blocker"};go.Add<HealthManager>();return new(){GameObject=go};}
 static AlertRange Range(Fsm f){var go=new GameObject{name="Range"};go.Add<Collider2D>().Size=new(8,8);var range=go.Add<AlertRange>();f.GameObject.transform.Children["Range"]=go.transform;return range;}
 static (GetDistance,GetXDistance,FloatCompare) Queries(Fsm f,GameObject target,bool horizontal=false){var d=new GetDistance{Fsm=f};var x=new GetXDistance{Fsm=f};d.target.Value=x.target.Value=target;var c=new FloatCompare{Fsm=f,float1=horizontal?x.storeResult:d.storeResult};f.ActiveState.Actions=horizontal?new FsmStateAction[]{x,c}:new FsmStateAction[]{d,c};return(d,x,c);}
 static void Occupancy(Fsm f,AlertRange range,bool expected){var a=new CheckAlertRange{Fsm=f};a.alertRange.Value=range;a.Run();var named=new CheckAlertRangeByName{Fsm=f};named.Run();Check(a.storeResult.Value==expected&&named.storeResult.Value==expected,"direct/named range disagreed with whole-party occupancy");}
 static void PartyRule(){
  foreach(int count in new[]{2,4,8})for(int last=0;last<count;last++)foreach(bool horizontal in new[]{false,true}){
   var s=Party(count);var f=Blocker();var range=Range(f);var survivor=s.Players[last];var leaving=s.Players.First(p=>p!=survivor);leaving.Hero.transform.position=new(20,0);
   var(d,x,c)=Queries(f,leaving.Hero.gameObject,horizontal);
   // The old single-hero action sees the departing player and opens too early.
   if(horizontal)x.Native();else d.Native();c.Native();Check(c.Selected?.Name=="FAR","single-hero negative control did not reproduce premature opening");
   Check(ElderBaldurRange.BlockFar(f,Far),"departing player's raw FAR opened while another actor remained near");
   if(horizontal)x.Run();else d.Run();c.Run();Check(c.Selected?.Name=="NEAR","native comparison did not use nearest living party member");Occupancy(f,range,true);
   foreach(var p in s.Players.Where(p=>p!=survivor)){
    p.Hero.transform.position=new(25+p.Index,0);range.IsHeroInRange=false;
    c.float1.Value=100;c.Run();Check(c.Selected?.Name=="NEAR"&&ElderBaldurRange.BlockFar(f,Far),"one player's exit ignored the last player still inside");Occupancy(f,range,true);
   }
   survivor.Hero.transform.position=new(40,0);range.IsHeroInRange=true;c.Run();
   Check(c.Selected?.Name=="FAR"&&!ElderBaldurRange.BlockFar(f,Far),"all players far could not authorize native opening");Occupancy(f,range,false);
   survivor.Hero.transform.position=new(1,0);c.Run();Check(c.Selected?.Name=="NEAR"&&ElderBaldurRange.BlockFar(f,Far),"returning last player did not restore closure");Occupancy(f,range,true);
  }
 }
 static void MetricsAndScope(){
  var s=Party(8);foreach(var p in s.Players)p.Hero.transform.position=new(20+p.Index,0);s.Players[0].Hero.transform.position=new(1,50);s.Players[7].Hero.transform.position=new(3,0);
  var f=Blocker();var(d,x,c)=Queries(f,s.Players[7].Hero.gameObject);d.Run();Check(d.storeResult.Value==3,"Euclidean party minimum changed native distance metric");x.Run();Check(x.storeResult.Value==1,"horizontal party minimum was replaced by a Euclidean target");
  d.gameObject.UseOwner=false;d.gameObject.GameObject.Value=s.Players[7].Hero.gameObject;d.target.Value=f.GameObject;d.Run();Check(d.storeResult.Value==3,"reversed hero/enemy query lost party semantics");
  var projectile=new GameObject();projectile.transform.position=new(12,0);var owned=projectile.Add<Collider2D>();s.Owned[owned]=s.Players[0];d.gameObject.UseOwner=true;d.target.Value=projectile;int calls=d.NativeCalls;d.Run();Check(d.NativeCalls==calls+1&&d.storeResult.Value==12,"projectile/summon ownership replaced a non-hero distance");
  (d,x,c)=Queries(f,s.Players[7].Hero.gameObject);f.ActiveState.Name="Spit";Check(!ElderBaldurRange.BlockFar(f,Far)&&!ElderBaldurRange.BlockFar(f,new("DAMAGE")),"party range guard intercepted unrelated state/damage");f.ActiveState.Name="Closed";
  f.GameObject.GetComponent<HealthManager>().Dead=true;Check(!ElderBaldurRange.BlockFar(f,Far),"dead enemy remained under opening guard");f.GameObject.GetComponent<HealthManager>().Dead=false;
  f.Name="Other Control";calls=d.NativeCalls;d.Run();Check(d.NativeCalls==calls+1,"Elder-specific fix changed another enemy FSM");
 }
 static void Ineligible(){
  foreach(string guard in new[]{"down","hazard","retiring","unready","disconnected","zerohealth","transfer","warp","waiting","inactive"}){
   var s=Party(2);s.Players[0].Hero.transform.position=new(25,0);var p=s.Players[1];p.Hero.transform.position=new(1,0);var f=Blocker();var range=Range(f);var(d,x,c)=Queries(f,s.Players[0].Hero.gameObject);
   switch(guard){case "down":p.Down=true;break;case "hazard":p.Hazard=true;break;case "retiring":p.Retiring=true;break;case "unready":p.Ready=false;break;case "disconnected":p.Connected=false;break;case "zerohealth":p.Vitals.Health=0;break;case "transfer":p.ArenaTransfer=true;break;case "warp":p.Warp=true;break;case "waiting":p.Holding=true;break;case "inactive":p.Hero.gameObject.activeInHierarchy=false;break;}
   d.Run();c.Run();Check(c.Selected?.Name=="FAR"&&!ElderBaldurRange.BlockFar(f,Far),"ineligible actor prevented opening: "+guard);Occupancy(f,range,false);
  }
 }
 static void Main(){ElderBaldurRange.Install();ElderBaldurRange.Install();Check(On.CheckAlertRange.Count_Apply==1&&On.HutongGames.PlayMaker.Actions.GetDistance.Count_DoGetDistance==1,"range hooks installed twice");PartyRule();MetricsAndScope();Ineligible();NativeBody();Encounter();ElderBaldurRange.Uninstall();ElderBaldurRange.Uninstall();Check(On.CheckAlertRange.Count_Apply==0&&On.HutongGames.PlayMaker.Actions.GetDistance.Count_DoGetDistance==0,"range hooks left installed");Console.WriteLine($"PASS {checks} production Elder Baldur party-proximity checks: 2/4/8 players, every last-inside actor, sequential exits, all-far opening, return, distance/X-distance, comparison refresh, direct/named ranges and raw FAR guard. BoolTest OnEnter/OnUpdate execute exact native IL; encounter flow uses the published FSM graph; Unity geometry and animation IO are controlled, not an in-game encounter.");}
}

using System;
using System.Linq;
using UnityEngine;
using KO.HollowKnight8;

partial class NativeProbe
{
 static ScuttlerControl Seed(bool health=true)
 {
  var go=new GameObject{name="Health Scuttler"};var seed=go.Add<ScuttlerControl>();seed.healthScuttler=health;
  seed.deathSound1=new();seed.deathSound2=new();go.Add<Renderer>();return seed;
 }
 static void Scuttlers()
 {
  LifebloodScuttlers.Uninstall();var s=Party(2);EventRegister.BlueEvents=0;var native=Seed();
  native.Hit(new(){AttackType=AttackTypes.Nail});var old=native.Routines.Single().Routine;
  Check(s.Players.All(p=>p.Vitals.Blue==0)&&old.Current is WaitForSeconds w&&w.Seconds==1.2f,"native scuttler death did not start its real delayed Heal");
  Check(!old.MoveNext()&&EventRegister.BlueEvents==1,"exact native Heal did not emit its global blue-health event");
  using(PlayerContext.Enter(s.Primary)){}
  Check(s.Primary.Vitals.Blue==1&&s.Players[1].Vitals.Blue==0,"native global event did not reproduce P1-only blue health");
  LifebloodScuttlers.Install();LifebloodScuttlers.Install();
  Check(On.ScuttlerControl.Count_Hit==1&&On.ScuttlerControl.Count_Heal==1,"scuttler hooks installed twice");
  foreach(int count in new[]{2,4,8})for(int actor=0;actor<count;actor++)
  {
   s=Party(count);foreach(var p in s.Players)p.Hero.transform.position=new(p.Index,0);EventRegister.BlueEvents=0;
   var seed=Seed();var outer=s.Players[actor];
   using(PlayerContext.Enter(outer))
   {
    seed.Hit(new(){AttackType=AttackTypes.Nail});
    Check(PlayerContext.Current==outer&&HeroController.instance==outer.Hero,"scuttler reward leaked the death caller's context");
   }
   Check(s.Players.All(p=>p.Vitals.Blue==1&&p.HealFlashUntil>Time.unscaledTime),"real native ScuttlerControl.Hit did not share with the whole nearby party");
   Check(Award(new(){GameObject=seed.gameObject,Name="Health Scuttler"})&&s.Players.All(p=>p.Vitals.Blue==1),"legacy event duplicated the already shared native scuttler reward");
   for(int n=0;n<4;n++)seed.Hit(new(){AttackType=AttackTypes.Spell});
   Check(seed.Routines.Count==1&&s.Players.All(p=>p.Vitals.Blue==1),"repeated hits duplicated a dead scuttler reward");
   GameManager.instance.Unload();Check(EventRegister.BlueEvents==0&&s.Players.All(p=>p.Vitals.Blue==1),"room unload duplicated or lost shared scuttler health");
   Check(!seed.Routines.Single().Routine.MoveNext()&&!seed.gameObject.activeInHierarchy,"shared scuttler visual cleanup did not finish");Context(s);
  }
  foreach(AttackTypes type in Enum.GetValues<AttackTypes>())
  {
   s=Party(2);var seed=Seed();seed.Hit(new(){AttackType=type});bool accepted=type is AttackTypes.Nail or AttackTypes.Spell or AttackTypes.Generic or AttackTypes.NailBeam;
   Check(s.Players.All(p=>p.Vitals.Blue==(accepted?1:0)),"shared reward changed native attack acceptance: "+type);
  }
  // A coroutine/delegate hook may enter Heal after the native Hit callback
  // unwound. Do not require the recipient to inherit that thread-local call.
  foreach(int count in new[]{2,4,8})for(int actor=0;actor<count;actor++)
  {
   s=Party(count);foreach(var p in s.Players)p.Hero.transform.position=new(p.Index,0);EventRegister.BlueEvents=0;
   var seed=Seed();On.ScuttlerControl.Deferred_Heal=true;
   try{using(PlayerContext.Enter(s.Players[actor]))seed.Hit(new(){AttackType=AttackTypes.Nail});}finally{On.ScuttlerControl.Deferred_Heal=false;}
   Check(s.Players.All(p=>p.Vitals.Blue==0),"deferred Heal ran before its coroutine callback");
   var routine=seed.Routines.Single().Routine;Check(routine.MoveNext()&&routine.Current is WaitForSeconds,"deferred native Heal lost its native visual wait");
   Check(s.Primary.Vitals.Blue==1&&s.Players.All(p=>p.Vitals.Blue==1),"post-Hit Heal missed P1 or another nearby player");Context(s);
   var duplicate=seed.Heal();Check(duplicate.MoveNext()&&s.Players.All(p=>p.Vitals.Blue==1),"repeated Heal callback duplicated nearby rewards");
   GameManager.instance.Unload();Check(EventRegister.BlueEvents==0,"deferred hook retained vanilla global P1 reward");
   Check(!routine.MoveNext()&&!duplicate.MoveNext()&&!seed.gameObject.activeInHierarchy,"deferred lifetime cleanup did not finish once");
  }
  foreach(string guard in new[]{"far","down","hazard","retired","unready","disconnected","spawn","transfer","warp","waiting","zerohealth","inactive"})
  {
   s=Party();foreach(var p in s.Players)p.Hero.transform.position=new();var excluded=s.Players[6];
   switch(guard){case "far":excluded.Hero.transform.position=new(Lifeblood.ShareRadius+.01f,0);break;case "down":excluded.Down=true;break;case "hazard":excluded.Hazard=true;break;case "retired":excluded.Retiring=true;break;case "unready":excluded.Ready=false;break;case "disconnected":excluded.Connected=false;break;case "spawn":excluded.SpawnPending=true;break;case "transfer":excluded.ArenaTransfer=true;break;case "warp":excluded.Warp=true;break;case "waiting":excluded.Holding=true;break;case "zerohealth":excluded.Vitals.Health=0;break;case "inactive":excluded.Hero.gameObject.SetActive(false);break;}
   Seed().Hit(new(){AttackType=AttackTypes.Nail});
   Check(excluded.Vitals.Blue==0&&s.Players.Where(p=>p!=excluded).All(p=>p.Vitals.Blue==1),"native scuttler sharing ignored eligibility "+guard);
  }
  s=Party();foreach(var p in s.Players)p.Hero.transform.position=new(100,0);s.Players[3].Hero.transform.position=new(12,0);var at=Seed();at.gameObject.name="Renamed native life bug";
  at.Hit(new(){AttackType=AttackTypes.Generic});Check(s.Players[3].Vitals.Blue==1&&s.Players.Where(p=>p.Index!=3).All(p=>p.Vitals.Blue==0),"native radius/identity used name, killer position or 3D depth");
  s=Party(2);var orange=Seed(false);orange.Hit(new(){AttackType=AttackTypes.Nail});Check(s.Players.All(p=>p.Vitals.Blue==0)&&!orange.gameObject.activeInHierarchy,"orange scuttler became a lifeblood reward");
  var guarded=Seed();guarded.Rearm(Time.time+2);guarded.Hit(new(){AttackType=AttackTypes.Nail});Check(guarded.Alive&&guarded.Routines.Count==0&&s.Players.All(p=>p.Vitals.Blue==0),"activation delay bypassed native Hit");
  s=Party(2);var pooled=Seed();pooled.Hit(new(){AttackType=AttackTypes.Nail});var stale=pooled.Routines.Last().Routine;
  pooled.Rearm();pooled.Hit(new(){AttackType=AttackTypes.Spell});var fresh=pooled.Routines.Last().Routine;
  Check(!stale.MoveNext()&&pooled.gameObject.activeInHierarchy&&s.Players.All(p=>p.Vitals.Blue==2),"old visual cleanup disabled a rearmed scuttler or blocked a new accepted life");
  Check(!fresh.MoveNext()&&!pooled.gameObject.activeInHierarchy,"new scuttler life did not clean up");
  var reset=Seed();reset.Hit(new(){AttackType=AttackTypes.Nail});Lifeblood.Reset();Check(!reset.gameObject.activeInHierarchy&&!reset.Routines.Last().Routine.MoveNext(),"scene reset left an invisible dead scuttler or stale cleanup");
  s=Party(2);var rearmed=Seed();rearmed.Hit(new(){AttackType=AttackTypes.Nail});rearmed.Rearm();Lifeblood.Reset();Check(rearmed.gameObject.activeInHierarchy,"scene cleanup disabled a living rearmed scuttler");
  s=Party(2);EventRegister.BlueEvents=0;s.Active=false;var solo=Seed();solo.Hit(new(){AttackType=AttackTypes.Nail});Check(s.Players.All(p=>p.Vitals.Blue==0),"inactive co-op changed native reward timing");solo.Routines.Single().Routine.MoveNext();using(PlayerContext.Enter(s.Primary)){}Check(EventRegister.BlueEvents==1&&s.Primary.Vitals.Blue==1&&s.Players[1].Vitals.Blue==0,"inactive co-op did not retain native Heal/event behavior");
  s=Party(2);var visual=Seed();visual.pool=new();visual.screenFlash=new();int spawned=0;ObjectPoolExtensions.Factory=(prefab,position,rotation)=>{spawned++;var go=new GameObject();go.Add<Renderer>();return go;};
  FlingUtils.ChildrenCalls=0;visual.Hit(new(){AttackType=AttackTypes.Nail});Check(FlingUtils.ChildrenCalls==1&&FlingUtils.LastMin==8&&FlingUtils.LastMax==10,"native initial scuttler particle burst changed");
  Check(!visual.Routines.Single().Routine.MoveNext()&&spawned==1&&!visual.gameObject.activeInHierarchy&&s.Players.All(p=>p.Vitals.Blue==1),"native screen flash/cleanup changed or awarded another mask");
  Console.WriteLine("PASS real native ScuttlerControl.Hit and original Heal iterator negative control; accepted attacks, 2/4/8 nearby parties, each hitter context, range/exclusions, repeats, unload, rearmed pool and cleanup. Rendering, player-data/event IO and coroutine timing are controlled.");
 }
}

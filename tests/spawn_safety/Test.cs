using System;using System.Linq;using System.Collections;using UnityEngine;using KO.HollowKnight8;

static partial class Test {
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
 static BoxCollider2D Terrain(float left,float bottom,float right,float top,int layer=8,bool trigger=false){var g=new GameObject{layer=layer,name="Terrain"};g.transform.position=new((left+right)*.5f,(bottom+top)*.5f);var c=g.Add<BoxCollider2D>();c.size=new(right-left,top-bottom);c.isTrigger=trigger;return c;}
 static PlayerSlot Actor(int id,Vector3 at,bool active=true){var g=new GameObject{name="P"+(id+1),layer=9};g.transform.position=at;var hero=g.Add<HeroController>();g.Add<BoxCollider2D>().size=new(.8f,2);g.Add<Rigidbody2D>();g.Add<Renderer>();g.SetActive(active);return new(){Index=id,Hero=hero,Ready=active,LifeStartedAt=id};}
 static CoopSession Start(bool floor=true){Physics2D.World.Clear();Physics2D.IgnoredPair=Physics2D.IgnoredLayer=false;Physics2D.Reverse=false;Physics2D.Syncs=Physics2D.Queries=0;Time.time=Time.unscaledTime=10;GameManager.instance=new();ArenaGather.Anchor=null;ArenaGather.Rule=at=>true;TransitionVote.Waiting.Clear();EmergencyWarp.Warping.Clear();CoopEnding.Held.Clear();HornetEncounterFlow.Held.Clear();WorldRouting.Recovering.Clear();Diagnostics.Lines.Clear();TimedRespawnRecovery.Restores=0;EntryGravityRecovery.Reset();TerrainEmbedRecovery.Reset();FallingPlatformHazard.Reset();BenchSeats.SeatedPlayers.Clear();CoopEnding.Active=ScriptedParty.Active=ArenaGather.TransferActive=false;
  if(floor)Terrain(0,0,100,2);var s=new CoopSession();s.Players.Add(Actor(0,new(10,3.02f)));Plugin.Self=s.plugin;Plugin.Self.Session=s;return s;}
 static PlayerSlot Guest(CoopSession s,int index=1,bool active=false){var p=Actor(index,new(150,-5),active);s.Players.Add(p);return p;}
 static void Finish(CoopSession s,PlayerSlot p,Action between=null){var routine=s.Finish(p);Check(routine.MoveNext(),"finish omitted first native initialization frame");Check(routine.MoveNext(),"finish omitted second native initialization frame");between?.Invoke();Check(!routine.MoveNext(),"finish did not terminate");}
 static void NegativeControls(){
  var s=Start(false);var p=Guest(s,active:true);var unsafeAt=s.LegacyJoinDestination();
  Check(!s.IsSafe(unsafeAt,s.Primary)&&unsafeAt==s.Primary.Hero.transform.position,"test7 did not reproduce unchecked join fallback");
  var air=new Vector3(10,8);Check(LegacyPlacement.Recovery(s,p,ref air),"test7 did not reproduce accepting clear air without ground");
  Check(!SpawnSafety.Recovery(s,p,ref air)&&!SpawnSafety.Join(s,p,out _),"empty room still accepted as a landing");
  s=Start();p=Guest(s);Terrain(8,2,12,8);unsafeAt=s.LegacyJoinDestination();Check(!s.IsSafe(unsafeAt,s.Primary),"legacy negative control did not reproduce under/inside terrain placement");
  s.InitialGuard(p);Check(s.NativeCreates==0&&!p.SpawnPending&&p.RetryAt>Time.unscaledTime,"unsafe initial clone was not deferred");
 }
 static void Geometry(){
  var s=Start();var p=s.Primary;Check(s.IsSafe(p.Hero.transform.position,p),"ordinary floor is not safe");
  var box=p.Hero.GetComponent<BoxCollider2D>();box.enabled=false;Check(s.IsSafe(p.Hero.transform.position,p),"disabled body lost its real dimensions");box.enabled=true;
  var edge=Terrain(10.39f,3,10.43f,4);Check(!s.IsSafe(p.Hero.transform.position,p),"thin wall at body edge was ignored");edge.enabled=false;
  var hazard=Terrain(9.6f,2,10.4f,2.06f,17,true);hazard.gameObject.Add<DamageHero>().hazardType=2;
  Check(!s.IsSafe(p.Hero.transform.position,p),"hazard touching feet was missed by shrunken query");hazard.enabled=false;
  var fsm=Terrain(9.5f,2.2f,10.5f,3,17,true);fsm.gameObject.Add<PlayMakerFSM>().Hazard=3;Check(!s.IsSafe(p.Hero.transform.position,p),"native FSM hazard accepted");fsm.enabled=false;
  foreach(var at in new[]{new Vector3(.1f,3),new Vector3(99.9f,3),new Vector3(10,.5f),new Vector3(10,49.5f),new Vector3(-1,3),new Vector3(10,-1)})Check(!s.IsSafe(at,p),"body protruded outside map at "+at);
  foreach(var at in new[]{new Vector3(float.NaN,3),new Vector3(float.PositiveInfinity,3),new Vector3(10,float.NaN),new Vector3(10,float.NegativeInfinity),new Vector3(10,3,float.NaN),new Vector3(10,3,float.PositiveInfinity)}){int q=Physics2D.Queries;Check(!s.IsSafe(at,p)&&Physics2D.Queries==q,"invalid coordinates reached physics queries");}
  box.offset=new(.3f,.2f);p.Hero.transform.localScale=new(-2,1.5f,1);p.Hero.transform.Angle=90;var body=DuelGround.Body(p);
  Check(Math.Abs(body.size.x-3)<.0001f&&Math.Abs(body.size.y-1.6f)<.0001f,"rotated/scaled body bounds wrong");
  Check(Math.Abs(body.center.x-9.7f)<.0001f&&Math.Abs(body.center.y-2.42f)<.0001f,"mirrored collider offset wrong");
  foreach(float invalid in new[]{0,float.NaN,float.PositiveInfinity}){GameManager.instance.sceneWidth=invalid;Check(!s.InRoom(new(10,3)),"invalid room dimensions accepted");}
  Check(!RecoveryRules.InRoom(double.PositiveInfinity,1,double.PositiveInfinity,10),"infinite bounds accepted infinite point");
  s=Start(false);p=s.Primary;Terrain(0,0,100,2,25);Check(s.IsSafe(p.Hero.transform.position,p),"soft terrain layer cannot support a landing");
  s=Start(false);p=s.Primary;Terrain(0,0,100,2,8,true);Check(!s.IsSafe(p.Hero.transform.position,p),"trigger was accepted as solid floor");
  s=Start(false);p=s.Primary;var slope=Terrain(0,0,100,2);slope.SurfaceNormal=new(.9f,.4f);Check(!s.IsSafe(p.Hero.transform.position,p),"steep surface was accepted as floor");
 }
 static void NearAndReachable(){
  var s=Start();var p=Guest(s);Vector3 at;
  Check(SpawnSafety.Join(s,p,out at)&&s.IsSafe(at,p),"safe template/body join failed");
  p.Hero.GetComponent<BoxCollider2D>().size=new(1.1f,3.4f);p.Hero.GetComponent<BoxCollider2D>().offset=new(.1f,-.3f);
  Check(SpawnSafety.NearAlly(s,s.Primary,p,out at)&&s.IsSafe(at,p)&&at.y>4,"different-height body feet were not aligned to floor");
  Check(at.z==s.Primary.Hero.transform.position.z,"landing changed depth unexpectedly");
  s=Start(false);p=Guest(s);s.Primary.Hero.transform.position=new(5,8);Terrain(9,0,10,30);Terrain(10,0,100,5);
  Check(!s.FindSafePosition(s.Primary.Hero.transform.position,p,out at),"floor search crossed a wall into another region");
  Check(!SpawnSafety.Join(s,p,out at),"join crossed wall over a pit");
  s=Start();p=Guest(s);var blocked=Terrain(8,2,12,10);var second=Actor(2,new(30,3.02f));second.LifeStartedAt=10;s.Players.Add(second);
  Check(SpawnSafety.Living(s,p,out at)&&at.x>25&&s.IsSafe(at,p),"unsafe oldest survivor prevented using another safe actor");
  TransitionVote.Waiting.Add(second.Index);Check(!SpawnSafety.Living(s,p,out at),"hidden room voter was used as landing anchor");TransitionVote.Waiting.Clear();
  EmergencyWarp.Warping.Add(second.Index);Check(!SpawnSafety.Living(s,p,out at),"rescue ghost was used as landing anchor");EmergencyWarp.Warping.Clear();
  CoopEnding.Held.Add(second.Index);Check(!SpawnSafety.Living(s,p,out at),"ending actor was used as landing anchor");CoopEnding.Held.Clear();
  second.SpawnPending=true;Check(!SpawnSafety.Living(s,p,out at),"unfinished clone was used as landing anchor");second.SpawnPending=false;
  ArenaGather.Rule=v=>v.x<20;Check(!SpawnSafety.Living(s,p,out at),"landing crossed active encounter bounds");
  s=Start();p=Guest(s);s.Primary.Down=true;s.HasArrival=true;s.Arrival=new(10,3.02f);Check(SpawnSafety.Join(s,p,out at)&&s.IsSafe(at,p),"downed native P1 arrival proxy stranded parked living guests");
  s.HasArrival=false;Check(!SpawnSafety.Join(s,p,out at),"dead P1 corpse used without current-room native arrival");
  s=Start(false);p=Guest(s);s.Primary.HasSafePoint=true;s.Primary.SafePoint=new(15,3.02f);Terrain(13,0,17,2);Check(SpawnSafety.Join(s,p,out at)&&at==s.Primary.SafePoint,"verified nearby safe point was ignored");
  s.Primary.SafePoint=new(50,3.02f);Check(!SpawnSafety.Join(s,p,out at),"join used distant stale point outside local region");
  s=Start(false);p=Guest(s);GameManager.instance.sceneHeight=48;GameManager.instance.cameraCtrl.sceneHeight=18.8484f;Terrain(20,16,35,19.4f);s.Primary.Hero.transform.position=new(25.7f,20.42f);
  Check(SpawnSafety.Join(s,p,out at)&&s.IsSafe(at,p),"camera framing height was used as room geometry bounds");
 }
 static void Lifecycles(){
  var s=Start();var p=Guest(s);s.InitialGuard(p);Check(s.NativeCreates==1,"safe initial clone not admitted");
  p.Hero.gameObject.SetActive(true);p.Ready=false;p.SpawnPending=true;Finish(s,p);
  Check(p.Ready&&!p.SpawnPending&&s.IsSafe(p.Hero.transform.position,p)&&p.Hero.Accepted==1,"finished clone exposed unsafe body/control");
  s=Start();p=Guest(s);p.Hero.gameObject.SetActive(true);p.SpawnPending=true;
  Finish(s,p,()=>Terrain(0,2,100,10));Check(!p.Ready&&!p.SpawnPending&&!p.Hero.gameObject.activeSelf&&p.RetryAt>Time.unscaledTime,"moving gate during initialization did not park/retry clone");
  foreach(var c in Physics2D.World.Where(c=>c.gameObject.name=="Terrain").Skip(1))c.enabled=false;
  s.Resume(p);Check(p.SpawnPending&&p.Hero.gameObject.activeSelf&&s.plugin.Routines.Count==1,"parked actor did not resume after space became safe");
  var retry=s.plugin.Routines.Last();Check(retry.MoveNext()&&retry.MoveNext()&&!retry.MoveNext(),"resumed finish lifecycle failed");Check(p.Ready&&s.IsSafe(p.Hero.transform.position,p),"resumed actor did not land safely");
  s=Start();p=Guest(s);p.Hero.gameObject.SetActive(true);p.SpawnPending=true;var ceiling=Terrain(0,4.5f,100,10);p.Hero.OnCharm=()=>p.Hero.GetComponent<BoxCollider2D>().size=new(.8f,4);
  Finish(s,p);Check(!p.Ready&&!p.Hero.gameObject.activeSelf&&p.Hero.Resets==0&&p.Hero.Accepted==0,"native charm resize restored body/input inside floor or ceiling");
  ceiling.enabled=false;s.Resume(p);retry=s.plugin.Routines.Last();retry.MoveNext();retry.MoveNext();retry.MoveNext();Check(p.Ready&&s.IsSafe(p.Hero.transform.position,p),"resized clone could not retry after ceiling cleared");
  s=Start(false);p=Guest(s);int activations=p.Hero.gameObject.Activations;s.Resume(p);Check(!p.SpawnPending&&!p.Hero.gameObject.activeSelf&&p.Hero.gameObject.Activations==activations,"unsafe parked actor activated before validation");
  s=Start();p=Guest(s);p.Hero.gameObject.SetActive(true);p.SpawnAsDown=p.SpawnPending=true;Finish(s,p);Check(p.Down&&p.Vitals.Health==0&&TimedRespawnRecovery.Restores==1,"safe reconstruction revived corpse instead of restoring its prior death");
  s=Start(false);p=Guest(s);p.Hero.gameObject.SetActive(true);p.SpawnAsDown=p.SpawnPending=true;Finish(s,p);Check(p.Down&&!p.Ready&&!p.Hero.gameObject.activeSelf,"delayed corpse lost downed state");
  s=Start();p=Guest(s);p.Hero.gameObject.SetActive(true);p.SpawnPending=true;var r=s.Finish(p);r.MoveNext();r.MoveNext();s.sceneEpoch++;var before=p.Hero.transform.position;Check(!r.MoveNext()&&!p.Ready&&p.Hero.transform.position==before,"old scene finish moved/activated successor actor");
 }
 static void BuffersAndMatrix(){
  var s=Start();var p=Guest(s);for(int i=0;i<130;i++)Terrain(0,0,100,2);
  Vector3 at;Check(!SpawnSafety.Join(s,p,out at),"saturated physics floor buffer accepted incomplete results");
  var rng=new Random(817203);int landed=0;
  for(int n=0;n<320;n++){
   s=Start();p=Guest(s,n%8);var box=p.Hero.GetComponent<BoxCollider2D>();box.size=new(.6f+(float)rng.NextDouble(),1.6f+(float)rng.NextDouble()*2);box.offset=new((float)rng.NextDouble()*.2f-.1f,(float)rng.NextDouble()*.6f-.3f);
   p.Hero.transform.localScale=new(n%2==0?1:-1,.8f+(float)rng.NextDouble()*.5f,1);s.Primary.Hero.transform.position=new(3+(float)rng.NextDouble()*90,3.02f);Physics2D.Reverse=n%2==0;
   Terrain(45,2,47,50);Terrain(65,2,66,6,25);
   bool ok=SpawnSafety.Join(s,p,out at);
   Check(!ok||s.IsSafe(at,p),"P"+(p.Index+1)+" matrix accepted unsafe spawn");
   if(ok){landed++;var body=DuelGround.Body(p);Check(SpawnSafety.BodyInside(at,body.center-p.Hero.transform.position,body.size),"matrix body crossed room bounds");}
  }Check(landed>280,"geometry validation unnecessarily rejected ordinary playable floor");
 }
 static EmergencyWarp.Trip Rescue(PlayerSlot p,Vector3 origin){
  var trip=new EmergencyWarp.Trip{Player=p,Hero=p.Hero,Origin=origin,Effect=new GameObject()};
  var collider=p.Hero.GetComponent<Collider2D>();trip.Colliders[collider]=true;collider.enabled=false;
  var renderer=p.Hero.GetComponent<Renderer>();trip.Renderers[renderer]=true;renderer.enabled=false;return trip;
 }
 static void OtherPlacementRoutes(){
  var s=Start();var p=Guest(s,active:true);p.Hero.transform.position=new(30,4.02f);p.Hero.GetComponent<BoxCollider2D>().size=new(.8f,4);
  GroupWarp.warp=s.Primary;GroupWarp.warpAt=s.Primary.Hero.transform.position;GroupWarp.warpTime=Time.unscaledTime;GroupWarp.Tick(s);
  Check(s.IsSafe(p.Hero.transform.position,p)&&p.Hero.transform.position.x<15&&p.HasSafePoint,"scripted group warp used the anchor's body instead of arriving actor");
  s=Start();p=Guest(s,active:true);p.Hero.transform.position=new(10,4.02f);p.Hero.GetComponent<BoxCollider2D>().size=new(.8f,4);s.Primary.Hero.transform.position=new(30,3.02f);GameManager.instance.sceneName="Room_Final_Boss_Core";
  InteractionRouter.DoorFallbackTick(s);Check(p.Hero.transform.position.x>22&&s.IsSafe(p.Hero.transform.position,p),"final boss gathering placed taller actor inside floor");
  s=Start();p=Guest(s,active:true);p.Hero.transform.position=new(10,4.02f);p.Hero.GetComponent<BoxCollider2D>().size=new(.8f,4);Terrain(52,4,60,6.4f);s.Primary.Hero.transform.position=new(56,7.42f);
  ShrineGather.Run(s);Check(s.IsSafe(p.Hero.transform.position,p)&&p.HasSafePoint&&p.Hero.transform.position.y>8,"dream shrine hard-coded point was accepted inside floor");
  s=Start(false);p=s.Primary;string reason;Check(!ArenaLanding.Clear(s,p,new(10,8),out reason),"arena accepted empty air as landing");
  Terrain(0,0,100,2);Check(ArenaLanding.Clear(s,p,new(10,3.02f),out reason),"arena rejected valid supported landing");
  foreach(int id in Enumerable.Range(0,8)){
   s=Start();p=id==0?s.Primary:Guest(s,id,active:true);if(id==0)s.Players.Add(Actor(1,new(10,3.02f)));p.Ready=true;p.Hero.transform.position=new(30,8);var origin=new Vector3(30,3.02f);var trip=Rescue(p,origin);int health=p.Vitals.Health;
   EmergencyWarp.Restore(trip);Check(s.IsSafe(p.Hero.transform.position,p)&&p.Hero.transform.position==origin&&p.Vitals.Health==health,"P"+(id+1)+" rescue return did not validate safe origin");
   p.Hero.transform.position=origin;trip=Rescue(p,origin);Terrain(29,2,31,10);EmergencyWarp.Restore(trip);Check(!p.Hazard&&s.IsSafe(p.Hero.transform.position,p)&&p.Hero.transform.position.x<15,"moving gate rescue return restored actor inside terrain");
  }
  s=Start(false);p=Guest(s,active:true);p.Ready=true;p.Hero.transform.position=new(30,8);var pending=Rescue(p,new(30,3.02f));int originalHealth=p.Vitals.Health;
  EmergencyWarp.Restore(pending);Check(p.Hazard&&p.InputBlocked&&!p.Hero.GetComponent<Rigidbody2D>().simulated&&!p.Hero.GetComponent<Collider2D>().enabled&&!p.Hero.GetComponent<Renderer>().enabled,"no-space rescue exposed/restored unsafe body");
  Check(p.Vitals.Health==originalHealth&&!p.Down&&p.ColliderStates.Count==1&&p.RendererStates.Count==1,"safe-landing wait lost health, death state or restoration flags");
  Terrain(0,0,100,2);Vector3 landing=p.Hero.transform.position;Check(SpawnSafety.Recovery(s,p,ref landing)&&s.IsSafe(landing,p),"delayed rescue cannot recover once floor becomes safe");
 }
 static void Main(){NegativeControls();Geometry();NearAndReachable();Lifecycles();BuffersAndMatrix();OtherPlacementRoutes();Motion();Crushing();Console.WriteLine("PASS "+checks+" spawn-safety checks: test7 negative controls, production body/terrain/hazard geometry, bounds/NaN, connected floor search, every-slot sizes/offsets/scales, initial/parked/finish lifecycle, moving gates and native charm resize, downed arrival proxy, saturated queries, group/shrine/final-boss placements, arena support and rescue restoration. Unity physics/rendering are controlled rectangular-world doubles.");}
}

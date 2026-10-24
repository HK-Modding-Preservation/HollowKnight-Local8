using System;
using UnityEngine;
using KO.HollowKnight8;

static partial class Test
{
 static void Step(CoopSession s,float dt=.15f){Time.unscaledTime+=dt;Time.time+=dt;EntryGravityRecovery.Tick(s);TerrainEmbedRecovery.Tick(s);}
 static void Motion()
 {
  foreach(string kind in new[]{"foot","effector","ignored-pair","ignored-layer","protection","trigger-body","kinematic-body","unsimulated-body","hornet-dialogue"})
  {
   var s=Start(false);var p=s.Primary;p.Hero.transform.position=new(30,kind=="foot"?2.5f:1.5f);p.HasSafePoint=true;p.SafePoint=new(20,3.02f);
   Terrain(19,0,21,2);var obstruction=Terrain(29,0,31,2);
   if(kind=="effector")obstruction.usedByEffector=true;
   if(kind=="ignored-pair")Physics2D.IgnoredPair=true;
   if(kind=="ignored-layer")Physics2D.IgnoredLayer=true;
   if(kind=="protection")p.ProtectionUntil=Time.time+5;
   if(kind=="trigger-body")p.Hero.GetComponent<Collider2D>().isTrigger=true;
   if(kind=="kinematic-body")p.Hero.GetComponent<Rigidbody2D>().isKinematic=true;
   if(kind=="unsimulated-body")p.Hero.GetComponent<Rigidbody2D>().simulated=false;
   if(kind=="hornet-dialogue")HornetEncounterFlow.Held.Add(p);
   for(int n=0;n<15;n++)Step(s);
   Check(s.Recoveries==0,"ordinary/nonblocking overlap caused automatic teleport: "+kind);
  }
  // Every trapped actor is recovered under an unrelated outer context. The
  // actual production Recover must move/reset only its explicit argument.
  for(int trapped=0;trapped<8;trapped++)
  {
   var s=Start();for(int i=1;i<8;i++){var added=Guest(s,i,active:true);added.Hero.transform.position=new(5+i*3,3.02f);}
   var p=s.Players[trapped];p.HasSafePoint=true;p.SafePoint=new(60,3.02f);p.Hero.transform.position=new(45,1.5f);
   var obstacle=Terrain(44,0,46,2);var before=new Vector3[8];var health=new int[8];var soul=new int[8];var resets=new int[8];
   for(int i=0;i<8;i++){before[i]=s.Players[i].Hero.transform.position;health[i]=s.Players[i].Vitals.Health;soul[i]=s.Players[i].Vitals.Soul;resets[i]=s.Players[i].Hero.Resets;}
   var outer=s.Players[(trapped+1)%8];using(PlayerContext.Enter(outer))
   {
    Step(s);Step(s);Step(s);Check(PlayerContext.Current==outer,"terrain recovery leaked another player's context");
   }
   Check(s.Recoveries==1&&WorldRouting.Recovering.Count==1&&WorldRouting.Recovering[0]==p,"terrain recovery marked an untrapped group member");
   for(int i=0;i<8;i++){
    var a=s.Players[i];Check(a.Vitals.Health==health[i]&&a.Vitals.Soul==soul[i],"terrain recovery changed another actor's vitals");
    Check(i==trapped?a.Hero.transform.position==p.SafePoint&&a.Hero.Resets==resets[i]+1:a.Hero.transform.position==before[i]&&a.Hero.Resets==resets[i],"terrain recovery moved or reset the wrong hero");
   }
  }
  for(int index=0;index<8;index++)
  {
   var s=Start();var p=index==0?s.Primary:Guest(s,index,active:true);var h=p.Hero;var b=h.GetComponent<Rigidbody2D>();
   b.gravityScale=0;h.prevGravityScale=0;b.velocity=new(2,4);h.DEFAULT_GRAVITY=1.5f;
   EntryGravityRecovery.Entered(s,h);
   Check(b.gravityScale==1.5f&&b.velocity.x==2&&b.velocity.y==4,"entry zero-gravity repair lost native jump momentum");
   b.gravityScale=0;Step(s);Check(b.gravityScale==0,"completed entry watchdog touched later native motion");
   foreach(string flag in new[]{"transitioning","dashing","shadowDashing","superDashing","recoiling","recoilFrozen","swimming","spellQuake","casting","freezeCharge","onConveyorV","superDashOnWall"})
   {
    var field=h.cState.GetType().GetField(flag);field.SetValue(h.cState,true);b.gravityScale=0;h.prevGravityScale=8;
    EntryGravityRecovery.Entered(s,h);Step(s);Check(b.gravityScale==0&&h.prevGravityScale==8,"entry interrupted intentional "+flag);
    field.SetValue(h.cState,false);Step(s);Check(b.gravityScale==1.5f&&h.prevGravityScale==0,"entry did not restore gravity after "+flag);
   }
   b.gravityScale=0;h.cState.superDashing=true;EntryGravityRecovery.Entered(s,h);Step(s,2.1f);h.cState.superDashing=false;Step(s);Check(b.gravityScale==0,"expired entry window changed sustained crystal dash");
   b.gravityScale=2.7f;EntryGravityRecovery.Entered(s,h);Check(b.gravityScale==2.7f,"entry changed nonzero mod gravity");
   b.gravityScale=-2;EntryGravityRecovery.Entered(s,h);Check(b.gravityScale==-2,"entry changed intentional reverse gravity");
   h.DEFAULT_GRAVITY=0;b.gravityScale=0;EntryGravityRecovery.Entered(s,h);Check(b.gravityScale==0,"entry invented gravity against a configured zero default");h.DEFAULT_GRAVITY=1.5f;
   b.gravityScale=0;EntryGravityRecovery.Entered(s,h);b.gravityScale=0;EntryGravityRecovery.Reset();Step(s);Check(b.gravityScale==0,"scene reset retained old gravity repair");

   s=Start();p=index==0?s.Primary:Guest(s,index,active:true);h=p.Hero;h.proxyFSM=h.gameObject.Add<PlayMakerFSM>();h.transform.position=new(30,3.02f);p.HasSafePoint=true;p.SafePoint=new(20,3.02f);int health=p.Vitals.Health;
   p.Vitals.Blue=2+index;p.Vitals.Soul=17+index;p.Vitals.Reserve=5+index;
   for(int n=0;n<10;n++)Step(s);Check(s.Recoveries==0,"ordinary floor contact caused a recovery");
   h.transform.position=new(30,2.95f);for(int n=0;n<10;n++)Step(s);Check(s.Recoveries==0,"minor contact penetration caused a recovery");
   h.transform.position=new(30,1.5f);Step(s);h.transform.position=new(30,3.02f);Step(s);Check(s.Recoveries==0,"one physics-step overlap caused recovery");
   h.transform.position=new(30,1.5f);h.GetComponent<Rigidbody2D>().velocity=new(3,0);
   Step(s);Step(s);Step(s);Check(s.Recoveries==1&&h.transform.position==p.SafePoint&&p.Vitals.Health==health,"walking player crushed into terrain did not recover safely without losing a mask");
   Check(p.Vitals.Blue==2+index&&p.Vitals.Soul==17+index&&p.Vitals.Reserve==5+index,"full production Recover changed blue masks, soul or reserve without respawn");
   Check(h.acceptingInput&&h.GetComponent<Rigidbody2D>().simulated&&!h.GetComponent<Rigidbody2D>().isKinematic&&h.GetComponent<Rigidbody2D>().velocity.magnitude==0,"full production Recover did not restore body/input");
   Check(h.gameObject.layer==p.RootLayer&&p.HasSafePoint&&p.LastInputFrame==-1&&p.ProtectionUntil>Time.time&&h.proxyFSM.Events.Contains("HeroCtrl-HeroInPosition"),"full production Recover omitted layer, native arrival event, protection or safe-point lifecycle");
   Step(s);Check(s.Recoveries==1,"successful terrain recovery repeated");
   foreach(string flag in new[]{"transitioning","dashing","shadowDashing","superDashing","recoiling","recoilFrozen","swimming","spellQuake","casting","freezeCharge","onConveyorV","superDashOnWall"})
   {
    TerrainEmbedRecovery.Reset();p.ProtectionUntil=0;h.transform.position=new(30,1.5f);var field=h.cState.GetType().GetField(flag);field.SetValue(h.cState,true);int before=s.Recoveries;
    for(int n=0;n<4;n++)Step(s);Check(s.Recoveries==before,"terrain watchdog interrupted "+flag);field.SetValue(h.cState,false);
    Step(s);Step(s);Step(s);Check(s.Recoveries==before+1&&s.IsSafe(h.transform.position,p),"terrain watchdog failed after "+flag);
   }
   p.ProtectionUntil=0;h.transform.position=new(30,1.5f);Terrain(19,2,21,8);p.HasPreviousSafePoint=true;p.PreviousSafePoint=new(10,3.02f);TerrainEmbedRecovery.Reset();Step(s);Step(s);Step(s);
   Check(h.transform.position==p.PreviousSafePoint&&p.Vitals.Health==health,"new obstruction at saved point was ignored");
   p.ProtectionUntil=0;h.transform.position=new(30,1.5f);TerrainEmbedRecovery.Reset();Step(s);int recovered=s.Recoveries;GameManager.instance.isPaused=true;Step(s,3);GameManager.instance.isPaused=false;Step(s);
   Check(s.Recoveries==recovered,"pause time counted as persistent active terrain penetration");Step(s);Step(s);Check(s.Recoveries==recovered+1,"terrain recovery did not resume after pause");

   s=Start(false);p=index==0?s.Primary:Guest(s,index,active:true);h=p.Hero;h.transform.position=new(30,1.5f);p.HasSafePoint=true;p.SafePoint=new(20,3.02f);Terrain(29,1,31,2);
   for(int n=0;n<6;n++)Step(s);Check(s.Recoveries==0&&h.transform.position==new Vector3(30,1.5f)&&p.Vitals.Health==5,"terrain recovery used an unsupported destination when no landing exists");
   Terrain(19,0,21,2);Step(s);Check(s.Recoveries==1&&s.IsSafe(h.transform.position,p)&&p.Vitals.Health==5,"terrain recovery did not retry when a safe landing became available");
  }
  foreach(string guard in new[]{"pause","story","ending","transfer","wipe","bench","vote","warp","blocked","unready","noinput","control","hazard","down","retired","spawn"})
  {
   var s=Start();var p=s.Primary;var h=p.Hero;h.transform.position=new(30,1.5f);p.HasSafePoint=true;p.SafePoint=new(20,3.02f);
   switch(guard){case "pause":GameManager.instance.isPaused=true;break;case "story":ScriptedParty.Active=true;break;case "ending":CoopEnding.Active=true;break;case "transfer":ArenaGather.TransferActive=true;break;case "wipe":s.TeamWipe=true;break;case "bench":BenchSeats.SeatedPlayers.Add(0);break;case "vote":TransitionVote.Waiting.Add(0);break;case "warp":EmergencyWarp.Warping.Add(0);break;case "blocked":p.InputBlocked=true;break;case "unready":p.Ready=false;break;case "noinput":h.acceptingInput=false;break;case "control":h.controlReqlinquished=true;break;case "hazard":p.Hazard=true;break;case "down":p.Down=true;break;case "retired":p.Retiring=true;break;case "spawn":p.SpawnPending=true;break;}
   for(int n=0;n<5;n++)Step(s);Check(s.Recoveries==0,"terrain watchdog ignored "+guard);
  }
  Console.WriteLine("PASS production entry-gravity and moving-terrain recovery: all eight slots, jump velocity, native dash/cast/conveyor holds, shallow/transient/deep contact, sideways movement, safe-point obstruction and no-landing retry. Complete production Recover/Commit/checkpoint methods execute with health, blue masks, soul, reserve, body/input, layer, native arrival event and protection assertions; Unity/event IO remains controlled.");
 }
}

using System;
using System.Linq;
using KO.HollowKnight8;
using UnityEngine;
static class Program {
 static int checks;
 static CoopSession s;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static CoopSession Party(int count){
  Time.time=Time.unscaledTime=0;PvpMatch.Running=PvpArena.Active=DreamSequence.InStoryDream=false;
  RadianceAscentCheckpoint.Refuse=false;RadianceAscentCheckpoint.Highest=null;
  EmergencyWarp.Trips.Clear();TransitionVote.Entrants.Clear();Diagnostics.Messages.Clear();CoopShades.Spawned=PvpCombat.Deaths=0;
  s=new();for(int i=0;i<count;i++)s.Players.Add(new(){Index=i,LifeStartedAt=i});return s;
 }
 static PlayerSlot Dead(int i=0,float when=0){Time.time=when;var p=s.Players[i];s.Down(p);return p;}
 static void At(float now,PlayerSlot p){Time.time=now;s.Dispatch(p);}
 static void Tick(PlayerSlot p)=>TimedRespawnRecovery.Tick(s,p,s.plugin.TimedRespawn.Value,s.plugin.RespawnSeconds.Value);
 static void Main(){
  // All slots and supported rosters, exact boundary, genuine new death and
  // room rebuild both before and after the original deadline.
  for(int count=2;count<=8;count++)for(int slot=0;slot<count;slot++)foreach(float death in new[]{0f,10f,1000f}){
   Party(count);var p=Dead(slot,death);
   Check(TimedRespawnRecovery.Remaining(p,15)==15,"Initial duration includes death at time zero");
   At(death+14.9f,p);Check(p.Down&&s.Restored.Count==0,"Never resurrect early");
   Check(ProductionHud.Label(p,s.plugin)=="1s","HUD uses same deadline");
   p.SpawnAsDown=true;p.Down=false;p.Ready=false;p.SpawnPending=true;
   Time.time=death+14.95f;s.CompleteRoomRebuild(p);p.Ready=true;p.SpawnPending=false;
   Check(p.DownAt==death&&p.Down,"Reconstruction preserves original death clock");
   Check(CoopShades.Spawned==1&&PvpCombat.Deaths==1,"Rebuild does not repeat death rewards/shade");
   At(death+15,p);Check(!p.Down&&p.Vitals.Health==3,"Resurrect on original deadline after rebuild");
   Check(s.Restored.Count==1,"Exactly one successful recovery");
   Time.time=death+20;s.Down(p,false);Check(p.DownAt==death+20,"Real no-shade death starts a new timer");
   At(death+34,p);Check(p.Down,"Fresh death is not restored early by old timer");
   p.SpawnAsDown=true;p.Down=false;p.Ready=false;p.SpawnPending=true;
   Time.time=death+40;s.CompleteRoomRebuild(p);p.SpawnPending=false;p.Ready=true;Tick(p);
   Check(!p.Down&&p.DownAt==death+20,"Load finishing after deadline recovers at first ready tick");
  }
  // Prefer the oldest actual living survivor, with deterministic tie handling.
  Party(4);var down=Dead();s.Players[1].LifeStartedAt=8;s.Players[2].LifeStartedAt=2;s.Players[3].LifeStartedAt=5;
  At(15,down);Check(s.Searches.SequenceEqual(new[]{2})&&s.Restored.SequenceEqual(new[]{2}),"Oldest living chosen instead of P1 or nearest entrance");
  Party(4);down=Dead();foreach(var q in s.Players)q.LifeStartedAt=0;
  At(15,down);Check(s.Restored.Single()==1,"Ties resolve to lower player index");
  for(int count=3;count<=8;count++)for(int blocked=1;blocked<count-1;blocked++){
   Party(count);down=Dead();for(int i=1;i<=blocked;i++)s.Unsafe.Add(i);
   At(15,down);Check(!down.Down&&s.Restored.Single()==blocked+1,"Blocked oldest survivors fall back to next safe one");
   Check(s.Searches.SequenceEqual(Enumerable.Range(1,blocked+1)),"Safe search keeps oldest-first order");
  }
  Party(4);down=Dead();s.Refused.Add(1);At(15,down);
  Check(!down.Down&&s.Restored.SequenceEqual(new[]{1,2}),"Final body/gate recheck refusing a landing tries another survivor");
  Party(8);down=Dead();foreach(var q in s.Players.Skip(1))s.Unsafe.Add(q.Index);At(15,down);
  Check(down.Down&&down.DownAt==0&&s.Searches.Count==7&&s.Restored.Count==0,"No forced wall/hazard spawn or room-entrance fallback");
  Check(ProductionHud.Label(down,s.plugin)=="hud.returning","Expired safety wait never misleading 0s");
  for(int i=1;i<10;i++)At(15+i*.001f,down);
  Check(s.Searches.Count==7,"Per-frame polling does not repeat expensive safety search");
  s.Unsafe.Remove(4);At(15.101f,down);
  Check(!down.Down&&s.Restored.Single()==4,"Retries promptly without adding another full respawn countdown");
  // Hidden entrants, ghosts and proxies cannot count as survivors or anchors.
  foreach(string invalid in new[]{"disconnected","not-ready","pending","dead-native","zero-health","hazard","retiring","transition","dream-rescue","room-vote","missing-hero"}){
   Party(3);down=Dead();var bad=s.Players[1];
   switch(invalid){
    case "disconnected":bad.Connected=false;break;case "not-ready":bad.Ready=false;break;
    case "pending":bad.SpawnPending=true;break;case "dead-native":bad.Hero.cState.dead=true;break;
    case "zero-health":bad.Vitals.Health=bad.Vitals.Blue=0;break;case "hazard":bad.Hazard=true;break;
    case "retiring":bad.Retiring=true;break;case "transition":bad.Hero.cState.transitioning=true;break;
    case "dream-rescue":EmergencyWarp.Trips.Add(bad);break;case "room-vote":TransitionVote.Entrants.Add(bad);break;
    case "missing-hero":bad.Hero=null;break;
   }
   At(15,down);Check(!down.Down&&s.Restored.Single()==2,"Excluded invalid anchor: "+invalid);
  }
  Party(2);down=Dead();s.Players[1].Vitals.Health=0;s.Players[1].Vitals.Blue=1;At(15,down);
  Check(!down.Down,"Only blue health still counts as a living survivor");
  // Native wipe/round/load state must remain authoritative.
  foreach(string blocked in new[]{"disabled","pvp","arena","wipe","inactive","loading","not-ready","pending","retiring","missing-hero","all-dead","solo","already-alive"}){
   Party(blocked=="solo"?1:2);down=Dead();
   switch(blocked){
    case "disabled":s.plugin.TimedRespawn.Value=false;break;case "pvp":PvpMatch.Running=true;break;
    case "arena":PvpArena.Active=true;break;case "wipe":s.TeamWipe=true;break;case "inactive":s.Active=false;break;
    case "loading":s.Gameplay=false;break;case "not-ready":down.Ready=false;break;case "pending":down.SpawnPending=true;break;
    case "retiring":down.Retiring=true;break;case "missing-hero":down.Hero=null;break;
    case "all-dead":s.Down(s.Players[1]);break;case "already-alive":down.Down=false;break;
   }
   Time.time=20;Tick(down);Check(s.Restored.Count==0,"No inappropriate auto-respawn: "+blocked);
  }
  // Timed recovery remains distinct from ordinary trap recovery.
  Party(2);down=Dead();down.Hazard=true;down.HazardUntil=0;At(1,down);
  Check(down.Down&&s.HazardRecoveries==0&&down.DownAt==0,"Nonfatal hazard path cannot erase a death and restart it");
  At(15,down);Check(!down.Down&&s.HazardRecoveries==0,"Fatal/hazard overlap uses actual timed resurrection");
  Party(2);var trapped=s.Players[1];Time.time=2;s.Hazard(trapped);At(2.5f,trapped);
  Check(trapped.Hazard&&s.HazardRecoveries==0,"Ordinary trap delay preserved");At(2.701f,trapped);
  Check(!trapped.Hazard&&s.HazardRecoveries==1,"Ordinary trap recovery remains nonfatal");
  Party(2);down=s.Players[1];down.Hazard=true;Time.time=8;s.Down(down);
  Check(down.Down&&!down.Hazard&&down.NextTimedRespawnAttempt==0,"Real death supersedes a trap and starts clean retry state");
  // Keep the shared highest verified Radiance platform and defer unsafe beams.
  Party(2);down=Dead();RadianceAscentCheckpoint.Highest=new(199);s.Unsafe.Add(1);At(15,down);
  Check(!down.Down&&s.Restored.Single()==99&&s.Searches.Count==0,"Radiance verified checkpoint remains preferred");
  Party(2);down=Dead();RadianceAscentCheckpoint.Refuse=true;At(15,down);
  Check(down.Down&&s.Restored.Count==0,"Unsafe Radiance checkpoint is never bypassed by ordinary landing");
  RadianceAscentCheckpoint.Refuse=false;At(15.101f,down);Check(!down.Down,"Radiance retry has no new full countdown");
  // Shared scaled gameplay clock: pause does not drain the timer, changing the
  // setting uses the same deadline as the HUD, and failed setup preserves it.
  Party(2);down=Dead(1,5);Time.time=10;Time.unscaledTime=200;
  Check(TimedRespawnRecovery.Remaining(down,15)==10&&ProductionHud.Label(down,s.plugin)=="10s","Paused scaled clock and HUD agree");
  s.plugin.RespawnSeconds.Value=10;At(15,down);Check(!down.Down,"Live duration change uses shared clock");
  Party(2);down=Dead(1,2);down.Down=false;down.SpawnAsDown=true;Time.time=9;s.ThrowOnHide=true;bool threw=false;
  try{s.CompleteRoomRebuild(down);}catch(InvalidOperationException){threw=true;}
  Check(threw&&down.DownAt==2,"Restored clock survives native setup failure");
  // Repeated reconstruction never stretches one death, even across many rooms.
  Party(2);down=Dead(1,3);
  for(int room=0;room<30;room++){Time.time=4+room;down.Down=false;down.SpawnAsDown=true;s.CompleteRoomRebuild(down);Check(down.DownAt==3,"Clock retained over repeated room reconstructions");}
  Console.WriteLine("PASS "+checks+" production timed-respawn/death/scene/HUD checks; terrain and native recovery use controlled doubles, Unity gameplay untested.");
 }
}

using System;using System.Linq;using UnityEngine;using KO.HollowKnight8;using HutongGames.PlayMaker;
static class Test {
 static int checks,calls;static PlayerSlot[] players;static NailSlash[] slashes;static Collider2D[] bodies;static Collider2D cap;static BounceShroom shroom;static CollisionEnterEvent bodyEvent;static Fsm fsm,foreign=new();
 static void Check(bool b,string reason){checks++;if(!b)throw new Exception(reason);}
 static GameObject Child(string n,Transform parent){var o=new GameObject(n);o.transform.parent=parent;return o;}
 static void ResetCounts(){foreach(var p in players){p.Hero.bounces=0;p.Hero.actions=0;p.Hero.xVelocity=7;p.Hero.yVelocity=-9;}calls=0;}
 static void Only(int actor){foreach(var p in players)Check(p.Hero.actions==(p.Index==actor?1:0),"Wrong bouncing player P"+(p.Index+1));Check(calls==1,"Native callback repeated or missing");}
 static void Context(int actor){Check(HeroController.instance==players[actor].Hero,"Wrong native singleton");Check(PlayerContext.Current==players[actor],"Wrong context");Check(MushroomBounceRouting.Resolve(fsm)==players[actor],"Wrong legacy FSM owner");Check(MushroomBounceRouting.Resolve(foreign)==null,"Unrelated FSM hijacked");}
 static void Restored(HeroController cache,NailSlash slash){Check(HeroController.instance==players[0].Hero,"Leaked singleton");Check(PlayerContext.Current==null,"Leaked context");Check(MushroomBounceRouting.Resolve(fsm)==null,"Leaked FSM owner");Check(slash.Cache()==cache,"Leaked nail cache");}
 static void Main(){
  var session=new CoopSession();Plugin.Self=new Plugin{Session=session};
  players=new PlayerSlot[8];slashes=new NailSlash[8];bodies=new Collider2D[8];
  for(int i=0;i<8;i++){var hero=new GameObject("P"+(i+1)).Add<HeroController>();players[i]=new PlayerSlot{Index=i,Hero=hero};session.Players.Add(players[i]);slashes[i]=Child("nail",hero.transform).Add<NailSlash>();slashes[i].Cache(players[0].Hero);bodies[i]=Child("body",hero.transform).Add<Collider2D>();}
  var mushroom=new GameObject("Mushroom");cap=mushroom.Add<Collider2D>();shroom=mushroom.Add<BounceShroom>();shroom.active=true;fsm=mushroom.Add<PlayMakerFSM>().Fsm;bodyEvent=Child("top",mushroom.transform).Add<CollisionEnterEvent>();HeroController.instance=players[0].Hero;
  // Reproduce the reported ownership defect without the production routing.
  ResetCounts();slashes[1].Cache().BounceAction(25);Check(players[0].Hero.bounces==1&&players[1].Hero.bounces==0,"Baseline nail repro");
  ResetCounts();HeroController.instance.BounceAction(12);Check(players[0].Hero.bounces==1&&players[1].Hero.bounces==0,"Baseline body repro");
  MushroomBounceRouting.Install();MushroomBounceRouting.Install();Check(On.NailSlash.Subscribers==1&&On.CollisionEnterEvent.Subscribers==1,"Duplicate subscriptions");
  // All eight owners and native directional actions, repeatedly with an incorrect cached P1.
  for(int repeat=0;repeat<80;repeat++)for(int i=0;i<8;i++)for(int direction=0;direction<4;direction++){
   int actor=i;ResetCounts();slashes[i].Cache(players[0].Hero);
   On.NailSlash.Emit((s,c)=>{calls++;Context(actor);Check(s.Cache()==players[actor].Hero,"Wrong cached actor");if(direction==3)s.Cache().BounceAction(25);else{s.Cache().actions++;s.Cache().xVelocity=direction==0?-15:15;}},slashes[i],cap);
   Only(i);Check(players[i].Hero.yVelocity==(direction==3?25:-9),"Native directional velocity replaced");Restored(players[0].Hero,slashes[i]);
  }
  Check(Diagnostics.Lines.Count==8,"Unbounded diagnostics per contact");
  // Real collider ownership beats an ambient context belonging to another player.
  using(PlayerContext.Enter(players[6])){
   On.NailSlash.Emit((s,c)=>{Context(2);Check(s.Cache()==players[2].Hero,"Ambient player stole contact");},slashes[2],cap);
   Check(PlayerContext.Current==players[6]&&HeroController.instance==players[6].Hero,"Ambient context not restored");
  }
  Restored(players[0].Hero,slashes[2]);
  // Nested simultaneous mushroom contacts are independent and restore the outer actor.
  On.NailSlash.Emit((s,c)=>{Context(1);On.NailSlash.Emit((n,k)=>Context(7),slashes[7],cap);Context(1);},slashes[1],cap);Restored(players[0].Hero,slashes[1]);Restored(players[0].Hero,slashes[7]);
  // Body contact handlers use a native singleton rather than the NailSlash cache.
  for(int i=0;i<8;i++){
   int actor=i;ResetCounts();var collision=new Collision2D{collider=bodies[i],gameObject=bodies[i].gameObject};
   On.CollisionEnterEvent.Emit((s,c)=>{calls++;Context(actor);HeroController.instance.BounceAction(12);},bodyEvent,collision);Only(i);Restored(players[0].Hero,slashes[i]);
   ResetCounts();collision.collider=null;On.CollisionEnterEvent.Emit((s,c)=>{calls++;Context(actor);HeroController.instance.BounceAction(12);},bodyEvent,collision);Only(i);Restored(players[0].Hero,slashes[i]);
  }
  bool failed=false;try{On.NailSlash.Emit((s,c)=>{Context(3);throw new InvalidOperationException();},slashes[3],cap);}catch(InvalidOperationException){failed=true;}Check(failed,"Exception swallowed");Restored(players[0].Hero,slashes[3]);
  failed=false;try{On.CollisionEnterEvent.Emit((s,c)=>{Context(4);throw new InvalidOperationException();},bodyEvent,new Collision2D{collider=bodies[4]});}catch(InvalidOperationException){failed=true;}Check(failed,"Body exception swallowed");Restored(players[0].Hero,slashes[4]);
  // Do not undo another mod's intentional reassignment of the cache.
  On.NailSlash.Emit((s,c)=>s.Cache(players[5].Hero),slashes[3],cap);Restored(players[5].Hero,slashes[3]);slashes[3].Cache(players[0].Hero);
  // Invalid/dead/transitioning actors cannot bounce P1 as a fallback.
  Action<PlayerSlot>[] blocked={p=>p.Down=true,p=>p.Alive=false,p=>p.Ready=false,p=>p.Hazard=true,p=>p.Retiring=true,p=>p.SpawnPending=true,p=>p.ArenaTransfer=true,p=>p.InputBlocked=true,p=>p.Hero.controlReqlinquished=true,p=>p.Hero.cState.transitioning=true};
  foreach(var block in blocked){var p=players[1];block(p);ResetCounts();On.NailSlash.Emit((s,c)=>calls++,slashes[1],cap);On.CollisionEnterEvent.Emit((s,c)=>calls++,bodyEvent,new Collision2D{collider=bodies[1]});Check(calls==0,"Blocked actor ran native action");p.Down=p.Hazard=p.Retiring=p.SpawnPending=p.ArenaTransfer=p.InputBlocked=p.Hero.controlReqlinquished=p.Hero.cState.transitioning=false;p.Alive=p.Ready=true;Restored(players[0].Hero,slashes[1]);}
  // Unrelated interactions and unregistered actors remain native.
  var plain=new GameObject("Terrain").Add<Collider2D>();ResetCounts();On.NailSlash.Emit((s,c)=>{calls++;Check(PlayerContext.Current==null,"Terrain was intercepted");},slashes[1],plain);Check(calls==1,"Terrain suppressed");
  var stranger=new GameObject("Foreign hero").Add<HeroController>();var strangeSlash=Child("nail",stranger.transform).Add<NailSlash>();strangeSlash.Cache(stranger);ResetCounts();On.NailSlash.Emit((s,c)=>{calls++;Check(PlayerContext.Current==null,"Foreign actor intercepted");},strangeSlash,cap);Check(calls==1,"Foreign actor suppressed");
  shroom.active=false;ResetCounts();On.CollisionEnterEvent.Emit((s,c)=>{calls++;Check(PlayerContext.Current==null,"Inactive body mushroom intercepted");},bodyEvent,new Collision2D{collider=bodies[1]});Check(calls==1,"Inactive native callback suppressed");shroom.active=true;
  session.Active=false;ResetCounts();On.NailSlash.Emit((s,c)=>{calls++;Check(s.Cache()==players[0].Hero,"Solo cache changed");},slashes[1],cap);Check(calls==1,"Solo callback suppressed");session.Active=true;
  MushroomBounceRouting.Reset();Check(MushroomBounceRouting.Resolve(fsm)==null,"Scene reset retained owner");
  for(int i=0;i<20;i++){MushroomBounceRouting.Uninstall();MushroomBounceRouting.Uninstall();Check(On.NailSlash.Subscribers==0&&On.CollisionEnterEvent.Subscribers==0,"Uninstall leaked hooks");MushroomBounceRouting.Install();Check(On.NailSlash.Subscribers==1&&On.CollisionEnterEvent.Subscribers==1,"Reload doubled hooks");}
  MushroomBounceRouting.Uninstall();Console.WriteLine("PASS "+checks+" mushroom ownership/isolation checks (P1-P8, nail/body, nesting, exceptions, lifecycle)");
 }
}

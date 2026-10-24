using System;
using UnityEngine;
using HutongGames.PlayMaker;
using KO.HollowKnight8;
using Obj=UnityEngine.Object;

static class Test {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static PlayerSlot Player(CoopSession s,int i){var root=new GameObject();var p=new PlayerSlot{Index=i,Hero=root.AddComponent<HeroController>()};s.Players.Add(p);return p;}
 static void OwnerChecks(){
  var s=new CoopSession();Plugin.Self.Session=s;var p=Player(s,0);var q=Player(s,1);s.Primary=p;s.RegisterHierarchy(p);s.RegisterHierarchy(q);
  var child=new GameObject(p.Hero.gameObject.Node);var c=child.AddComponent<MonoBehaviour>();s.RegisterHierarchy(p);
  object[] keys={p.Hero.gameObject,p.Hero,p.Hero.gameObject.Node,child,c,new Fsm{Owner=child},new FsmStateAction{Fsm=new Fsm{Owner=c}},new object(),null};
  for(int frame=0;frame<20;frame++){Time.frameCount=frame;foreach(var key in keys)for(int i=0;i<300;i++)Check(ReferenceEquals(s.Resolve(key),s.Raw(key)),"resolver differs from alpha217");}
  var effect=new GameObject();effect.AddComponent<OwnerTag>();s.RefreshOwnership(effect,p);Check(s.Resolve(effect)==p,"tag ownership");
  s.RefreshOwnership(effect,q);Check(s.Resolve(effect)==q,"pooled object reassignment in same frame");
  s.RefreshOwnership(effect,null);Check(s.Resolve(effect)==null,"remove pooled ownership in same frame");
  var world=new GameObject();Check(s.Resolve(world)==null,"unowned world object");world.AddComponent<OwnerTag>().Player=q;
  Check(s.Resolve(world)==s.Raw(world),"retain existing same-frame negative policy");Time.frameCount++;Check(s.Resolve(world)==q,"negative expires at frame boundary");
  var pending=new GameObject();var hero=pending.AddComponent<HeroController>();Check(s.Resolve(pending)==null,"unassigned hero is initially unowned");
  var late=new PlayerSlot{Index=2,Hero=hero};s.Players.Add(late);Check(s.Resolve(pending)==late,"hero assignment visible within same frame without caching pending null");
  var clone=new GameObject();clone.AddComponent<HeroController>();Check(s.Resolve(clone)==null,"clone before initialization");
  var joining=new PlayerSlot{Index=3};s.Cloning=joining;Check(s.Resolve(clone)==joining&&joining.Hero!=null,"cloning side effect retained");s.Cloning=null;
  Check(s.Resolve(clone)==joining,"cloning must invalidate earlier negative entries");
  s.Resolve(child);p.Hero=new GameObject().AddComponent<HeroController>();Check(s.Resolve(child)==s.Raw(child),"changed hero invalidates cached positive");
  c.Alive=false;Check(s.Resolve(c)==null,"destroyed component");child.Alive=false;child.Node.Alive=false;Check(s.Resolve(child)==null,"destroyed game object");
  s.RefreshOwnership(effect,q);s.Resolve(effect);Obj.TransformReads=0;Obj.IdReads=0;for(int i=0;i<100000;i++)Check(s.Resolve(effect)==q,"steady owned answer");
  Check(Obj.TransformReads==0&&Obj.IdReads==0,"cache avoids repeated Transform/GetInstanceID APIs");
  var none=new GameObject();s.Resolve(none);Obj.TransformReads=0;Obj.IdReads=0;for(int i=0;i<100000;i++)Check(s.Resolve(none)==null,"steady unowned answer");
  Check(Obj.TransformReads==0&&Obj.IdReads==0,"stable negative avoids repeated native lookups");
  for(int i=0;i<9000;i++)s.Resolve(new GameObject());Check(s.Resolve(effect)==q,"bounded cache eviction retains answer");
  Console.WriteLine("Ownership: same answers as original resolver; same-frame reassignment, pending heroes, clone side effects, destruction and bounded eviction verified. Steady Transform/GetInstanceID lookups: 0 per 100000 cached queries.");
 }
 static void InteractionChecks(){
  var s=new CoopSession();Plugin.Self.Session=s;for(int i=0;i<8;i++){var p=Player(s,i);p.Hero.transform.position=new Vector3(i*.8f,i%2,0);}s.Primary=s.Players[0];
  var rng=new Random(78118);Time.unscaledTime=12;
  for(int n=0;n<10000;n++){
   foreach(var p in s.Players){p.Valid=rng.Next(4)!=0;p.Eligible=rng.Next(2)==0;p.Inside=rng.Next(2)==0;p.Seat=rng.Next(2)==0;p.Competing=rng.Next(4)==0;p.BlockExit=rng.Next(10)==0;p.Actions.up.IsPressed=rng.Next(3)==0;p.Actions.down.IsPressed=rng.Next(3)==0;p.Actions.up.WasPressed=rng.Next(2)==0;p.Actions.down.WasPressed=rng.Next(2)==0;}
   var go=new GameObject();var f=new Fsm{Owner=go,Name=n%3==0?"Stag Control":"Bench Control",ActiveState=new State()};
   var a=new Entry{Fsm=f,Owner=rng.Next(2)==0?s.Players[rng.Next(8)]:null,Busy=rng.Next(7)==0,Bench=rng.Next(2)==0,Up=rng.Next(2)==0,Down=rng.Next(2)==0,Scene=rng.Next(3)==0,NextAllowed=rng.Next(9)==0?13:0,LastTouch=rng.Next(2)==0?11.9f:9};
   var b=new Entry{Fsm=a.Fsm,Owner=a.Owner,Busy=a.Busy,Bench=a.Bench,Up=a.Up,Down=a.Down,Scene=a.Scene,NextAllowed=a.NextAllowed,LastTouch=a.LastTouch,RequestedAt=a.RequestedAt};
   PlayerSlot touched=rng.Next(3)==0?s.Players[rng.Next(8)]:null;
   InteractionUnderTest.active=null;StagMenuRouting.Notes=0;InteractionUnderTest.RunLegacy(b,touched);int notes=StagMenuRouting.Notes;
   StagMenuRouting.Notes=0;InteractionUnderTest.Run(a,touched);
   Check(a.Owner==b.Owner&&a.LastTouch==b.LastTouch&&a.RequestedAt==b.RequestedAt&&notes==StagMenuRouting.Notes,"interaction owner/timing changed");
  }
  foreach(var p in s.Players){p.Valid=true;p.Actions.up.IsPressed=p.Actions.down.IsPressed=false;}
  var e=new Entry{Fsm=new Fsm{Owner=new GameObject()},Owner=s.Players[0],Up=true};
  InteractionUnderTest.EligibleCalls=0;InteractionUnderTest.RunLegacy(e,null);int old=InteractionUnderTest.EligibleCalls;
  InteractionUnderTest.EligibleCalls=0;InteractionUnderTest.Run(e,null);int current=InteractionUnderTest.EligibleCalls;
  Check(current<old,"idle actors must avoid request-only range queries");
  Console.WriteLine("Interaction: 10000 randomized eight-player selections match the original owner and timing; idle request-only range checks reduced.");
 }
 static void ProjectionChecks(){
  var r=new Random(217);var go=new GameObject();var camera=new GameObject().AddComponent<Camera>();var vertices=new Vector2[32];var uv=new Vector2[32];var projected=new LightVertex[32];
  for(int n=0;n<1000;n++){
   for(int i=0;i<32;i++){vertices[i]=new Vector2((float)r.NextDouble()*5-2,(float)r.NextDouble()*4-1);uv[i]=new Vector2((float)r.NextDouble(),(float)r.NextDouble());}
   go.Node.position=new Vector3(n*.003f,-n*.005f,n%11);go.Node.SX=(n%5+1)*.3f;go.Node.SY=(n%7+1)*.4f;camera.Scale=(n%4+1)*.1f;
   bool fx=(n&1)!=0,fy=(n&2)!=0;Obj.ProjectionReads=0;LightProjection.Prepare(camera,go.Node,vertices,uv,fx,fy,projected);
   Check(Obj.ProjectionReads==vertices.Length*2,"project unique vertices once");
   for(int players=1;players<=8;players++)for(int i=0;i<96;i++){
    int index=i%32;var local=vertices[index];if(fx)local.x=0f-local.x;if(fy)local.y=0f-local.y;
    Vector3 old=camera.WorldToViewportPoint(go.Node.TransformPoint(local));var v=projected[index];float dx=players*.031f,dy=players*-.07f;
    Check(v.X+dx==old.x+dx&&v.Y+dy==old.y+dy&&v.Z==old.z&&v.U==uv[index].x&&v.V==uv[index].y,"projection/UV differs from original triangle math");
   }
  }
  Console.WriteLine("Light: moving transforms/cameras, all flips and 1–8 players preserve original projected coordinates and UVs; projection is computed once per source vertex/rebuild.");
 }
 static void InteractionGuards(){
  var s=new CoopSession();var p=Player(s,0);p.Actions.up.IsPressed=true;var e=new Entry{Up=true};
  Check(InteractionUnderTest.TestRequest(e,p),"normal up request is ignored");
  p.Hero.controlReqlinquished=true;Check(!InteractionUnderTest.TestRequest(e,p),"relinquished actor can request interaction");p.Hero.controlReqlinquished=false;
  p.Vitals.AtBench=true;Check(!InteractionUnderTest.TestRequest(e,p),"bench actor opens foreign prompt");e.Bench=true;Check(InteractionUnderTest.TestRequest(e,p),"bench request itself is blocked");p.Vitals.AtBench=false;e.Bench=false;
  p.SeatOccupied=true;Check(!InteractionUnderTest.TestRequest(e,p),"seated actor opens foreign prompt");p.SeatOccupied=false;
  var inventory=new GameObject().AddComponent<PlayMakerFSM>();GameManager.instance=new(){inventoryFSM=inventory};
  foreach(string state in new[]{"Opened","Open","Open Current Pane","Tween Panes"}){inventory.ActiveStateName=state;Check(!InteractionUnderTest.TestRequest(e,p),"open inventory leaks up into interaction");}
  inventory.isActiveAndEnabled=false;Check(InteractionUnderTest.TestRequest(e,p),"disabled inventory blocks interaction");inventory.isActiveAndEnabled=true;inventory.gameObject.activeInHierarchy=false;
  Check(InteractionUnderTest.TestRequest(e,p),"inactive inventory root blocks interaction");GameManager.instance=null;
 }
 static void Main(){OwnerChecks();InteractionChecks();InteractionGuards();ProjectionChecks();Console.WriteLine("PASS "+checks+" optimization correctness checks; controlled API-call comparisons, not Unity FPS.");}
}

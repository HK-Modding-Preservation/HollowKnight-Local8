using System;
using System.Linq;
using UnityEngine;
using HutongGames.PlayMaker;
using KO.HollowKnight8;

class Test
{
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static BoxCollider2D Box(string name,float x,float y,float w,float h,int layer=8,bool trigger=false)
 {
  var go=new GameObject{name=name,layer=layer};go.transform.position=new(x,y);var c=go.Add<BoxCollider2D>();c.size=new(w,h);c.isTrigger=trigger;return c;
 }
 static PlayerSlot Actor(int index,float x=60)
 {
  var go=new GameObject{name="Knight P"+(index+1),layer=9};go.transform.position=new(x,32.42f);
  var p=new PlayerSlot{Index=index,Hero=go.Add<HeroController>()};go.Add<BoxCollider2D>().size=new(.8f,2);go.Add<Rigidbody2D>();go.Add<tk2dSprite>();return p;
 }
 static CoopSession Setup(int count=2)
 {
  ArenaGather.Reset();HornetEncounterFlow.Reset();GameObject.All.Clear();Physics2D.World.Clear();NativeDreamFx.Moved.Clear();
  Time.time=Time.unscaledTime=100;Time.unscaledDeltaTime=.05f;GameManager.instance=new(){sceneName="Fungus1_04"};
  var s=new CoopSession();for(int i=0;i<count;i++)s.Players.Add(Actor(i,60+i*2));Plugin.Self=s.plugin;Plugin.Self.Session=s;
  Box("floor",50,13.7f,100,27.4f);Box("corridor ledge",70,29.4f,60,4);
  return s;
 }
 static HealthManager Boss()
 {
  var go=new GameObject{name="Hornet Boss 1"};go.transform.position=new(19.6f,28.6f);return go.Add<HealthManager>();
 }
 static CameraLockArea Corridor()
 {
  var parent=new GameObject{name="Boss camera zones"};var c=Box("CameraLockArea B2",51.3f,37.9f,25.6f,15,0,true);c.transform.parent=parent.transform;return c.gameObject.Add<CameraLockArea>();
 }
 static Fsm Encounter(HealthManager boss)
 {
  var f=new Fsm{Name="Encounter",GameObject=new GameObject{name="Hornet Infected Knight Encounter"}};
  f.Variables.Boss.Value=boss.gameObject;f.States=new[]{"Idle","Point","Box Up","Dialogue","Set Hero Active","Leap Antic","Start Fight","Destroy","Attacked"}.Select(name=>new FsmState{Name=name,Fsm=f}).ToArray();return f;
 }
 static void Enter(Fsm f,string state,PlayerSlot p){f.ActiveState=f.GetState(state);HornetEncounterFlow.ObserveState(f.ActiveState,p);}
 static void Run(CoopSession s,float seconds=1)
 {
  int frames=(int)Math.Ceiling(seconds/.05f);
  for(int i=0;i<frames;i++){
   Time.time+=.05f;Time.unscaledTime+=.05f;
   ArenaGather.Tick(s);
   foreach(var r in s.plugin.Routines.ToArray()){
    if(s.plugin.Ready.TryGetValue(r,out var at)&&Time.unscaledTime<at)continue;
    if(!r.MoveNext()){s.plugin.Routines.Remove(r);s.plugin.Ready.Remove(r);continue;}
    s.plugin.Ready[r]=Time.unscaledTime+(r.Current is WaitForSecondsRealtime wait?wait.Time:0);
   }
  }
 }
 static void Main()
 {
  var s=Setup();var boss=Boss();var corridor=Corridor();
  s.Players[1].Hero.transform.position=new(67.5f,32.42f);
  ArenaGather.CameraContact(corridor,s.Primary.Hero.GetComponent<Collider2D>());Run(s,.35f);
  ArenaGather.CameraContact(corridor,s.Primary.Hero.GetComponent<Collider2D>());
  Check(!ArenaGather.TransferActive&&s.plugin.Routines.Count==0&&NativeDreamFx.Moved.Count==0,"corridor camera lock started an encounter for a boss outside its bounds");
  Check(ArenaGather.Allows(boss.transform.position),"corridor camera lock restricted the actual boss arena");
  foreach(int count in new[]{2,4,8})for(int entrant=0;entrant<count;entrant++)
  {
   s=Setup(count);boss=Boss();corridor=Corridor();
   var left=Box("Battle Gate A",13.1f,37.6f,3.8f,21.4f);var right=Box("Battle Gate (1)",38.4f,35.8f,1,17.6f);
   left.enabled=right.enabled=false;
   var owner=s.Players[entrant];owner.Hero.transform.position=new(32.2f,28.42f);owner.Hero.controlReqlinquished=true;var original=owner.Hero.transform.position;
   var f=Encounter(boss);Enter(f,"Point",owner);
   Check(ArenaGather.TransferActive&&s.plugin.Routines.Count==1,"first dialogue did not start party gathering");
   Run(s);
   Check(owner.Hero.transform.position==original&&HornetEncounterFlow.Holds(owner),"gathering moved or released the native speaker");
   foreach(var p in s.Players){Check(p.Hero.transform.position.x>15&&p.Hero.transform.position.x<37.9f&&s.IsSafe(p.Hero.transform.position,p),"dialogue left a player outside or without safe floor");Check(!p.ArenaTransfer&&p.ArenaAlpha==1&&p.Vitals.Health==5&&p.Vitals.Blue==2&&p.Vitals.Soul==40,"gathering leaked fade/hold or changed player resources");}
   Check(NativeDreamFx.Moved.Count==count-1&&NativeDreamFx.Moved.All(p=>p!=owner),"dialogue gathered the wrong roster");
   Enter(f,"Dialogue",owner);Run(s,6);
   var friend=s.Players[(entrant+1)%count];friend.Hero.transform.position=new(50,32.42f);
   Enter(f,"Set Hero Active",owner);owner.Hero.controlReqlinquished=false;Enter(f,"Leap Antic",owner);
   left.enabled=right.enabled=true;ArenaGather.Observe(f,new(){Name="BG CLOSE"},new(){SentByFsm=f});Run(s);
   Check(friend.Hero.transform.position.x<37.9f&&s.IsSafe(friend.Hero.transform.position,friend),"long dialogue's expired window stranded a friend at native gate closure");
   Check(owner.Hero.transform.position==original&&!HornetEncounterFlow.Holds(owner),"gate-closure gather changed the native control handoff");
   Enter(f,"Start Fight",owner);Check(HornetEncounterFlow.Resolve(f)==null,"fight retained intro routing");
   ArenaGather.Reset();Check(s.Players.All(p=>!p.ArenaTransfer&&p.ArenaAlpha==1),"scene reset retained a gather fade or input hold");
  }
  s=Setup();boss=Boss();var active=Box("Boss arena",26,34,30,16,0,true).gameObject.Add<CameraLockArea>();s.Primary.Hero.transform.position=new(30,30);
  ArenaGather.CameraContact(active,s.Primary.Hero.GetComponent<Collider2D>());Run(s,.3f);ArenaGather.CameraContact(active,s.Primary.Hero.GetComponent<Collider2D>());
  Check(ArenaGather.TransferActive||s.plugin.Routines.Count>0,"a real camera lock containing its boss stopped gathering");
  s=Setup();boss=Boss();var blocked=Encounter(boss);s.Primary.Hero.transform.position=new(32,28.42f);
  Box("full obstruction",50,50,100,100);var friendBefore=s.Players[1].Hero.transform.position;Enter(blocked,"Point",s.Primary);Run(s,2);
  Check(s.Players[1].Hero.transform.position==friendBefore&&NativeDreamFx.Moved.Count==0,"unavailable landing bypassed safe placement");
  ArenaGather.Reset();Run(s,1);Check(s.Players.All(p=>!p.ArenaTransfer),"cancelled gather moved or held an old actor");
  Console.WriteLine("PASS "+checks+" whole-production arena-gather checks: supplied corridor geometry, all 2/4/8 intro owners, open native gates, safe floor and unchanged resources, long dialogue/re-entry at closure, valid boss locks, obstruction and reset. Physics and coroutine scheduling are controlled, not a Unity fight.");
 }
}

using System;using System.Collections.Generic;using UnityEngine;using UnityEngine.SceneManagement;
using HutongGames.PlayMaker;using HutongGames.PlayMaker.Actions;using KO.HollowKnight8;
static class Test {
 static int checks;static void Check(bool value,string why){checks++;if(!value)throw new Exception(why);}
 static PlayMakerFSM Fsm(string name,FsmState[] states,bool collider=false){var obj=new GameObject(name);if(collider)obj.Add<Collider2D>().isTrigger=true;var f=obj.Add<PlayMakerFSM>();f.Fsm=new Fsm{GameObject=obj,States=states,ActiveState=states[0]};return f;}
 static FsmState[] States(string method="unrelated",float delay=.1f)=>new[]{new FsmState{Name="State",Actions=new FsmStateAction[]{new CallMethodProper{methodName=new FsmString{Value=method}},new Wait{time=new FsmFloat{Value=delay}}}}};
 static void Step(float delta=.11f){Time.time+=delta;Time.unscaledTime+=delta;CoopBath.Tick(RoleSystem.Session);}
 static void Main(){
  CoopBath.Reset();UnityEngine.Object.All.Clear();SceneManager.Handle=1;Time.time=Time.unscaledTime=1;
  GameManager.instance=new GameObject("Manager").Add<GameManager>();var s=RoleSystem.Session=new CoopSession();
  for(int i=0;i<8;i++){var obj=new GameObject("Hero "+i);obj.Add<Collider2D>();s.Players.Add(new PlayerSlot{Index=i,Hero=obj.Add<HeroController>(),MP=i*2});}
  for(int i=0;i<1000;i++)Fsm("Statue "+i,States());
  var spring=Fsm("Modded Pool",States("TryAddMPChargeSpa",.1f),true);
  FsmString.Reads=0;Step();int first=FsmString.Reads;Check(first>=1001,"Room discovery missed modded FSM bath");
  for(int i=0;i<8;i++)Check(s.Players[i].MP==i*2+1,"Bath gives mana to the wrong player");
  FsmString.Reads=0;for(int i=0;i<60;i++)Step();
  Check(FsmString.Reads==0,"Warm bath scans still read thousands of unrelated method actions");
  for(int i=0;i<8;i++)Check(s.Players[i].MP==i*2+61,"Concurrent bath refill requires other players to leave");
  s.Players[0].Hero.transform.position=new Vector3{x=20};int primary=s.Players[0].MP;Step();
  Check(s.Players[0].MP==primary&&s.Players[1].MP==64,"P2 bathing credits P1 or blocks remaining bathers");
  for(int i=1;i<8;i++){s.Players[i].MP=99;s.Players[i].Reserve=0;}Step();
  for(int i=1;i<8;i++)Check(s.Players[i].Reserve==1,"Extra soul vessel does not refill independently");
  s.Players[2].Alive=false;int dead=s.Players[2].Reserve;Step();Check(s.Players[2].Reserve==dead,"Dead player receives soul");
  s.Players[3].SpawnPending=true;int pending=s.Players[3].Reserve;Step();Check(s.Players[3].Reserve==pending,"Pending room transfer receives soul");
  spring.gameObject.GetComponent<Collider2D>().destroyed=true;Step(3.1f);int stopped=s.Players[1].Reserve;Step();Check(s.Players[1].Reserve==stopped,"Destroyed bath persists in region cache");
  var dynamic=Fsm("New hidden spring",States(),true);Step(3.1f);dynamic.Fsm.States=States("TryAddMPChargeSpa",.2f);dynamic.Fsm.ActiveState=dynamic.Fsm.States[0];Step(3.1f);
  Check(s.Players[1].Reserve==stopped+1,"Replacing FSM states does not invalidate negative classification");
  var inPlace=Fsm("Dynamically changed spring",States(),true);Step(3.1f);
  ((CallMethodProper)inPlace.Fsm.States[0].Actions[0]).methodName.Value="TryAddMPChargeSpa";WorldRouting.Current=inPlace.Fsm;
  Check(CoopBath.Refill((h,n)=>throw new Exception("Vanilla refill invoked in co-op"),s.Players[1].Hero,2),"Native callback cannot register in-place modded bath");
  dynamic.gameObject.GetComponent<Collider2D>().destroyed=true;
  float time=Time.time;((Wait)inPlace.Fsm.ActiveState.Actions[1]).time.Value=.5f;Step(3.1f);
  stopped=s.Players[1].Reserve;CoopBath.Refill((h,n)=>false,s.Players[1].Hero,2);Step(.11f);CoopBath.Refill((h,n)=>false,s.Players[1].Hero,2);
  int at=s.Players[1].Reserve;Step(.11f);Check(s.Players[1].Reserve==at,"Changing active Wait value is ignored by cached bath definition");
  GameManager.instance.isPaused=true;stopped=s.Players[1].Reserve;Step(5);Check(s.Players[1].Reserve==stopped,"Paused bath refills players");GameManager.instance.isPaused=false;
  SceneManager.Handle=2;inPlace.gameObject.GetComponent<Collider2D>().destroyed=true;Step(3.1f);stopped=s.Players[1].Reserve;Step();Check(s.Players[1].Reserve==stopped,"Scene switch carries previous bath region");
  Console.WriteLine("PASS "+checks+" bath cache/ownership checks; 1000 unrelated FSMs classified once; warm method-action reads = 0");
 }
}

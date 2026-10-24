using System;
using System.Linq;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;
using UnityEngine.SceneManagement;
using KO.HollowKnight8;

internal static class Test {
 static int checks,failures;
 static void Check(bool condition,string message){checks++;if(!condition)throw new InvalidOperationException(message);}
 static bool Same(Color a,Color b)=>Math.Abs(a.r-b.r)<.0001f&&Math.Abs(a.g-b.g)<.0001f&&Math.Abs(a.b-b.b)<.0001f&&Math.Abs(a.a-b.a)<.0001f;
 static bool Same(Vector2 a,Vector2 b)=>a.x==b.x&&a.y==b.y;
 static bool Same(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;
 sealed class Saved {
  internal PlayerSlot Player;internal HeroController Hero;internal Rigidbody2D Body;internal HeroAnimationController Animation;internal tk2dSpriteAnimator Animator;
  internal tk2dBaseSprite Sprite,SkinBody;internal SpriteRenderer Skin,Child,Effect;internal Renderer Renderer;
  internal Color Original,SkinColor,ChildColor,EffectColor;internal Vector3 Position;internal Vector2 Velocity;internal float Angular,Gravity;internal bool Simulated,Kinematic,Controlled;
  internal Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip> Completed;internal Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip,int> Triggered;
  internal int Events;
  internal Saved(int index){
   var go=new GameObject("Knight P"+(index+1));Hero=go.Add<HeroController>();Player=new PlayerSlot{Index=index,Hero=Hero};
   Position=go.transform.position=new Vector3(index*3+2,10+index,index*.01f);
   Hero.spellControl=go.Add<PlayMakerFSM>();Hero.spellControl.Fsm=new Fsm{Name="Spell Control",GameObject=go};
   Body=go.Add<Rigidbody2D>();Body.simulated=Simulated=index%3!=2;Body.isKinematic=Kinematic=index%3==1;Body.gravityScale=Gravity=1.5f+index;
   Body.velocity=Velocity=new Vector2(index+1,-index-2);Body.angularVelocity=Angular=index*4+1;
   Animation=go.Add<HeroAnimationController>();Controlled=index%3!=2;Animation.Seed(Controlled);
   Animator=go.Add<tk2dSpriteAnimator>();Completed=(_,__)=>Events++;Triggered=(_,__,___)=>Events++;
   Animator.AnimationCompleted=Completed;Animator.AnimationEventTriggered=Triggered;
   var focus=new tk2dSpriteAnimationClip{name="Focus"};Animator.Clips.Add(focus.name,focus);
   Sprite=go.Add<tk2dBaseSprite>();Original=Sprite.color=new Color(.1f+index*.04f,.3f,.6f,.8f);
   Renderer=go.Add<Renderer>();Renderer.State.SetFloat("_FlashAmount",.13f);Renderer.State.SetFloat("Unrelated",index+9);
   var skin=new GameObject("Skin");go.Children.Add(skin);Skin=skin.Add<SpriteRenderer>();SkinColor=Skin.color=new Color(.4f,.2f,.1f,.75f);Player.SkinRenderer=Skin;
   SkinBody=skin.Add<tk2dBaseSprite>();SkinBody.color=Original;
   var child=new GameObject("custom knight skin");go.Children.Add(child);Child=child.Add<SpriteRenderer>();ChildColor=Child.color=new Color(.5f,.1f,.4f,.6f);
   var effect=new GameObject("focus effect");go.Children.Add(effect);Effect=effect.Add<SpriteRenderer>();EffectColor=Effect.color=new Color(.4f,.7f,.6f,.9f);
  }
  internal void Unchanged(string where,bool motion=true){
   Check(Body.simulated==Simulated&&Body.isKinematic==Kinematic&&Body.gravityScale==Gravity,where+" physics");
   if(motion)Check(Same(Body.velocity,Velocity)&&Body.angularVelocity==Angular,where+" motion");
   Check(Animation.controlEnabled==Controlled,where+" animation control");
   Check(Animator.AnimationCompleted==Completed&&Animator.AnimationEventTriggered==Triggered,where+" callbacks");
   Check(Same(Sprite.color,Original)&&Same(SkinBody.color,Original)&&Same(Skin.color,SkinColor)&&Same(Child.color,ChildColor)&&Same(Effect.color,EffectColor),where+" artwork");
   Check((float)Renderer.State.Values["_FlashAmount"]==.13f&&(float)Renderer.State.Values["Unrelated"]==Player.Index+9,where+" material block");
   Check(Same(Hero.transform.position,Position),where+" position");
  }
  internal void Held(){Check(EndingAbsorption.Holds(Player),"guest held");Check(!Body.simulated&&Body.isKinematic&&Body.gravityScale==0&&Same(Body.velocity,Vector2.zero)&&Body.angularVelocity==0,"guest frozen");Check(!Animation.controlEnabled&&Animator.AnimationCompleted==null&&Animator.AnimationEventTriggered==null,"guest callbacks isolated");Check(Events==0,"no native guest animation events");}
 }
 sealed class Fixture {
  internal CoopSession Session;internal Saved[] Actors;internal GameManager Manager=>GameManager.instance;
  internal Fixture(int count=2){
   EndingAbsorption.Reset();EndingEssenceFx.Created.Clear();EndingEssenceFx.Cues=EndingEssenceFx.Climaxes=0;EndingEssenceFx.FailIndex=-1;Diagnostics.Messages.Clear();
   Time.time=100;Time.deltaTime=0;SceneManager.Handle=17;GameManager.instance=new GameManager{sceneName="Room_Final_Boss_Core"};
   Session=new CoopSession();Actors=Enumerable.Range(0,count).Select(i=>new Saved(i)).ToArray();Session.Players.AddRange(Actors.Select(x=>x.Player));Plugin.Self=new Runtime{Session=Session};
  }
  internal FsmState NativeState(string name,int index=0)=>new(){Fsm=Actors[index].Hero.spellControl.Fsm,Name=name};
  internal void Observe(string name,int index=0)=>EndingAbsorption.Observe(NativeState(name,index));
  internal void Begin(bool alias=false){
   if(alias)EndingAbsorption.Observe(new FsmState{Fsm=new Fsm{Name="Corpse",GameObject=new GameObject("Boss Corpse")},Name="Set Knight Focus"});
   Observe(alias?"Focus Start":"Focus Start D");Check(EndingAbsorption.Active==Actors.Skip(1).Any(p=>p.Player.Alive&&p.Player.Ready&&p.Player.Connected&&!p.Player.Down&&!p.Player.Hazard&&!p.Player.Retiring),"expected cohort started");
  }
  internal void Tick(float delta){Time.deltaTime=delta;Time.time+=delta;EndingAbsorption.Tick(Session);EndingAbsorption.Visual(Session);}
  internal void Advance(float seconds,float step=.1f){for(float remaining=seconds;remaining>.0001f;){float dt=Math.Min(step,remaining);Tick(dt);remaining-=dt;}}
  internal void Restored(){Check(!EndingAbsorption.Active,"ending released");foreach(var a in Actors)a.Unchanged("P"+(a.Player.Index+1));Check(EndingEssenceFx.Created.All(x=>x.Disposes>0),"all cosmetic emitters disposed");}
 }
 static void Scope(){
  var f=new Fixture(4);EndingAbsorption.Observe(null);EndingAbsorption.Observe(new FsmState());f.Observe("Focus Start D",1);f.Observe("Focus Start");f.Observe("Focus Heal");Check(!EndingAbsorption.Active,"guest/normal focus ignored");
  f.Manager.sceneName="Dream_Final_Boss";f.Observe("Focus Start D");Check(!EndingAbsorption.Active,"Radiance room ignored");
  f.Manager.sceneName="Room_Final_Boss_Core";f.Session.Active=false;f.Observe("Focus Start D");Check(!EndingAbsorption.Active,"inactive session ignored");
  f.Session.Active=true;Plugin.Self=null;f.Observe("Focus Start D");Check(!EndingAbsorption.Active,"missing runtime ignored");
  f=new Fixture(1);f.Begin();f.Actors[0].Unchanged("solo native primary");Check(EndingEssenceFx.Cues==0,"solo no cosmetic cue");
  f=new Fixture();EndingAbsorption.Observe(new FsmState{Fsm=new Fsm{Name="Corpse",GameObject=new GameObject("Boss Corpse")},Name="Set Knight Focus"});Time.time+=31;f.Observe("Focus Start");Check(!EndingAbsorption.Active,"expired alias arm ignored");
 }
 static void NormalEnding(){
  foreach(int count in new[]{2,4,8})foreach(float step in new[]{1f/120,.05f,.5f})foreach(bool alias in new[]{false,true}){
   var f=new Fixture(count);f.Begin(alias);foreach(var a in f.Actors.Skip(1))a.Held();f.Actors[0].Unchanged("native primary capture");
   f.Advance(20.5f,step);Check(EndingAbsorption.Active,"full native corpse sequence can exceed twelve seconds");Check(EndingEssenceFx.Climaxes==1,"one visual climax");
   foreach(var a in f.Actors.Skip(1)){a.Held();Check(a.Sprite.color.a==0&&a.Skin.color.a==0&&a.Child.color.a==0,"dissolved guest artwork");Check(Same(a.Effect.color,a.EffectColor),"unrelated effects untouched");}
   f.Manager.IsLoadingSceneTransition=true;f.Tick(.1f);f.Advance(90,1);Check(EndingAbsorption.Active,"native loader retains its handoff regardless of cosmetic deadline");
   SceneManager.Handle++;f.Tick(.1f);f.Restored();Check(EndingEssenceFx.Cues==2,"no duplicate start/climax");
  }
 }
 static void Cancel(){
  foreach(int count in new[]{2,4,8})foreach(float at in new[]{.1f,3f,5.5f,18f}){
   var f=new Fixture(count);f.Begin();f.Advance(at);f.Observe("Regain Control",1);Check(EndingAbsorption.Active,"guest cancellation cannot release native ending");
   var foreign=new FsmState{Name="Regain Control",Fsm=new Fsm{Name="Spell Control",GameObject=new GameObject("Other Knight")}};EndingAbsorption.Observe(foreign);Check(EndingAbsorption.Active,"foreign same-named FSM cannot release ending");
   f.Observe("Regain Control");f.Restored();f.Begin();Check(EndingAbsorption.Active,"can retry ending after cancellation");EndingAbsorption.Reset();f.Restored();
  }
 }
 static void MissedCancellation(){
  foreach(int count in new[]{2,4,8})foreach(float step in new[]{1f/60,.1f,1f}){
   var f=new Fixture(count);f.Begin();f.Advance(6,step);Check(EndingEssenceFx.Climaxes==1,"watchdog case reached climax");
   f.Advance(EndingAbsorption.SafetySeconds+1,step);f.Restored();
   Check(Diagnostics.Messages.Any(x=>x.Contains("timeout")),"fallback logged");
  }
 }
 static void Handoff(){
  foreach(bool blankRoom in new[]{false,true}){
   var f=new Fixture(8);f.Begin();if(blankRoom)f.Manager.sceneName="";else f.Manager.IsLoadingSceneTransition=true;
   f.Tick(.01f);Check(EndingAbsorption.Active,"immediate native transition accepted");Check(!EndingAbsorption.Holds(f.Actors[0].Player),"native primary never held");
   foreach(var a in f.Actors.Skip(1))Check(a.Sprite.color.a==0&&a.Skin.color.a==0&&a.Child.color.a==0,"all guests hidden even for immediate native fade");
   f.Advance(80,1);Check(EndingAbsorption.Active,"loading has no cosmetic deadline");
   // A loader that explicitly returns to this same room must not leave a
   // permanent hold behind: it receives the same bounded cancellation fallback.
   f.Manager.sceneName="Room_Final_Boss_Core";f.Manager.IsLoadingSceneTransition=false;f.Advance(EndingAbsorption.SafetySeconds+1);f.Restored();
  }
 }
 static void Pause(){
  var f=new Fixture(8);f.Begin();f.Advance(1);f.Manager.isPaused=true;float before=f.Actors[1].Sprite.color.a;f.Advance(100,1);Check(EndingAbsorption.Active&&f.Actors[1].Sprite.color.a==before,"paused native animation/deadline frozen");
  f.Manager.isPaused=false;f.Advance(EndingAbsorption.SafetySeconds+1);f.Restored();
 }
 static void Invalidations(){
  Action<Fixture>[] cases={f=>f.Session.Active=false,f=>f.Session.TeamWipe=true,f=>SceneManager.Handle++,f=>f.Manager.sceneName="Town",f=>GameManager.instance=null,f=>f.Actors[0].Hero.Exists=false};
  foreach(var invalidate in cases){var f=new Fixture(4);f.Begin();f.Advance(7);invalidate(f);f.Tick(.1f);f.Restored();}
  var x=new Fixture(4);x.Begin();x.Advance(7);EndingAbsorption.Tick(new CoopSession());x.Restored();
  x=new Fixture(4);x.Begin();x.Advance(7);var old=x.Actors[0];x.Actors[0].Player.Hero=new Saved(0).Hero;x.Tick(.1f);x.Restored();
 }
 static void OwnBodyOnly(){
  foreach(float at in new[]{1f,7f}){
   var f=new Fixture(4);f.Begin();f.Advance(at);var old=f.Actors[1];var replacement=new Saved(1);old.Player.Hero=replacement.Hero;f.Tick(.1f);
   Check(!EndingAbsorption.Holds(old.Player),"replaced guest no longer held");replacement.Unchanged("replacement body");EndingAbsorption.Reset();old.Unchanged("captured old body",motion:false);replacement.Unchanged("replacement body after reset");
  }
  foreach(string reason in new[]{"dead","down","hazard","retiring"}){
   var f=new Fixture(4);f.Begin();f.Advance(7);var a=f.Actors[1];
   switch(reason){case "dead":a.Player.Alive=false;break;case "down":a.Player.Down=true;a.Player.Alive=false;break;case "hazard":a.Player.Hazard=true;a.Player.Alive=false;break;case "retiring":a.Player.Retiring=true;a.Player.Alive=false;break;}
   a.Body.simulated=false;a.Body.isKinematic=true;a.Body.gravityScale=0;a.Body.velocity=new Vector2(14,-2);a.Body.angularVelocity=20;
   f.Tick(.1f);Check(!EndingAbsorption.Holds(a.Player),"unavailable guest released");EndingAbsorption.Reset();
   Check(!a.Body.simulated&&a.Body.isKinematic&&a.Body.gravityScale==0&&a.Body.angularVelocity==20&&Same(a.Body.velocity,new Vector2(14,-2)),"native death/hazard retains body ownership");
   Check(!a.Animation.controlEnabled,"native death animation retains ownership");Check(a.Animator.AnimationCompleted==a.Completed&&a.Animator.AnimationEventTriggered==a.Triggered,"unavailable actor callbacks restored");
  }
  var removed=new Fixture(4);removed.Begin();removed.Session.Players.Remove(removed.Actors[1].Player);removed.Tick(.1f);removed.Actors[1].Unchanged("removed captured actor");EndingAbsorption.Reset();removed.Restored();
 }
 static void SetupFailure(){
  var f=new Fixture(4);EndingEssenceFx.FailIndex=1;f.Begin();f.Advance(7);f.Observe("Regain Control");f.Restored();Check(Diagnostics.Messages.Any(x=>x.Contains("injected emitter")),"partial setup error recorded");
  f=new Fixture(8);f.Actors[1].Player.Connected=false;f.Actors[2].Player.Ready=false;f.Actors[3].Player.Alive=false;f.Actors[4].Player.Down=true;f.Actors[5].Player.Hazard=true;f.Actors[6].Player.Retiring=true;
  f.Actors[7].Animator.Clips.Clear();f.Begin();for(int i=1;i<7;i++){Check(!EndingAbsorption.Holds(f.Actors[i].Player),"invalid initial guest skipped");f.Actors[i].Unchanged("excluded guest");}
  Check(EndingAbsorption.Holds(f.Actors[7].Player),"missing cosmetic clip cannot prevent valid follower release");f.Advance(EndingAbsorption.SafetySeconds+1);f.Restored();
 }
 static void Run(string name,Action action){try{action();Console.WriteLine("PASS "+name);}catch(Exception ex){failures++;Console.WriteLine("FAIL "+name+": "+ex.Message);}finally{EndingAbsorption.Reset();}}
 static int Main(){
  Run("ending scope and native primary ownership",Scope);Run("normal native ending with 2/4/8 players at multiple frame rates",NormalEnding);
  Run("native focus cancellation before and after cosmetic climax",Cancel);Run("missing native cancellation callback has bounded recovery",MissedCancellation);
  Run("immediate native fade, loading handoff and cancelled loader",Handoff);Run("paused ending does not consume animation time",Pause);
  Run("scene/session/primary invalidation restores captured guests",Invalidations);Run("replaced and unavailable bodies retain native ownership",OwnBodyOnly);Run("partial cosmetic setup and invalid recipients",SetupFailure);
  Console.WriteLine($"{(failures==0?"PASS":"FAIL")} ending lifecycle: {checks} assertions, {failures} failed cases");return failures==0?0:1;
 }
}

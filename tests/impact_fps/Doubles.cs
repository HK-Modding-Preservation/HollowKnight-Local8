using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 internal class Object {internal bool Destroyed;public static implicit operator bool(Object x)=>x!=null&&!x.Destroyed;}
 internal class GameObject:Object {
  internal string name;internal bool activeInHierarchy=true;internal readonly List<Component> Components=new();internal readonly List<GameObject>Children=new();internal readonly Transform transform=new();
  internal GameObject(string n="object"){name=n;transform.gameObject=this;}
  internal T Add<T>(T c)where T:Component{Components.Add(c);c.gameObject=this;return c;}
  internal T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault(c=>c);
  internal T GetComponentInChildren<T>(bool inactive)where T:Component{var c=GetComponent<T>();return c??Children.Select(x=>x.GetComponentInChildren<T>(inactive)).FirstOrDefault(x=>x);}
 }
 internal class Transform:Object{internal GameObject gameObject;internal Vector3 position;}
 internal class Component:Object{internal GameObject gameObject;internal Transform transform=>gameObject.transform;internal T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();}
 internal struct Vector2:IEquatable<Vector2>{internal float x,y;internal Vector2(float x,float y){this.x=x;this.y=y;}internal static Vector2 zero=>new();public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);internal static float Distance(Vector2 a,Vector2 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));public bool Equals(Vector2 b)=>x.Equals(b.x)&&y.Equals(b.y);public override bool Equals(object o)=>o is Vector2 v&&Equals(v);public override int GetHashCode()=>HashCode.Combine(x,y);public override string ToString()=>x+","+y;}
 internal struct Vector3:IEquatable<Vector3>{internal float x,y,z;internal Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}internal static Vector3 zero=>new();public bool Equals(Vector3 b)=>x.Equals(b.x)&&y.Equals(b.y)&&z.Equals(b.z);public override bool Equals(object o)=>o is Vector3 v&&Equals(v);public override int GetHashCode()=>HashCode.Combine(x,y,z);public override string ToString()=>x+","+y+","+z;}
 internal class Rigidbody2D:Component{internal bool simulated=true,isKinematic;internal float gravityScale=2.4f;internal Vector2 velocity;}
 internal enum ParticleSystemStopBehavior{StopEmittingAndClear}
 internal class ParticleSystem:Component{internal int Stops,Plays,Emits;internal bool isPlaying,Emission;internal void Stop(bool children,ParticleSystemStopBehavior behavior){if(children)throw new Exception("Recursive FX suppression");Stops++;isPlaying=false;}internal void Play(){Plays++;isPlaying=true;}internal void Emit(int n){Emits+=n;}}
 internal static class Time{internal static float unscaledTime,time,deltaTime=.1f;}
 internal static class Application{internal static bool isFocused=true;}
}
namespace GlobalEnums{internal enum ActorStates{idle,grounded,airborne,no_input}internal enum GameState{INACTIVE,PLAYING,PAUSED,CUTSCENE,ENTERING_LEVEL,EXITING_LEVEL} }
internal class HeroControllerStates{internal bool wallSliding,superDashOnWall,transitioning,swimming,dead,recoilFrozen,recoiling,dashing,superDashing,casting,freezeCharge,bouncing,doubleJumping,invulnerable,shadowDashing;}
internal class HeroController:UnityEngine.Component {
 internal bool controlReqlinquished,acceptingInput=true,inAcid;internal readonly HeroControllerStates cState=new();internal float DEFAULT_GRAVITY=2.4f;internal float prevGravityScale,recoilTimer;internal GlobalEnums.ActorStates hero_state=GlobalEnums.ActorStates.grounded;internal PlayMakerFSM damageEffectFSM;
 internal HeroController(){new UnityEngine.GameObject("Knight").Add(this);gameObject.Add(new UnityEngine.Rigidbody2D());}
 internal void AffectedByGravity(bool value){var b=GetComponent<UnityEngine.Rigidbody2D>();if(b.gravityScale>float.Epsilon&&!value){prevGravityScale=b.gravityScale;b.gravityScale=0;}else if(!(b.gravityScale>float.Epsilon)&&value){b.gravityScale=prevGravityScale;prevGravityScale=0;}}
 internal void NativeUpdate(){if(hero_state==GlobalEnums.ActorStates.no_input){if(cState.recoiling){if(recoilTimer<.5f)recoilTimer+=UnityEngine.Time.deltaTime;else{cState.recoiling=false;recoilTimer=0;GetComponent<UnityEngine.Rigidbody2D>().velocity=UnityEngine.Vector2.zero;AffectedByGravity(true);hero_state=GlobalEnums.ActorStates.grounded;}}}else if(cState.recoiling){cState.recoiling=false;AffectedByGravity(true);}}
 internal void NativeRelinquish(){if(controlReqlinquished||cState.dead)return;GetComponent<UnityEngine.Rigidbody2D>().velocity=UnityEngine.Vector2.zero;AffectedByGravity(true);acceptingInput=false;controlReqlinquished=true;}
 internal void NativeRegain(){acceptingInput=true;hero_state=GlobalEnums.ActorStates.grounded;if(controlReqlinquished&&!cState.dead){AffectedByGravity(true);controlReqlinquished=false;}}
}
internal class ConveyorMovementHero:UnityEngine.Component {internal HeroController heroCon;internal bool enabled=true,onConveyor,gravityOff;internal float ySpeed;}
internal class HealthManager:UnityEngine.Component{}
internal class DamageHero:UnityEngine.Component{}
internal class DamageEnemies:UnityEngine.Component{}
internal class KnightHatchling:UnityEngine.Component{}
internal class InvulnerablePulse:UnityEngine.Component{internal bool pulsing=true;internal int Stops;internal void stopInvulnerablePulse(){Stops++;pulsing=false;}}
internal class GameManager:UnityEngine.Object {internal static GameManager instance;internal bool isPaused,TimeSlowed,IsLoadingSceneTransition,HasFinishedEnteringScene=true,GameplayScene=true;internal GlobalEnums.GameState gameState=GlobalEnums.GameState.PLAYING;internal int WorldChecks;internal bool IsGameplayScene(){WorldChecks++;return GameplayScene;}}
internal static class BossSceneController{internal static bool IsBossScene=true;}
internal class PlayMakerFSM:UnityEngine.Object{internal HutongGames.PlayMaker.Fsm Fsm;internal string FsmName=>Fsm.Name;}
namespace HutongGames.PlayMaker {
 internal class Fsm{internal string Name,ActiveStateName;internal FsmState[] States;internal KO.HollowKnight8.PlayerSlot Player;internal UnityEngine.GameObject GameObject;internal UnityEngine.GameObject GetOwnerDefaultTarget(FsmOwnerDefault v)=>v?.GameObject;}
 internal class FsmState{internal string Name;internal FsmStateAction[] Actions;}
 internal class FsmOwnerDefault{internal UnityEngine.GameObject GameObject;}
 internal class FsmGameObject{internal UnityEngine.GameObject Value;}
 internal class FsmBool{internal bool Value;}
 internal class FsmInt{internal int Value;}
 internal class FsmFloat{internal float Value;internal bool IsNone;}
 internal class FsmStateAction{internal Fsm Fsm;internal int Finishes,NativeCalls;internal void Finish(){Finishes++;}}
}
namespace HutongGames.PlayMaker.Actions{
 internal class FlingObjectsFromGlobalPool:HutongGames.PlayMaker.FsmStateAction{internal HutongGames.PlayMaker.FsmGameObject gameObject;}
 internal class FlingObjectsFromGlobalPoolVel:HutongGames.PlayMaker.FsmStateAction{internal HutongGames.PlayMaker.FsmGameObject gameObject;}
 internal class FlingObjectsFromGlobalPoolTime:HutongGames.PlayMaker.FsmStateAction{internal HutongGames.PlayMaker.FsmGameObject gameObject;}
 internal class PlayParticleEmitter:HutongGames.PlayMaker.FsmStateAction{internal HutongGames.PlayMaker.FsmOwnerDefault gameObject;internal HutongGames.PlayMaker.FsmInt emit=new(){Value=20};}
 internal class PlayParticleEmitterInState:HutongGames.PlayMaker.FsmStateAction{internal HutongGames.PlayMaker.FsmOwnerDefault gameObject;}
 internal class SetParticleEmission:HutongGames.PlayMaker.FsmStateAction{internal HutongGames.PlayMaker.FsmOwnerDefault gameObject;internal HutongGames.PlayMaker.FsmBool emission;}
 internal class SetVelocity2d:HutongGames.PlayMaker.FsmStateAction{internal HutongGames.PlayMaker.FsmFloat x,y;internal bool everyFrame;}
 internal class SetGravity2dScale:HutongGames.PlayMaker.FsmStateAction{internal HutongGames.PlayMaker.FsmFloat gravityScale;}
}

namespace On {
 internal static class HeroController {
  internal delegate void orig_RelinquishControl(global::HeroController h);internal delegate void hook_RelinquishControl(orig_RelinquishControl o,global::HeroController h);internal static event hook_RelinquishControl RelinquishControl;
  internal delegate void orig_RegainControl(global::HeroController h);internal delegate void hook_RegainControl(orig_RegainControl o,global::HeroController h);internal static event hook_RegainControl RegainControl;
  internal static void Relinquish(global::HeroController h){if(RelinquishControl!=null)RelinquishControl(x=>x.NativeRelinquish(),h);else h.NativeRelinquish();}
  internal static void Regain(global::HeroController h){if(RegainControl!=null)RegainControl(x=>x.NativeRegain(),h);else h.NativeRegain();}
  internal static int Subscribers=>(RelinquishControl?.GetInvocationList().Length??0)+(RegainControl?.GetInvocationList().Length??0);
 }
}
namespace KO.HollowKnight8 {
 internal sealed class Setting<T>{internal T Value;}
 internal class Local8Runtime:UnityEngine.Object{internal CoopSession Session;internal bool Panel;internal Setting<bool>PerformanceMode=new(),ReduceHitEffects=new();}
 internal static class Plugin{internal static Local8Runtime Self;}
 internal class Vitals{internal bool Invincible;}
 internal class PlayerSlot {internal int Index;internal HeroController Hero=new();internal bool AliveOverride=true;internal bool Alive{get=>AliveOverride&&Hero&&!Down&&!Hazard&&!Retiring&&Hero.gameObject.activeInHierarchy;set=>AliveOverride=value;}internal bool Ready=true,Connected=true,Down,Hazard,Retiring,InputBlocked,Reviving,ArenaTransfer,SpawnPending,AcidAssistActive,HeroBoxInactive;internal float WakeUntil,LastDamageAt,ProtectionUntil;internal readonly Vitals Vitals=new();internal bool Scripted,Challenge,Bench,CustomBench,Warp,Voting;}
 internal class CoopSession{internal bool Active=true,EntryProxy,Gameplay=true,InBounds=true;internal readonly List<PlayerSlot>Players=new();internal PlayerSlot Primary=>Players.FirstOrDefault();internal PlayerSlot Resolve(object value)=>value is HutongGames.PlayMaker.Fsm f?f.Player:Players.FirstOrDefault(p=>p.Hero==value);internal bool InRoom(UnityEngine.Vector3 at)=>InBounds;}
 internal static class CombatEffects{internal static bool Enabled=>Plugin.Self&&Plugin.Self.Session!=null&&Plugin.Self.Session.Active&&Plugin.Self.ReduceHitEffects.Value;}
 internal static class PerformanceTuning{internal static bool Enabled=>Plugin.Self&&Plugin.Self.PerformanceMode.Value;internal static bool ReduceDamage=>Enabled;}
 internal static class PlayerContext{internal static bool TargetingEnemy;internal static PlayerSlot Current;}
 internal static class Controls{internal static bool LastBlocked;internal static int Pumps;internal static void Pump(PlayerSlot p,bool block){Pumps++;LastBlocked=block;}}
 internal static class ScriptedParty{internal static bool Active,TracksChamber;internal static int NativeMoves;internal static void NativeMove(CoopSession s,PlayerSlot p,UnityEngine.Vector3 from,UnityEngine.Vector3 to){NativeMoves++;}internal static bool Holds(PlayerSlot p)=>p.Scripted;}
 internal static class ChallengeSequence{internal static bool Holds(PlayerSlot p)=>p.Challenge;}
 internal static class BenchSeats{internal static bool Seated(PlayerSlot p)=>p.Bench;internal static bool Custom(PlayerSlot p)=>p.CustomBench;}
 internal static class EmergencyWarp{internal static bool Active(PlayerSlot p)=>p.Warp;}
 internal static class CoopEnding{internal static bool Active;internal static bool HoldsActor(PlayerSlot p)=>Active;}
 internal static class RoleSystem{internal static PlayerSlot Joining;internal static bool BlockHero(HeroController hero)=>Joining!=null&&Joining.Hero==hero;}
 internal static class Charms{internal static bool NativeMenuOpen,Editing;internal static int Selected;}
 internal static class TransitionVote{internal static bool Pending;internal static bool Holding(PlayerSlot p)=>p.Voting;}
 internal static class PvpMatch{internal static bool BlocksInput;}
 internal static class PickupCard{internal static PlayerSlot Owner;}
 internal static class InteractionRouter{internal static PlayerSlot ActivePlayer;internal static bool Interaction;internal static bool IsInteraction(HutongGames.PlayMaker.Fsm f)=>Interaction;}
 internal static class ArenaGather{internal static bool TransferActive;}
 internal static class NativeDreamFx{internal static int Trails;internal static void TeleportTrail(PlayerSlot p,UnityEngine.Vector3 a,UnityEngine.Vector3 b){Trails++;}}
 internal static class Diagnostics{internal static readonly List<string>Lines=new();internal static void Write(string s)=>Lines.Add(s);}
}

namespace On.HutongGames.PlayMaker.Actions {
internal static class FlingObjectsFromGlobalPool {internal delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool a);internal delegate void hook_OnEnter(orig_OnEnter o,global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool a);internal static event hook_OnEnter OnEnter;internal static int Subscribers=>OnEnter?.GetInvocationList().Length??0;private static void Native(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool a){a.NativeCalls++;a.Finish();}internal static void Run(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool a){if(OnEnter!=null)OnEnter(Native,a);else Native(a);}}
internal static class FlingObjectsFromGlobalPoolVel {internal delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolVel a);internal delegate void hook_OnEnter(orig_OnEnter o,global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolVel a);internal static event hook_OnEnter OnEnter;internal static int Subscribers=>OnEnter?.GetInvocationList().Length??0;private static void Native(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolVel a){a.NativeCalls++;a.Finish();}internal static void Run(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolVel a){if(OnEnter!=null)OnEnter(Native,a);else Native(a);}}
internal static class FlingObjectsFromGlobalPoolTime {internal delegate void orig_OnUpdate(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime a);internal delegate void hook_OnUpdate(orig_OnUpdate o,global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime a);internal static event hook_OnUpdate OnUpdate;internal static int Subscribers=>OnUpdate?.GetInvocationList().Length??0;private static void Native(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime a){a.NativeCalls++;}internal static void Run(global::HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPoolTime a){if(OnUpdate!=null)OnUpdate(Native,a);else Native(a);}}
internal static class PlayParticleEmitter {internal delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.PlayParticleEmitter a);internal delegate void hook_OnEnter(orig_OnEnter o,global::HutongGames.PlayMaker.Actions.PlayParticleEmitter a);internal static event hook_OnEnter OnEnter;internal static int Subscribers=>OnEnter?.GetInvocationList().Length??0;private static void Native(global::HutongGames.PlayMaker.Actions.PlayParticleEmitter a){a.NativeCalls++;a.Finish();var ps=a.gameObject?.GameObject?.GetComponent<UnityEngine.ParticleSystem>();if(ps){if(a.emit.Value>0)ps.Emit(a.emit.Value);else ps.Play();}}internal static void Run(global::HutongGames.PlayMaker.Actions.PlayParticleEmitter a){if(OnEnter!=null)OnEnter(Native,a);else Native(a);}}
internal static class PlayParticleEmitterInState {internal delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.PlayParticleEmitterInState a);internal delegate void hook_OnEnter(orig_OnEnter o,global::HutongGames.PlayMaker.Actions.PlayParticleEmitterInState a);internal static event hook_OnEnter OnEnter;internal static int Subscribers=>OnEnter?.GetInvocationList().Length??0;private static void Native(global::HutongGames.PlayMaker.Actions.PlayParticleEmitterInState a){a.NativeCalls++;a.gameObject?.GameObject?.GetComponent<UnityEngine.ParticleSystem>()?.Play();}internal static void Run(global::HutongGames.PlayMaker.Actions.PlayParticleEmitterInState a){if(OnEnter!=null)OnEnter(Native,a);else Native(a);}}
internal static class SetParticleEmission {internal delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.SetParticleEmission a);internal delegate void hook_OnEnter(orig_OnEnter o,global::HutongGames.PlayMaker.Actions.SetParticleEmission a);internal static event hook_OnEnter OnEnter;internal static int Subscribers=>OnEnter?.GetInvocationList().Length??0;private static void Native(global::HutongGames.PlayMaker.Actions.SetParticleEmission a){a.NativeCalls++;a.Finish();var ps=a.gameObject?.GameObject?.GetComponent<UnityEngine.ParticleSystem>();if(ps)ps.Emission=a.emission.Value;}internal static void Run(global::HutongGames.PlayMaker.Actions.SetParticleEmission a){if(OnEnter!=null)OnEnter(Native,a);else Native(a);}}
}

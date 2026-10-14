using System;using System.Linq;using System.Collections.Generic;
using UnityEngine;using HutongGames.PlayMaker;
namespace UnityEngine {
 public class Object {public bool Alive=true;public string name="";public static readonly List<Object> All=new();public static int Searches;public static implicit operator bool(Object o)=>o!=null&&o.Alive;public static T[] FindObjectsOfType<T>()where T:Object{Searches++;return All.OfType<T>().Where(x=>x&&(!(x is Component c)||c.gameObject.activeInHierarchy)).ToArray();}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class MonoBehaviour:Component {public bool enabled=true;}
 public class GameObject:Object {public readonly Transform transform;public bool activeInHierarchy=true;public readonly List<Component> Components=new();public static int ParentQueries;
  public GameObject(string n,Transform parent=null){name=n;transform=new(){gameObject=this,parent=parent};Components.Add(transform);}
  public T Add<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);Object.All.Add(c);return c;}
  public T AddComponent<T>()where T:Component,new()=>Add<T>();
  public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault(x=>x);
  public T GetComponentInParent<T>()where T:Component{ParentQueries++;for(var t=transform;t!=null;t=t.parent){var c=t.gameObject.Components.OfType<T>().FirstOrDefault(x=>x);if(c)return c;}return null;}
 }
 public class Transform:Component {public Transform parent;public new string name=>gameObject.name;private Vector3 at;public static int PositionReads;public Vector3 position{get{PositionReads++;return at;}set=>at=value;}public Vector3 localScale=new(){x=1,y=1,z=1};}
 public struct Vector3{public float x,y,z;public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new(){x=a.x-b.x,y=a.y-b.y,z=a.z-b.z};}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public class Collider2D:Component{}
 public class Rigidbody2D:Component{public Vector2 velocity;}
 public static class Mathf{public static float Abs(float f)=>Math.Abs(f);}
 public static class Time {public static float unscaledTime,timeScale=1;public static int frameCount;}
}
namespace UnityEngine.SceneManagement {public struct Scene{public string name;}public static class SceneManager{public static string Name="GG_Soul_Tyrant";public static Scene GetActiveScene()=>new(){name=Name};}}
namespace UnityEngine.EventSystems {public class BaseInputModule:UnityEngine.MonoBehaviour{}}
namespace InControl {
 public class PlayerAction {public bool IsPressed;}
 public class PlayerTwoAxisAction{}
 public class InControlInputModule:UnityEngine.EventSystems.BaseInputModule {public PlayerTwoAxisAction MoveAction;public PlayerAction SubmitAction,CancelAction;}
 public class HollowKnightInputModule:UnityEngine.EventSystems.BaseInputModule {public PlayerTwoAxisAction MoveAction;public PlayerAction SubmitAction,CancelAction,JumpAction,AttackAction,CastAction;}
}
public class HeroActions {public readonly InControl.PlayerTwoAxisAction moveVector=new();public readonly InControl.PlayerAction menuSubmit=new(),menuCancel=new(),jump=new(),attack=new(),cast=new(),pause=new(),openInventory=new(),paneLeft=new(),paneRight=new();}
public class HeroController:UnityEngine.Component{public HeroControllerStates cState=new();}
public class HeroControllerStates{public bool facingRight;}
public class HealthManager:UnityEngine.Component{}
public class DamageHero:UnityEngine.Component{}
public class InputHandler:UnityEngine.Object{public HeroActions inputActions;}
public class GameManager:UnityEngine.Object{public static GameManager instance=new(){inputHandler=new()};public InputHandler inputHandler;}
namespace HutongGames.PlayMaker {
 public class Fsm {public string Name;public UnityEngine.GameObject GameObject;public FsmState[] States;public FsmState ActiveState;internal KO.HollowKnight8.PlayerSlot Player;public bool Enemy,Hud;}
 public class FsmEvent{public string Name;}
 public class FsmEventData{public Fsm SentByFsm;}
 public class FsmState {public string Name;public FsmStateAction[] Actions;public FsmTransition[] Transitions;}
 public class FsmTransition{public string EventName,ToState;}
 public class FsmStateAction{}
 public class FsmString{public string Value;}
 public class FsmBool{public bool Value;}
 public class FsmInt{public int Value;}
}
namespace HutongGames.PlayMaker.Actions {
 public class SetPlayerDataBool:HutongGames.PlayMaker.FsmStateAction {public HutongGames.PlayMaker.FsmString boolName;public HutongGames.PlayMaker.FsmBool value;}
 public class SetPlayerDataInt:HutongGames.PlayMaker.FsmStateAction {public HutongGames.PlayMaker.FsmString intName;public HutongGames.PlayMaker.FsmInt value;}
}
namespace On.HutongGames.PlayMaker {public static class Fsm {public delegate void orig_Awake(global::HutongGames.PlayMaker.Fsm f);public delegate void orig_OnEnable(global::HutongGames.PlayMaker.Fsm f);public delegate void orig_Start(global::HutongGames.PlayMaker.Fsm f);public delegate void orig_ProcessEvent(global::HutongGames.PlayMaker.Fsm f,global::HutongGames.PlayMaker.FsmEvent e,global::HutongGames.PlayMaker.FsmEventData d);}}
namespace KO.HollowKnight8 {
 internal static class Recorder {internal static readonly List<string> Steps=new();internal static bool Record=true;internal static string Failure;internal static void Add(string s){if(Record)Steps.Add(s+":P"+PlayerContext.Current?.Index+":enemy="+PlayerContext.TargetingEnemy);}internal static void ThrowAt(string stage){if(Failure==stage)throw new InvalidOperationException(stage);}internal static void Reset(){Steps.Clear();Failure=null;Record=true;PlayerContext.Current=null;PlayerContext.TargetingEnemy=false;WorldRouting.Current=null;}}
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal HeroActions Actions=new();internal bool InputBlocked,Down,Hazard,Retiring,Ready=true,Alive=true,Reviving,FocusReleased=true;}
 internal class CoopSession {internal readonly List<PlayerSlot> Players=new();internal PlayerSlot Primary=>Players[0];internal bool Active=true;internal PlayerSlot Resolve(object f)=>(f as Fsm)?.Player;internal void RefreshOwnership(GameObject root,PlayerSlot p)=>Recorder.Add("ownership:"+p.Index);}
 internal sealed class OwnerTag:Component{internal PlayerSlot Player;}
 internal sealed class PvpSwing{internal int touched;}
 internal class Runtime {internal CoopSession Session;internal bool Panel;}
 internal static class Plugin {internal static Runtime Self;}
 internal sealed class PlayerContext:IDisposable {internal static PlayerSlot Current;internal static bool TargetingEnemy;private PlayerSlot before;private bool enemy;internal static PlayerContext Enter(PlayerSlot p,bool e=false){Recorder.Add("context.enter");Recorder.ThrowAt("context");if(p==null||Current==p&&TargetingEnemy==e)return null;var c=new PlayerContext{before=Current,enemy=TargetingEnemy};Current=p;TargetingEnemy=e;return c;}public void Dispose(){Recorder.Add("context.exit");Current=before;TargetingEnemy=enemy;}}
 internal sealed class SmallScope:IDisposable {private string label;internal SmallScope(string n){label=n;Recorder.Add(n+".enter");Recorder.ThrowAt(n);}public void Dispose()=>Recorder.Add(label+".exit");}
 internal static class CharmNativeUi {internal static HeroActions view;internal static IDisposable Enter(Fsm f)=>new SmallScope("ui");}
 internal static class Lifeblood {internal static IDisposable Enter(Fsm f)=>new SmallScope("life");internal static bool Award(FsmEvent e){Recorder.Add("life.award");return Guards.Life;}}
 internal static class WorldRouting {internal struct Call {internal Fsm Before;}internal static Fsm Current;internal static Call Begin(Fsm f){Recorder.Add("world.begin");Recorder.ThrowAt("world.begin");var c=new Call{Before=Current};Current=f;return c;}internal static void End(Fsm f,Call c){Recorder.Add("world.end");Current=c.Before;Recorder.ThrowAt("world.end");}}
 internal static class Guards {internal static bool Death,Baldur,Challenge,Interaction,Denied,Life;internal static void Reset(){Death=Baldur=Challenge=Interaction=Denied=Life=false;}}
 internal static class PartyDeath {internal static bool BlockEvent(Fsm f,FsmEvent e,FsmEventData d){Recorder.Add("death");return Guards.Death;}internal static bool IsDeath(FsmEvent e)=>e?.Name=="HERO DEATH"||e?.Name=="HERO DEAD";}
 internal static class ElderBaldurRange {internal static bool BlockFar(Fsm f,FsmEvent e){Recorder.Add("baldur");return Guards.Baldur;}}
 internal static class ShadeCloakRitual {internal static void BeforeEvent(Fsm f,FsmEvent e,FsmEventData d)=>Recorder.Add("shade");internal static bool HoldsFsm(PlayerSlot p,Fsm f)=>false;}
 internal static class ChallengeSequence {internal static bool Intercept(Fsm f,FsmEvent e){Recorder.Add("challenge");return Guards.Challenge;}}
 internal static class InteractionRouter {internal static bool Blocks(Fsm f){Recorder.Add("interact");return Guards.Interaction;}internal static bool EventAllowed(Fsm f,FsmEventData d){Recorder.Add("allowed");return !Guards.Denied;}internal static void Ready(Fsm f)=>Recorder.Add("ready");internal static bool IsUi(Fsm f)=>f.Hud;internal static bool ScreenList(Fsm f)=>f.Name=="ui_list";internal static PlayerSlot ActivePlayer;internal static bool ActiveIsBench;}
 internal static class StagMenuRouting {internal static bool HasOwner;internal static void Observe(Fsm f,FsmEvent e,FsmEventData d)=>Recorder.Add("stag");internal static void AfterEvent(Fsm f,FsmEvent e)=>Recorder.Add("stag.after");}
 internal static class ArenaGather {internal static void Observe(Fsm f,FsmEvent e,FsmEventData d)=>Recorder.Add("arena");}
 internal static class SharedAcquisition {internal static void BeforeEvent(Fsm f,FsmEvent e,FsmEventData d)=>Recorder.Add("acquisition");}
 internal static partial class PvpCombat {internal static void TrackFsm(Fsm f)=>Recorder.Add("track");}
 internal static class Charms {internal static bool NativeMenuOpen,Editing;internal static int Selected;}
 internal static class ShopMenuRouting {internal static PlayerSlot Buyer;internal static bool MenuVisible;}
 internal static class AcquisitionRules {internal static bool Flag(string n)=>n=="hasShadowDash"||n=="hasAcidArmour"||n=="hasDash";internal static bool Level(string n)=>n=="fireballLevel"||n=="quakeLevel"||n=="screamLevel";}
 internal static class Diagnostics {internal static readonly List<string> Messages=new();internal static void Write(string s)=>Messages.Add(s);internal static void Throttled(string s,Exception e)=>Messages.Add(s+":"+e.GetType());}
 internal static class ReferenceField {internal static int Compiled,Fallback;}
}

namespace KO.HollowKnight8 {internal static class DamageParticleBudget {internal static string Report()=>"";internal static void ResetWindow(){}}}

using System;using System.Collections.Generic;using System.Linq;using UnityEngine;using HutongGames.PlayMaker;
namespace KO.HollowKnight8 {internal static class SoulOrbRouting {internal static void StateEntered(FsmState state){}}}
namespace UnityEngine {
 public class Object {public bool Exists=true;public static implicit operator bool(Object o)=>o!=null&&o.Exists;}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.Components.OfType<T>().FirstOrDefault();}
 public class GameObject:Object {public string name;public bool activeInHierarchy=true;public Transform transform;public List<Component> Components=new();public GameObject(string name){this.name=name;transform=new(){gameObject=this};}public T Add<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}}
 public class Transform:Component {public Vector3 position;public Transform parent;public bool IsChildOf(Transform t){for(var p=parent;p!=null;p=p.parent)if(p==t)return true;return false;}}
 public class Rigidbody2D:Component {public bool simulated=true,isKinematic;public float gravityScale=1,angularVelocity;public Vector2 velocity;}
 public class Collider2D:Component{}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float sqrMagnitude=>x*x+y*y;public static Vector2 zero=>new();public static Vector2 operator -(Vector2 a,Vector2 b)=>new(a.x-b.x,a.y-b.y);public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new();public override string ToString()=>$"({x},{y},{z})";}
 public static class Time {public static int frameCount;public static float time,unscaledTime;}
 public static class Mathf {public static float Abs(float f)=>Math.Abs(f);}
}
namespace UnityEngine.EventSystems {public class EventSystem {public static EventSystem current;public GameObject currentSelectedGameObject;}}
namespace GlobalEnums {public enum UIState {PLAYING,PAUSED,DIALOGUE}}
namespace HutongGames.PlayMaker {public class Fsm {public string Name;public GameObject GameObject;public FsmState ActiveState;public string ActiveStateName=>ActiveState?.Name;public List<FsmState> States=new();public FsmState GetState(string name)=>States.Find(s=>s.Name==name);public void Event(FsmEvent evt){}public bool Ui,Actor;internal HashSet<KO.HollowKnight8.PlayerSlot> Range=new();}public class FsmState {public Fsm Fsm;public string Name;public FsmStateAction[] Actions=Array.Empty<FsmStateAction>();public FsmTransition[] Transitions=Array.Empty<FsmTransition>();}public class FsmStateAction {public bool Enabled=true;}public class FsmString {public string Value;}public class FsmFloat {public float Value;public bool UseVariable;public string Name;}public class FsmTransition {public string EventName,ToState;}public class FsmEvent {public string Name;public static FsmEvent GetFsmEvent(string name)=>new(){Name=name};}}
namespace HutongGames.PlayMaker.Actions {public class ListenForUp:FsmStateAction{}public class ListenForDown:FsmStateAction{}public class GetPosition:FsmStateAction {}public class FloatCompare:FsmStateAction {public FsmFloat float1,float2;}public class CallMethodProper {public Fsm Fsm;public FsmString methodName=new();}}
public class TriggerEnterEvent:Component{}
public class HeroControllerStates {public bool transitioning,onGround=true,swimming;}
public class HeroController:Component {public bool controlReqlinquished,acceptingInput=true,IsDreamReturning;public HeroControllerStates cState=new();}
public class HeroAnimationController:Component {public bool controlEnabled=true;}
public class InvCharmBackboard:Component{}
public class Action {public bool IsPressed,WasPressed;}
public class Move {public float X;}
public class HeroActions {public Action up=new(),down=new();public Move moveVector=new();}
public class PlayMakerFSM:Component {public bool isActiveAndEnabled=true;public string ActiveStateName="Closed";}
public class GameManager:UnityEngine.Object {public static GameManager instance;public string sceneName="Fungus1_04";public bool isPaused,IsLoadingSceneTransition;public PlayMakerFSM inventoryFSM;}
public class UIManager:UnityEngine.Object {public static UIManager instance;public GlobalEnums.UIState uiState=GlobalEnums.UIState.PLAYING;}
namespace On {
 public static class HeroController {
  public delegate void orig_RelinquishControl(global::HeroController h);public delegate void hook_RelinquishControl(orig_RelinquishControl o,global::HeroController h);public static event hook_RelinquishControl RelinquishControl;
  public delegate void orig_RelinquishControlNotVelocity(global::HeroController h);public delegate void hook_RelinquishControlNotVelocity(orig_RelinquishControlNotVelocity o,global::HeroController h);public static event hook_RelinquishControlNotVelocity RelinquishControlNotVelocity;
  public delegate void orig_RegainControl(global::HeroController h);public delegate void hook_RegainControl(orig_RegainControl o,global::HeroController h);public static event hook_RegainControl RegainControl;
  public static int TakeCount=>RelinquishControl?.GetInvocationList().Length??0;public static int TakeVelocityCount=>RelinquishControlNotVelocity?.GetInvocationList().Length??0;public static int ReturnCount=>RegainControl?.GetInvocationList().Length??0;
  public static void Take(global::HeroController h,bool velocity=false,bool cancel=false,bool throws=false){if(velocity){orig_RelinquishControlNotVelocity native=p=>{if(throws)throw new InvalidOperationException();if(!cancel)p.controlReqlinquished=true;};if(RelinquishControlNotVelocity!=null)RelinquishControlNotVelocity(native,h);else native(h);}else{orig_RelinquishControl native=p=>{if(throws)throw new InvalidOperationException();if(!cancel)p.controlReqlinquished=true;};if(RelinquishControl!=null)RelinquishControl(native,h);else native(h);}}
  public static void Return(global::HeroController h){orig_RegainControl native=p=>p.controlReqlinquished=false;if(RegainControl!=null)RegainControl(native,h);else native(h);}
 }
}
namespace On.HutongGames.PlayMaker {public static class FsmState {public delegate void orig_OnEnter(global::HutongGames.PlayMaker.FsmState state);}}
namespace On.HutongGames.PlayMaker.Actions {public static class CallMethodProper {public delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.CallMethodProper action);}}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal HeroActions Actions=new();internal Vitals Vitals=new();internal bool Ready=true,Connected=true,Alive=true,InputBlocked,Retiring;internal bool AcidAssistActive;internal float ProtectionUntil;}
 internal class Vitals {internal bool AtBench;}
 internal partial class CoopSession {internal bool Active=true,Gameplay=true,TeamWipe;internal Data Data=new();internal List<PlayerSlot> Players=new();internal PlayerSlot Primary=>Players[0];internal PlayerSlot Resolve(HeroController h)=>Players.Find(p=>p.Hero==h);}
 internal class Data {internal bool hornet1Defeated;}internal class Runtime:UnityEngine.Object {internal CoopSession Session;}
 internal static class Plugin {internal static Runtime Self;}
 internal static class PlayerContext {internal static PlayerSlot Current;sealed class Scope:IDisposable {PlayerSlot old;public Scope(PlayerSlot p){old=Current;Current=p;}public void Dispose()=>Current=old;}internal static IDisposable Enter(PlayerSlot p,bool enemy=false)=>new Scope(p);}
 internal static class Diagnostics {internal static List<string> Messages=new();internal static void Write(string s)=>Messages.Add(s);}
 internal static class WorldRouting {internal static Fsm Current;internal static PlayerSlot Routed;internal static PlayerSlot Resolve(Fsm f)=>Routed;}
 internal static class ActorRecovery {internal static List<PlayerSlot> Resets=new();internal static void Reset(PlayerSlot p){Resets.Add(p);p.Hero.controlReqlinquished=false;p.Hero.acceptingInput=true;var body=p.Hero.GetComponent<Rigidbody2D>();body.isKinematic=false;body.simulated=true;body.gravityScale=1;}}
 internal static class BenchSeats {internal static HashSet<PlayerSlot> Seats=new(),CustomSeats=new();internal static bool Seated(PlayerSlot p)=>Seats.Contains(p);internal static bool Custom(PlayerSlot p)=>CustomSeats.Contains(p);internal static void Observe(Fsm f,PlayerSlot p){} }
 internal static class ShopMenuRouting {internal static PlayerSlot Buyer;internal static bool MenuVisible;internal static PlayerSlot Resolve(Fsm f)=>null;}
 internal static class StagMenuRouting {internal static bool HasOwner;internal static void NoteInteraction(PlayerSlot p){} }
 internal static class PickupCard {internal static PlayerSlot Owner;internal static PlayerSlot Resolve(Fsm f)=>null;internal static void ObserveCall(Fsm f,PlayerSlot p,string method){} }
 internal static class ScriptedParty {internal static bool Active;}
 internal static class CoopEnding {internal static bool Active;internal static void BeforeState(FsmState state){} }
 internal static class PvpMatch {internal static bool Running;internal static bool BlockExit(PlayerSlot p)=>false;}
 internal static class EmergencyWarp {internal static HashSet<PlayerSlot> Held=new();internal static bool Active(PlayerSlot p)=>Held.Contains(p);}
 internal static class TransitionVote {internal static HashSet<PlayerSlot> Held=new();internal static bool Holding(PlayerSlot p)=>Held.Contains(p);}
 internal static class DreamSequence {internal static bool InStoryDream;}
 internal static class CharmMenuDiagnostics {internal static bool Report(bool open,string reason)=>open;}
 internal static class RoleSystem {internal static void ClearNativeFocus(){} }
 internal static class CharmCatalogTint {internal static void Restore(){} }
 internal static class CharmNativeUi {internal static void Release(){}internal static IDisposable Enter(Fsm f)=>null;}
 internal static class Reflect {internal static void Set(object o,string field,object value){} }
 internal static class ArenaGather {internal static void Encounter(Fsm f,PlayerSlot p){}internal static void ObserveState(FsmState state){} }
 internal static class ChallengeSequence {internal static void ObserveState(FsmState state,PlayerSlot p){} }
 internal static class SharedAcquisition {internal static void ObserveState(FsmState state,PlayerSlot p){} }
 internal static class RadianceAscentCheckpoint {internal static void Observed(FsmState state){} }
 internal static class ChallengeAnimationSync {internal static void AfterState(FsmState state){} }
}

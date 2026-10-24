using System;using System.Collections.Generic;using UnityEngine;using HutongGames.PlayMaker;using GlobalEnums;

namespace UnityEngine {
 public class Object {public string name="Object";public static implicit operator bool(Object o)=>o!=null;}
 public class GameObject:Object {public bool activeInHierarchy=true;public int layer=9;}
 public class Transform {public Vector3 position;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new();public static float Distance(Vector2 a,Vector2 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);}
 public struct Bounds {public Vector3 center,extents;public Vector3 min=>new(center.x-extents.x,center.y-extents.y);public Vector3 max=>new(center.x+extents.x,center.y+extents.y);}
 public class Collider2D:Object {public GameObject gameObject=new();public bool enabled=true;public Bounds bounds;}
 public class Rigidbody2D:Object {public bool isKinematic;public float gravityScale=1;public Vector2 velocity;}
 public class Renderer:Object {public bool enabled=true;}
 public static class Mathf {public static float Clamp01(float v)=>Math.Clamp(v,0,1);public static float Min(float a,float b)=>Math.Min(a,b);public static float Abs(float v)=>Math.Abs(v);}
 public static class Time {public static float unscaledTime,unscaledDeltaTime=.02f;}
}
namespace UnityEngine.SceneManagement {public struct Scene {public string name;}public static class SceneManager {public static string Name="Town";public static Scene GetActiveScene()=>new(){name=Name};}}
namespace GlobalEnums {public enum GatePosition {door,left,right,top,bottom}public enum CameraFadeType {LEVEL_TRANSITION,HERO_DEATH,HERO_HAZARD_DEATH,JUST_FADE,START_FADE}}
namespace HutongGames.PlayMaker {
 public class FsmEvent {public string Name;public FsmEvent(string n){Name=n;}}
 public class FsmEventData {}
 public class FsmStateAction {public bool Enabled=true;}
 public class FsmState {public string Name;public FsmStateAction[] Actions=Array.Empty<FsmStateAction>();public FsmTransition[] Transitions=Array.Empty<FsmTransition>();}
 public class FsmTransition {public string EventName;}
 public class Fsm {public string Name="Door Control";public GameObject GameObject=new();public FsmState ActiveState;public FsmState[] States=Array.Empty<FsmState>();public bool HasNormal=true;public int Events;public string LastEvent;
  public FsmState GetState(string name)=>name=="Normal"&&HasNormal?new(){Name=name}:null;
  public void Event(string name)=>On.HutongGames.PlayMaker.Fsm.Run(this,new FsmEvent(name),new());
  public void NativeEvent(FsmEvent evt){Events++;LastEvent=evt.Name;if(evt.Name=="FADE SCENE IN")ActiveState=new(){Name="Normal"};else if(evt.Name.Contains("FADE"))ActiveState=new(){Name=evt.Name};}
 }
 public class FsmString {public string Value;public static implicit operator FsmString(string s)=>new(){Value=s};}
 public class FsmFloat {public float Value;}
 public class FsmEnum {public object Value;}
}
namespace HutongGames.PlayMaker.Actions {
 public class ListenForUp:FsmStateAction {}
 public class TriggerEnterEvent {}
 public class BeginSceneTransition:FsmStateAction {public FsmString sceneName,entryGateName;public FsmFloat entryDelay;public FsmEnum visualization;}
}
public class PlayMakerFSM:UnityEngine.Object {
 public Fsm Fsm=new();public bool enabled=true;public string FsmName=>Fsm.Name;public GameObject gameObject=>Fsm.GameObject;public Transform transform=new();
 public string ActiveStateName=>Fsm.ActiveState?.Name;public void SetState(string n)=>Fsm.ActiveState=new(){Name=n};
}
public class HeroController:UnityEngine.Object {
 public Transform transform=new();public Rigidbody2D Body=new();public Renderer[] Renderers={new(),new(){enabled=false}};public bool Interact=true,controlReqlinquished;public State cState=new();
 public class State {public bool onGround=true;}
 public T GetComponent<T>() where T:class=>Body as T;public T[] GetComponentsInChildren<T>(bool includeInactive) where T:class=>Renderers as T[];public bool CanInteract()=>Interact&&!controlReqlinquished;
}
public class TransitionPoint:UnityEngine.Object {
 public static List<TransitionPoint> TransitionPoints=new();public bool enabled=true,isADoor=true;public string targetScene="Room_Town_Stag_Station",entryPoint="door1";public GameObject gameObject=new();public Transform transform=new();public Collider2D Collider;
 public float entryDelay;public bool forceWaitFetch;public GameManager.SceneLoadVisualizations sceneLoadVisualization;public GatePosition GetGatePosition()=>GatePosition.door;
 public T GetComponent<T>() where T:class=>Collider as T;
}
public class CameraController:UnityEngine.Object {
 public enum CameraMode {FROZEN,FOLLOWING,LOCKED,PANNING,FADEOUT,FADEIN,PREVIOUS}
 public CameraMode mode=CameraMode.LOCKED,Previous=CameraMode.LOCKED;public int Fades;
 public void SetMode(CameraMode m){if(m==CameraMode.PREVIOUS)mode=Previous;else if(mode!=m){Previous=mode;mode=m;}}
 public void FadeOut(CameraFadeType t)=>On.CameraController.Run(this,t);
 public void NativeFadeOut(CameraFadeType t){Fades++;SetMode(CameraMode.FROZEN);GameCameras.instance.cameraFadeFSM.Fsm.Event(t switch {CameraFadeType.LEVEL_TRANSITION=>"FADE OUT",CameraFadeType.HERO_DEATH=>"RESPAWN FADE",CameraFadeType.HERO_HAZARD_DEATH=>"HAZARD FADE",CameraFadeType.JUST_FADE=>"JUST FADE",_=>"START FADE"});}
}
public class GameCameras:UnityEngine.Object {public static GameCameras instance;public PlayMakerFSM cameraFadeFSM=new();}
public class GameManager:UnityEngine.Object {
 public static GameManager instance;public bool isPaused,IsLoadingSceneTransition;public CameraController cameraCtrl=new();public int Loads,Ins;public SceneLoadInfo Last;internal KO.HollowKnight8.PlayerSlot Entrant;
 public enum SceneLoadVisualizations {Default,Custom}
 public class SceneLoadInfo {public string SceneName="Room_Town_Stag_Station",EntryGateName="door1";public bool PreventCameraFadeOut,WaitForSceneTransitionCameraFade,forceWaitFetch;public GatePosition HeroLeaveDirection=GatePosition.door;public float EntryDelay;public SceneLoadVisualizations Visualization;}
 public void BeginSceneTransition(SceneLoadInfo info){if(KO.HollowKnight8.TransitionVote.Pending)throw new Exception("load began while party still waiting");Loads++;Last=info;Entrant=KO.HollowKnight8.PlayerContext.Current;cameraCtrl.FadeOut(CameraFadeType.LEVEL_TRANSITION);IsLoadingSceneTransition=true;}
 public void FadeSceneIn(){Ins++;GameCameras.instance.cameraFadeFSM.Fsm.Event("FADE SCENE IN");}
 public string GetSceneNameString()=>UnityEngine.SceneManagement.SceneManager.Name;
}
public static class BossSequenceController {public static bool IsInSequence;}
namespace On.HutongGames.PlayMaker {
 public static class Fsm {
  public delegate void orig_ProcessEvent(global::HutongGames.PlayMaker.Fsm f,global::HutongGames.PlayMaker.FsmEvent e,global::HutongGames.PlayMaker.FsmEventData d);
  public delegate void hook_ProcessEvent(orig_ProcessEvent o,global::HutongGames.PlayMaker.Fsm f,global::HutongGames.PlayMaker.FsmEvent e,global::HutongGames.PlayMaker.FsmEventData d);
  public static event hook_ProcessEvent ProcessEvent;public static int Count=>ProcessEvent?.GetInvocationList().Length??0;
  public static void Run(global::HutongGames.PlayMaker.Fsm f,global::HutongGames.PlayMaker.FsmEvent e,global::HutongGames.PlayMaker.FsmEventData d){orig_ProcessEvent next=(f,e,d)=>f.NativeEvent(e);if(ProcessEvent!=null)foreach(hook_ProcessEvent h in ProcessEvent.GetInvocationList()){var prev=next;next=(f,e,d)=>h(prev,f,e,d);}next(f,e,d);}
 }
}
namespace On {
 public static class CameraController {
  public delegate void orig_FadeOut(global::CameraController c,CameraFadeType t);public delegate void hook_FadeOut(orig_FadeOut o,global::CameraController c,CameraFadeType t);
  public static event hook_FadeOut FadeOut;public static int Count=>FadeOut?.GetInvocationList().Length??0;
  public static void Run(global::CameraController c,CameraFadeType t){orig_FadeOut next=(c,t)=>c.NativeFadeOut(t);if(FadeOut!=null)foreach(hook_FadeOut h in FadeOut.GetInvocationList()){var prev=next;next=(c,t)=>h(prev,c,t);}next(c,t);}
 }
}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {internal int Index;internal HeroController Hero=new();internal bool Ready=true,Alive=true,Connected=true,InputBlocked,ArenaTransfer,Down,Hazard;internal HeroActions Actions=new();}
 internal class HeroActions {internal ActionButton up=new();}internal class ActionButton {internal bool IsPressed;}
 internal class CoopSession {internal bool Active=true,Gameplay=true,TeamWipe;internal List<PlayerSlot> Players=new();internal Data Data=new();}
 internal class Data {internal bool jijiDoorUnlocked;}
 internal class Setting {internal bool Value=true;}
 internal class Local8Runtime {internal CoopSession Session;internal Setting WaitForParty=new();internal bool Panel;}
 internal static class Plugin {internal static Local8Runtime Self;}
 internal static class EmergencyWarp {internal static bool Active(PlayerSlot p)=>false;}
 internal static class PvpMatch {internal static bool Running;internal static bool BlockExit(PlayerSlot p)=>false;}
 internal static class ScriptedParty {internal static bool Active;}
 internal static class CoopEnding {internal static bool Active;}
 internal static class ArenaGather {internal static bool TransferActive;}
 internal static class StagMenuRouting {internal static bool HasOwner;}
 internal static class DreamSequence {internal static bool InStoryDream;}
 internal static class CrystalDashTransit {internal static void CaptureVote(PlayerSlot p,string key){}}
 internal static class RescueHint {internal static void HideDistant(){}}
 internal static class Diagnostics {internal static List<string> Lines=new();internal static void Write(string text)=>Lines.Add(text);internal static void Throttled(string text,Exception e)=>throw new Exception(text,e);}
 internal static class Charms {internal static bool NativeMenuOpen;}
 internal static class ShopMenuRouting {internal static PlayerSlot Buyer;}
 internal static class BenchSeats {internal static bool Seated(PlayerSlot p)=>false;}
 internal static class DoorRouteFix {internal static string GateScene(TransitionPoint t)=>t.targetScene;internal static string GateEntry(TransitionPoint t)=>t.entryPoint;}
 internal sealed class PlayerContext:IDisposable {internal static PlayerSlot Current;readonly PlayerSlot previous;PlayerContext(PlayerSlot p){previous=Current;Current=p;}internal static PlayerContext Enter(PlayerSlot p)=>new(p);public void Dispose(){Current=previous;}}
 internal static class InteractionMotion {internal static void Door(GameManager m,GameManager.SceneLoadInfo i){var s=Plugin.Self.Session;if(!TransitionVote.Direct(s,PlayerContext.Current,m,i))m.BeginSceneTransition(i);}}
}

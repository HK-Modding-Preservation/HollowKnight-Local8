using System;using System.Collections.Generic;using System.Linq;
namespace UnityEngine {
 public class Object {public bool Destroyed;public static implicit operator bool(Object v)=>v!=null&&!v.Destroyed;public static void Destroy(Object o){if(o!=null)o.Destroyed=true;}}
 public class Component:Object {public GameObject gameObject;public string name=>gameObject.name;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T GetComponentInChildren<T>(bool includeInactive=false)where T:Component=>GetComponentsInChildren<T>(includeInactive).FirstOrDefault();public T[] GetComponentsInChildren<T>(bool includeInactive=false)where T:Component=>gameObject.GetComponentsInChildren<T>(includeInactive);}
 public class MonoBehaviour:Component {public bool enabled=true;}
 public class Transform:Component {public Transform parent;public Vector3 position;public bool IsChildOf(Transform t){for(var p=parent;p!=null;p=p.parent)if(p==t)return true;return false;}}
 public class GameObject:Object {
  public string name;public int layer=9;public bool activeSelf=true;public bool activeInHierarchy=>activeSelf&&(transform.parent==null||transform.parent.gameObject.activeInHierarchy);public Transform transform;
  public readonly List<Component> components=new();public static readonly List<GameObject> all=new();
  public GameObject(string n="Object"){name=n;transform=new(){gameObject=this};components.Add(transform);all.Add(this);}
  public void SetActive(bool active){activeSelf=active;}
  public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}
  public T GetComponent<T>()where T:Component=>components.OfType<T>().FirstOrDefault();public T[] GetComponents<T>()where T:Component=>components.OfType<T>().ToArray();
  public T[] GetComponentsInChildren<T>(bool includeInactive=false)where T:Component=>all.Where(o=>o&&(o==this||o.transform.IsChildOf(transform))&&(includeInactive||o.activeInHierarchy)).SelectMany(o=>o.components.OfType<T>()).ToArray();
 }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new(0,0);public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);public override string ToString()=>$"({x},{y})";}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new(0,0,0);public static Vector3 operator+(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);}
 public struct Bounds {public bool Intersects(Bounds other)=>true;}
 public struct ColliderDistance2D {public float distance;}
 public class Collider2D:MonoBehaviour {public Bounds bounds;public bool ActualOverlap=true;public bool OverlapPoint(Vector2 point)=>ActualOverlap;public ColliderDistance2D Distance(Collider2D body)=>new(){distance=ActualOverlap?-.1f:1};}
 public class Rigidbody2D:Component {public Vector2 velocity;public float angularVelocity,gravityScale=3;public bool isKinematic,simulated=true;}
 public class Renderer:MonoBehaviour {}public class MeshRenderer:Renderer {}
 public enum ParticleSystemStopBehavior {StopEmittingAndClear}
 public class ParticleSystem:Component {public Main main=new();public Emission emission=new();public bool isPlaying;public int Starts,Stops;public class Main{public bool playOnAwake,loop=true;public int maxParticles=100;}public class Emission{public bool enabled=true;public float rateOverTimeMultiplier=100,rateOverDistanceMultiplier=20;}public void Stop(bool children=false,ParticleSystemStopBehavior behavior=ParticleSystemStopBehavior.StopEmittingAndClear){isPlaying=false;Stops++;}public void Clear(bool children=false){}public void Play(bool children=false){isPlaying=true;Starts++;}}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);}
 public static class Time {public static float time,unscaledDeltaTime=.1f;}
}
namespace UnityEngine.SceneManagement {public static class SceneManager{public struct Scene{public string name;}public static string Name="Room_A";public static Scene GetActiveScene()=>new(){name=Name};}}
namespace GlobalEnums {public enum GatePosition{left,right,top,bottom,door}public enum ActorStates{no_input,idle}public enum DamageMode{FULL_DAMAGE}public enum HeroTransitionState{WAITING_TO_TRANSITION}}
namespace HutongGames.PlayMaker {
 public class FsmStateAction{}public class FsmOwnerDefault{public UnityEngine.GameObject Object;}
 public class FsmState{public string Name;public FsmStateAction[] Actions=Array.Empty<FsmStateAction>();}
 public class Fsm{public string Name;public PlayMakerFSM Owner;public FsmState[] States=Array.Empty<FsmState>();public FsmState GetState(string name)=>States.FirstOrDefault(s=>s.Name==name);public UnityEngine.GameObject GetOwnerDefaultTarget(FsmOwnerDefault source)=>source.Object;}
}
namespace HutongGames.PlayMaker.Actions {public class SetMeshRenderer:HutongGames.PlayMaker.FsmStateAction{public HutongGames.PlayMaker.FsmOwnerDefault gameObject;}}
public class PlayMakerFSM:UnityEngine.MonoBehaviour {public string FsmName;public HutongGames.PlayMaker.Fsm Fsm=new();public HutongGames.PlayMaker.FsmState[] FsmStates{get=>Fsm.States;set=>Fsm.States=value;}public string ActiveStateName="Inactive";public Action<string> Sent;public void SetState(string state){ActiveStateName=state;}public void SendEvent(string name){Sent?.Invoke(name);}}
public class HeroControllerStates {public bool inWalkZone,dead,superDashing,freezeCharge,transitioning,swimming,inAcid,facingRight;public void Reset(){dead=transitioning=false;} /* vanilla deliberately leaves superDashing/freezeCharge */}
public class HeroController:UnityEngine.MonoBehaviour {
 public HeroControllerStates cState=new();public PlayMakerFSM proxyFSM,superDash;public bool exitedSuperDashing,inAcid,acceptingInput=true,controlReqlinquished,Animation=true;public float DEFAULT_GRAVITY=3;public GlobalEnums.ActorStates hero_state=GlobalEnums.ActorStates.idle;
 public void SetWalkZone(bool v){cState.inWalkZone=v;}public void StopAllCoroutines(){}public void ResetState(){cState.Reset();}public void RegainControl(){hero_state=GlobalEnums.ActorStates.idle;controlReqlinquished=false;acceptingInput=true;}
 public void StartAnimationControl(){Animation=true;}public void StopAnimationControl(){Animation=false;}public void AcceptInput(){acceptingInput=true;}public void IgnoreInput(){acceptingInput=false;}public void AffectedByGravity(bool v){GetComponent<UnityEngine.Rigidbody2D>().gravityScale=v?3:0;}public void SetDamageMode(GlobalEnums.DamageMode m){}public void FaceRight(){cState.facingRight=true;}public void FaceLeft(){cState.facingRight=false;}
}
public class HeroAnimationController:UnityEngine.MonoBehaviour{}public class HeroBox:UnityEngine.MonoBehaviour{public static bool inactive;}public class InvulnerablePulse:UnityEngine.Component{public void stopInvulnerablePulse(){}}
public class tk2dSpriteAnimator:UnityEngine.Component{}public class HealthManager:UnityEngine.Component{}public class WalkArea:UnityEngine.MonoBehaviour{}
public class TransitionPoint:UnityEngine.Component {public static List<TransitionPoint> TransitionPoints=new();public GlobalEnums.GatePosition Position;public GlobalEnums.GatePosition GetGatePosition()=>Position;}
public class GameManager:UnityEngine.Object {public static GameManager instance;public bool isPaused,IsLoadingSceneTransition,HasFinishedEnteringScene=true;public class SceneLoadInfo{public string SceneName,EntryGateName;public GlobalEnums.GatePosition HeroLeaveDirection;}}
public static class BossSequenceController{public static bool IsInSequence;}
public static class ObjectPool{public static readonly List<UnityEngine.GameObject> Recycled=new();public static void Recycle(UnityEngine.GameObject o){Recycled.Add(o);o.SetActive(false);}}
namespace On {public static class WalkArea {
 public delegate void orig_OnTriggerEnter2D(global::WalkArea self,UnityEngine.Collider2D c);public delegate void orig_OnTriggerStay2D(global::WalkArea self,UnityEngine.Collider2D c);public delegate void orig_OnTriggerExit2D(global::WalkArea self,UnityEngine.Collider2D c);
}}
namespace KO.HollowKnight8 {
 public class OwnerTag:UnityEngine.Component{internal PlayerSlot Player;}
 internal class Vitals{internal bool AtBench;}
 internal class CharmLoadout{internal bool[] Equipped=new bool[40];}
 internal class PlayerSlot{internal int Index;internal HeroController Hero;internal bool Down,Hazard,Retiring,ArenaTransfer,InputBlocked,SpawnPending,AcidAssistActive,HeroBoxInactive;internal bool Connected=true,Ready=true;internal Vitals Vitals=new();internal CharmLoadout Charms=new();internal bool Alive=>Hero&&Hero.gameObject.activeInHierarchy&&!Down&&!Hazard;}
 internal class Runtime:UnityEngine.Object{internal CoopSession Session;}
 internal static class Plugin{internal static Runtime Self;}
 internal class CoopSession{
  internal bool Active=true,TeamWipe,Gameplay=true;internal PlayerSlot Primary;internal List<PlayerSlot> Players=new();
  internal PlayerSlot Resolve(object obj){var c=obj as UnityEngine.Component;if(obj is UnityEngine.GameObject go)c=go.transform;return c==null?null:Players.FirstOrDefault(p=>p.Hero&&(c.gameObject==p.Hero.gameObject||c.transform.IsChildOf(p.Hero.transform)))??c.gameObject.GetComponent<OwnerTag>()?.Player;}
  internal void RefreshOwnership(UnityEngine.GameObject obj,PlayerSlot p){obj.GetComponent<OwnerTag>().Player=p;}
  internal void PrepareTransition(PlayerSlot entrant){foreach(var p in Players)if(p.Index>0){p.Ready=false;p.Hero.gameObject.SetActive(false);}}
 }
 internal sealed class PlayerContext:IDisposable{internal static PlayerSlot Current;internal static bool TargetingEnemy;private PlayerSlot old;internal static PlayerContext Enter(PlayerSlot p)=>new PlayerContext{old=Current}.Begin(p);private PlayerContext Begin(PlayerSlot p){Current=p;return this;}public void Dispose(){Current=old;}}
 internal static class Diagnostics{internal static List<string> Logs=new();internal static void Write(string s){Logs.Add(s);}internal static void Throttled(string s,Exception e)=>throw new Exception(s,e);}
 internal static partial class Hooks{private static CoopSession Session=>Plugin.Self?.Session;internal static void RebindReferences(UnityEngine.GameObject o,PlayerSlot p){}internal static void TestEnter(WalkArea a,UnityEngine.Collider2D c,On.WalkArea.orig_OnTriggerEnter2D orig)=>WalkEnter(orig,a,c);internal static void TestStay(WalkArea a,UnityEngine.Collider2D c,On.WalkArea.orig_OnTriggerStay2D orig)=>WalkStay(orig,a,c);internal static void TestExit(WalkArea a,UnityEngine.Collider2D c,On.WalkArea.orig_OnTriggerExit2D orig)=>WalkExit(orig,a,c);internal static void TestRefresh(PlayerSlot p)=>RefreshWalk(p);}
 internal static class Reflect{internal static void Set(object o,string name,object value){o.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)?.SetValue(o,value);}internal static object Call(object o,string n)=>null;}
 internal static class EmergencyWarp{internal static HashSet<PlayerSlot> Held=new();internal static bool Active(PlayerSlot p)=>Held.Contains(p);}
 internal static class TransitionVote{internal static HashSet<PlayerSlot> Votes=new();internal static bool Holding(PlayerSlot p)=>Votes.Contains(p);}
 internal static class PvpMatch{internal static bool Running;}
 internal static class ScriptedParty{internal static bool Active;}
 internal static class CoopEnding{internal static bool Active;}
 internal static class SpawnSafety{internal static bool Clear=true;internal static bool Path(PlayerSlot p,UnityEngine.Vector3 from,UnityEngine.Vector3 to)=>Clear;}
 internal static class CrystalDashCoop{internal static int[] Counts=new int[8];internal static int Participants(PlayerSlot p)=>2;internal static void ContinueFlight(PlayerSlot p,int count){Counts[p.Index]=count;NativeDashEffects.Flight(p);}}
}

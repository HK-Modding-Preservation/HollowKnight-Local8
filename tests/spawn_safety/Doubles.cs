using System;using System.Collections;using System.Collections.Generic;using System.Linq;using UnityEngine;

namespace UnityEngine {
 public class Object {public bool Exists=true;public static implicit operator bool(Object o)=>o!=null&&o.Exists;public static void Destroy(Object o){if(o!=null)o.Exists=false;}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public string name=>gameObject.name;public T GetComponent<T>()where T:class=>gameObject.GetComponent<T>();public T GetComponentInParent<T>()where T:class=>gameObject.GetComponent<T>();}
 public class GameObject:Object {
  public string name="Object";public int layer;public bool activeSelf=true;public bool activeInHierarchy=>activeSelf;public Transform transform;internal List<Component> Components=new();public int Activations;
  public GameObject(){transform=new(){gameObject=this};}public void SetActive(bool value){if(value&&!activeSelf)Activations++;activeSelf=value;}
  public T Add<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);if(c is Collider2D collider)Physics2D.World.Add(collider);return c;}
  public T GetComponent<T>()where T:class=>Components.OfType<T>().FirstOrDefault();
 }
 public class Transform {public GameObject gameObject;public Vector3 position,localScale=new(1,1,1);public float Angle;
  public Vector3 TransformVector(Vector3 v){float a=Angle*(float)Math.PI/180;float x=v.x*localScale.x,y=v.y*localScale.y;return new(x*(float)Math.Cos(a)-y*(float)Math.Sin(a),x*(float)Math.Sin(a)+y*(float)Math.Cos(a),v.z*localScale.z);}
  public Vector3 TransformPoint(Vector3 p)=>position+TransformVector(p);
 }
 public struct Vector2 {
  public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new();public static Vector2 down=>new(0,-1);public float magnitude=>(float)Math.Sqrt(x*x+y*y);public static Vector2 operator /(Vector2 a,float d)=>new(a.x/d,a.y/d);public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.x+b.x,a.y+b.y);public static Vector2 operator -(Vector2 a,Vector2 b)=>new(a.x-b.x,a.y-b.y);public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);public static implicit operator Vector3(Vector2 v)=>new(v.x,v.y);public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;
 }
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new();public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);public static bool operator ==(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;public static bool operator !=(Vector3 a,Vector3 b)=>!(a==b);public override bool Equals(object o)=>o is Vector3 v&&this==v;public override int GetHashCode()=>HashCode.Combine(x,y,z);public override string ToString()=>$"({x:F2},{y:F2},{z:F2})";
 }
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 extents=>new(size.x*.5f,size.y*.5f,size.z*.5f);public Vector3 min=>center-extents;public Vector3 max=>center+extents;}
 public class Collider2D:Component {public ColliderDistance2D Distance(Collider2D other){var a=bounds;var b=other.bounds;float x=(a.size.x+b.size.x)*.5f-Math.Abs(a.center.x-b.center.x),y=(a.size.y+b.size.y)*.5f-Math.Abs(a.center.y-b.center.y);return new(){isValid=true,isOverlapped=x>0&&y>0,distance=x>0&&y>0?-Math.Min(x,y):Math.Max(-x,-y)};}public bool enabled=true,isTrigger,usedByEffector;public bool OverlapPoint(Vector2 p){var b=bounds;return p.x>=b.min.x&&p.x<=b.max.x&&p.y>=b.min.y&&p.y<=b.max.y;}public virtual Bounds bounds=>new(transform.position,new(1,1));public Vector2 SurfaceNormal=new(0,1);}
 public struct ColliderDistance2D {public bool isValid,isOverlapped;public float distance;}
 public class BoxCollider2D:Collider2D {public Vector2 offset,size=new(1,2);public override Bounds bounds {get {var x=transform.TransformVector(new(size.x,0));var y=transform.TransformVector(new(0,size.y));return new(transform.TransformPoint(offset),new(Math.Abs(x.x)+Math.Abs(y.x),Math.Abs(x.y)+Math.Abs(y.y),.1f));}}}
 public class Rigidbody2D:Component {public float gravityScale=1;public Vector2 velocity;public bool simulated=true,isKinematic;}
 public class Renderer:Component {public bool enabled=true;}
 public struct RaycastHit2D {public Collider2D collider;public Vector2 point,normal;public float distance;}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Abs(float v)=>Math.Abs(v);public static float Clamp(float v,float min,float max)=>Math.Clamp(v,min,max);}
 public static class Time {public static float time=10,unscaledTime=10;}
 public static class Physics2D {
  public static bool IgnoredPair,IgnoredLayer;public static bool GetIgnoreCollision(Collider2D a,Collider2D b)=>IgnoredPair;public static bool GetIgnoreLayerCollision(int a,int b)=>IgnoredLayer;public static readonly List<Collider2D> World=new();public static bool Reverse;public static int Syncs,Queries;
  public static void SyncTransforms(){Syncs++;}
  static bool Match(Collider2D c,int mask)=>c&&c.enabled&&c.gameObject.activeInHierarchy&&(mask==-1||(mask&(1<<c.gameObject.layer))!=0);
  static IEnumerable<Collider2D> Colliders(int mask)=>Reverse?World.Where(c=>Match(c,mask)).Reverse():World.Where(c=>Match(c,mask));
  public static int OverlapBoxNonAlloc(Vector2 p,Vector2 size,float angle,Collider2D[] results,int layerMask=-1){Queries++;int n=0;foreach(var c in Colliders(layerMask)){var b=c.bounds;if(Math.Abs(p.x-b.center.x)<(size.x+b.size.x)*.5f-.00001f&&Math.Abs(p.y-b.center.y)<(size.y+b.size.y)*.5f-.00001f){if(n==results.Length)return n;results[n++]=c;}}return n;}
  static bool Slab(Vector2 origin,Vector2 direction,float distance,Bounds b,Vector2 expand,out float time,out Vector2 normal){
   float near=0,far=distance;normal=new(-direction.x,-direction.y);time=0;
   for(int axis=0;axis<2;axis++){
    float at=axis==0?origin.x:origin.y,dir=axis==0?direction.x:direction.y,center=axis==0?b.center.x:b.center.y,half=(axis==0?b.size.x+expand.x:b.size.y+expand.y)*.5f;
    float min=center-half,max=center+half;
    if(Math.Abs(dir)<.000001f){if(at<min||at>max)return false;continue;}
    float a=(min-at)/dir,z=(max-at)/dir;var norm=axis==0?new Vector2(dir>0?-1:1,0):new Vector2(0,dir>0?-1:1);
    if(a>z)(a,z)=(z,a);if(a>near){near=a;normal=norm;}far=Math.Min(far,z);if(near>far)return false;
   }time=near;return far>=0&&near<=distance;
  }
  static IEnumerable<RaycastHit2D> Cast(Vector2 from,Vector2 dir,float distance,int mask,Vector2 expand){foreach(var c in Colliders(mask)){if(Slab(from,dir,distance,c.bounds,expand,out float t,out var normal)){if(normal.y>.9f)normal=c.SurfaceNormal;yield return new(){collider=c,point=new(from.x+dir.x*t,from.y+dir.y*t),normal=normal,distance=t};}}}
  public static int RaycastNonAlloc(Vector2 p,Vector2 dir,RaycastHit2D[] results,float distance,int layerMask){Queries++;int n=0;foreach(var hit in Cast(p,dir,distance,layerMask,Vector2.zero)){if(n==results.Length)return n;results[n++]=hit;}return n;}
  public static int BoxCastNonAlloc(Vector2 p,Vector2 size,float angle,Vector2 dir,RaycastHit2D[] results,float distance,int layerMask){Queries++;int n=0;foreach(var hit in Cast(p,dir,distance,layerMask,size)){if(n==results.Length)return n;results[n++]=hit;}return n;}
  public static RaycastHit2D Raycast(Vector2 from,Vector2 dir,float distance,int layerMask)=>Cast(from,dir,distance,layerMask,Vector2.zero).OrderBy(h=>h.distance).FirstOrDefault();
 }
}
namespace GlobalEnums {public enum CollisionSide {top}public enum DamageMode {FULL_DAMAGE}public enum HeroTransitionState {WAITING_TO_TRANSITION}}
public class HeroAnimationController:Component {}
public class PlayMakerFSM:Component {public int Hazard;public List<string> Events=new();public void SendEvent(string name)=>Events.Add(name);}
public class DamageHero:Component {public int damageDealt=1,hazardType;}
public static class FSMUtility {public static bool ContainsFSM(GameObject g,string name)=>g.GetComponent<PlayMakerFSM>()!=null;public static PlayMakerFSM LocateFSM(GameObject g,string name)=>g.GetComponent<PlayMakerFSM>();public static int GetInt(PlayMakerFSM f,string name)=>f.Hazard;}
public class HeroController:Component {
 public class State {public bool transitioning,dashing,shadowDashing,superDashing,recoiling,recoilFrozen,swimming,spellQuake,casting,freezeCharge,onConveyorV,superDashOnWall,hazardRespawning,hazardDeath,dead,invulnerable;}
 public PlayMakerFSM proxyFSM;private Rigidbody2D rb2d=>GetComponent<Rigidbody2D>();public float DEFAULT_GRAVITY=1,prevGravityScale;public bool acceptingInput=true;public State cState=new();public Action OnCharm;public bool controlReqlinquished;public int Resets,Accepted,Stopped,DamageCalls;public Action OnDamage;public void TakeDamage(GameObject source,GlobalEnums.CollisionSide side,int amount,int type){
 var p=KO.HollowKnight8.PlayerContext.Current;
 if(p==null||p.Hero!=this||amount!=1||type!=2)throw new Exception("invalid native platform damage call");
 DamageCalls++;if(!p.Vitals.Invincible){if(p.Vitals.Blue>0)p.Vitals.Blue--;else p.Vitals.Health--;}
 if(p.Vitals.Health<=0){p.Down=true;p.Hazard=false;}OnDamage?.Invoke();
 }public void CharmUpdate()=>OnCharm?.Invoke();public void AcceptInput(){Accepted++;acceptingInput=true;}public void AffectedByGravity(bool gravityApplies){if(rb2d.gravityScale>float.Epsilon&&!gravityApplies){prevGravityScale=rb2d.gravityScale;rb2d.gravityScale=0;}else if(rb2d.gravityScale<=float.Epsilon&&gravityApplies){rb2d.gravityScale=prevGravityScale;prevGravityScale=0;}}public void SetDamageMode(GlobalEnums.DamageMode d){}public void StopAllCoroutines(){Stopped++;}
 public T[] GetComponentsInChildren<T>(bool includeInactive)where T:class=>gameObject.Components.OfType<T>().ToArray();
 public Vector3 FindGroundPoint(Vector2 from,bool useExtended){var hit=Physics2D.Raycast(from,Vector2.down,30,1<<8);var b=KO.HollowKnight8.DuelGround.Body(KO.HollowKnight8.Plugin.Self.Session.Primary);return new(from.x,hit.point.y+b.extents.y-(b.center.y-transform.position.y),0);}
}
public class CameraController:UnityEngine.Object {public float sceneWidth=100,sceneHeight=50;}
public class GameManager:UnityEngine.Object {public bool isPaused;public static GameManager instance=new();public string sceneName="Test";public float sceneWidth=100,sceneHeight=50;public CameraController cameraCtrl=new();}
namespace KO.HollowKnight8 {
 internal class Vitals {internal int HazardHealth=5;internal int Health=5,Blue=2,Joni,Soul=40,Reserve=8;internal bool Invincible,DisablePause;internal Vector3 HazardPoint;}
 internal class PlayerSlot {internal int RootLayer=9,LastInputFrame;internal float WakeUntil;internal void Apply(Data d){d.health=Vitals.Health;d.healthBlue=Vitals.Blue;d.MPCharge=Vitals.Soul;d.MPReserve=Vitals.Reserve;}internal int Index;internal HeroController Hero;internal Vitals Vitals=new();internal bool Ready=true,Connected=true,Down,Hazard,Retiring,SpawnPending,SpawnAsDown,InputBlocked,ArenaTransfer,HasSafePoint,HasPreviousSafePoint,AcidAssistActive;internal float LifeStartedAt,ProtectionUntil,JoinVisualUntil,RetryAt,HazardUntil,SafeAt;internal Vector3 SafePoint,PreviousSafePoint,LastHazardPosition;internal readonly Dictionary<Collider2D,bool> ColliderStates=new();internal readonly Dictionary<Renderer,bool> RendererStates=new();internal int CurrentMaxHealth=>5;internal bool Alive=>Hero&&!Down&&!Hazard&&!Retiring&&Hero.gameObject.activeInHierarchy;}
 internal class Data {internal int MPCharge,MPReserve,joniHealthBlue;internal int health=5,healthBlue;internal bool GetBool(string name)=>false;internal int CurrentMaxHealth=>5;}
 internal sealed partial class CoopSession {internal bool Active=true,Gameplay=true,HasArrival,TeamWipe;internal bool restedBench;internal Vector3 benchPosition;internal int Recoveries;internal void Hazard(PlayerSlot p){p.Hazard=true;p.HazardUntil=Time.time+.7f;}internal PlayerSlot Resolve(HeroController h)=>Players.FirstOrDefault(p=>p.Hero==h);internal Vector3 Arrival;internal List<PlayerSlot> Players=new();internal PlayerSlot Primary=>Players.FirstOrDefault();internal Data Data=new();internal static void RestoreLivingVisuals(PlayerSlot p){}internal bool NearAlly(PlayerSlot a,PlayerSlot p,out Vector3 at)=>SpawnSafety.NearAlly(this,a,p,out at);internal PlayerSlot Nearest(Vector3 point)=>Players.Where(p=>p.Ready&&p.Alive).OrderBy(p=>Vector2.Distance(p.Hero.transform.position,point)).FirstOrDefault(); }
 internal class Runtime {internal CoopSession Session;internal bool Panel;internal List<IEnumerator> Routines=new();internal void StartCoroutine(IEnumerator routine){Routines.Add(routine);}}
 internal static class Plugin {internal static Runtime Self;}
 internal static class Diagnostics {internal static List<string> Lines=new();internal static void Write(string text)=>Lines.Add(text);internal static void Snapshot(CoopSession s,string reason){}internal static void Throttled(string text,Exception e)=>Lines.Add(text);}
 internal static class ArenaGather {internal static bool TransferActive;internal static PlayerSlot Anchor;internal static Func<Vector3,bool> Rule=at=>true;internal static bool Allows(Vector3 at)=>Rule(at);internal static bool TryDestination(CoopSession s,PlayerSlot p,out Vector3 at){at=Vector3.zero;return false;}}
 internal static class TransitionVote {internal static HashSet<int> Waiting=new();internal static bool Holding(PlayerSlot p)=>Waiting.Contains(p.Index);}
 internal static partial class EmergencyWarp {internal sealed class Trip {internal PlayerSlot Player;internal HeroController Hero;internal GameObject Effect;internal Vector3 Origin;internal Dictionary<Collider2D,bool> Colliders=new();internal Dictionary<Renderer,bool> Renderers=new();}internal static HashSet<int> Warping=new();internal static bool Active(PlayerSlot p)=>Warping.Contains(p.Index);internal static void Cancel(PlayerSlot p)=>Warping.Remove(p.Index);}
 internal static class CoopEnding {internal static bool Active;internal static HashSet<int> Held=new();internal static bool HoldsActor(PlayerSlot p)=>Held.Contains(p.Index);}
 internal static class PvpArena {internal static bool PlacementSafe(bool normal,Vector3 at,PlayerSlot p)=>normal;}
 internal static class Hooks {internal static void RebindActor(PlayerSlot p){}}
 internal static class PaleCourtInterop {internal static void RepairClone(CoopSession s,PlayerSlot p){}}
 internal sealed class PlayerContext:IDisposable {internal static PlayerSlot Current;readonly PlayerSlot before;internal PlayerContext(PlayerSlot p){before=Current;Current=p;}internal static PlayerContext Enter(PlayerSlot p)=>new(p);public void Dispose(){Current=before;}}
 internal static class ActorRecovery {internal static void Reset(PlayerSlot p){p.Hero.Resets++;p.Hero.GetComponent<Rigidbody2D>().simulated=true;p.Hero.GetComponent<Collider2D>().enabled=true;}internal static void Freeze(PlayerSlot p){p.Hero.GetComponent<Rigidbody2D>().simulated=false;p.Hero.GetComponent<Rigidbody2D>().velocity=Vector2.zero;}}
 internal static class Reflect {internal static void Set(object o,string field,object value){o.GetType().GetField(field)?.SetValue(o,value);}}
 internal static class PlayerLighting {internal static void Sync(PlayerSlot a,PlayerSlot b){}}
 internal static class SummonRouting {internal static void Refresh(PlayerSlot p){}}
 internal static class TimedRespawnRecovery {internal static int Restores;internal static void RestoreDown(CoopSession s,PlayerSlot p){Restores++;p.Down=true;p.Vitals.Health=0;}}
 internal static class Revival {internal static void End(PlayerSlot p){}}
 internal static class CombatEffects {internal static void Restore(PlayerSlot p){}}
 internal sealed class SpawnMetrics:IDisposable {internal SpawnMetrics(PlayerSlot p,bool reuse){}internal void Mark(string label){}public void Dispose(){}}
 internal static class BenchSeats {internal static HashSet<int> SeatedPlayers=new();internal static bool Seated(PlayerSlot p)=>SeatedPlayers.Contains(p.Index);}
 internal static class ScriptedParty {internal static bool Active;}
 internal static class Doorways {internal static bool Tick(CoopSession s)=>false;}
 internal static class NativeDreamFx {internal static void TeleportTrail(PlayerSlot p,Vector3 from,Vector3 to){}}
 internal static class CoopRules {internal static double PlayerDepth(int index)=>index*.001;}
}

namespace KO.HollowKnight8 {
 internal static class HornetEncounterFlow {internal static readonly HashSet<PlayerSlot> Held=new();internal static bool Holds(PlayerSlot p)=>Held.Contains(p);}
 internal static class WorldRouting {internal static readonly List<PlayerSlot> Recovering=new();internal static void LocalRecovery(PlayerSlot p)=>Recovering.Add(p);}
 internal static class RadianceAscentCheckpoint {internal static bool BeforeRecovery(CoopSession s,PlayerSlot p,ref Vector3? at)=>true;internal static bool Allows(bool normal,CoopSession s,PlayerSlot p,Vector3 at)=>normal;}
 internal static class DreamSequence {internal static bool InStoryDream;}
 internal static class PvpMatch {internal static bool Running;}
 internal static class PvpCombat {internal static void Recovered(PlayerSlot p)=>Plugin.Self.Session.Recoveries++;}
}

public class VinePlatform:Component {public Collider2D collider;public Rigidbody2D Body;public int Lands;public void Land()=>On.VinePlatform.Run(this);public void Native_Land(){Lands++;Body.isKinematic=true;}}
public class VinePlatformCut:Component {public Rigidbody2D body;public void Cut()=>On.VinePlatformCut.Run(this);public void Native_Cut(){body.isKinematic=false;}}
namespace On {public static class VinePlatform {public delegate void orig_Land(global::VinePlatform self);public delegate void hook_Land(orig_Land orig,global::VinePlatform self);public static event hook_Land Land;public static int Count=>Land?.GetInvocationList().Length??0;public static void Run(global::VinePlatform self){orig_Land next=x=>x.Native_Land();if(Land!=null)foreach(hook_Land hook in Land.GetInvocationList()){var old=next;next=x=>hook(old,x);}next(self);}}
public static class VinePlatformCut {public delegate void orig_Cut(global::VinePlatformCut self);public delegate void hook_Cut(orig_Cut orig,global::VinePlatformCut self);public static event hook_Cut Cut;public static int Count=>Cut?.GetInvocationList().Length??0;public static void Run(global::VinePlatformCut self){orig_Cut next=x=>x.Native_Cut();if(Cut!=null)foreach(hook_Cut hook in Cut.GetInvocationList()){var old=next;next=x=>hook(old,x);}next(self);}}}

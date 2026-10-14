using System;using System.Collections.Generic;using System.Linq;
namespace UnityEngine {
 public class Object {
  public static readonly List<Object> All=new();public bool destroyed;public string name;
  public static implicit operator bool(Object o)=>o!=null&&!o.destroyed;
  public static T[] FindObjectsOfType<T>() where T:Object=>All.OfType<T>().Where(o=>o&&(!(o is Component c)||c.gameObject.activeInHierarchy)).ToArray();
 }
 public class Component:Object {
  public GameObject gameObject;public Transform transform=>gameObject.transform;
  public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
  public T[] GetComponents<T>() where T:Component=>gameObject.GetComponents<T>();
  public T GetComponentInParent<T>() where T:Component{for(Transform t=transform;t;t=t.parent){var v=t.gameObject.GetComponent<T>();if(v)return v;}return null;}
 }
 public class Transform:Component {public Transform parent;public Vector3 position;public bool IsChildOf(Transform root){for(var t=this;t;t=t.parent)if(t==root)return true;return false;}}
 public class GameObject:Object {
  public Transform transform;public bool activeInHierarchy=true;public readonly List<Component> Components=new();
  public GameObject(string name){this.name=name;All.Add(this);transform=Add<Transform>();}
  public T Add<T>() where T:Component,new(){var c=new T{name=name,gameObject=this};Components.Add(c);All.Add(c);return c;}
  public T GetComponent<T>() where T:Component=>Components.OfType<T>().FirstOrDefault(c=>c);
  public T[] GetComponents<T>() where T:Component=>Components.OfType<T>().Where(c=>c).ToArray();
  public T[] GetComponentsInChildren<T>(bool inactive) where T:Component=>All.OfType<T>().Where(c=>c&&c.transform.IsChildOf(transform)&&(inactive||c.gameObject.activeInHierarchy)).ToArray();
 }
 public struct Vector3 {public float x,y,z;}
 public struct Vector2 {public float x,y;public static implicit operator Vector2(Vector3 v)=>new(){x=v.x,y=v.y};}
 public struct ColliderDistance2D {public bool isOverlapped;}
 public class Collider2D:Component {
  public bool isTrigger,enabled=true;public float Radius=2;
  public bool OverlapPoint(Vector2 v)=>Math.Abs(v.x-transform.position.x)<Radius&&Math.Abs(v.y-transform.position.y)<Radius;
  public ColliderDistance2D Distance(Collider2D c)=>new(){isOverlapped=OverlapPoint(c.transform.position)};
 }
 public static class Time {public static float unscaledTime,time,timeScale=1;}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);}
}
namespace UnityEngine.SceneManagement {public struct Scene{public int handle;} public static class SceneManager{public static int Handle;public static Scene GetActiveScene()=>new(){handle=Handle};}}
namespace HutongGames.PlayMaker {
 public class Fsm {public UnityEngine.GameObject GameObject;public FsmState[] States;public FsmState ActiveState;}
 public class FsmState {public string Name;public FsmStateAction[] Actions;}
 public class FsmStateAction {}
 public class FsmString {string text;public static int Reads;public string Value{get{Reads++;return text;}set=>text=value;}}
 public class FsmFloat {public float Value;}
}
namespace HutongGames.PlayMaker.Actions {
 public class CallMethodProper:HutongGames.PlayMaker.FsmStateAction {public HutongGames.PlayMaker.FsmString methodName=new();}
 public class Wait:HutongGames.PlayMaker.FsmStateAction {public HutongGames.PlayMaker.FsmFloat time=new();}
}
public class PlayMakerFSM:UnityEngine.Component {public HutongGames.PlayMaker.Fsm Fsm;}
public class HeroController:UnityEngine.Component {}
public class HealthManager:UnityEngine.Component {}
public class DamageHero:UnityEngine.Component {}
public class GameManager:UnityEngine.Component {public static GameManager instance;public bool isPaused,IsLoadingSceneTransition;public bool HasFinishedEnteringScene=true;}
public class PlayerData {
 public int MPCharge,maxMP=99,MPReserve,MPReserveMax=66;
 public void AddMPCharge(int n){int overflow=Math.Max(0,MPCharge+n-maxMP);MPCharge=Math.Min(maxMP,MPCharge+n);MPReserve=Math.Min(MPReserveMax,MPReserve+overflow);}
}
public static class BossSequenceController{public static bool BoundSoul;}
namespace On {public static class HeroController{public delegate bool orig_TryAddMPChargeSpa(global::HeroController hero,int amount);}}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {
  internal int Index,MP,Reserve;internal bool Alive=true,Ready=true,SpawnPending,ArenaTransfer;internal HeroController Hero;
  internal void Capture(PlayerData d){MP=d.MPCharge;Reserve=d.MPReserve;}
 }
 internal class CoopSession {
  internal bool Active=true,Gameplay=true;internal PlayerData Data=new();internal List<PlayerSlot> Players=new();
  internal PlayerSlot Resolve(HutongGames.PlayMaker.Fsm f)=>Players.FirstOrDefault(p=>f.GameObject.transform.IsChildOf(p.Hero.transform));
 }
 internal static class RoleSystem {internal static CoopSession Session;}
 internal static class WorldRouting {internal static HutongGames.PlayMaker.Fsm Current;}
 internal sealed class PlayerContext:IDisposable {
  PlayerSlot player;int mp,reserve;private PlayerContext(PlayerSlot p){player=p;mp=RoleSystem.Session.Data.MPCharge;reserve=RoleSystem.Session.Data.MPReserve;RoleSystem.Session.Data.MPCharge=p.MP;RoleSystem.Session.Data.MPReserve=p.Reserve;}
  internal static PlayerContext Enter(PlayerSlot p)=>new(p);
  public void Dispose(){player.Capture(RoleSystem.Session.Data);RoleSystem.Session.Data.MPCharge=mp;RoleSystem.Session.Data.MPReserve=reserve;}
 }
 internal static class Diagnostics {internal static void Write(string text){} }
}

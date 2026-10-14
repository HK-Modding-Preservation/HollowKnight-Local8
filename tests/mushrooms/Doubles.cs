using System; using System.Collections.Generic; using System.Linq; using System.Reflection;
namespace UnityEngine {
 public class Object {public bool destroyed;public string name;public static implicit operator bool(Object o)=>o!=null&&!o.destroyed;}
 public class Component:Object {
  public GameObject gameObject;public Transform transform=>gameObject.transform;
  public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
  public T GetComponentInParent<T>() where T:Component {for(var t=transform;t;t=t.parent){var c=t.GetComponent<T>();if(c)return c;}return null;}
 }
 public class Transform:Component {public Transform parent;}
 public class GameObject:Object {
  public Transform transform;public List<Component> components=new();
  public GameObject(string n){name=n;transform=Add<Transform>();}
  public T Add<T>() where T:Component,new(){var c=new T{gameObject=this,name=name};components.Add(c);return c;}
  public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault(c=>c);
 }
 public class Collider2D:Component {}
 public class Collision2D {public Collider2D collider;public GameObject gameObject;}
}
namespace HutongGames.PlayMaker {public class Fsm {}}
public class PlayMakerFSM:UnityEngine.Component {public HutongGames.PlayMaker.Fsm Fsm=new();}
public static class FSMUtility {public static PlayMakerFSM LocateFSM(UnityEngine.GameObject g,string n)=>g.GetComponent<PlayMakerFSM>();}
public class HeroController:UnityEngine.Component {
 public static HeroController instance;public bool controlReqlinquished;public States cState=new();
 public class States {public bool transitioning;}
 public int bounces,actions;public float xVelocity,yVelocity;public void BounceAction(float y){bounces++;actions++;yVelocity=y;}
}
public class NailSlash:UnityEngine.Component {private HeroController heroCtrl;public void Cache(HeroController h)=>heroCtrl=h;public HeroController Cache()=>heroCtrl;}
public class BounceShroom:UnityEngine.Component {public bool active;}
public class CollisionEnterEvent:UnityEngine.Component {}
namespace On {
 public static class NailSlash {
  public delegate void orig_OnTriggerEnter2D(global::NailSlash self,UnityEngine.Collider2D other);
  public delegate void hook_OnTriggerEnter2D(orig_OnTriggerEnter2D orig,global::NailSlash self,UnityEngine.Collider2D other);
  public static event hook_OnTriggerEnter2D OnTriggerEnter2D;
  public static int Subscribers=>OnTriggerEnter2D?.GetInvocationList().Length??0;
  public static void Emit(orig_OnTriggerEnter2D orig,global::NailSlash self,UnityEngine.Collider2D other){if(OnTriggerEnter2D==null)orig(self,other);else OnTriggerEnter2D(orig,self,other);}
 }
 public static class CollisionEnterEvent {
  public delegate void orig_HandleCollision(global::CollisionEnterEvent self,UnityEngine.Collision2D collision);
  public delegate void hook_HandleCollision(orig_HandleCollision orig,global::CollisionEnterEvent self,UnityEngine.Collision2D collision);
  public static event hook_HandleCollision HandleCollision;
  public static int Subscribers=>HandleCollision?.GetInvocationList().Length??0;
  public static void Emit(orig_HandleCollision orig,global::CollisionEnterEvent self,UnityEngine.Collision2D collision){if(HandleCollision==null)orig(self,collision);else HandleCollision(orig,self,collision);}
 }
}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal bool Alive=true,Ready=true,Down,Hazard,Retiring,SpawnPending,ArenaTransfer,InputBlocked;}
 internal class CoopSession {internal bool Active=true;internal List<PlayerSlot> Players=new();internal PlayerSlot Resolve(HeroController h)=>Players.FirstOrDefault(p=>p.Hero==h);}
 internal class Plugin {internal static Plugin Self;internal CoopSession Session;}
 internal static class Reflect {internal static FieldInfo Field(Type t,string n)=>t.GetField(n,BindingFlags.Instance|BindingFlags.NonPublic);}
 internal sealed class PlayerContext:IDisposable {
  internal static PlayerSlot Current;PlayerSlot old;HeroController oldHero;
  internal static PlayerContext Enter(PlayerSlot p)=>new(p);
  private PlayerContext(PlayerSlot p){old=Current;oldHero=HeroController.instance;Current=p;HeroController.instance=p.Hero;}
  public void Dispose(){Current=old;HeroController.instance=oldHero;}
 }
 internal static class Diagnostics {internal static List<string> Lines=new();internal static void Write(string t)=>Lines.Add(t);}
}

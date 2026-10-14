using System;using System.Collections.Generic;using System.Linq;
namespace UnityEngine {
 public class Object {public bool Alive=true;public static implicit operator bool(Object o)=>!ReferenceEquals(o,null)&&o.Alive;}
 public class Component:Object {
  public GameObject gameObject;public Transform transform=>gameObject.transform;
  public T GetComponentInParent<T>()where T:Component{for(var t=transform;t!=null;t=t.parent){var c=t.gameObject.GetComponent<T>();if(c)return c;}return null;}
 }
 public class MonoBehaviour:Component{}
 public class GameObject:Object {
  public string name;public readonly Transform transform;public readonly List<Component> Components=new();
  public GameObject(string name,Transform parent=null){this.name=name;transform=new(){gameObject=this,parent=parent};Components.Add(transform);parent?.Children.Add(transform);}
  public T Add<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}
  public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault(x=>x.Alive);
  public T[] GetComponents<T>()where T:Component=>Components.OfType<T>().Where(x=>x.Alive).ToArray();
  public Component[] GetComponents(Type t)=>Components.Where(x=>x.Alive&&t.IsInstanceOfType(x)).ToArray();
 }
 public class Transform:Component {
  public Transform parent;public string name=>gameObject.name;public readonly List<Transform> Children=new();
  public bool IsChildOf(Transform t){for(var n=this;n!=null;n=n.parent)if(n==t)return true;return false;}
  public Transform Find(string path){var at=this;foreach(var part in path.Split('/')){at=at.Children.FirstOrDefault(x=>x.Alive&&x.name==part);if(at==null)return null;}return at;}
 }
 public class Sprite:Object{}
 public struct Vector3{public float x,y,z;}
 public static class Time{public static int frameCount;public static float unscaledTime,timeScale=1;}
}
namespace Modding {
 public static class ModHooks{
  public delegate bool SetBoolProxy(string name,bool value);public static event SetBoolProxy SetPlayerBoolHook;
  public static bool Set(string n,bool v){if(SetPlayerBoolHook!=null)foreach(SetBoolProxy h in SetPlayerBoolHook.GetInvocationList())v=h(n,v);return v;}
 }
}
public class CharmIconList:UnityEngine.Object{public static CharmIconList Instance;public UnityEngine.Sprite[] spriteList;}
public partial class PlayerData {
 public int health,healthBlue,MPCharge,MPReserve,maxMP,prevHealth,blockerHits,maxHealth,joniHealthBlue,focusMP_amount,charmSlotsFilled,charmSlots=11;
 public bool isInvincible,soulLimited,hazardRespawnFacingRight,damagedBlue,atBench,overcharmed,canOvercharm;
 public UnityEngine.Vector3 hazardRespawnLocation;public List<int> equippedCharms=new();
 public readonly Dictionary<int,bool> Provider=new();
 public bool GetBool(string n){if(n.StartsWith("equippedCharm_")){int id=int.Parse(n.Substring(14));return id<=40?(bool)GetType().GetField(n).GetValue(this):Provider.TryGetValue(id,out var v)&&v;}return true;}
 public void SetBool(string n,bool v){v=Modding.ModHooks.Set(n,v);int id=int.Parse(n.Substring(14));if(id<=40)GetType().GetField(n).SetValue(this,v);else Provider[id]=v;}
 public int GetInt(string n)=>1;
}
public class Button{public float Value;}
public class HeroActions{public readonly Button up=new(),down=new(),left=new(),right=new();}
public class HeroControllerStates{public bool Mark;}
public class HeroController:UnityEngine.MonoBehaviour {
 private static HeroController _instance;
 public static HeroController instance=>_instance;
 public static void NativeAssign(HeroController h)=>_instance=h;
 public HeroControllerStates cState=new();
}
public class InputHandler:UnityEngine.Object {
 private HeroController heroCtrl;private HeroControllerStates cState;
 public HeroActions inputActions;public float inputX,inputY;
 public HeroController Hero=>heroCtrl;public HeroControllerStates State=>cState;
 public void NativeAssign(HeroController h){heroCtrl=h;cState=h.cState;}
}
public class GameManager:UnityEngine.Object{public static GameManager instance;public HeroController hero_ctrl{get;private set;}public InputHandler inputHandler;public void NativeAssign(HeroController h)=>hero_ctrl=h;}
public static class HeroBox{public static bool inactive;}
public class PlayMakerFSM:UnityEngine.Component{public string FsmName;}
namespace HutongGames.PlayMaker {
 public class Fsm {public string Name;public UnityEngine.GameObject GameObject;}
 public class FsmStateAction {public Fsm Fsm;}
 public class FsmGameObject {public UnityEngine.GameObject Value;}
 public class FsmObject {public UnityEngine.Object Value;}
 public class FsmOwnerDefault {public FsmGameObject GameObject;}
 public class FsmEventTarget {public FsmOwnerDefault gameObject;public PlayMakerFSM fsmComponent;}
 public class FsmVariables {
  public static FsmVariables GlobalVariables=new();public FsmGameObject[] GameObjectVariables=Array.Empty<FsmGameObject>();public FsmObject[] ObjectVariables=Array.Empty<FsmObject>();
 }
}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {
  internal int Index;internal HeroController Hero;internal HeroActions Actions=new();internal bool Retiring,HeroBoxInactive;
  internal Vitals Vitals=new();internal CharmLoadout Charms=new();
  internal void Capture(PlayerData d){Vitals.Read(d);Charms.Read(d);}
  internal void Apply(PlayerData d){Vitals.Write(d);Charms.Write(d);}
 }
 internal class CoopSession {
  internal PlayerData Data=new();internal PlayerSlot Primary=>Players[0];internal readonly List<PlayerSlot> Players=new();
  internal PlayerSlot Resolve(object o){var c=o as UnityEngine.Component;if(o is UnityEngine.GameObject g)c=g.transform;if(!c)return null;return Players.FirstOrDefault(p=>p.Hero&&c.transform.IsChildOf(p.Hero.transform));}
  internal UnityEngine.GameObject Remap(UnityEngine.GameObject g,PlayerSlot from,PlayerSlot to){if(!g||!from.Hero||!to.Hero)return g;if(g==from.Hero.gameObject)return to.Hero.gameObject;var t=g.transform;if(!t.IsChildOf(from.Hero.transform))return g;string path=t.name;while(t.parent&&t.parent!=from.Hero.transform){t=t.parent;path=t.name+"/"+path;}var n=to.Hero.transform.Find(path);return n?n.gameObject:g;}
 }
 internal class Runtime {internal CoopSession Session;}
 internal static class Plugin {internal static Runtime Self;}
 internal static class PvpFamiliars {internal static bool Rebind(object o,PlayerSlot p)=>false;}
 internal static class Diagnostics{internal static readonly List<string> Messages=new();internal static void Write(string s)=>Messages.Add(s);}
}

namespace KO.HollowKnight8 {internal static class DamageParticleBudget {internal static string Report()=>"";internal static void ResetWindow(){}}}

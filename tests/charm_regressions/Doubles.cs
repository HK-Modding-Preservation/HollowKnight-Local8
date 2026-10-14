using System;using System.Collections.Generic;using System.Reflection;
namespace UnityEngine {
 public class Object {public bool Destroyed;public static implicit operator bool(Object obj)=>obj!=null&&!obj.Destroyed;public static void Destroy(Object obj){if(obj!=null)obj.Destroyed=true;}public static T Instantiate<T>(T original,Transform parent)where T:Object{if(original is Component c)c.transform.parent=parent;return original;}}
 public class Transform:Object {public string name;public Transform parent;public bool IsChildOf(Transform other){for(var p=parent;p!=null;p=p.parent)if(p==other)return true;return false;}}
 public class GameObject:Object {public string name {get=>transform.name;set=>transform.name=value;}public bool activeInHierarchy=true;public Transform transform=new(){name="Object"};public readonly List<Component> components=new();public void SetActive(bool active){activeInHierarchy=active;}public T GetComponent<T>()where T:Component=>components.Find(c=>c is T)as T;public T GetComponentInParent<T>()where T:Component{foreach(var c in components)if(c is T item)return item;return null;}public T Add<T>(T c)where T:Component{c.gameObject=this;components.Add(c);return c;}}
 public class Component:Object {public UnityEngine.Transform transform=>gameObject.transform;public GameObject gameObject;public Component GetComponent(Type t)=>gameObject.components.Find(c=>t.IsInstanceOfType(c));}
 public class MonoBehaviour:Component {}
 public static class Time {public static float unscaledTime;}
}
namespace UnityEngine.EventSystems {public class EventSystem {public static EventSystem current;public UnityEngine.GameObject currentSelectedGameObject;}}
namespace GlobalEnums {public enum UIState {PLAYING,PAUSED}}
namespace HutongGames.PlayMaker {
 public class Fsm {public string Name;public UnityEngine.GameObject GameObject;}
 public class FsmState {public string Name;public FsmStateAction[] Actions;}
 public class FsmStateAction {public FsmState State;public bool Finished;public virtual void OnEnter(){}public void Init(FsmState state){State=state;}public void Finish(){Finished=true;}}
}
public class PlayMakerFSM:UnityEngine.Component {public bool isActiveAndEnabled=true;public string ActiveStateName="Open";public HutongGames.PlayMaker.FsmState[] FsmStates=Array.Empty<HutongGames.PlayMaker.FsmState>();}
public class HeroController:UnityEngine.Component {public static HeroController instance;public PlayMakerFSM spellControl;}
public class ShopMenuStock:UnityEngine.Component{}
public class HeroActions{}
public class GameManager:UnityEngine.Object {public static GameManager instance;public bool isPaused,IsLoadingSceneTransition;public PlayMakerFSM inventoryFSM;}
public class UIManager:UnityEngine.Object {public static UIManager instance;public GlobalEnums.UIState uiState;}
namespace On {public static class HeroController {public delegate void orig_Start(global::HeroController hero);}}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {internal int Index;internal bool Alive=true,Ready=true;internal HeroController Hero;internal HeroActions Actions=new();internal Vitals Vitals=new();}
 internal class Vitals {internal bool AtBench;}
 internal class CoopSession {internal bool Active=true;internal PlayerSlot Primary,Cloning;internal List<PlayerSlot> Players=new();internal PlayerSlot Resolve(UnityEngine.Component c)=>c==null?null:Players.Find(p=>p.Hero!=null&&(p.Hero.gameObject==c.gameObject||c.transform.IsChildOf(p.Hero.transform)));}
 internal class Runtime {internal CoopSession Session;}
 internal static class Plugin {internal static Runtime Self;}
 internal static class Diagnostics {internal static int Errors;internal static void Write(string s){}internal static void Throttled(string s,Exception e){Errors++;}}
 internal static class InteractionRouter {internal static PlayerSlot ActivePlayer;internal static bool ActiveIsBench;internal static bool ScreenList(HutongGames.PlayMaker.Fsm f)=>f!=null&&(string.Equals(f.Name,"ui_list",StringComparison.OrdinalIgnoreCase)||string.Equals(f.Name,"ui_list_getinput",StringComparison.OrdinalIgnoreCase));}
 internal static class StagMenuRouting {internal static bool HasOwner;}
 internal static class PickupCard {internal static PlayerSlot Owner;}
 internal static class Charms {internal static UnityEngine.GameObject NativePane;internal static string NativeProbeReason="hidden";}
 internal static class CharmMenuDiagnostics {internal static bool Report(bool open,string reason)=>open;}
 internal static partial class ShopMenuRouting {
  internal static PlayerSlot owner;internal static HutongGames.PlayMaker.Fsm list,confirmation;internal static float lastInput;internal static int Binds;
  internal static PlayerSlot Buyer=>owner;internal static bool MenuVisible=>list!=null&&list.GameObject.activeInHierarchy;
  private static void Release(){owner=null;list=confirmation=null;}private static void Bind(){Binds++;}
  internal static void Reset(){Release();Binds=0;}
 }
 internal sealed class PlayerContext:IDisposable {internal static PlayerSlot Current;private readonly PlayerSlot previous;private readonly HeroController old;internal static PlayerContext Enter(PlayerSlot p)=>new(p);private PlayerContext(PlayerSlot p){previous=Current;old=HeroController.instance;Current=p;HeroController.instance=p.Hero;}public void Dispose(){Current=previous;HeroController.instance=old;}}
}
namespace FiveKnights {
 public class Amulets:UnityEngine.MonoBehaviour {public void On_HeroController_Start(On.HeroController.orig_Start orig,HeroController hero){orig(hero);throw new Exception("provider body sentinel");}}
 public class CheckBloomStage:UnityEngine.MonoBehaviour {private void CheckStage(){throw new Exception("provider stage sentinel");}}
 public class AbyssalBloom:UnityEngine.MonoBehaviour {private List<object> _nailSlashes;public void SetLevel(int level){if(_nailSlashes==null)throw new Exception("provider bloom sentinel");}}
 public class BoonSpells:UnityEngine.MonoBehaviour {public PlayMakerFSM Template,Last;public string ReadState;private void OnEnable(){Last=UnityEngine.Object.Instantiate(Template,HeroController.instance.transform);ReadState=Last.ActiveStateName;}public List<(string method,bool upgrade,HeroController hero)> Casts=new();public bool Throws;public void CastDaggers(bool u)=>Cast("CastDaggers",u);public void CastPlumes(bool u)=>Cast("CastPlumes",u);public void CastBlasts(bool u)=>Cast("CastBlasts",u);void Cast(string method,bool u){if(Throws)throw new Exception("spell sentinel");Casts.Add((method,u,HeroController.instance));}}
 public class InvokeMethod:HutongGames.PlayMaker.FsmStateAction{}
 public class MethodAction:HutongGames.PlayMaker.FsmStateAction{}
 public class MissingAction:HutongGames.PlayMaker.FsmStateAction{}
}

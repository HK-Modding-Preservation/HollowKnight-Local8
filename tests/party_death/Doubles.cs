using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public class Object {public bool destroyed; public static implicit operator bool(Object o)=>o!=null&&!o.destroyed; public static int serial; public readonly int id=++serial; public int GetInstanceID()=>id;}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponentInParent<T>()where T:Component=>gameObject.GetComponentInParent<T>();}
 public class Transform:Component {public Transform parent;public bool IsChildOf(Transform t){for(var p=parent;p!=null;p=p.parent)if(p==t)return true;return false;}}
 public class GameObject:Object {public string name;public bool activeInHierarchy=true;public readonly Transform transform;private readonly List<Component> components=new();public GameObject(string name){this.name=name;transform=new Transform{gameObject=this};components.Add(transform);}public T Add<T>()where T:Component,new(){var c=new T{gameObject=this};components.Add(c);return c;}public T GetComponentInParent<T>()where T:Component {for(var g=this;g!=null;g=g.transform.parent?.gameObject){var c=g.components.OfType<T>().FirstOrDefault();if(c!=null)return c;}return null;}}
}
public class HeroController:UnityEngine.Component{public UnityEngine.GameObject heroDeathPrefab;}
public class GameManager{}
public class PlayerData {public bool hasDreamNail;}
namespace HutongGames.PlayMaker {public class Fsm{public string Name;public UnityEngine.GameObject GameObject;}public class FsmEvent{public string Name;public FsmEvent(string name){Name=name;}}public class FsmEventData{public UnityEngine.GameObject SentByGameObject;public Fsm SentByFsm;}}
namespace KO.HollowKnight8 {
 internal class Vitals {internal int Health=5,Blue;}
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal bool Down,Hazard,Retiring,Ready=true,Connected=true,SpawnPending;internal Vitals Vitals=new();}
 internal class CoopSession {internal bool Active=true,TeamWipe,AllowVanillaDeath;internal List<PlayerSlot> Players=new();internal PlayerSlot Primary=>Players.Count==0?null:Players[0];internal PlayerData Data=new();internal readonly Dictionary<object,PlayerSlot> Owners=new();internal int downCalls;internal void Down(PlayerSlot p){downCalls++;p.Down=true;p.Vitals.Health=0;p.Vitals.Blue=0;}internal PlayerSlot Resolve(object obj){if(obj is HutongGames.PlayMaker.Fsm f)obj=f.GameObject;if(obj is UnityEngine.GameObject g)obj=g.GetComponentInParent<HeroController>();foreach(var p in Players)if(p.Hero==obj)return p;return obj!=null&&Owners.TryGetValue(obj,out var owner)?owner:null;}}
 internal class Plugin {internal static Plugin Self;internal CoopSession Session;}
 internal static class PvpMatch {internal static bool Running;}
 internal static class CoopEnding {internal static bool Active;}
 internal static class DreamSequence {internal static bool InStoryDream;}
 internal sealed class PlayerContext:IDisposable {internal static PlayerSlot Current;internal static int entries;readonly PlayerSlot previous;private PlayerContext(PlayerSlot p){previous=Current;Current=p;entries++;}internal static PlayerContext Enter(PlayerSlot p)=>new(p);public void Dispose(){Current=previous;}}
 internal static class Diagnostics {internal static List<string> Messages=new();internal static void Write(string s)=>Messages.Add(s);}
 internal static class ElderBaldurRange {internal static bool BlockFar(HutongGames.PlayMaker.Fsm f,HutongGames.PlayMaker.FsmEvent e)=>false;}
 internal static class ShadeCloakRitual {internal static void BeforeEvent(HutongGames.PlayMaker.Fsm f,HutongGames.PlayMaker.FsmEvent e,HutongGames.PlayMaker.FsmEventData d){}}
 internal static class ChallengeSequence {internal static bool Intercept(HutongGames.PlayMaker.Fsm f,HutongGames.PlayMaker.FsmEvent e)=>false;}
 internal static class InteractionRouter {internal static bool Blocks(HutongGames.PlayMaker.Fsm f)=>false;internal static bool EventAllowed(HutongGames.PlayMaker.Fsm f,HutongGames.PlayMaker.FsmEventData d)=>true;}
 internal static class StagMenuRouting {internal static void Observe(HutongGames.PlayMaker.Fsm f,HutongGames.PlayMaker.FsmEvent e,HutongGames.PlayMaker.FsmEventData d){}internal static void AfterEvent(HutongGames.PlayMaker.Fsm f,HutongGames.PlayMaker.FsmEvent e){}}
 internal static class ArenaGather {internal static void Observe(HutongGames.PlayMaker.Fsm f,HutongGames.PlayMaker.FsmEvent e,HutongGames.PlayMaker.FsmEventData d){}}
 internal static class Lifeblood {internal static bool Award(HutongGames.PlayMaker.FsmEvent e)=>false;}
 internal static class SharedAcquisition {internal static void BeforeEvent(HutongGames.PlayMaker.Fsm f,HutongGames.PlayMaker.FsmEvent e,HutongGames.PlayMaker.FsmEventData d){}}
}
namespace On {public static class HeroController{public delegate IEnumerator orig_Die(global::HeroController self);}public static class GameManager {public delegate IEnumerator orig_PlayerDead(global::GameManager self,float wait);public delegate IEnumerator orig_PlayerDeadFromHazard(global::GameManager self,float wait);}}
namespace On.HutongGames.PlayMaker {public static class Fsm {public delegate void orig_ProcessEvent(global::HutongGames.PlayMaker.Fsm f,global::HutongGames.PlayMaker.FsmEvent e,global::HutongGames.PlayMaker.FsmEventData d);}}

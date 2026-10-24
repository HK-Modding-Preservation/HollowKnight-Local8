using System;
using System.Collections.Generic;
using System.Linq;

namespace GlobalEnums {public enum HazardType {SPIKES,ACID,PIT}}
namespace UnityEngine {
 public class Object {public bool destroyed;public static implicit operator bool(Object o)=>o!=null&&!o.destroyed;}
 public class Component:Object {
  public GameObject gameObject;public Transform transform=>gameObject.transform;
  public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
  public T[] GetComponentsInChildren<T>(bool inactive) where T:class=>gameObject.All().SelectMany(g=>g.Components).OfType<T>().ToArray();
 }
 public class GameObject:Object {
  public string name;public int layer=9;public Transform transform;public List<Component> Components=new();public List<GameObject> Children=new();
  public Func<Vector3,GameObject> Factory;
  public GameObject(string name="Object"){this.name=name;transform=new Transform{gameObject=this};}
  public T Add<T>(T c)where T:Component{c.gameObject=this;Components.Add(c);return c;}
  public T GetComponent<T>()where T:class=>Components.OfType<T>().FirstOrDefault();
  public IEnumerable<GameObject> All(){yield return this;foreach(var child in Children)foreach(var g in child.All())yield return g;}
 }
 public class Transform:Component {public Vector3 position,localScale=new(1,1,1);}
 public class Collider2D:Component {}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color clear=>new(0,0,0,0);}
 public static class Time {public static float unscaledTime,time;}
 public static class Mathf {public const float Rad2Deg=57.29578f;public static float Abs(float n)=>Math.Abs(n);}
 namespace SceneManagement {public struct Scene {public string name;}public static class SceneManager {public static string Name="Town";public static Scene GetActiveScene()=>new(){name=Name};}}
}
namespace HutongGames.PlayMaker {
 public class FsmFloat {public float Value;}
 public class FsmVariables {public FsmFloat Direction=new();public FsmFloat FindFsmFloat(string name)=>Direction;}
 public class Fsm {public bool HasDeactivate=true;public object GetState(string name)=>HasDeactivate&&name=="Deactivate"?this:null;}
}
public class PlayMakerFSM:UnityEngine.Component {
 public string FsmName="Fury",ActiveStateName="Stay Furied";public HutongGames.PlayMaker.Fsm Fsm=new();
 public HutongGames.PlayMaker.FsmVariables FsmVariables=new();public int Deactivated;public Action OnDeactivate;
 public void SetState(string state){ActiveStateName=state;Deactivated++;OnDeactivate?.Invoke();}
}
public class SpriteFlash:UnityEngine.Component {
 public bool repeatFlash;public UnityEngine.Color flashColour;public int Cancelled,White;
 public void CancelFlash(){Cancelled++;}
 public void flashWhiteQuick(){White++;}
}
public enum HeroSounds {TAKE_HIT}
public class HeroAudioController:UnityEngine.Component {public int Hits;public void PlaySound(HeroSounds sound){Hits++;}}
public class HeroController:UnityEngine.Component {public UnityEngine.GameObject spikeDeathPrefab,acidDeathPrefab,takeHitPrefab;}
public static class Pool {public static UnityEngine.GameObject Spawn(this UnityEngine.GameObject prefab,UnityEngine.Vector3 at)=>prefab.Factory(at);}
public class TransitionPoint {public string targetScene,entryPoint;public bool isADoor;}
public class GameManager {
 public class SceneLoadInfo {public string SceneName,EntryGateName;public bool PreventCameraFadeOut,WaitForSceneTransitionCameraFade;public float EntryDelay;}
 public int Starts;public SceneLoadInfo Received;
 public void BeginSceneTransition(SceneLoadInfo info){Starts++;Received=info;}
}
namespace On {public static class HeroController {public delegate System.Collections.IEnumerator orig_DieFromHazard(global::HeroController hero,GlobalEnums.HazardType type,float angle);}}
namespace KO.HollowKnight8 {
 internal class Vitals {internal int Health=5,Blue;}
 internal class CharmLoadout {internal bool[] Equipped=new bool[64];}
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal Vitals Vitals=new();internal CharmLoadout Charms=new();internal bool Down,Hazard;internal float HazardUntil;internal UnityEngine.Vector3 LastHazardPosition;}
 internal class Data {internal bool hasAcidArmour;internal int health;}
 internal partial class CoopSession {
  internal bool Active=true,TeamWipe;internal List<PlayerSlot> Players=new();internal Data Data=new();
  internal void Down(PlayerSlot p){p.Down=true;p.Vitals.Health=0;}
  internal PlayerSlot Resolve(HeroController hero)=>Players.FirstOrDefault(p=>p.Hero==hero);
 }
 internal class Runtime {internal CoopSession Session;internal PerformanceConfiguration Performance=new();}
 internal static class Plugin {internal static Runtime Self;}
 internal sealed class PlayerContext:IDisposable {
  internal static PlayerSlot Current;private PlayerSlot previous;private int health;
  internal static PlayerContext Enter(PlayerSlot p){var context=new PlayerContext{previous=Current,health=Plugin.Self.Session.Data.health};Current=p;Plugin.Self.Session.Data.health=p.Vitals.Health;return context;}
  public void Dispose(){Plugin.Self.Session.Data.health=health;Current=previous;}
 }
 internal static class Diagnostics {internal static List<string> Messages=new();internal static void Write(string text)=>Messages.Add(text);internal static void Throttled(string text,Exception error)=>Messages.Add(text+error.Message);}
 internal static class CrystalDashCoop {internal static void StopPlayer(PlayerSlot p){}}
 internal static class EmergencyWarp {internal static void Cancel(PlayerSlot p){}}
 internal static class BenchSeats {internal static void Leave(PlayerSlot p){}}
 internal static class Revival {internal static void End(PlayerSlot p){}}
 internal static class DreamSequence {internal static bool InStoryDream;}
 internal static class ScriptedParty {internal static bool Active;}
 internal static class CoopEnding {internal static bool Active;}
 internal static class ArenaGather {internal static bool TransferActive;}
 internal static class InteractionRouter {internal static object CompleteRoomEntry(PlayerSlot p)=>null;}
 internal static class Doorways {internal static UnityEngine.GameObject WaitSource(PlayerSlot p,GameManager.SceneLoadInfo info,object source)=>null;}
}

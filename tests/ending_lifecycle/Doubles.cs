using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Only Unity/PlayMaker IO and the cosmetic emitter are doubled. The complete
// production EndingAbsorption class is compiled and executed by this suite.
namespace UnityEngine {
 public class Object {public bool Exists=true;public string name;public static implicit operator bool(Object o)=>o!=null&&o.Exists;}
 public class Component:Object {
  public GameObject gameObject;public Transform transform=>gameObject.transform;
  public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();
  public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>gameObject.GetComponentsInChildren<T>();
 }
 public class GameObject:Object {
  public Transform transform;public readonly List<Component> Components=new();public readonly List<GameObject> Children=new();
  public GameObject(string name){this.name=name;transform=new Transform{gameObject=this,name=name};}
  public T Add<T>()where T:Component,new(){var c=new T{gameObject=this,name=name};Components.Add(c);return c;}
  public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault(c=>c);
  public T[] GetComponentsInChildren<T>()where T:Component=>Components.OfType<T>().Concat(Children.SelectMany(g=>g.GetComponentsInChildren<T>())).ToArray();
 }
 public class Transform:Component {public Vector3 position;}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}
  public static Vector3 up=>new(0,1);public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator *(Vector3 a,float n)=>new(a.x*n,a.y*n,a.z*n);
 }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new();}
 public struct Color {
  public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
  public static Color white=>new(1,1,1);public static Color Lerp(Color a,Color b,float t)=>new(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);
 }
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float n)=>Math.Clamp(n,0,1);}
 public static class Time {public static float time,deltaTime;}
 public class Rigidbody2D:Component {public bool simulated=true,isKinematic;public float gravityScale=2.4f,angularVelocity;public Vector2 velocity;}
 public class Material:Object {public bool Flash=true;public bool HasProperty(string name)=>Flash&&name=="_FlashAmount";}
 public class MaterialPropertyBlock {
  public readonly Dictionary<string,object> Values=new();public void SetFloat(string key,float n)=>Values[key]=n;public void SetColor(string key,Color n)=>Values[key]=n;
  public void CopyFrom(MaterialPropertyBlock other){Values.Clear();foreach(var p in other.Values)Values[p.Key]=p.Value;}
 }
 public class Renderer:Component {public Material sharedMaterial=new();public readonly MaterialPropertyBlock State=new();public void GetPropertyBlock(MaterialPropertyBlock target)=>target.CopyFrom(State);public void SetPropertyBlock(MaterialPropertyBlock value)=>State.CopyFrom(value);}
 public class SpriteRenderer:Renderer {public Color color=Color.white;}
}
namespace UnityEngine.SceneManagement {
 public struct Scene {public int handle;}
 public static class SceneManager {public static int Handle=1;public static Scene GetActiveScene()=>new(){handle=Handle};}
}
namespace HutongGames.PlayMaker {
 public class Fsm {public string Name;public GameObject GameObject;}
 public class FsmState {public string Name;public Fsm Fsm;}
}
public class PlayMakerFSM:Component {public HutongGames.PlayMaker.Fsm Fsm;}
public class tk2dBaseSprite:Component {public Color color=Color.white;}
public class tk2dSpriteAnimationClip {public string name;public object[] frames=new object[1];}
public class tk2dSpriteAnimator:Component {
 public Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip> AnimationCompleted;
 public Action<tk2dSpriteAnimator,tk2dSpriteAnimationClip,int> AnimationEventTriggered;
 public tk2dSpriteAnimationClip CurrentClip;public bool Playing;public int Plays;
 public readonly Dictionary<string,tk2dSpriteAnimationClip> Clips=new();
 public tk2dSpriteAnimationClip GetClipByName(string name)=>Clips.TryGetValue(name,out var clip)?clip:null;
 public void Play(tk2dSpriteAnimationClip clip){Plays++;CurrentClip=clip;Playing=true;}
}
public class HeroAnimationController:Component {public bool controlEnabled{get;private set;}=true;public void Seed(bool enabled)=>controlEnabled=enabled;}
public class HeroController:Component {public PlayMakerFSM spellControl;}
// No scene-loader, progression setter, damage or native-hero control APIs are
// exposed here: a cosmetic follower must never acquire those responsibilities.
public class GameManager:UnityEngine.Object {public static GameManager instance;public string sceneName;public bool IsLoadingSceneTransition,isPaused;}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal Renderer SkinRenderer;internal bool Present=true,Ready=true,Connected=true,Hazard,Down,Retiring;internal bool Alive{get=>Present&&Hero&&!Down&&!Hazard&&!Retiring;set=>Present=value;}}
 internal class CoopSession {internal bool Active=true,TeamWipe;internal readonly List<PlayerSlot> Players=new();internal PlayerSlot Primary=>Players.FirstOrDefault();}
 internal class Runtime:UnityEngine.Object {internal CoopSession Session;}
 internal static class Plugin {internal static Runtime Self;}
 internal static class Reflect {internal static void Set(object target,string field,object value)=>target.GetType().GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public).SetValue(target,value);}
 internal static class CoopEnding {internal static readonly HashSet<string> observed=new();}
 internal static class Diagnostics {internal static readonly List<string> Messages=new();internal static void Write(string text)=>Messages.Add(text);internal static void Throttled(string text,Exception ex)=>Messages.Add(text+": "+ex.Message);}
 internal sealed class EndingEssenceFx:IDisposable {
  internal static readonly List<EndingEssenceFx> Created=new();internal static int Cues,Climaxes,Resets,FailIndex=-1;
  internal int Ticks,Disposes;internal PlayerSlot Player;
  internal static EndingEssenceFx Create(PlayerSlot p){if(p.Index==FailIndex)throw new InvalidOperationException("injected emitter creation failure");var fx=new EndingEssenceFx{Player=p};Created.Add(fx);return fx;}
  internal static void Cue(HeroController primary,bool climax){Cues++;if(climax)Climaxes++;}
  internal void Tick(Vector3 from,Vector3 to,float elapsed,float duration)=>Ticks++;
  public void Dispose()=>Disposes++;
  internal static void Reset()=>Resets++;
 }
}

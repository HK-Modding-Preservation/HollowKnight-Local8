using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine {
 public class Object {public static T FindObjectOfType<T>()where T:class=>null;public static T Instantiate<T>(T original)=>original;public static T Instantiate<T>(T original,Vector3 at,Quaternion rotation)=>original;static int next;readonly int id=++next;public bool Exists=true;public string name {get;set;}="Object";public int GetInstanceID()=>id;public static implicit operator bool(Object o)=>o!=null&&o.Exists;public static bool operator ==(Object a,Object b)=>ReferenceEquals(a,b);public static bool operator !=(Object a,Object b)=>!ReferenceEquals(a,b);public override bool Equals(object o)=>ReferenceEquals(this,o);public override int GetHashCode()=>id;}
 public class Component:Object {public GameObject gameObject {get;set;}public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T GetComponentInParent<T>()where T:Component=>GetComponent<T>()??transform.parent?.GetComponentInParent<T>();public bool CompareTag(string tag)=>gameObject.CompareTag(tag);}
 public class Behaviour:Component {public bool enabled {get;set;}=true;}
 public class MonoBehaviour:Behaviour {public readonly List<Coroutine> Routines=new();public Coroutine StartCoroutine(IEnumerator r){var c=new Coroutine{Routine=r};Routines.Add(c);if(this is ScuttlerControl)c.Running=r.MoveNext();return c;}public void StopCoroutine(Coroutine c)=>(c.Routine as IDisposable)?.Dispose();}
 public class Coroutine {public IEnumerator Routine;public bool Running;}
 public class GameObject:Object {public string tag="Untagged";public bool activeInHierarchy {get;private set;}=true;public Transform transform {get;}public readonly List<Component> Components=new();public GameObject(){transform=new(){gameObject=this};}public T Add<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault(c=>c);public T GetComponentInParent<T>()where T:Component=>transform.GetComponentInParent<T>();public bool CompareTag(string t)=>tag==t;public void SetActive(bool on){bool before=activeInHierarchy;activeInHierarchy=on;if(before&&!on)GetComponent<SoulOrb>()?.OnDisable();}}
 public class Transform:Component {public Vector3 position {get;set;}public Vector3 localScale {get;set;}=new(1,1,1);public Quaternion rotation {get;set;}public Vector3 localEulerAngles {get;set;}public Transform parent;}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}public static float Distance(Vector3 a,Vector3 b)=>Vector2.Distance(a,b);public static Vector3 zero=>new();public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 operator *(Vector2 a,float b)=>new(a.x*b,a.y*b);public static Vector2 zero=>new();public float magnitude=>(float)Math.Sqrt(x*x+y*y);public static float Distance(Vector2 a,Vector2 b)=>new Vector2(a.x-b.x,a.y-b.y).magnitude;public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);}
 public class Rigidbody2D:Component {public bool isKinematic {get;set;}public Vector2 velocity {get;set;}}
 public class Collider2D:Behaviour {}
 public class Material {public Color LastColor;public void SetColor(string name,Color color)=>LastColor=color;}public class Renderer:Component {public Material material {get;}=new();public bool enabled {get;set;}=true;}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b){this.r=r;this.g=g;this.b=b;a=1;}public static Color white=>new(){r=1,g=1,b=1,a=1};}
 public struct Quaternion {public static Quaternion Euler(Vector3 p)=>new();}
 public class SpriteRenderer:Renderer {public Color color {get;set;}=Color.white;}
 public class TrailRenderer:Renderer {}
 public class AudioSource:Component {}
 public class ParticleSystem:Component {public int Plays;public void Play()=>Plays++;}
 public class WaitForSeconds {public float Seconds;public WaitForSeconds(float seconds)=>Seconds=seconds;}
 public static class Time {public static float unscaledTime=100;public static float time {get;set;}=100;public static float deltaTime=.1f;}
 public static class Random {public static bool Maximum;public static int Range(int a,int b)=>Maximum?b-1:a;public static float Range(float a,float b)=>a;}
 public static class Mathf {public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);public static float Atan2(float a,float b)=>(float)Math.Atan2(a,b);public static float Cos(float x)=>(float)Math.Cos(x);public static float Sin(float x)=>(float)Math.Sin(x);}
 public static class Debug {public static void LogError(object msg){} }
}
namespace HutongGames.PlayMaker {
 public class Fsm {public GameObject GameObject;public string Name;}
 public class FsmState {public Fsm Fsm;}
 public class FsmStateAction {public Fsm Fsm {get;set;}public bool Finished;public void Finish(){Finished=true;}}
 public class NamedVariable {public bool IsNone {get;set;}=true;}
 public class FsmGameObject {public GameObject Value {get;set;}}
 public class FsmObject {public UnityEngine.Object Value;}
 public class FsmVariables {public static FsmVariables GlobalVariables=new();public FsmGameObject[] GameObjectVariables=Array.Empty<FsmGameObject>();public FsmObject[] ObjectVariables=Array.Empty<FsmObject>();}
 public class FsmInt {public int Value {get;set;}}
 public class FsmFloat {public float Value {get;set;}}
 public class FsmVector3:NamedVariable {public Vector3 Value {get;set;}}
 public class FsmString:NamedVariable {public string Value {get;set;}}
 public class FsmEvent {public string Name;public FsmEvent(string name)=>Name=name;}
}
public static class Extensions {public static void SetRotation2D(Transform t,float angle){}public static void SetPositionZ(Transform t,float z){var p=t.position;p.z=z;t.position=p;}}
public class RandomAudioClipTable:UnityEngine.Object {}
public static class RandomAudioClipTableExtensions {public static void PlayOneShot(RandomAudioClipTable t,AudioSource a){} }
public static class ObjectPoolExtensions {public static Func<GameObject,Vector3,Quaternion,GameObject> Factory;public static GameObject Spawn(this GameObject prefab,Vector3 position,Quaternion rotation)=>Factory(prefab,position,rotation);public static GameObject Spawn(this GameObject prefab)=>Spawn(prefab,Vector3.zero,Quaternion.Euler(Vector3.zero));public static GameObject Spawn(this GameObject prefab,Vector3 at)=>Spawn(prefab,at,Quaternion.Euler(Vector3.zero));public static int Recycles;public static void Recycle(this GameObject go){Recycles++;go.SetActive(false);}}
public class SpriteFlash:Component {public int SoulFlashes;public void flashSoulGet()=>SoulFlashes++;}
public class HeroControllerStates {}
public class DirectionAction {public float Value;}
public class HeroActions {public DirectionAction left=new(),right=new(),up=new(),down=new();}
public class HeroController:MonoBehaviour {private static HeroController _instance;public static HeroController instance{get=>_instance;set=>_instance=value;}public HeroControllerStates cState=new();public Action OnSoul;public void AddMPCharge(int amount){OnSoul?.Invoke();KO.HollowKnight8.Plugin.Self.Session.Data.MPCharge+=amount;}}
public static class HeroBox {public static bool inactive;}
public class InputHandler:UnityEngine.Object {private HeroController heroCtrl;private HeroControllerStates cState;public HeroActions inputActions;public float inputX,inputY;public void NativeAssign(HeroController hero){heroCtrl=hero;cState=hero.cState;}}
public class PlayerData {public int MPCharge,healthBlue;public bool GetBool(string name)=>false;public void SetBool(string name,bool value){}public int GetInt(string name)=>0;public void SetInt(string name,int value){}}
public class GameManager:UnityEngine.Object {public static GameManager instance {get;set;}=new();public PlayerData playerData;public HeroController hero_ctrl{get;private set;}public InputHandler inputHandler;internal void NativeAssign(KO.HollowKnight8.PlayerSlot player){hero_ctrl=player.Hero;inputHandler=new(){inputActions=player.Actions};inputHandler.NativeAssign(player.Hero);}public delegate void UnloadLevel();public event UnloadLevel UnloadingLevel;public void Unload()=>UnloadingLevel?.Invoke();}
public class HealthManager:MonoBehaviour {public int Enables;public void OnEnable()=>On.HealthManager.Run_OnEnable(this);public void Native_OnEnable()=>Enables++;}
public class SoulOrb:MonoBehaviour {
 public RandomAudioClipTable soulOrbCollectSounds;public ParticleSystem getParticles;public bool awardSoul=true,dontRecycle;public Transform target;public float speed,acceleration;public SpriteRenderer sprite;public TrailRenderer trail;public Rigidbody2D body;public AudioSource source;public Coroutine zoomRoutine;public float stretchFactor=2,stretchMinY=1,stretchMaxX=2;public float scaleModifier,scaleModifierMin=1,scaleModifierMax=2;
 public void Update()=>Native_Update();public void Native_Update()=>throw new NotImplementedException();public void FaceAngle()=>Native_FaceAngle();public void Native_FaceAngle()=>throw new NotImplementedException();public void ProjectileSquash()=>Native_ProjectileSquash();public void Native_ProjectileSquash()=>throw new NotImplementedException();
 public void Awake()=>Native_Awake();public void Native_Awake()=>throw new NotImplementedException();
 public void Start()=>On.SoulOrb.Run_Start(this);public void Native_Start()=>throw new NotImplementedException();
 public void OnEnable()=>On.SoulOrb.Run_OnEnable(this);public void Native_OnEnable()=>throw new NotImplementedException();
 public void OnDisable()=>On.SoulOrb.Run_OnDisable(this);public void Native_OnDisable()=>throw new NotImplementedException();
 public IEnumerator Zoom(bool zoom=true)=>On.SoulOrb.Run_Zoom(this,zoom);public IEnumerator Native_Zoom(bool zoom)=>throw new NotImplementedException();
 public void FireAtTarget()=>Native_FireAtTarget();public void Native_FireAtTarget()=>throw new NotImplementedException();
 public void SceneLoading(){}
}
namespace KO.HollowKnight8 {
 internal class Vitals {internal int Health=5,Blue,Soul;}
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal HeroActions Actions=new();internal bool HeroBoxInactive;internal Vitals Vitals=new();internal bool Ready=true,Connected=true,Down,Hazard,Retiring,SpawnPending,ArenaTransfer,Warp,Holding;internal float HealFlashUntil;internal bool Alive=>Hero&&Hero.gameObject.activeInHierarchy&&!Down&&!Hazard&&!Retiring;internal void Capture(PlayerData d){Vitals.Soul=d.MPCharge;Vitals.Blue=d.healthBlue;}internal void Apply(PlayerData d){d.MPCharge=Vitals.Soul;d.healthBlue=Vitals.Blue;}}
 internal class CoopSession {internal bool Active=true;internal readonly List<PlayerSlot> Players=new();internal PlayerData Data=new();internal PlayerSlot Primary=>Players.FirstOrDefault();internal PlayerSlot Resolve(Component c)=>c?Players.FirstOrDefault(p=>p.Hero==c.GetComponentInParent<HeroController>()):null;internal PlayerSlot Nearest(Vector3 at)=>Players.Where(p=>p.Alive).OrderBy(p=>Vector3.Distance(p.Hero.transform.position,at)).FirstOrDefault();internal GameObject Remap(GameObject go,PlayerSlot from,PlayerSlot to)=>go==from.Hero.gameObject?to.Hero.gameObject:go;}
 internal class Runtime:UnityEngine.Object {internal CoopSession Session;}
 internal static class Plugin {internal static Runtime Self;}
 internal static class WorldContactRouting {internal static bool Eligible(PlayerSlot p)=>p!=null&&p.Alive&&p.Ready&&!p.SpawnPending&&!p.ArenaTransfer&&(p.Index==0||p.Connected)&&!p.Warp;}
 internal static class TransitionVote {internal static bool Holding(PlayerSlot p)=>p.Holding;}
 internal static class Diagnostics {internal static List<string> Messages=new();internal static void Write(string text)=>Messages.Add(text);}
 internal static class Hooks {internal static UnityEngine.Object Mapped(Component c,PlayerSlot p)=>p.Hero;}
}

public class PlayMakerFSM:MonoBehaviour {public void SendEvent(string value){}}
public static class FSMUtility {public static void SetFloat(PlayMakerFSM f,string name,float value){}public static void SetBool(PlayMakerFSM f,string name,bool value){}public static PlayMakerFSM LocateFSM(GameObject go,string name)=>go.GetComponent<PlayMakerFSM>();}
namespace HutongGames.PlayMaker.Actions {public class RigidBody2dActionBase:FsmStateAction {protected Rigidbody2D rb2d;protected void CacheRigidBody2d(GameObject go){rb2d=go.GetComponent<Rigidbody2D>();}}
public class FlingObjectsFromGlobalPool:RigidBody2dActionBase {public FsmGameObject gameObject=new(),spawnPoint=new();public FsmVector3 position=new();public FsmInt spawnMin=new(){Value=8},spawnMax=new(){Value=9};public FsmFloat speedMin=new(){Value=10},speedMax=new(){Value=20},angleMin=new(),angleMax=new(){Value=360},originVariationX=new(),originVariationY=new();public FsmString FSM=new(),FSMEvent=new();private float vectorX,vectorY;private bool originAdjusted;public void OnEnter()=>On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.Run_OnEnter(this);public void Native_OnEnter()=>throw new NotImplementedException();}}

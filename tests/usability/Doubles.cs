using Object=UnityEngine.Object;using System;using System.Collections;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine {
 public class Object {public bool Exists=true;public static List<Object> All=new();public static int Queries;public static implicit operator bool(Object o)=>o!=null&&o.Exists;public static T[] FindObjectsOfType<T>()where T:Object {Queries++;return All.OfType<T>().Where(x=>x&&(!(x is Component c)||c.gameObject.activeInHierarchy)).ToArray();}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;}
 public class GameObject:Object {public string name;public bool activeInHierarchy=true;public readonly Transform transform;public int Recycled;public GameObject(string name=""){this.name=name;transform=new(){gameObject=this};}public T Add<T>()where T:Component,new(){var c=new T{gameObject=this};All.Add(c);return c;}}
 public class Transform:Component {public Vector3 position;}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new();public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);public override string ToString()=>$"({x},{y},{z})";}
 public struct Bounds {public Vector3 center,size;}
 public class Rigidbody2D:Component{}
 public static class Time {public static int frameCount;public static float time,unscaledTime;}
 public static class Application {public static bool Available=true;public static bool CanStreamedLevelBeLoaded(string s)=>Available;}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static int CeilToInt(float f)=>(int)Math.Ceiling(f);}
 public enum KeyCode {None,Escape,F1,F2,F3,F4,F5,F6,F7,F8,F10,F11,F12,Insert,Home,Tab,Alpha1,Alpha2,Mouse0,Mouse1,Mouse2,Mouse3,Mouse4,Mouse5,Mouse6,JoystickButton0=330,JoystickButton7=337,JoystickButton19=349}
}
namespace UnityEngine.SceneManagement {public struct Scene {public string name;}public static class SceneManager {public static Scene GetActiveScene()=>new(){name="Town"};}}
namespace UnityEngine.EventSystems {public class BaseInputModule:Component {public bool enabled=true;}}
namespace InControl {public class PlayerAction {public bool WasPressed;public List<int> Bindings=new();}public class PlayerTwoAxisAction{}public class InControlInputModule:UnityEngine.EventSystems.BaseInputModule {public PlayerTwoAxisAction MoveAction;public PlayerAction SubmitAction,CancelAction;}public class HollowKnightInputModule:UnityEngine.EventSystems.BaseInputModule {public PlayerTwoAxisAction MoveAction;public PlayerAction SubmitAction,CancelAction,JumpAction,AttackAction,CastAction;}}
namespace GlobalEnums {public enum UIState {PLAYING,PAUSED,DIALOGUE}public enum GameState {PLAYING,PAUSED}}
public class HeroActions {public InControl.PlayerTwoAxisAction moveVector=new();public InControl.PlayerAction menuSubmit=new(),menuCancel=new(),jump=new(),attack=new(),cast=new(),pause=new(),openInventory=new(),paneLeft=new(),paneRight=new();}
public class HeroControllerStates {public bool transitioning,nearBench,onGround,facingRight,invulnerable;}
public class HeroController:Component {public HeroControllerStates cState=new();public bool IsDreamReturning,controlReqlinquished;public void FaceRight()=>cState.facingRight=true;public void FaceLeft()=>cState.facingRight=false;}
public class PlayerData {public bool disablePause,atBench,isInvincible;public int geo=777;public List<int> equippedCharms=new(){1,6};}
public class InputHandler:Object {public bool acceptingInput=true,pauseAllowed=true;public HeroActions inputActions;public HeroController Attached;public void AttachHeroController(HeroController h)=>Attached=h;public void StartCoroutine(IEnumerator e){while(e.MoveNext()){}}}
public class GameManager:Object {public static GameManager instance;public InputHandler inputHandler=new();public bool isPaused;public GlobalEnums.GameState gameState=GlobalEnums.GameState.PLAYING;public bool HasFinishedEnteringScene=true;public int Saved,Toggled;public bool IsGameplayScene()=>true;public void SaveGame()=>Saved++;public IEnumerator PauseGameToggle(){Toggled++;isPaused=!isPaused;yield break;}}
public class UIManager:Object {public static UIManager instance;public GlobalEnums.UIState uiState=GlobalEnums.UIState.PLAYING;}
public static class BossSequenceController {public static bool IsInSequence;}
public class BossSceneController:Object {public static BossSceneController Instance;}
public class JumpEffects:Component {private GameObject heroObject;private Rigidbody2D heroRigidBody;public int Updated;public void Bind(HeroController h,Rigidbody2D body){heroObject=h?.gameObject;heroRigidBody=body;}public void InvalidateBody(){heroRigidBody.Exists=false;}public void NativeUpdate(){if(!heroObject||!heroRigidBody)throw new NullReferenceException("native stale JumpEffects");Updated++;}}
public static class PoolExtensions {public static void Recycle(this GameObject g){g.activeInHierarchy=false;g.Recycled++;}}
namespace On {
 public static class GameManager {public delegate IEnumerator orig_PauseGameToggle(global::GameManager gm);}
 public static class InputHandler {public delegate void orig_Update(global::InputHandler input);}
 public static class JumpEffects {public delegate void orig_GetHero(global::JumpEffects fx);public delegate void hook_GetHero(orig_GetHero orig,global::JumpEffects fx);public delegate void orig_Update(global::JumpEffects fx);public delegate void hook_Update(orig_Update orig,global::JumpEffects fx);public static event hook_GetHero GetHero;public static event hook_Update Update;public static int CaptureCount=>GetHero?.GetInvocationList().Length??0;public static int UpdateCount=>Update?.GetInvocationList().Length??0;public static void Capture(global::JumpEffects fx){if(GetHero!=null)GetHero(_=>{},fx);}public static void Tick(global::JumpEffects fx){if(Update!=null)Update(f=>f.NativeUpdate(),fx);else fx.NativeUpdate();}}
}
namespace KO.HollowKnight8 {
 internal class Vitals {internal int Health=5,Blue=2,Soul=63,Reserve=11,MaxSoul=99,MaxHealth=5,Joni;internal bool AtBench,DisablePause,Invincible;}
 internal class PlayerSlot {internal int Index,Team;internal HeroController Hero;internal HeroActions Actions=new();internal Vitals Vitals=new();internal bool Connected=true,Ready=true,SpawnPending,ArenaTransfer,Retiring,Down,Hazard,Reviving,InputBlocked;internal bool Alive=>Hero&&!Down&&!Hazard&&!Retiring&&Hero.gameObject.activeInHierarchy;internal int CurrentMaxHealth=>Vitals.MaxHealth;internal bool HasSafePoint=true,HasPreviousSafePoint=true;internal Vector3 SafePoint,PreviousSafePoint;internal float ProtectionUntil,LifeStartedAt=123,FarSince=-1;}
 internal class CoopSession {internal List<PlayerSlot> Players=new();internal bool Active=true,Gameplay=true;internal PlayerData Data=new();internal PlayerSlot Primary=>Players.FirstOrDefault();internal HashSet<float> InvalidFloors=new();internal bool FloorAvailable=true;internal int Recoveries,Commits;internal bool IsSafe(Vector3 at,PlayerSlot p)=>FloorAvailable&&at.y==0&&!InvalidFloors.Contains(at.x);internal bool FindSafePosition(Vector3 from,PlayerSlot p,out Vector3 at){at=new(from.x+1,0);return IsSafe(at,p);}internal void Recover(PlayerSlot p,bool respawn,Vector3 at){if(!IsSafe(at,p))throw new Exception("unsafe test placement");Recoveries++;p.Hero.transform.position=at;p.Hero.controlReqlinquished=false;p.Vitals.AtBench=false;p.Down=p.Hazard=false;}internal void Commit(PlayerSlot p)=>Commits++;internal PlayerSlot Resolve(GameObject g)=>Players.Find(p=>p.Hero&&p.Hero.gameObject==g);}
 internal class Runtime:Object {internal CoopSession Session;internal bool Panel;internal string LastNotice;internal int Exports;internal void SetPanel(bool open)=>Panel=open;internal void Notice(string s)=>LastNotice=s;internal void ExportSettings()=>Exports++;}
 internal static class Plugin {internal static Runtime Self;}
 internal class Settings {internal string MenuKey="F8",PvpKey="F6",RescueKey="Alpha1";internal int PvpMode=2,DuelBestOf=3,DuelSoul=99;}
 internal static class Local8Mod {internal static Settings Settings=new();}
 internal static class UiLocalization {internal static string Get(string k)=>k;}
 internal static class Diagnostics {internal static List<string> Messages=new();internal static void Write(string s)=>Messages.Add(s);internal static void Throttled(string s,Exception ex)=>Messages.Add(s+":"+ex.GetType().Name);internal static void ControlSnapshot(CoopSession s,string reason){} }
 internal static class DuelGround {internal static Bounds Body(PlayerSlot p)=>new(){center=p.Hero.transform.position,size=new(1,2)};internal static bool Safe(PlayerSlot p,Vector3 at,Vector3 offset,Vector3 size,out string why){why="";return Plugin.Self.Session.IsSafe(at,p);}}
 internal sealed class PlayerContext:IDisposable {internal static PlayerSlot Current;PlayerSlot before;internal static PlayerContext Enter(PlayerSlot p){var c=new PlayerContext{before=Current};Current=p;return c;}public void Dispose()=>Current=before;}
 internal static class BenchSeats {internal static HashSet<PlayerSlot> Sitting=new();internal static int Leaves;internal static bool Seated(PlayerSlot p)=>Sitting.Contains(p);internal static void Leave(PlayerSlot p){Sitting.Remove(p);Leaves++;}}
 internal static class InteractionRouter {internal static PlayerSlot ActivePlayer;internal static bool ActiveIsBench;internal static int Closed;internal static void CloseForTransition(){Closed++;ActivePlayer=null;ActiveIsBench=false;}}
 internal static class ActorRecovery {internal static int Resets;internal static void Reset(PlayerSlot p){Resets++;p.Hero.controlReqlinquished=false;p.Hero.cState.nearBench=false;}}
 internal static class ShopMenuRouting {internal static PlayerSlot Buyer;internal static bool MenuVisible;}
 internal static class StagMenuRouting {internal static bool HasOwner;}
 internal static class PickupCard {internal static PlayerSlot Owner;}
 internal static class Charms {internal static bool NativeMenuOpen;}
 internal static class RoleSystem {internal static bool JoiningMenu;}
 internal static class ScriptedParty {internal static bool Active;}
 internal static class CoopEnding {internal static bool Active;}
 internal static class ArenaGather {internal static bool EngagedBossAlive;}
 internal static class EmergencyWarp {internal static HashSet<PlayerSlot> Warping=new();internal static bool Active(PlayerSlot p)=>p!=null&&Warping.Contains(p);internal static bool Holding(PlayerSlot p)=>false;internal static void CancelAll(){} }
 internal static class TransitionVote {internal static bool Pending;internal static void Reset()=>Pending=false;}
 internal static class CoopShades {internal static void Reset(){} }
 internal static class PvpCharms {internal static int Began,Ended;internal static void Begin(CoopSession s)=>Began++;internal static void End()=>Ended++;internal static void NormalRound(CoopSession s){} }
 internal static class PvpSpellOnly {internal static string Issue="";internal static string StartIssue(CoopSession s)=>Issue;internal static void Begin(CoopSession s){}internal static void End(){}internal static void Round(){}internal static void ArenaRules(PvpArenaOptions o,CoopSession s){} }
 internal static class PvpCombat {internal static CoopSession Session=>Plugin.Self?.Session;internal static int Team(PlayerSlot p)=>p.Team;internal static void Invalidate(){} }
 internal static class Revival {internal static void End(PlayerSlot p){} }
 internal sealed class PvpArenaOptions {internal int Arena=1;internal PvpArenaOptions Copy()=>(PvpArenaOptions)MemberwiseClone();}
 internal static class PvpArena {internal static PvpArenaOptions Options=new();internal static int Travels;internal static bool StopRequest(bool restore)=>false;}
 internal static class PvpArenaCatalog {internal static string[] Scenes={"","GG_Test"};}
 internal static class CharmNativeUi {internal static HeroActions view;}
 internal static class ActionKeys {internal static int Capturing;internal static KeyCode Pvp=>KeyCode.F6;internal static KeyCode Rescue=>KeyCode.Alpha1;}
 internal static class KeypadInput {internal static HashSet<KeyCode> Down=new();internal static bool Pressed(KeyCode k)=>Down.Contains(k);}
}

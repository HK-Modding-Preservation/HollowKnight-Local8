using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
namespace UnityEngine {
 public class Object {public string name;public static implicit operator bool(Object o)=>o!=null;}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Vector3 {public float x,y,z;}
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;}}
 public static class Time {public static float unscaledTime,unscaledDeltaTime=.02f;}
 public static class Mathf {public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));public static int CeilToInt(float x)=>(int)Math.Ceiling(x);}
 public static class ColorUtility {public static string ToHtmlStringRGB(Color c)=>$"{(int)(c.r*255):X2}{(int)(c.g*255):X2}{(int)(c.b*255):X2}";public static bool TryParseHtmlString(string s,out Color c){c=new Color();try {c=new Color(Convert.ToInt32(s.Substring(1,2),16)/255f,Convert.ToInt32(s.Substring(3,2),16)/255f,Convert.ToInt32(s.Substring(5,2),16)/255f);return true;}catch{return false;}}}
}
namespace UnityEngine.Audio {public class AudioMixerSnapshot:UnityEngine.Object{}}
namespace Modding {public class Mod {public static string Json;public static bool Fail;public static int Writes;protected void SaveGlobalSettings(){if(Fail)throw new Exception("disk");Writes++;Json=JsonSerializer.Serialize(KO.HollowKnight8.Local8Mod.Settings,new JsonSerializerOptions{IncludeFields=true});}}}
namespace InControl {
 public class InputDevice {public static InputDevice Null=new InputDevice();}
 public class BindingSource {public string Name;}
 public class KeyBindingSource:BindingSource {public KeyBindingSource(string s){Name=s;}}
 public class PlayerAction {public List<BindingSource> Bindings=new List<BindingSource>();}
}
public class HeroActions {
 public static int Created,Destroyed;public List<InControl.PlayerAction> Actions=new List<InControl.PlayerAction>();public string Saved;
 public HeroActions(){Created++;}
 public void Load(string value){if(value=="invalid")throw new Exception("invalid keys");Saved=value;Actions.Clear();foreach(string key in value.Split(','))Actions.Add(new InControl.PlayerAction{Bindings=new List<InControl.BindingSource>{new InControl.KeyBindingSource(key)}});}
 public string Save()=>Saved;public void Destroy(){Destroyed++;}public void ClearInputState(){}
}
public class HeroController:UnityEngine.Object {
 public class State {public bool transitioning;}public State cState=new State();public PlayMakerFSM damageEffectFSM,proxyFSM;public bool ThrowCharm;public int CharmCalls;
 public void CharmUpdate(){CharmCalls++;if(ThrowCharm){ThrowCharm=false;throw new Exception("charm provider");}var d=KO.HollowKnight8.Plugin.Self.Session.Data;d.health=99;d.healthBlue=99;}
}
public partial class PlayerData {
 public int health=5,healthBlue,MPCharge=99,MPReserve,maxMP=99,prevHealth,blockerHits,maxHealth=5,joniHealthBlue,focusMP_amount,geo=100,charmSlots=3,charmSlotsFilled;
 public bool isInvincible,soulLimited,damagedBlue,atBench,overcharmed,canOvercharm=true;public UnityEngine.Vector3 hazardRespawnLocation;public bool hazardRespawnFacingRight;
 public List<int> equippedCharms=new List<int>();public HashSet<int> Owned=new HashSet<int>{1,2,3};public Dictionary<int,int> Costs=new Dictionary<int,int>{{1,1},{2,2},{3,3}};
 public int GetInt(string name)=>Costs.TryGetValue(int.Parse(name.Substring("charmCost_".Length)),out int n)?n:1;
}
public enum AttackTypes{Nail,Spell}
public struct HitInstance {public object Source;public int DamageDealt;}
public class HealthManager:UnityEngine.Object {public int hp=20;public bool Dead,Shade;public bool GetIsDead()=>Dead;public T GetComponent<T>()where T:class=>Shade?new KO.HollowKnight8.LocalShade() as T:null;}
public class AudioManager{}
public class GameManager:UnityEngine.Object {public static GameManager instance=new GameManager();public bool isPaused,IsLoadingSceneTransition;public UnityEngine.Audio.AudioMixerSnapshot actorSnapshotUnpaused,actorSnapshotPaused;}
public class PlayMakerFSM:UnityEngine.Object {public HutongGames.PlayMaker.Fsm Fsm=new HutongGames.PlayMaker.Fsm();}
namespace HutongGames.PlayMaker {public class Fsm {public string Name="Damage",ActiveStateName="Damage";}public class FsmObject{public object Value;}}
namespace HutongGames.PlayMaker.Actions {public class TransitionToAudioSnapshot {public HutongGames.PlayMaker.FsmObject snapshot=new HutongGames.PlayMaker.FsmObject();public HutongGames.PlayMaker.Fsm Fsm;public bool Finished;public void Finish(){Finished=true;}}}
namespace On {
 public static class HealthManager {
  public delegate void orig_Hit(global::HealthManager e,HitInstance h);public delegate void hook_Hit(orig_Hit o,global::HealthManager e,HitInstance h);public static event hook_Hit Hit;
  public delegate void orig_ApplyExtraDamage(global::HealthManager e,int n);public delegate void hook_ApplyExtraDamage(orig_ApplyExtraDamage o,global::HealthManager e,int n);public static event hook_ApplyExtraDamage ApplyExtraDamage;
  public delegate void orig_Die(global::HealthManager e,float? d,AttackTypes t,bool v);public delegate void hook_Die(orig_Die o,global::HealthManager e,float? d,AttackTypes t,bool v);public static event hook_Die Die;
  public static void Strike(global::HealthManager e,HitInstance h,orig_Hit native=null){Hit(native??((e,h)=>{e.hp=Math.Max(-50,e.hp-h.DamageDealt);if(e.hp<=0)Kill(e);}),e,h);}
  public static void Extra(global::HealthManager e,int n){ApplyExtraDamage((e,n)=>{e.hp=Math.Max(0,e.hp-n);if(e.hp<=0)Kill(e);},e,n);}
  public static void Kill(global::HealthManager e){Die((e,d,t,v)=>e.Dead=true,e,null,AttackTypes.Nail,false);}
 }
 public static class HeroController {public delegate void orig_AddHealth(global::HeroController h,int n);public delegate void hook_AddHealth(orig_AddHealth o,global::HeroController h,int n);public static event hook_AddHealth AddHealth;public static void Heal(global::HeroController h,int n){AddHealth((h,n)=>On.PlayerData.Heal(KO.HollowKnight8.Plugin.Self.Session.Data,n),h,n);}}
 public static class PlayerData {public delegate void orig_AddHealth(global::PlayerData d,int n);public delegate void hook_AddHealth(orig_AddHealth o,global::PlayerData d,int n);public static event hook_AddHealth AddHealth;public static void Heal(global::PlayerData d,int n){AddHealth((d,n)=>d.health=Math.Min(d.maxHealth,d.health+n),d,n);}}
 public static class AudioManager {public delegate void orig_ApplyMusicSnapshot(global::AudioManager a,UnityEngine.Audio.AudioMixerSnapshot s,float d,float t);public delegate void hook_ApplyMusicSnapshot(orig_ApplyMusicSnapshot o,global::AudioManager a,UnityEngine.Audio.AudioMixerSnapshot s,float d,float t);public static event hook_ApplyMusicSnapshot ApplyMusicSnapshot;public static int Calls;public static void Mix(UnityEngine.Audio.AudioMixerSnapshot s){ApplyMusicSnapshot((a,s,d,t)=>Calls++,new global::AudioManager(),s,0,0);}}
}
namespace On.HutongGames.PlayMaker.Actions {public static class TransitionToAudioSnapshot {public delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot a);public delegate void hook_OnEnter(orig_OnEnter o,global::HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot a);public static event hook_OnEnter OnEnter;public static int Calls;public static void Enter(global::HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot a){OnEnter(a=>{Calls++;a.Finish();},a);}}}
namespace KO.HollowKnight8 {
 internal class Setting<T>{internal T Value;internal Setting(T v){Value=v;}}
 public sealed class Local8Settings {public PlayerProfileData[] PlayerProfiles=Array.Empty<PlayerProfileData>();public string[] Colors=new string[8],SkinIds=new string[8],ExtraKeyboardBindings=new string[8];}
 internal class LocalSave {internal PlayerAdventureStatistics[] AdventureStats;internal string[] RoleIds=new string[8];}
 internal class Local8Mod:Modding.Mod {internal static Local8Mod Instance=new Local8Mod();internal static Local8Settings Settings=new Local8Settings();internal static LocalSave Save=new LocalSave();}
 internal class PlayerSlot {internal int Index;internal string ProfileId,SkinId;internal PlayerAdventureStatistics Adventure;internal bool ProfileLoadedThisJoin,Ready=true,Down,Hazard,SpawnPending,ArenaTransfer,InputBlocked,Connected=true;internal bool Alive=>!Down&&Vitals.Health>0;internal float LifeStartedAt;internal HeroController Hero=new HeroController();internal HeroActions Actions=new HeroActions();internal InControl.InputDevice Device=InControl.InputDevice.Null;internal UnityEngine.Color Color=new UnityEngine.Color(.2f,.4f,.8f);internal Vitals Vitals=new Vitals{Health=5,MaxHealth=5,Soul=99,MaxSoul=99};internal CharmLoadout Charms=new CharmLoadout();internal int CurrentMaxHealth=>Vitals.MaxHealth;internal void Capture(PlayerData d){Vitals.Read(d);Charms.Read(d);}internal void Apply(PlayerData d){Vitals.Write(d);Charms.Write(d);}}
 internal class CoopSession {internal List<PlayerSlot> Players=new List<PlayerSlot>();internal PlayerData Data=new PlayerData();internal bool TeamWipe,Gameplay=true;internal PlayerSlot Primary=>Players[0];internal PlayerSlot Resolve(object o)=>Players.FirstOrDefault(p=>ReferenceEquals(p,o)||ReferenceEquals(p.Hero,o));}
 internal class Local8Runtime:UnityEngine.Object {internal CoopSession Session=new CoopSession();internal Setting<bool> Enabled=new Setting<bool>(true);internal bool Panel;internal string Message;internal void Notice(string s){Message=s;}}
 internal static class Plugin{internal static Local8Runtime Self;}
 internal static class PlayerContext {internal static PlayerSlot Current;internal sealed class Scope:IDisposable {private PlayerSlot old;private Vitals data;private CharmLoadout charms=new CharmLoadout();internal Scope(PlayerSlot p){old=Current;data=new Vitals();data.Read(Plugin.Self.Session.Data);charms.Read(Plugin.Self.Session.Data);Current=p;p.Apply(Plugin.Self.Session.Data);}public void Dispose(){Current.Capture(Plugin.Self.Session.Data);data.Write(Plugin.Self.Session.Data);charms.Write(Plugin.Self.Session.Data);Current=old;}}internal static IDisposable Enter(PlayerSlot p)=>new Scope(p);}
 internal static class PvpCharms{internal static bool Active;}internal static class PvpArena{internal static bool Active;}internal static class PvpMatch{internal static bool Running;}internal static class ScriptedParty{internal static bool Active;}internal static class CoopEnding{internal static bool Active;}
 internal static class PickupCard{internal static PlayerSlot Owner;}internal static class InteractionRouter{internal static PlayerSlot ActivePlayer;}internal static class ShopMenuRouting{internal static bool MenuVisible;}internal static class StagMenuRouting{internal static bool HasOwner;}
 internal static class Controls{internal static bool IsKeyboard(InControl.InputDevice d)=>d==InControl.InputDevice.Null;}
 internal static class Charms {internal static bool Bench;internal static bool CanEdit(PlayerSlot p)=>Bench;internal static void UpdateMaximum(PlayerSlot p,PlayerData d,bool refill){p.Vitals.MaxHealth=5;d.maxHealth=5;}internal static void Save(CoopSession s){PlayerProfiles.Capture(s);PlayerProfiles.Dirty();}}
 internal static class SkinBridge{internal static void Release(PlayerSlot p){}}
 internal static class SummonRouting{internal static void Refresh(PlayerSlot p){}}
 internal static class KeypadInput{internal static int Repairs;internal static void RepairMenus(PlayerSlot p){Repairs++;}}
 internal static class RoleCatalog{internal static bool Known(string id)=>id=="none"||id=="mage"||id=="healer";}
 internal static class RoleSystem{internal static void ApplyProfile(PlayerSlot p,string id){Local8Mod.Save.RoleIds[p.Index]=id;}}
 internal static class UiLocalization{internal static string Get(string key)=>key;}
 internal static class MenuTheme {
  internal static readonly UnityEngine.Color Muted=new UnityEngine.Color(.5f,.5f,.5f),Paper=new UnityEngine.Color(1,1,1);
  internal sealed class Paint {internal UnityEngine.Rect Rect;internal string Text;}
  internal static readonly List<Paint> Paints=new List<Paint>();
  internal static void Section(string value,float width,ref float row){row+=48;}
  internal static void Text(UnityEngine.Rect r,string text,UnityEngine.Color color,int size){Paints.Add(new Paint{Rect=r,Text=text});}
  internal static void Fill(UnityEngine.Rect r,UnityEngine.Color color){}internal static void Border(UnityEngine.Rect r,UnityEngine.Color color){}
 }
 internal static class Diagnostics {internal static void Throttled(string key,Exception e){}internal static void Write(string message){}}
 internal static class CharmExtensions {internal sealed class Entry{internal int Id;}internal static Entry[] Entries=Array.Empty<Entry>();internal static void Discover(){}internal static void Register(int id){}internal static bool Contains(int id)=>id>0&&id<=40;internal static bool Owned(PlayerData d,int id)=>d.Owned.Contains(id);internal static bool Equipped(PlayerData d,Entry e)=>d.equippedCharms.Contains(e.Id);internal static void Equip(PlayerData d,Entry e,bool on){}}
 internal static class CombatEffects{internal static bool Enabled=true;}
 internal class LocalShade:UnityEngine.Object{}
}

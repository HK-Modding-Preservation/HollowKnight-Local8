using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public class Object {public string name="";public static implicit operator bool(Object x)=>x!=null;}
 public class Transform:Object {public Transform parent;}
 public class Component:Object {public Transform transform=new Transform();}
 public class Collider2D:Component {}
 public static class Time {public static float deltaTime;}
 public static class Application {public static bool isFocused=true;}
 public static class Mathf {public static int Clamp(int n,int a,int b)=>Math.Max(a,Math.Min(b,n));public static float Clamp(float n,float a,float b)=>Math.Max(a,Math.Min(b,n));}
}
namespace InControl {
 public class InputDevice {public static readonly InputDevice Null=new InputDevice();public bool IsAttached=true;}
 public class Action {public bool Pressed;public int Clears;public void ClearInputState(){Pressed=false;Clears++;}}
}
public class HeroController:Component {public bool controlReqlinquished;public State cState=new State();public sealed class State{public bool transitioning;}}
public class GameManager:UnityEngine.Object {public static GameManager instance=new GameManager();public bool isPaused,IsLoadingSceneTransition;}
public class PlayerData {public int fireballLevel=1,quakeLevel,screamLevel,MPCharge,MPReserve;}
namespace KO.HollowKnight8 {
 internal class Actions {internal InControl.Action attack=new InControl.Action(),dreamNail=new InControl.Action(),superDash=new InControl.Action(),cast=new InControl.Action(),jump=new InControl.Action(),dash=new InControl.Action(),focus=new InControl.Action(),pause=new InControl.Action();internal void ClearInputState(){foreach(var a in new[]{attack,dreamNail,superDash,cast,jump,dash,focus,pause})a.ClearInputState();}}
 internal class Vitals {internal int Soul,Reserve,MaxSoul=99;internal bool AtBench;}
 internal class PlayerSlot {internal int Index;internal bool Alive=true,Ready=true,Connected=true,Hazard,ArenaTransfer,SpawnPending,InputBlocked;internal HeroController Hero=new HeroController();internal Actions Actions=new Actions();internal Vitals Vitals=new Vitals();internal InControl.InputDevice Device=InControl.InputDevice.Null;internal void Apply(PlayerData d){d.MPCharge=Vitals.Soul;d.MPReserve=Vitals.Reserve;}}
 internal partial class CoopSession {internal List<PlayerSlot> Players=new List<PlayerSlot>();internal PlayerSlot Primary;internal bool Active=true,Gameplay=true,TeamWipe;internal PlayerData Data=new PlayerData();}
 internal class Setting<T> {internal T Value;internal Setting(T v){Value=v;}}
 internal class Local8Runtime {internal CoopSession Session;internal bool Panel;internal Setting<bool> Enabled=new Setting<bool>(true);}
 internal static class Plugin {internal static Local8Runtime Self;}
 internal static class Local8Mod {internal static Local8Settings Settings=new Local8Settings();}
 internal static class PvpMatch {internal static bool Running,CanFight;}
 internal static class PvpCombat {internal sealed class Attack {internal PvpKind Kind;internal Collider2D Collider;internal PlayerSlot Owner;}}
 internal static class UiLocalization {internal static string Get(string key)=>key;}
 internal static class PlayerContext {internal static PlayerSlot Current;}
 internal static class Charms {internal static bool NativeMenuOpen;}
 internal static class RoleSystem {internal static bool JoiningMenu;}
 internal static class ShopMenuRouting {internal static bool MenuVisible;}
 internal static class StagMenuRouting {internal static bool HasOwner;}
}

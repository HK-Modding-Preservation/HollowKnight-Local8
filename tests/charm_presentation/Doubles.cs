using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public class Object { public bool Destroyed;public string name="";public static implicit operator bool(Object x)=>x!=null&&!x.Destroyed;public static void Destroy(Object x){if(x!=null)x.Destroyed=true;} }
 public class Texture2D:Object { }
 public class Sprite:Object { }
 public class Component:Object {public GameObject gameObject;}
 public class GameObject:Object {
  public bool activeInHierarchy=true;public readonly List<Component> Components=new();public readonly List<GameObject> Children=new();
  public T Add<T>(T component)where T:Component{component.gameObject=this;Components.Add(component);return component;}
  public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>Components.OfType<T>().Concat(Children.SelectMany(c=>c.GetComponentsInChildren<T>(inactive))).ToArray();
  public T GetComponentInChildren<T>(bool inactive)where T:Component=>GetComponentsInChildren<T>(inactive).FirstOrDefault();
 }
 public static class Resources {public static int Queries;public static CharmDisplay[] Displays=Array.Empty<CharmDisplay>();public static T[] FindObjectsOfTypeAll<T>(){Queries++;return Displays.Cast<T>().ToArray();}}
 public static class Time {public static float unscaledTime;}
}
namespace HutongGames.PlayMaker {
 public class FsmObject {public object Value;}
 public class FsmStateAction { }
 public class FsmState {public FsmStateAction[] Actions=Array.Empty<FsmStateAction>();}
 public class Fsm {public Dictionary<string,FsmState> States=new();public FsmState GetState(string name)=>States.TryGetValue(name,out var s)?s:null;}
}
namespace HutongGames.PlayMaker.Actions {public class SetSpriteRendererSprite:HutongGames.PlayMaker.FsmStateAction {public HutongGames.PlayMaker.FsmObject sprite=new();}}
public class PlayMakerFSM:UnityEngine.Component {public HutongGames.PlayMaker.Fsm Fsm=new();}
public class InvCharmBackboard:UnityEngine.Component {public int charmNum;public UnityEngine.GameObject charmObject;}
public class CharmDisplay:UnityEngine.Component {public int id;public UnityEngine.Sprite whiteCharm,blackCharm;}
public class PlayerData {public int Royal;public int GetInt(string name){if(name!="royalCharmState")throw new Exception(name);return Royal;}}
public class GameManager:UnityEngine.Object {public static GameManager instance;public PlayerData playerData=new();}
public class CharmIconList:UnityEngine.Object {public static CharmIconList Instance;public Dictionary<int,UnityEngine.Sprite> Sprites=new();public int RoyalRequests;public UnityEngine.Sprite GetSprite(int id){if(id==36)RoyalRequests++;return Sprites.TryGetValue(id,out var s)?s:null;}}
namespace Language {
 public static class Language {
  public static readonly List<string> Keys=new();public static bool Throw;
  public static string Get(string key,string sheet){Keys.Add(key);if(Throw)throw new Exception("language");if(key=="CHARM_NAME_36")return "Silksong Unlocker";if(key=="CHARM_DESC_36")return "Custom Knight Says Hi!";return key+"<br>text";}
 }
}
namespace KO.HollowKnight8 {
 internal static class Diagnostics {public static int Errors;public static void Throttled(string text,Exception ex){Errors++;}}
 internal static class CharmMenuDiagnostics {internal static void Reset(){} }
 internal static class HudAssets {public static int Copies;public static bool Throw;public static UnityEngine.Texture2D FromSprite(UnityEngine.Sprite s){if(Throw)throw new Exception("sprite extraction");Copies++;return new(){name=s.name};}}
 internal static class CharmExtensions {public static int Resets;public static void Reset(){Resets++;}}
}

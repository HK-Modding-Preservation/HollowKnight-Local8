using System;using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace UnityEngine {
 public class Object {public bool Exists=true;public string name;public static implicit operator bool(Object o)=>o!=null&&o.Exists;public static void Destroy(Object o){if(o!=null){o.Exists=false;if(o is GameObject g)foreach(var c in g.Components)c.Exists=false;}}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:class=>gameObject.GetComponent<T>();}
 public class Transform:Component {public Transform parent;public void SetParent(Transform t,bool worldPositionStays)=>parent=t;}
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,offsetMin,offsetMax;}
 public class GameObject:Object {public static readonly List<GameObject> All=new();public bool activeInHierarchy=true;public readonly List<Component> Components=new();public Transform transform;public Scene scene=new();public GameObject(string name,params Type[] types){this.name=name;transform=types.Contains(typeof(RectTransform))?new RectTransform():new Transform();transform.gameObject=this;Components.Add(transform);All.Add(this);}public T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}public T GetComponent<T>()where T:class=>Components.OfType<T>().FirstOrDefault(c=>c is not Object o||o);}
 public struct Scene {public bool IsValid()=>true;}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new();}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp01(float n)=>Math.Clamp(n,0,1);}
 public static class Time {public static float unscaledTime=100,unscaledDeltaTime=.1f;}
 public enum KeyCode {F8,F7,F6}
 public static class Resources {public static readonly List<Object> All=new();public static T[] FindObjectsOfTypeAll<T>()where T:Object=>All.OfType<T>().Where(x=>x).ToArray();}
}
namespace UnityEngine.UI {}
namespace TMPro {public class TMP_FontAsset:UnityEngine.Object {}public enum TextAlignmentOptions{Center=5}public class TMP_Text:Component {public TMP_FontAsset font;}public class TextMeshProUGUI:TMP_Text {public float fontSize;public TextAlignmentOptions alignment;public bool raycastTarget,enableWordWrapping,enabled;public string text;public Color color;}}
namespace Modding {public static class ModHooks {public static event Action NewGameHook;public static int Subscribers=>NewGameHook?.GetInvocationList().Length??0;public static void NewGame()=>NewGameHook?.Invoke();}}
namespace Language {public static class Language {public static string Code="EN";public static string CurrentLanguage()=>Code;}}
namespace GlobalEnums {public enum UIState{INACTIVE,PLAYING,PAUSED}}
public class UIManager:UnityEngine.Object {public static UIManager instance;public RectTransform UICanvas;public GlobalEnums.UIState uiState=GlobalEnums.UIState.PLAYING;}
public class GameManager:UnityEngine.Object {public static GameManager instance;public bool isPaused,IsLoadingSceneTransition;}
namespace KO.HollowKnight8 {
 public class PlayerAdventureStatistics {}
 internal static class Local8Mod {internal static Local8SaveData Save=new();}
 internal class Setting<T>{internal T Value;internal Setting(T value)=>Value=value;}
 internal class PlayerSlot {internal int Index;internal bool Ready=true,Alive=true;}
 internal class CoopSession {internal bool Active=true,Gameplay=true,CanJoin=true;internal List<PlayerSlot> Players=new(){new(){Index=0}};internal PlayerSlot Primary=>Players.FirstOrDefault();}
 internal class Local8Runtime:UnityEngine.Object {internal Setting<bool> Enabled=new(true);internal bool Panel;internal CoopSession Session=new();}
 internal static class Plugin {internal static Local8Runtime Self;}
 internal static class MenuInput {internal static KeyCode Key=KeyCode.F8;internal static string Display(KeyCode key)=>key.ToString();}
 internal static class Charms {internal static bool NativeMenuOpen;internal static string SanitizeLocalized(string key,string sheet,string fallback)=>RoleText.Has(key)?RoleText.Get(key):fallback;}
 internal static class ScriptedParty {internal static bool Active;}
 internal static class CoopEnding {internal static bool Active;}
 internal static class PvpMatch {internal static bool BlocksInput;}
 internal static class PickupCard {internal static PlayerSlot Owner;}
 internal static class ShopMenuRouting {internal static PlayerSlot Buyer;}
 internal static class StagMenuRouting {internal static bool HasOwner;}
 internal static class InteractionRouter {internal static PlayerSlot ActivePlayer;}
 internal static class TransitionVote {internal static bool Exit;internal static bool ApproachingExit(CoopSession s)=>Exit;}
 internal static class RescueHintVisibility {internal static void BeforeHint(CoopSession s){}internal static void Request(){}internal static void Reset(){}}
 internal static class ActionKeys {internal static string ButtonLabel=>"RB";internal static string RescueLabel=>"1";}
 internal static class Diagnostics {internal static readonly List<string> Messages=new();internal static void Write(string message)=>Messages.Add(message);}
}

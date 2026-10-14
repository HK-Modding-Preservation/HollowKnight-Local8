using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine {
 public class Object { public bool destroyed; public static implicit operator bool(Object value)=>value!=null&&!value.destroyed; }
 public class Component:Object { public GameObject gameObject; public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>(); }
 public class GameObject:Object {
  public readonly List<Component> Components=new(); public readonly List<GameObject> Children=new();
  public T Add<T>() where T:Component,new(){var value=new T{gameObject=this};Components.Add(value);return value;}
  public T GetComponent<T>() where T:Component=>Components.OfType<T>().FirstOrDefault(v=>v);
  public T[] GetComponentsInChildren<T>(bool inactive) where T:Component=>Components.OfType<T>().Concat(Children.SelectMany(c=>c.GetComponentsInChildren<T>(inactive))).ToArray();
 }
 public enum ParticleSystemCurveMode {Constant,Curve,TwoCurves,TwoConstants}
 public class ParticleSystem:Component {
  public struct MinMaxCurve {public float constant;public ParticleSystemCurveMode mode;public int CurveId;public static implicit operator MinMaxCurve(float value)=>new(){constant=value};}
  public struct EmissionModule {internal ParticleSystem p;public MinMaxCurve rateOverTime{get=>p.Rate;set{if(p.ThrowRateOnce){p.ThrowRateOnce=false;throw new Exception("particle setter");}p.Rate=value;}}}
  public struct MainModule {internal ParticleSystem p;public int maxParticles{get=>p.Max;set=>p.Max=value;}}
  public MinMaxCurve Rate;public int Max;public bool ThrowRateOnce;
  public EmissionModule emission=>new(){p=this};public MainModule main=>new(){p=this};
 }
 public struct Color {}
 public struct Rect {public float x,y,width,height;public float xMax=>x+width;public float yMax=>y+height;public Rect(float a,float b,float w,float h){x=a;y=b;width=w;height=h;}}
 public class GUIStyle {public float CalcHeight(GUIContent text,float width)=>(float)Math.Ceiling(text.text.Length*8/width)*20;}
 public class GUIContent {public string text,tooltip;public GUIContent(string text,string tip=""){this.text=text;tooltip=tip;}}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Clamp(int n,int min,int max)=>Math.Clamp(n,min,max);public static float Clamp(float n,float min,float max)=>Math.Clamp(n,min,max);public static int CeilToInt(float n)=>(int)Math.Ceiling(n);}
 public static class Time {public static float unscaledTime;}
}
public enum ShaderQualities {Low,Medium,High}
public class GameSettings {
 public int particleEffectsLevel=1;public ShaderQualities shaderQuality=ShaderQualities.Medium;
 public int resolution=7,frameCap=144;public bool fullscreen=true,vsync=true;public string controls="keyboard-p1-and-p2";
 public readonly List<(int particles,ShaderQualities blur,int resolution,int cap,bool fullscreen,bool vsync,string controls)> Saved=new();
 public Action DuringSave;public bool ThrowSave;
 public void SaveVideoSettings()=>On.GameSettings.Invoke(this);
 internal void OriginalSave(){Saved.Add((particleEffectsLevel,shaderQuality,resolution,frameCap,fullscreen,vsync,controls));DuringSave?.Invoke();if(ThrowSave)throw new Exception("native save");}
}
public class GameManager:UnityEngine.Object {
 public static GameManager instance;public GameSettings gameSettings=new();public int Refreshes,FailRefreshes;public Action OnRefresh;
 public void RefreshParticleSystems(){Refreshes++;if(FailRefreshes>0){FailRefreshes--;throw new Exception("external particle listener");}OnRefresh?.Invoke();}
}
public class ReduceParticleEffects:UnityEngine.Component {
 private bool init;private float emissionRateHigh,emissionRateLow;private int maxParticlesHigh,maxParticlesLow;
 public void Start(){var p=GetComponent<UnityEngine.ParticleSystem>();emissionRateHigh=p.Rate.constant;emissionRateLow=emissionRateHigh/2;maxParticlesHigh=p.Max;maxParticlesLow=maxParticlesHigh/2;init=true;SetEmission();}
 public void SetEmission(){var p=GetComponent<UnityEngine.ParticleSystem>();if(!p)return;p.Rate=GameManager.instance.gameSettings.particleEffectsLevel==0?emissionRateLow:emissionRateHigh;p.Max=GameManager.instance.gameSettings.particleEffectsLevel==0?maxParticlesLow:maxParticlesHigh;}
}
namespace On {
 public static class GameSettings {
  public delegate void orig_SaveVideoSettings(global::GameSettings s);
  public delegate void hook_SaveVideoSettings(orig_SaveVideoSettings orig,global::GameSettings s);
  public static event hook_SaveVideoSettings SaveVideoSettings;
  public static int Count=>SaveVideoSettings?.GetInvocationList().Length??0;
  public static void Invoke(global::GameSettings s){orig_SaveVideoSettings orig=v=>v.OriginalSave();if(SaveVideoSettings!=null)foreach(hook_SaveVideoSettings hook in SaveVideoSettings.GetInvocationList()){var next=orig;orig=v=>hook(next,v);}orig(s);}
 }
}
namespace KO.HollowKnight8 {
 internal sealed class Setting<T>{internal T Value;internal Setting(T value){Value=value;}}
 internal class PlayerSlot {public bool Alive=true;public int Health=5,Role=4;public string Skin="CustomKnight";public int[] Charms={1,6,40,81};}
 internal class CoopSession {internal bool Active=true;internal readonly List<PlayerSlot> Players=new();}
 internal class Local8Runtime:UnityEngine.Object {
  internal PerformanceConfiguration Performance=new();internal CoopSession Session=new();
  internal Setting<bool> Enabled=new(true),PerformanceMode=new(false),ReduceHitEffects=new(true);internal int Exports;
  internal void ExportSettings(){Exports++;}
 }
 internal static class Plugin {internal static Local8Runtime Self;}
 internal static class NativeRenderBudget {internal static int Resets;internal static void Reset(){Resets++;}}
 internal static class Diagnostics {internal static readonly List<string> Messages=new();internal static void Write(string message)=>Messages.Add(message);internal static void Throttled(string key,Exception e)=>Messages.Add(key+": "+e.Message);}
 public class PlayerProfileData {}
 internal static class PvpSpellOnly {internal const int DefaultRegeneration=11;internal static int Rate(int n)=>Math.Clamp(n,1,99);}
 internal static class PvpRules {internal static int SeriesLength(int n)=>Math.Clamp(n,0,21);}
 internal static class UiLocalization {internal static string Get(string key)=>key=="performance.status"?"Players {players} | {preset} | recommended {recommended}":key=="performance.actual"?"Native {particles} / {blur}":key;}
 internal class Control {internal UnityEngine.Rect Rect;internal string Text,Tip;internal bool Selected;}
 internal static class MenuTheme {
  internal static UnityEngine.Color Paper,Muted;internal static UnityEngine.GUIStyle ButtonStyle=new(),Label=new();
  internal static readonly List<Control> Controls=new();internal static readonly List<string> Paragraphs=new();internal static Func<Control,bool> Click;
  internal static void Clear(){Controls.Clear();Paragraphs.Clear();}
  internal static bool Add(UnityEngine.Rect r,string text,string tip,bool selected){var c=new Control{Rect=r,Text=text,Tip=tip,Selected=selected};Controls.Add(c);return Click?.Invoke(c)??false;}
  internal static void Section(string text,float width,ref float row){row+=35;}
  internal static bool Toggle(UnityEngine.Rect r,string text,bool value,string tip)=>Add(r,text,tip,value);
  internal static bool Choice(UnityEngine.Rect r,UnityEngine.GUIContent text,bool value)=>Add(r,text.text,text.tooltip,value);
  internal static bool Button(UnityEngine.Rect r,UnityEngine.GUIContent text,UnityEngine.GUIStyle style)=>Add(r,text.text,text.tooltip,false);
  internal static float Paragraph(float x,float y,float width,string text,UnityEngine.Color color,int size=13){Paragraphs.Add(text);return (float)Math.Ceiling(text.Length*size*.5/width)*size*1.2f;}
 }
}

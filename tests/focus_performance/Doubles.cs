using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HutongGames.PlayMaker;

namespace UnityEngine {
 public class Object {
  public bool destroyed; public string name;
  public static implicit operator bool(Object o) => o != null && !o.destroyed;
  public int GetInstanceID() => GetHashCode();
 }
 public class Component : Object {
  private GameObject owner;
  public GameObject gameObject { get { if (destroyed) throw new NullReferenceException("destroyed native Component.gameObject"); return owner; } set => owner = value; }
  public Transform transform => gameObject.transform;
  public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
  public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component => gameObject.GetComponentsInChildren<T>(includeInactive);
  public void GetComponentsInChildren<T>(bool includeInactive, List<T> values) where T : Component => values.AddRange(GetComponentsInChildren<T>(includeInactive));
 }
 public class MonoBehaviour : Component { public bool enabled = true; }
 public class Transform : Component {
  public Transform parent; public Vector3 position, lossyScale; public Quaternion rotation;
  public int childCount => GameObject.All.Count(g => g && g.transform.parent == this);
  public bool IsChildOf(Transform other) { for (Transform t = this; t; t = t.parent) if (t == other) return true; return false; }
 }
 public class GameObject : Object {
  public int layer; public bool activeInHierarchy = true;
  public readonly Transform transform;
  public readonly List<Component> components = new();
  public static readonly List<GameObject> All = new(); public static int HierarchyReads, RendererVisits;
  public GameObject(string label) { name = label; transform = new Transform { name = label, gameObject = this }; components.Add(transform); All.Add(this); }
  public T Add<T>() where T : Component, new() { var c = new T { name = name, gameObject = this }; components.Add(c); return c; }
  public T GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault(c => c);
  public T[] GetComponents<T>() where T : Component => components.OfType<T>().Where(c => c).ToArray();
  public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component {
   HierarchyReads++;
   var values = All.Where(g => g && g.transform.IsChildOf(transform) && (includeInactive || g.activeInHierarchy)).SelectMany(g => g.components.OfType<T>()).Where(c => c).ToArray();
   if(typeof(T)==typeof(Renderer)) RendererVisits += values.Length; return values;
  }
  public void GetComponentsInChildren<T>(bool includeInactive, List<T> values) where T : Component => values.AddRange(GetComponentsInChildren<T>(includeInactive));
 }
 public class Renderer : Component { public bool enabled = true, forceRenderingOff; public Material sharedMaterial; }
 public class MeshRenderer : Renderer { }
 public class ParticleSystemRenderer : Renderer { }
 public class Sprite : Object { }
 public class Shader : Object { }
 public class Material : Object { public Shader shader; public Object mainTexture; }
 public struct Color { public float a; }
 public struct Vector3 { public float x, y, z; }
 public struct Quaternion { public float x, y, z, w; }
 public struct Matrix4x4 {
  public float m00,m01,m02,m03,m10,m11,m12,m13,m20,m21,m22,m23,m30,m31,m32,m33;
  public Vector3 MultiplyPoint3x4(Vector3 p)=>new(){x=m00*p.x+m01*p.y+m02*p.z+m03,y=m10*p.x+m11*p.y+m12*p.z+m13,z=m20*p.x+m21*p.y+m22*p.z+m23};
 }
 public class RenderTexture : Object { public int width, height; public bool Created=true; public float[] Pixels; public bool IsCreated()=>Created; }
 public enum RenderingPath { Forward, DeferredShading, DeferredLighting }
 public struct Rect { public float x,y,width,height; }
 public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); }
 public class SpriteRenderer : Renderer { public Sprite sprite; public Color color; }
 public class Camera : Component {
  public delegate void CameraCallback(Camera camera);
  public static CameraCallback onPreCull, onPreRender, onPostRender;
  public int cullingMask = -1;
  public float nearClipPlane=.3f, farClipPlane=500f, fieldOfView=60f, orthographicSize=7f;
  public bool orthographic; public Matrix4x4 projectionMatrix, worldToCameraMatrix;
  public RenderTexture targetTexture;
  public bool enabled=true; public float depth; public RenderingPath actualRenderingPath=RenderingPath.Forward;
  public Rect pixelRect=new(){width=1920,height=1080};
  public readonly Dictionary<UnityEngine.Rendering.CameraEvent,List<UnityEngine.Rendering.CommandBuffer>> Commands=new();
  public void AddCommandBuffer(UnityEngine.Rendering.CameraEvent e,UnityEngine.Rendering.CommandBuffer b){if(!Commands.TryGetValue(e,out var list))Commands[e]=list=new();list.Add(b);}
  public void RemoveCommandBuffer(UnityEngine.Rendering.CameraEvent e,UnityEngine.Rendering.CommandBuffer b){if(Commands.TryGetValue(e,out var list))list.Remove(b);}
  public int CommandCount=>Commands.Values.Sum(l=>l.Count);
  public float[] ExecuteBackdrop(){float[] result=null;foreach(var b in Commands.Values.SelectMany(l=>l)){if(b.Source?.Pixels!=null)result=(float[])b.Source.Pixels.Clone();}return result;}
  public int ViewResets; public void ResetWorldToCameraMatrix(){worldToCameraMatrix=default;ViewResets++;}
 }
 public static class Time { public static int frameCount; public static float unscaledTime; }
}
namespace UnityEngine.Rendering {
 public enum CameraEvent { BeforeForwardOpaque, BeforeGBuffer }
 public enum BuiltinRenderTextureType { CameraTarget }
 public class CommandBuffer {
  public string name; public static int Allocations, Releases; public bool Released; public UnityEngine.RenderTexture Source;
  public BuiltinRenderTextureType Destination, Target; public UnityEngine.Rect Viewport;
  public CommandBuffer(){Allocations++;}
  public void Blit(UnityEngine.RenderTexture source,BuiltinRenderTextureType dest){Source=source;Destination=dest;}
  public void SetRenderTarget(BuiltinRenderTextureType target){Target=target;}
  public void SetViewport(UnityEngine.Rect viewport){Viewport=viewport;}
  public void Clear(){Source=null;}
  public void Release(){Clear();Released=true;Releases++;}
 }
}
namespace HutongGames.PlayMaker {
 public class FsmStateAction { }
 public class FsmState { public string Name; public FsmStateAction[] Actions; }
 public class FsmGameObject { public UnityEngine.GameObject Value; }
 public class FsmObject { public UnityEngine.Object Value; }
 public enum OwnerDefaultOption { UseOwner, SpecifyGameObject }
 public class FsmOwnerDefault { public OwnerDefaultOption OwnerOption; public FsmGameObject GameObject; }
}
public class PlayMakerFSM : UnityEngine.Component {
 public string ActiveStateName; public HutongGames.PlayMaker.FsmState[] FsmStates;
}
public class HeroControllerStates { public bool focusing; }
public class PlayerData { }
public class HeroActions { }
public class HeroController : UnityEngine.Component {
 public PlayMakerFSM spellControl, vignetteFSM; public UnityEngine.SpriteRenderer vignette, heroLight;
 public HeroControllerStates cState = new(); public bool wieldingLantern;
}
public class GameManager : UnityEngine.Component {
 public static GameManager instance; public bool IsLoadingSceneTransition, Gameplay = true;
 public UnityEngine.SpriteRenderer heroLight; public bool IsGameplayScene() => Gameplay;
 public GameSettings gameSettings=new();
}
public enum ShaderQualities {Low,Medium,High}
public class GameSettings {public ShaderQualities shaderQuality=ShaderQualities.Medium;}
public class GameCameras : UnityEngine.Component {
 public static GameCameras instance; public UnityEngine.Camera mainCamera; public UnityEngine.GameObject hudCanvas;
}
public class LightBlurredBackground : UnityEngine.MonoBehaviour {
 private UnityEngine.Camera sceneCamera, backgroundCamera; private float distantFarClipPlane;
 private float clipEpsilon=.001f;
 private UnityEngine.RenderTexture renderTexture;
 public int RenderTextureHeight=540, PassGroupCount=2, ResizeCount;
 public void Initialize(UnityEngine.Camera main,UnityEngine.Camera background,float distant){sceneCamera=main;backgroundCamera=background;distantFarClipPlane=distant;OnCameraAspectChanged(1.77778f);}
 private void OnCameraAspectChanged(float aspect){if(!enabled)return;renderTexture=new UnityEngine.RenderTexture{width=(int)(RenderTextureHeight*aspect),height=RenderTextureHeight};backgroundCamera.targetTexture=renderTexture;ResizeCount++;}
 public UnityEngine.RenderTexture Texture=>renderTexture;
}
public class BlurPlane : UnityEngine.MonoBehaviour {
 public static readonly List<BlurPlane> All=new();
 public static int BlurPlaneCount=>All.Count;public static BlurPlane GetBlurPlane(int i)=>All[i];
 public static BlurPlane ClosestBlurPlane=>All.Count==0?null:All[0];public float PlaneZ=>transform.position.z;
}
public static class ForceCameraAspect {public static float CurrentViewportAspect=1.77778f;}
namespace UnityStandardAssets.ImageEffects {
 public class BloomOptimized : UnityEngine.MonoBehaviour {
  public enum Resolution {Low,High} private Resolution resolution=Resolution.High;
  public int blurIterations=3;public Resolution CurrentResolution=>resolution;
  public void SetResolution(Resolution value)=>resolution=value;
 }
 public class FastNoise : UnityEngine.MonoBehaviour {}
}
public class tk2dSpriteDefinition { public string name; }
public class tk2dBaseSprite : UnityEngine.Component {
 public tk2dSpriteDefinition Definition; public tk2dSpriteDefinition GetCurrentSpriteDef() => Definition;
}
namespace KO.HollowKnight8 {
 internal class Flag { public bool Value = true; }
 internal class Local8Runtime { public PerformanceConfiguration Performance; public Flag ReduceHitEffects=new(); public CoopSession Session; public Flag Enabled = new(), PlayerLights = new(), GroupDarkness = new(),HideOriginalHud=new(),PerformanceMode=new(){Value=false}; }
 internal static class Plugin { internal static Local8Runtime Self; }
 internal class CoopSession {
  internal bool Active = true, TeamWipe; internal readonly List<PlayerSlot> Players = new();
  internal PlayerSlot Primary => Players.Count == 0 ? null : Players[0];
 }
 internal class PlayerSlot {
  internal int Index; internal HeroController Hero, LightOwner; internal Renderer[] LightRenderers;
  internal bool Alive = true, Ready = true, Down, Hazard;
 }
 internal static class CoopEnding { internal static bool Active; }
 internal static class EndingAbsorption { internal static bool Active; }
 internal static class Diagnostics {
  internal static readonly List<string> Messages = new();
  internal static void Write(string message) => Messages.Add(message);
  internal static void Throttled(string message, Exception e) => Messages.Add(message + " " + e.GetType().Name);
 }
 internal static class Hooks {
  internal static ReferenceField[] GetReferenceAccesses(Type type) => GetReferenceFields(type).Select(ReferenceField.For).ToArray();
  static readonly Dictionary<Type, FieldInfo[]> fields = new(); internal static int ReflectionReads;
  internal static FieldInfo[] GetReferenceFields(Type type) {
   if (fields.TryGetValue(type, out var result)) return result;
   ReflectionReads++;
   return fields[type] = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    .Where(f => !f.IsInitOnly && (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType) || f.FieldType == typeof(FsmGameObject) || f.FieldType == typeof(FsmObject) || f.FieldType == typeof(FsmOwnerDefault))).ToArray();
  }
 }
 internal sealed class GroupVignette : IDisposable {
  internal static bool Throw; internal static int Renders; internal string Status = "fixture";
  internal bool Render(Camera camera, Camera gameCamera, CoopSession session, SpriteRenderer source, bool light) {
   Renders++; if (Throw) throw new InvalidOperationException("render fixture"); return true;
  }
  internal void Restore() { } internal void Finish(Camera camera) { } public void Dispose() { }
 }
 internal class SharedVisibility : IDisposable {
  internal string Status = "fixture"; public void Dispose() { }
  internal void Render(Camera camera, CoopSession session, SpriteRenderer source, bool darkness) { }
  internal void Hide() { }
 }
 internal static class ArenaVisualRecovery { internal static void BeforeCamera(Camera camera) { } }
 internal static class PvpGeoHud {internal static void BeforeCamera(Camera camera){} }
}

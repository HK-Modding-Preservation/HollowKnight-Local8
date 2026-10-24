using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public class Object {
  public string name;public bool Destroyed;
  public static implicit operator bool(Object o)=>o!=null&&!o.Destroyed;
  public static void Destroy(Object o){if(o==null)return;o.Destroyed=true;if(o is GameObject g)foreach(var child in GameObject.All.Where(x=>x.transform.IsChildOf(g.transform)))child.Destroyed=true;}
  public static GameObject Instantiate(GameObject source,Transform parent){
   var clone=new GameObject(source.name);clone.transform.parent=parent;clone.transform.localPosition=source.transform.localPosition;clone.transform.localScale=source.transform.localScale;
   foreach(var r in source.Components.OfType<SpriteRenderer>()){var copy=clone.Add<SpriteRenderer>();copy.sprite=r.sprite;copy.enabled=r.enabled;}
   foreach(var child in GameObject.All.Where(x=>x.transform.parent==source.transform).ToArray())Instantiate(child,clone.transform);
   return clone;
  }
 }
 public class Component:Object {
  public GameObject gameObject;public Transform transform=>gameObject.transform;
  public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();
  public void GetComponentsInChildren<T>(bool includeInactive,List<T> values)where T:Component=>gameObject.GetComponentsInChildren(includeInactive,values);
 }
 public class Transform:Component {
  public Transform parent;public Vector3 localPosition,localScale=new(1,1,1);
  public Vector3 position{get=>parent?parent.position+localPosition:localPosition;set=>localPosition=parent?value-parent.position:value;}
  public Vector3 lossyScale=>parent?new(parent.lossyScale.x*localScale.x,parent.lossyScale.y*localScale.y,parent.lossyScale.z*localScale.z):localScale;
  public bool IsChildOf(Transform root){for(var t=this;t;t=t.parent)if(t==root)return true;return false;}
  public Transform Find(string path){var t=this;foreach(string part in path.Split('/')){t=GameObject.All.FirstOrDefault(g=>g&&g.transform.parent==t&&g.name==part)?.transform;if(!t)return null;}return t;}
 }
 public class GameObject:Object {
  public static readonly List<GameObject> All=new();public readonly List<Component> Components=new();public readonly Transform transform;
  public bool activeSelf=true;public bool activeInHierarchy=>this&&activeSelf&&(!transform.parent||transform.parent.gameObject.activeInHierarchy);
  public GameObject(string name){this.name=name;transform=new(){gameObject=this,name=name};Components.Add(transform);All.Add(this);}
  public void SetActive(bool value)=>activeSelf=value;
  public T Add<T>()where T:Component,new(){var c=new T{gameObject=this,name=name};Components.Add(c);return c;}
  public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault();
  public void GetComponentsInChildren<T>(bool includeInactive,List<T> values)where T:Component=>values.AddRange(All.Where(g=>g&&g.transform.IsChildOf(transform)&&(includeInactive||g.activeInHierarchy)).SelectMany(g=>g.Components.OfType<T>()));
 }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
 }
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMin=>x;public float yMin=>y;public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new(x+width/2,y+height/2);public static Rect MinMaxRect(float a,float b,float c,float d)=>new(a,b,c-a,d-b);}
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public Vector3 min=>center-size*.5f;public Vector3 max=>center+size*.5f;}
 public class Texture2D:Object {public int width=64,height=64;}
 public class Sprite:Object {
  public static int Created;public Texture2D texture;public Bounds bounds;
  public static Sprite Create(Texture2D t,Rect r,Vector2 pivot,float ppu){Created++;return new(){texture=t,bounds=new(new(),new(r.width/ppu,r.height/ppu))};}
 }
 public class Renderer:Component {public bool enabled=true;public Bounds Box;public virtual Bounds bounds=>Box;}
 public class SpriteRenderer:Renderer {public Sprite sprite;public override Bounds bounds=>new(transform.position,new(sprite.bounds.size.x*transform.lossyScale.x,sprite.bounds.size.y*transform.lossyScale.y));}
 public class Camera:Component {
  public Rect View=new(0,0,16,9);public int Reads;
  public Vector3 WorldToViewportPoint(Vector3 v){Reads++;return new((v.x-View.x)/View.width,(v.y-View.y)/View.height,2);}
  public Vector3 ViewportToWorldPoint(Vector3 v){Reads++;return new(View.x+v.x*View.width,View.y+v.y*View.height,v.z);}
 }
 public class Canvas:Component {public Camera worldCamera;}
 public class CanvasGroup:Component {}
 public class RectTransform:Component {public Vector3[] Corners;public void GetWorldCorners(Vector3[] target)=>Array.Copy(Corners,target,4);}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Floor(float value)=>(float)Math.Floor(value);}
 public static class Time {public static float unscaledTime;}
}
namespace SFCore {
 public static class DlcIconHelper {
  private static readonly List<UnityEngine.Sprite> CustomSprites=new();
  public static void AddDlcIcon(UnityEngine.Sprite sprite)=>CustomSprites.Add(sprite);
  public static int Count=>CustomSprites.Count;
 }
}
namespace UnityEngine.UI {public class Graphic:UnityEngine.Component {public bool enabled=true;public UnityEngine.RectTransform rectTransform;}}
public class UIManager:UnityEngine.Component {public static UIManager instance;public UnityEngine.Canvas UICanvas;public UnityEngine.CanvasGroup mainMenuScreen;}
public class GameCameras:UnityEngine.Object {public static GameCameras instance;public UnityEngine.Camera mainCamera,hudCamera;}
namespace KO.HollowKnight8 {
 internal class Flag {internal bool Value=true;}
 internal class Local8Runtime {internal Flag Enabled=new();}
 internal static class TitleLobby {internal static bool Visible=true;}
 internal static class MenuArt {internal const string Players="icon_players";internal static readonly UnityEngine.Texture2D Glyph=new(){name=Players};internal static UnityEngine.Texture2D Texture(string key){if(key!=Players)throw new Exception("unexpected artwork");return Glyph;}}
 internal static class Diagnostics {internal static readonly List<string> Messages=new();internal static void Write(string text)=>Messages.Add(text);internal static void Throttled(string text,Exception error)=>Messages.Add(text);}
}

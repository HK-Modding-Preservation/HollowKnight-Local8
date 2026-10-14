using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Buffers.Binary;
using System.Text.Json;
namespace Language {public static class Language {public static string Code="EN";public static string CurrentLanguage()=>Code;}}
namespace UnityEngine {
 public class Object {public string name="";public bool Destroyed;public HideFlags hideFlags;public static implicit operator bool(Object x)=>x!=null&&!x.Destroyed;public static void Destroy(Object x){if(x!=null)x.Destroyed=true;}public static T[] FindObjectsOfType<T>()where T:Component=>GameObject.Objects.Where(o=>o.activeInHierarchy).SelectMany(o=>o.Components).OfType<T>().ToArray();public static T Instantiate<T>(T x)where T:Object{if(x is GUISkin skin)return (T)(Object)skin.Copy();throw new Exception("unexpected clone");}}
 public enum HideFlags{HideAndDontSave}public enum FilterMode{Bilinear}public enum TextureWrapMode{Clamp}public enum TextureFormat{RGBA32}public enum ScaleMode{ScaleToFit,StretchToFill}public enum FontStyle{Normal,Bold}public enum TextAnchor{MiddleLeft,MiddleCenter,UpperLeft,MiddleRight,UpperCenter}public enum TextClipping{Clip}public enum EventType{Repaint,MouseMove}public enum KeyCode{F8,F9,Alpha1}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);public static Color black=>new Color(0,0,0);public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);public static Color clear=>new Color(0,0,0,0);}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new Vector2();}
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public float xMax=>x+width;public float yMax=>y+height;public Vector2 center=>new Vector2(x+width/2,y+height/2);public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;}
 public class RectOffset {public int left,right,top,bottom;public RectOffset(int l,int r,int t,int b){left=l;right=r;top=t;bottom=b;}}
 public class Texture:Object {public int width,height;}
 public class Texture2D:Texture {
  public static int Allocations,Decodes;public static readonly List<Texture2D> Created=new List<Texture2D>();public Color[] Pixels;public bool Readable=true;public FilterMode filterMode;public TextureWrapMode wrapMode;
  public static readonly Texture2D whiteTexture=new Texture2D(1,1,TextureFormat.RGBA32,false){Pixels=new[]{Color.white},name="white"};
  public Texture2D(int w,int h,TextureFormat f,bool m){width=w;height=h;Allocations++;name="texture"+Allocations;Created.Add(this);}
  public void SetPixels(Color[] c){Pixels=c;}
  public void Apply(bool a=false,bool b=false){if(b)Readable=false;}
  public Color[] GetPixels(){if(!Readable)throw new InvalidOperationException("texture was sealed too early");return Pixels.ToArray();}
  public Color[] GetPixels(int x,int y,int w,int h){if(!Readable)throw new InvalidOperationException("unreadable atlas");Color[] c=new Color[w*h];for(int r=0;r<h;r++)Array.Copy(Pixels,(y+r)*width+x,c,r*w,w);return c;}
  public bool LoadImage(byte[] png){
   Decodes++;if(png.Length<33||png[0]!=137)return false;
   width=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16,4));height=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20,4));
   if(png[24]!=8||png[25]!=3)throw new Exception("unexpected UI PNG format");
   byte[] palette=null,alpha=null;using var packed=new MemoryStream();
   for(int i=8;i<png.Length;){int n=BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(i,4));string kind=System.Text.Encoding.ASCII.GetString(png,i+4,4);if(kind=="PLTE")palette=png.AsSpan(i+8,n).ToArray();if(kind=="tRNS")alpha=png.AsSpan(i+8,n).ToArray();if(kind=="IDAT")packed.Write(png,i+8,n);i+=n+12;}
   packed.Position=0;using var z=new ZLibStream(packed,CompressionMode.Decompress);byte[] raw=new byte[(width+1)*height];z.ReadExactly(raw);byte[] prior=new byte[width];Pixels=new Color[width*height];
   for(int y=0;y<height;y++){byte[] row=new byte[width];int mode=raw[y*(width+1)];for(int x=0;x<width;x++){int a=x>0?row[x-1]:0,b=prior[x],c=x>0?prior[x-1]:0;int predict=mode switch{0=>0,1=>a,2=>b,3=>(a+b)/2,4=>Paeth(a,b,c),_=>throw new Exception("invalid PNG filter")};row[x]=(byte)(raw[y*(width+1)+1+x]+predict);int q=row[x];Pixels[(height-1-y)*width+x]=new Color(palette[q*3]/255f,palette[q*3+1]/255f,palette[q*3+2]/255f,alpha!=null&&q<alpha.Length?alpha[q]/255f:1);}prior=row;}
   return true;
  }
  static int Paeth(int a,int b,int c){int p=a+b-c,aa=Math.Abs(p-a),bb=Math.Abs(p-b),cc=Math.Abs(p-c);return aa<=bb&&aa<=cc?a:bb<=cc?b:c;}
 }
 public class Font:Object {public bool AllGlyphs;public bool HasCharacter(char c)=>AllGlyphs||c<400;}
 public static class Resources {public static int Queries;public static Font[] Fonts={new Font{name="Perpetua"},new Font{name="Noto CJK fallback",AllGlyphs=true}};public static T[] FindObjectsOfTypeAll<T>()where T:class{Queries++;return Fonts.Cast<T>().ToArray();}}
 public static class Time {public static float unscaledTime;public static int frameCount;}
 public static class Mathf {public const float PI=(float)Math.PI;public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(x,b));public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(x,b));public static float Clamp01(float x)=>Clamp(x,0,1);public static int RoundToInt(float v)=>(int)Math.Round(v,MidpointRounding.AwayFromZero);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static int FloorToInt(float v)=>(int)Math.Floor(v);public static float Abs(float v)=>Math.Abs(v);public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Cos(float v)=>(float)Math.Cos(v);public static float Sin(float v)=>(float)Math.Sin(v);}
 public class GUIContent {public string text,tooltip;public GUIContent(string t,string tip=""){text=t;tooltip=tip;}}
 public class GUIStyleState {public Color textColor=Color.white;public Texture2D background;}
 public class GUIStyle {public int fontSize=14;public Font font;public FontStyle fontStyle;public TextAnchor alignment;public TextClipping clipping;public bool richText,wordWrap;public float fixedHeight,fixedWidth;public RectOffset padding=new RectOffset(0,0,0,0),border=new RectOffset(0,0,0,0),margin=new RectOffset(0,0,0,0);public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState(),onNormal=new GUIStyleState(),onHover=new GUIStyleState(),onActive=new GUIStyleState(),onFocused=new GUIStyleState();public static readonly GUIStyle none=new GUIStyle();public GUIStyle(){}public GUIStyle(GUIStyle s){fontSize=s.fontSize;font=s.font;fontStyle=s.fontStyle;alignment=s.alignment;clipping=s.clipping;richText=s.richText;wordWrap=s.wordWrap;padding=s.padding;border=s.border;margin=s.margin;fixedHeight=s.fixedHeight;fixedWidth=s.fixedWidth;Copy(s.normal,normal);Copy(s.hover,hover);Copy(s.active,active);Copy(s.focused,focused);Copy(s.onNormal,onNormal);Copy(s.onHover,onHover);Copy(s.onActive,onActive);Copy(s.onFocused,onFocused);}static void Copy(GUIStyleState a,GUIStyleState b){b.background=a.background;b.textColor=a.textColor;}public float CalcHeight(GUIContent c,float width){float w=Math.Max(1,width-padding.left-padding.right),len=0;float lines=0;foreach(string line in c.text.Split('\n')){len=0;foreach(char ch in line)len+=(ch>1000?1:.5f)*fontSize;lines+=(float)Math.Max(1,Math.Ceiling(len/w));}return lines*fontSize*1.25f+padding.top+padding.bottom;}}
 public class GUISkin:Object {public Font font=new Font{name="Default",AllGlyphs=true};public GUIStyle label=new GUIStyle(),button=new GUIStyle(),horizontalSlider=new GUIStyle(),horizontalSliderThumb=new GUIStyle(),verticalScrollbar=new GUIStyle(),verticalScrollbarThumb=new GUIStyle(),verticalScrollbarUpButton=new GUIStyle(),verticalScrollbarDownButton=new GUIStyle();public GUISkin Copy()=>new GUISkin{font=font,label=new GUIStyle(label),button=new GUIStyle(button),horizontalSlider=new GUIStyle(horizontalSlider),horizontalSliderThumb=new GUIStyle(horizontalSliderThumb),verticalScrollbar=new GUIStyle(verticalScrollbar),verticalScrollbarThumb=new GUIStyle(verticalScrollbarThumb),verticalScrollbarUpButton=new GUIStyle(verticalScrollbarUpButton),verticalScrollbarDownButton=new GUIStyle(verticalScrollbarDownButton)};}
 public class Event {public static Event current=new Event();public EventType type=EventType.Repaint;public Vector2 mousePosition=new Vector2(-100,-100);}
 public static class ColorUtility {public static string ToHtmlStringRGB(Color c)=>$"{Mathf.RoundToInt(c.r*255):X2}{Mathf.RoundToInt(c.g*255):X2}{Mathf.RoundToInt(c.b*255):X2}";}
 public sealed class Paint {public string kind,text,tooltip,texture,font;public float[] rect,color,clip,uv;public int[] padding,border;public int size;public bool enabled,wrap;public int align;}
 public sealed class HostControl {public string Text,Tip;public Rect Rect,Clip;public bool Enabled;}
 public static class GUI {
  public static Matrix4x4 matrix;public static int depth;public static GUISkin skin=new GUISkin();public static Color color=Color.white;public static bool enabled=true;public static string tooltip="";public static bool ThrowTexture;
  public static readonly List<Paint> Paints=new List<Paint>();public static readonly List<HostControl> Controls=new List<HostControl>();public static readonly Dictionary<string,Texture2D> Textures=new Dictionary<string,Texture2D>();
  public static Vector2? ClickPoint;public static float? SliderValue;public static bool ClickConsumed;public static float ViewWidth=1600,ViewHeight=900;
  static Vector2 offset;static Rect clip=new Rect(0,0,1600,900);static Stack<(Vector2,Rect)> stack=new Stack<(Vector2,Rect)>();
  public static void Reset(float w=1600,float h=900){Paints.Clear();Controls.Clear();tooltip="";ClickConsumed=false;offset=Vector2.zero;clip=new Rect(0,0,w,h);ViewWidth=w;ViewHeight=h;stack.Clear();}
  static Rect Absolute(Rect r)=>new Rect(r.x+offset.x,r.y+offset.y,r.width,r.height);
  static bool Visible(Rect r)=>r.x<clip.xMax&&r.xMax>clip.x&&r.y<clip.yMax&&r.yMax>clip.y;
  static float[] Values(Rect r)=>new[]{r.x,r.y,r.width,r.height};static float[] Values(Color c)=>new[]{c.r,c.g,c.b,c.a};
  static void Paint(string kind,Rect local,string value,GUIStyle style=null,Texture tex=null,Rect? uv=null,string tip="") {
   Rect r=Absolute(local);if(!Visible(r))return;
   Texture2D t=tex as Texture2D;if(t!=null)Textures[t.name]=t;
   Paints.Add(new Paint{kind=kind,text=value,tooltip=tip,rect=Values(r),clip=Values(clip),color=Values(style?.normal.textColor??color),texture=tex?.name,font=style?.font?.name,padding=style==null?null:new[]{style.padding.left,style.padding.right,style.padding.top,style.padding.bottom},border=style==null?null:new[]{style.border.left,style.border.right,style.border.top,style.border.bottom},size=style?.fontSize??0,enabled=enabled,wrap=style?.wordWrap??false,align=(int)(style?.alignment??TextAnchor.MiddleLeft),uv=uv.HasValue?Values(uv.Value):null});
  }
  public static void DrawTexture(Rect r,Texture t,ScaleMode scale=ScaleMode.ScaleToFit,bool alphaBlend=true){if(ThrowTexture)throw new InvalidOperationException("controlled texture failure");Paint("texture",r,"",tex:t);}
  public static void DrawTextureWithTexCoords(Rect r,Texture t,Rect uv,bool alphaBlend=true){if(ThrowTexture)throw new InvalidOperationException("controlled texture failure");Paint("texture",r,"",tex:t,uv:uv);}
  public static string FieldInput;public static void SetNextControlName(string n){}public static void FocusControl(string n){}public static string TextField(Rect r,string value,int max,GUIStyle style){Paint("textfield",r,value,style);return FieldInput??value;}
  public static void Label(Rect r,string value,GUIStyle style){Paint("label",r,value,style);}
  public static void Label(Rect r,GUIContent value,GUIStyle style){Hover(r,value.tooltip);Paint("label",r,value.text,style,tip:value.tooltip);}
  static void Hover(Rect r,string tip){if(enabled&&Visible(Absolute(r))&&r.Contains(Event.current.mousePosition)&&tip.Length>0)tooltip=tip;}
  public static bool Button(Rect r,string value,GUIStyle style)=>Button(r,new GUIContent(value),style);
  public static bool Button(Rect r,GUIContent value,GUIStyle style){Hover(r,value.tooltip);Paint("button",r,value.text,style,style.normal.background,tip:value.tooltip);Rect abs=Absolute(r);Controls.Add(new HostControl{Rect=abs,Clip=clip,Text=value.text,Tip=value.tooltip,Enabled=enabled});if(enabled&&!ClickConsumed&&ClickPoint.HasValue&&abs.Contains(ClickPoint.Value)&&clip.Contains(ClickPoint.Value)){ClickConsumed=true;return true;}return false;}
  public static float HorizontalSlider(Rect r,float value,float min,float max,GUIStyle track=null,GUIStyle thumb=null){Paint("slider",r,value.ToString("0.##"),skin.horizontalSlider);return enabled&&SliderValue.HasValue?Mathf.Clamp(SliderValue.Value,min,max):value;}
  public static int SelectionGrid(Rect r,int selected,string[] values,int columns,GUIStyle style){int rows=(values.Length+columns-1)/columns;for(int i=0;i<values.Length;i++){var s=new GUIStyle(style);if(i==selected)s.normal.background=style.onNormal.background;if(Button(new Rect(r.x+i%columns*r.width/columns,r.y+i/columns*r.height/rows,r.width/columns-4,r.height/rows-4),values[i],s))selected=i;}return selected;}
  public static Vector2 BeginScrollView(Rect r,Vector2 scroll,Rect view,bool a=false,bool b=false,GUIStyle horizontal=null,GUIStyle vertical=null){Rect area=Absolute(r);stack.Push((offset,clip));offset=new Vector2(area.x-scroll.x,area.y-scroll.y);clip=area;Event.current.mousePosition=new Vector2(Event.current.mousePosition.x-offset.x,Event.current.mousePosition.y-offset.y);return scroll;}
  public static void EndScrollView(){var old=stack.Pop();Event.current.mousePosition=new Vector2(Event.current.mousePosition.x+offset.x-old.Item1.x,Event.current.mousePosition.y+offset.y-old.Item1.y);offset=old.Item1;clip=old.Item2;}
 }
}

namespace UnityEngine {
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=1){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new(0,0,0);}
 public struct Matrix4x4 {public static Matrix4x4 Scale(Vector3 v)=>new();}
 public static class Screen {public static int width=1600,height=900;}
 public struct Bounds {public Vector3 center,min,max;}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponentInParent<T>()where T:Component=>gameObject.GetComponentInParent<T>();}
 public class Transform:Object {public GameObject gameObject;public Vector3 position=new(.5f,.5f,1);public Transform parent;public bool IsChildOf(Transform other){for(var p=this;p!=null;p=p.parent)if(p==other)return true;return false;}}
 public class GameObject:Object {
  public bool activeInHierarchy=true;public Transform transform;public readonly List<Component> Components=new();
  public static readonly List<GameObject> Objects=new();public static GameObject TaggedPane;public static GameObject FindWithTag(string tag)=>TaggedPane;
  public GameObject(){transform=new(){gameObject=this};Objects.Add(this);}
  public T Add<T>(T component)where T:Component{component.gameObject=this;Components.Add(component);return component;}
  public T GetComponentInParent<T>()where T:Component{for(var t=transform;t!=null;t=t.parent){var c=t.gameObject.Components.OfType<T>().FirstOrDefault();if(c!=null)return c;}return null;}
  public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault();public T GetComponentInChildren<T>(bool includeInactive)where T:Component=>GetComponentsInChildren<T>(includeInactive).FirstOrDefault();
  public static readonly List<Renderer> All=new();
  public void GetComponentsInChildren<T>(bool inactive,List<T> target)where T:Component{target.Clear();if(typeof(T)==typeof(Renderer)){foreach(var r in All)if(r.transform.IsChildOf(transform))target.Add((T)(Component)r);}else foreach(var obj in Objects)if(obj.transform.IsChildOf(transform))target.AddRange(obj.Components.OfType<T>());}
  public T[] GetComponentsInChildren<T>(bool includeInactive)where T:Component{var l=new List<T>();GetComponentsInChildren(includeInactive,l);return l.ToArray();}
 }
 public class Renderer:Component {public Bounds bounds;public bool forceRenderingOff;}
 public class SpriteRenderer:Renderer {public Color color=Color.white;}
 public class Camera:Object {public delegate void CameraCallback(Camera camera);public static CameraCallback onPreCull,onPostRender;public Vector3 WorldToViewportPoint(Vector3 v)=>v;public Vector3 WorldToScreenPoint(Vector3 v)=>new(v.x*Screen.width,v.y*Screen.height,v.z);}
 public static class ImageConversion {public static void LoadImage(Texture2D t,byte[] bytes,bool readable){t.width=BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4));t.height=BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4));}}
}
namespace UnityEngine.EventSystems {public class EventSystem {public static EventSystem current;public UnityEngine.GameObject currentSelectedGameObject;}}
public class PlayMakerFSM:UnityEngine.Component {public bool isActiveAndEnabled=true;public string ActiveStateName="Open";}
public class GameManager:UnityEngine.Object {public static GameManager instance;public bool isPaused,IsLoadingSceneTransition;public bool IsGameplayScene()=>true;public PlayMakerFSM inventoryFSM;}
public class GameCameras:UnityEngine.Object {public static GameCameras instance;public UnityEngine.Camera hudCamera;}
public class InvCharmBackboard:UnityEngine.Component {public int charmNum;public string gotCharmString="";public UnityEngine.GameObject charmObject,newOrb;}
public class CharmDisplay:UnityEngine.Component {public UnityEngine.SpriteRenderer spriteRenderer;}
public class PlayerData {public int charmSlots=11;public HashSet<int> Unowned=new();public int ThrowOwned;public bool GetBool(string key){if(key=="gotCharm_"+ThrowOwned&&ThrowOwned>0)throw new Exception("provider fault");return !key.StartsWith("gotCharm_")||!int.TryParse(key.Substring(9),out var id)||!Unowned.Contains(id);}}
namespace KO.HollowKnight8 {
 using UnityEngine;
 internal class Binding {public string Name="A";}
 internal class Action {public List<Binding> Bindings=new(){new()};}
 internal class HeroActions {public Action left=new(),menuSubmit=new(),menuCancel=new();public int Dir;public bool Submit;}
 internal class CharmSet {public List<int> Order=new();public int Used;public bool Overcharmed;public bool IsEquipped(int id)=>Order.Contains(id);public int[] Ids()=>Order.ToArray();public void Read(PlayerData d){}}
 internal class Vitals {public bool AtBench=true;}
 internal class PlayerSlot {public int Index;public bool Editable=true,Alive=true,Ready=true,Connected=true;public Vitals Vitals=new();public HeroActions Actions=new();public CharmSet Charms=new();public Color Color=Color.white;}
 internal class CoopSession {public bool Active=true;public List<PlayerSlot> Players=new();public PlayerData Data=new();public PlayerSlot Primary=>Players.FirstOrDefault();}
 internal class Local8Runtime {public bool Panel;public CoopSession Session;}
 internal static class Plugin {public static Local8Runtime Self;}
 internal static class PlayerContext {public static PlayerSlot Current;}
 internal static class InteractionRouter {public static PlayerSlot ActivePlayer;public static bool ActiveIsBench;public static string ActiveSummary=>"test";}
 internal static class ShopMenuRouting {public static PlayerSlot Buyer;public static bool MenuVisible;}
 internal static class StagMenuRouting {public static bool HasOwner;}
 internal static class PickupCard {public static PlayerSlot Owner;}
 internal static partial class Charms {
  public static string NativeProbeReason="hidden";public static Vector3 NativeAnchor;
  public static bool wasNative;public static List<(int player,int id)> Toggles=new();public static void Freeze(CoopSession s){}public static void Toggle(PlayerSlot p,int id){Toggles.Add((p.Index,id));if(p.Editable)p.Charms.Order.Remove(id);}
  public static int[] NativeCursor=new int[8];public static InvCharmBackboard[] NativeBoards=Array.Empty<InvCharmBackboard>();public static GameObject NativePane;public static bool NativeMenuOpen=true;
  public static bool CanEdit(PlayerSlot p)=>p.Editable;public static string Name(int id)=>"Gathering Swarm";public static string DescriptionOverride;public static string Description(int id)=>DescriptionOverride??"A swarm will follow the bearer and gather up any loose Geo.\n\nUseful for those who cannot leave anything behind.";
  public static Texture2D Icon(int id){var t=new Texture2D(48,48,TextureFormat.RGBA32,false){name="charm-"+id};return t;}
 }
 internal static class CharmExtensions {public static HashSet<int> Registered=new(Enumerable.Range(1,40));public static void Discover(){}public static int[] Ids()=>Registered.OrderBy(id=>id).ToArray();public static bool Owned(PlayerData d,int id)=>d.GetBool("gotCharm_"+id);public static void Register(int id)=>Registered.Add(id);public static bool Contains(int id)=>Registered.Contains(id);public static bool IsRole(int id)=>id<0;public static int Role(int i)=>-i-1;}
 internal static class UiLocalization {public static string Get(string key)=>key;}
 internal static class Diagnostics {public static List<string> Logs=new();public static int Errors;public static void Throttled(string context,Exception ex){Errors++;}public static void Write(string text)=>Logs.Add(text);}
 internal static class RoleText {public static string Language=>"EN";public static string Get(string key)=>key;}
 internal class RoleDefinition {public string Name="No role",Description="Role bonus and tradeoff.";}
 internal static class RoleCatalog {public static RoleDefinition[] All=Enumerable.Range(0,9).Select(i=>new RoleDefinition{Name="Role "+i}).ToArray();public static int VerticalNext(int id,int dx,int dy)=>dx<0?-1:Mathf.Clamp(id-dy,0,8);}
 internal class RoleState {public PlayerSlot Player;public int Cursor,Chosen,LastCharm=1,Direction;public float NextMove;public bool SubmitHeld,CancelHeld;public readonly EquippedCharmCursor Equipped=new();}
 internal static partial class RoleSystem {public static RoleState[] States=new RoleState[8];public static int LastMoved,RoleCalls;public static bool JoiningMenu;public static PlayerSlot Joining;public static bool SameRole;public static int Direction(PlayerSlot p)=>p.Actions.Dir;public static bool Submit(PlayerSlot p)=>p.Actions.Submit;public static bool Choose(PlayerSlot p,int role,bool joining){RoleCalls++;State(p).Chosen=role;return true;}public static RoleState State(PlayerSlot p){var x=States[p.Index];if(x==null||x.Player!=p)States[p.Index]=x=new(){Player=p,Chosen=p.Index%9,Cursor=p.Index%9};if(SameRole){x.Chosen=x.Cursor=0;}return x;}}
}

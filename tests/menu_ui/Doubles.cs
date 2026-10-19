using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Buffers.Binary;
using System.Text.Json;
namespace Language {public static class Language {public static string Code="EN";public static string CurrentLanguage()=>Code;}}
namespace UnityEngine {
 public class Object {public string name="";public bool Destroyed;public HideFlags hideFlags;public static implicit operator bool(Object x)=>x!=null&&!x.Destroyed;public static void Destroy(Object x){if(x!=null)x.Destroyed=true;}public static T Instantiate<T>(T x)where T:Object{if(x is GUISkin skin)return (T)(Object)skin.Copy();throw new Exception("unexpected clone");}}
 public enum HideFlags{HideAndDontSave}public enum FilterMode{Bilinear}public enum TextureWrapMode{Clamp}public enum TextureFormat{RGBA32}public enum ScaleMode{ScaleToFit}public enum FontStyle{Normal,Bold}public enum TextAnchor{MiddleLeft,MiddleCenter,UpperLeft}public enum TextClipping{Clip}public enum EventType{Repaint,MouseMove}public enum KeyCode{F8,F9,Alpha1}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);public static Color clear=>new Color(0,0,0,0);}
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
 public static class Time {public static float unscaledTime;}
 public static class Mathf {public const float PI=(float)Math.PI;public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(x,b));public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(x,b));public static float Clamp01(float x)=>Clamp(x,0,1);public static int RoundToInt(float v)=>(int)Math.Round(v,MidpointRounding.AwayFromZero);public static int CeilToInt(float v)=>(int)Math.Ceiling(v);public static int FloorToInt(float v)=>(int)Math.Floor(v);public static float Abs(float v)=>Math.Abs(v);public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Cos(float v)=>(float)Math.Cos(v);public static float Sin(float v)=>(float)Math.Sin(v);}
 public class GUIContent {public string text,tooltip;public GUIContent(string t,string tip=""){text=t;tooltip=tip;}}
 public class GUIStyleState {public Color textColor=Color.white;public Texture2D background;}
 public class GUIStyle {public int fontSize=14;public Font font;public FontStyle fontStyle;public TextAnchor alignment;public TextClipping clipping;public bool richText,wordWrap;public float fixedHeight,fixedWidth;public RectOffset padding=new RectOffset(0,0,0,0),border=new RectOffset(0,0,0,0),margin=new RectOffset(0,0,0,0);public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState(),onNormal=new GUIStyleState(),onHover=new GUIStyleState(),onActive=new GUIStyleState(),onFocused=new GUIStyleState();public static readonly GUIStyle none=new GUIStyle();public GUIStyle(){}public GUIStyle(GUIStyle s){fontSize=s.fontSize;font=s.font;fontStyle=s.fontStyle;alignment=s.alignment;clipping=s.clipping;richText=s.richText;wordWrap=s.wordWrap;padding=s.padding;border=s.border;margin=s.margin;fixedHeight=s.fixedHeight;fixedWidth=s.fixedWidth;Copy(s.normal,normal);Copy(s.hover,hover);Copy(s.active,active);Copy(s.focused,focused);Copy(s.onNormal,onNormal);Copy(s.onHover,onHover);Copy(s.onActive,onActive);Copy(s.onFocused,onFocused);}static void Copy(GUIStyleState a,GUIStyleState b){b.background=a.background;b.textColor=a.textColor;}public float CalcHeight(GUIContent c,float width){float w=Math.Max(1,width-padding.left-padding.right),len=0;foreach(char ch in c.text)len+=(ch>1000?1:.5f)*fontSize;return (float)Math.Max(1,Math.Ceiling(len/w))*fontSize*1.25f+padding.top+padding.bottom;}}
 public class GUISkin:Object {public Font font=new Font{name="Default",AllGlyphs=true};public GUIStyle label=new GUIStyle(),button=new GUIStyle(),horizontalSlider=new GUIStyle(),horizontalSliderThumb=new GUIStyle(),verticalScrollbar=new GUIStyle(),verticalScrollbarThumb=new GUIStyle(),verticalScrollbarUpButton=new GUIStyle(),verticalScrollbarDownButton=new GUIStyle();public GUISkin Copy()=>new GUISkin{font=font,label=new GUIStyle(label),button=new GUIStyle(button),horizontalSlider=new GUIStyle(horizontalSlider),horizontalSliderThumb=new GUIStyle(horizontalSliderThumb),verticalScrollbar=new GUIStyle(verticalScrollbar),verticalScrollbarThumb=new GUIStyle(verticalScrollbarThumb),verticalScrollbarUpButton=new GUIStyle(verticalScrollbarUpButton),verticalScrollbarDownButton=new GUIStyle(verticalScrollbarDownButton)};}
 public class Event {public static Event current=new Event();public EventType type=EventType.Repaint;public Vector2 mousePosition=new Vector2(-100,-100);}
 public static class ColorUtility {public static string ToHtmlStringRGB(Color c)=>$"{Mathf.RoundToInt(c.r*255):X2}{Mathf.RoundToInt(c.g*255):X2}{Mathf.RoundToInt(c.b*255):X2}";}
 public sealed class Paint {public string kind,text,tooltip,texture,font;public float[] rect,color,clip,uv;public int[] padding,border;public int size;public bool enabled,wrap;public int align;}
 public sealed class HostControl {public string Text,Tip;public Rect Rect,Clip;public bool Enabled;}
 public static class GUI {
  public static GUISkin skin=new GUISkin();public static Color color=Color.white;public static bool enabled=true;public static string tooltip="";public static bool ThrowTexture;
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
namespace InControl {
 public class BindingSource {public string Name;public BindingSource(string n){Name=n;}}
 public class PlayerAction {public string Name;public List<BindingSource> Bindings=new List<BindingSource>();public PlayerAction(string n){Name=n;Bindings.Add(new BindingSource(n+"Key"));}}
 public class InputDevice {public static readonly InputDevice Null=new InputDevice{Name="Keyboard"};public string Name="Controller";}
}
namespace KO.HollowKnight8 {
 using UnityEngine;using InControl;
 internal sealed class Setting<T> {internal T Value;internal Setting(T v){Value=v;}}
 internal sealed class Actions {internal PlayerAction up=new PlayerAction("up"),down=new PlayerAction("down"),left=new PlayerAction("left"),right=new PlayerAction("right"),jump=new PlayerAction("jump"),attack=new PlayerAction("attack"),dash=new PlayerAction("dash"),cast=new PlayerAction("cast"),focus=new PlayerAction("focus"),dreamNail=new PlayerAction("dreamNail"),superDash=new PlayerAction("superDash"),quickMap=new PlayerAction("quickMap"),paneRight=new PlayerAction("rescue");}
 internal sealed class CharmSet {internal int Used;internal bool Overcharmed;internal HashSet<int> Equipped=new HashSet<int>();internal bool IsEquipped(int i)=>Equipped.Contains(i);}
 internal sealed class PlayerSlot {internal int Index;internal string ProfileId;internal PlayerAdventureStatistics Adventure=new PlayerAdventureStatistics();internal InputDevice Device;internal bool Connected=true;internal Color Color=MenuTheme.Accent;internal Actions Actions=new Actions();internal CharmSet Charms=new CharmSet();internal string DeviceName=>Device.Name;}
 internal sealed class Data {internal int charmSlots=11;internal bool GetBool(string n)=>true;internal int GetInt(string n)=>1;}
 internal sealed class CoopSession {internal bool Active=true;internal List<PlayerSlot> Players=new List<PlayerSlot>();internal Data Data=new Data();internal bool CanJoin=true;internal PlayerSlot Primary=>Players.FirstOrDefault();internal int JoinCalls,GatherCalls,RepairCalls,RemoveCalls,AssignCalls;internal void Join(InputDevice d){JoinCalls++;Players.Add(new PlayerSlot{Index=Players.Count,Device=d});}internal void AssignPrimary(InputDevice d){AssignCalls++;Primary.Device=d;}internal void AssignPrimaryAutomatic(){AssignCalls++;}internal void JoinFirstAvailable(){JoinCalls++;}internal void GatherNow(){GatherCalls++;}internal void RepairPlayers(){RepairCalls++;}internal void RemoveLast(){RemoveCalls++;Players.RemoveAt(Players.Count-1);}}
 internal sealed class Local8Runtime {internal Setting<string> PrimaryDevice=new Setting<string>("Auto");internal PerformanceConfiguration Performance=new PerformanceConfiguration();internal Setting<bool> Enabled=new Setting<bool>(true);internal int Exports;internal void ExportSettings(){Exports++;}internal CoopSession Session=new CoopSession();internal bool Panel=true;internal Setting<bool> ShowLabels=new Setting<bool>(true),BasicHud=new Setting<bool>(false),ZoomByPlayerCount=new Setting<bool>(true),GroupDarkness=new Setting<bool>(false),WaitForParty=new Setting<bool>(true),TimedRespawn=new Setting<bool>(false),SpawnShades=new Setting<bool>(false),PerformanceMode=new Setting<bool>(false),PlayerLights=new Setting<bool>(true),SmoothPlayerLights=new Setting<bool>(true),ReduceHitEffects=new Setting<bool>(true);internal Setting<float> HudScale=new Setting<float>(1),SideMargin=new Setting<float>(10),GatherDistance=new Setting<float>(28),RespawnSeconds=new Setting<float>(15);internal Setting<int> Difficulty=new Setting<int>(2);internal float MessageUntil;internal string Message="";internal void SetPanel(bool v){Panel=v;}internal void Notice(string m){Message=m;MessageUntil=10;}}
 internal sealed class Local8Settings {internal string[] Colors=new string[8];internal int RescueButton,PvpMode,PvpNailDamage=1,PvpSpellDamage=1,PvpArtDamage=1,PvpCharmDamage=1,DuelSoul=99,DuelSeconds=0,DuelBestOf=0,PvpSoulRegeneration=11;internal bool PvpParry=true,PvpCharmAttacks=true,PvpSoulOnHit=true,PvpSpellsOnly;internal int[] PvpTeams=new int[8];}
 internal static class PvpSpellOnly {internal static int Rate(int value)=>Math.Max(1,Math.Min(99,value));}
 internal static class Local8Mod {internal static Local8Settings Settings=new Local8Settings();}
 internal static class Plugin {internal static Local8Runtime Self;}
 internal static class PlayerProfiles {
  internal static PlayerProfileData[] All=Array.Empty<PlayerProfileData>();internal static int Loads,Saves,Detaches;internal static bool LoadAllowed=true;
  internal static PlayerProfileData Find(string id)=>All.FirstOrDefault(p=>p.Id==id);
  internal static string Display(PlayerSlot p)=>Find(p.ProfileId)?.Name??("P"+(p.Index+1));internal static PlayerAdventureStatistics Stats(PlayerSlot p)=>p.Adventure;
  internal static bool Busy(PlayerProfileData p,CoopSession s,PlayerSlot except)=>s.Players.Any(q=>q!=except&&q.ProfileId==p.Id);
  internal static bool CanLoad(PlayerSlot p,CoopSession s)=>LoadAllowed;
  internal static bool Load(PlayerSlot p,PlayerProfileData saved){Loads++;p.ProfileId=saved.Id;return true;}
  internal static bool Save(PlayerSlot p,string name){Saves++;var profile=Find(p.ProfileId);if(profile==null){profile=new PlayerProfileData{Id="fixture-"+Saves};All=All.Concat(new[]{profile}).ToArray();p.ProfileId=profile.Id;}profile.Name=name;return true;}
  internal static void Detach(PlayerSlot p){Detaches++;p.ProfileId=null;}
 }
 internal static class Controls {internal static InputDevice[] Available=Array.Empty<InputDevice>();internal static int VirtualCount;internal static bool NativeActive;internal static IEnumerable<InputDevice> Devices()=>Available;internal static bool IsKeyboard(InputDevice d)=>d==InputDevice.Null;internal static string Label(InputDevice d)=>d.Name;internal static void EnableDirectInput(bool requested){NativeActive=true;}internal static void Dispose(PlayerSlot p){}internal static void InitSecondary(PlayerSlot p,Actions a,InputDevice d){p.Actions=new Actions();}}
 internal static class ActionKeys {internal static int Capturing;internal static string RescueLabel="1",PvpLabel="F6",ButtonLabel="RB";}
 internal static class MenuInput {internal static bool Waiting;internal static KeyCode Key=KeyCode.F8;internal static string Label=>Key.ToString();internal static void Set(KeyCode k){Key=k;Waiting=false;}}
 internal static class SkinBridge {internal static string Display(PlayerSlot p)=>"Knight";internal static void Cycle(PlayerSlot p,int n){}}
 internal static class Charms {internal static bool Editing;internal static int Selected,Cursor=1;internal static bool ThrowIcon;internal static string SanitizeLocalized(string k,string s,string f)=>RoleText.Has("ui."+k)?UiLocalization.Get(k):f;internal static void Open(PlayerSlot p){Editing=true;Selected=p.Index;}internal static void Select(PlayerSlot p){Selected=Hud.selected=p.Index;}internal static void Close(){Editing=false;}internal static bool Bench=true;internal static bool CanEdit(PlayerSlot p)=>Bench;internal static Texture2D Icon(int i){if(ThrowIcon)throw new Exception("controlled charm failure");return null;}internal static void Toggle(PlayerSlot p,int i){if(p.Charms.Equipped.Contains(i))p.Charms.Equipped.Remove(i);else p.Charms.Equipped.Add(i);}internal static string Name(int i)=>"Charm "+i;internal static string Description(int i)=>"Native charm description";}
 internal static class CharmExtensions {internal static int[] Ids()=>Enumerable.Range(1,40).ToArray();}
 internal static class HudAssets {internal static bool Ready=true;}
 internal static class Diagnostics {internal static void Write(string s){}internal static void Throttled(string s,Exception e){}internal static void OpenFolder(){}internal static void CopyFolder(){}}
 internal static class NativeRenderBudget {internal static void Reset(){}}
 internal static class PvpCombat {internal static int[] Wins=new int[8],Kills=new int[8],Deaths=new int[8],Parries=new int[8];internal static void Invalidate(){}internal static void ResetScores(){}}
 internal static class PvpMatch {internal static bool Running;internal static string StartIssue="";internal static int Starts,Stops;internal static void SetMode(int n){Local8Mod.Settings.PvpMode=n;}internal static void Stop(bool restorePosition){Running=false;Stops++;}internal static void RequestStart(){Starts++;}}
 internal static class PvpRules {internal static int SeriesLength(int n)=>Math.Max(0,n);}
 internal sealed class PvpArenaOptions {internal int Arena,Masks,BlueMasks,SoulCapacity=99,Soul=99,Reserve,DashCooldown=-1,HazardMasks=1,SpellLevel=-1;internal bool AllowCharms=true,InfiniteSoul,InfiniteWings,InfiniteDash,AllAbilities,AllowHealing=true;internal int[] PlayerMasks=new int[8];internal void Normalize(){}}
 internal static class PvpArenaCatalog {internal static string[] Keys={"here","flat","large","spikes","void","markoth","xero","noeyes","mantis","gorb","markothfloor"};}
 internal static class PvpArena {internal static PvpArenaOptions Options=new PvpArenaOptions();internal static bool Active;internal static string Text(string key)=>RoleText.Get("arena."+key);internal static void SaveOptions(){}internal static void Back(){Active=false;}internal static void StartRequest(){Active=true;}}
}

public enum ShaderQualities {Low,Medium,High}
public class GameSettings {public int particleEffectsLevel=1;public ShaderQualities shaderQuality=ShaderQualities.Medium;}
public class GameManager:UnityEngine.Object {public static GameManager instance;public GameSettings gameSettings=new GameSettings();public int Refreshes;public void RefreshParticleSystems(){Refreshes++;}}
namespace On {public static class GameSettings {public delegate void orig_SaveVideoSettings(global::GameSettings s);public delegate void hook_SaveVideoSettings(orig_SaveVideoSettings orig,global::GameSettings s);public static event hook_SaveVideoSettings SaveVideoSettings;}}

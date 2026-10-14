using System;using System.Collections.Generic;using System.Linq;
namespace UnityEngine {public static class Time {public static int frameCount;} public class Sprite {public static implicit operator bool(Sprite s)=>s!=null;} }
namespace Modding {public static class ModHooks {public delegate bool SetBoolProxy(string name,bool value);public static event SetBoolProxy SetPlayerBoolHook;public static bool Set(string name,bool value){if(SetPlayerBoolHook!=null)foreach(SetBoolProxy hook in SetPlayerBoolHook.GetInvocationList())value=hook(name,value);return value;}}}
public class CharmIconList {public static CharmIconList Instance;public UnityEngine.Sprite[] spriteList;public static implicit operator bool(CharmIconList s)=>s!=null;}
public class PlayerData {
public bool equippedCharm_1;
public bool equippedCharm_2;
public bool equippedCharm_3;
public bool equippedCharm_4;
public bool equippedCharm_5;
public bool equippedCharm_6;
public bool equippedCharm_7;
public bool equippedCharm_8;
public bool equippedCharm_9;
public bool equippedCharm_10;
public bool equippedCharm_11;
public bool equippedCharm_12;
public bool equippedCharm_13;
public bool equippedCharm_14;
public bool equippedCharm_15;
public bool equippedCharm_16;
public bool equippedCharm_17;
public bool equippedCharm_18;
public bool equippedCharm_19;
public bool equippedCharm_20;
public bool equippedCharm_21;
public bool equippedCharm_22;
public bool equippedCharm_23;
public bool equippedCharm_24;
public bool equippedCharm_25;
public bool equippedCharm_26;
public bool equippedCharm_27;
public bool equippedCharm_28;
public bool equippedCharm_29;
public bool equippedCharm_30;
public bool equippedCharm_31;
public bool equippedCharm_32;
public bool equippedCharm_33;
public bool equippedCharm_34;
public bool equippedCharm_35;
public bool equippedCharm_36;
public bool equippedCharm_37;
public bool equippedCharm_38;
public bool equippedCharm_39;
public bool equippedCharm_40;
 public readonly HashSet<int> Owned=new();public readonly Dictionary<int,bool> Provider=new();public readonly Dictionary<int,int> Costs=new();
 public List<int> equippedCharms=new();public int charmSlots=11,charmSlotsFilled,joniHealthBlue,healthBlue;public bool canOvercharm,overcharmed;public int Writes,Reads,MissingProviderCalls;
 public bool GetBool(string key){if(key.StartsWith("gotCharm_"))return Owned.Contains(int.Parse(key.Substring(9)));if(key.StartsWith("brokenCharm_"))return false;if(key.StartsWith("equippedCharm_")){int id=int.Parse(key.Substring(14));if(id<=40)return (bool)GetType().GetField(key).GetValue(this);Reads++;if(!Provider.ContainsKey(id))MissingProviderCalls++;return Provider.TryGetValue(id,out bool value)&&value;}return false;}
 public void SetBool(string key,bool value){value=Modding.ModHooks.Set(key,value);int id=int.Parse(key.Substring(14));if(id<=40)GetType().GetField(key).SetValue(this,value);else {if(!Provider.ContainsKey(id))MissingProviderCalls++;Provider[id]=value;Writes++;}}
 public int Royal;public int GetInt(string key)=>key=="royalCharmState"?Royal:Costs.TryGetValue(int.Parse(key.Substring(10)),out int cost)?cost:1;
 public void CalculateNotchesUsed(){charmSlotsFilled=equippedCharms.Sum(id=>GetBool("equippedCharm_"+id)?Math.Max(0,GetInt("charmCost_"+id)):0);}
}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {internal int Index;internal CharmLoadout Charms=new();internal Hero Hero;internal bool Editable=true,Ready=true,Alive=true;internal void Capture(PlayerData d)=>Charms.Read(d);internal void Apply(PlayerData d)=>Charms.Write(d);}
 internal class Hero {public static implicit operator bool(Hero h)=>h!=null;internal PlayerData Data;internal int Updates;internal void CharmUpdate(){Data.CalculateNotchesUsed();Updates++;}}
 internal class CoopSession {internal bool Active=true;internal PlayerData Data;internal PlayerSlot Primary;internal List<PlayerSlot> Players=new();}
 internal class Runtime {internal CoopSession Session;internal int Notices;internal void Notice(string s){Notices++;}}
 internal static class Plugin {internal static Runtime Self;}
 internal static class Diagnostics {internal static void Write(string s){} }
 internal static class SummonRouting {internal static void Refresh(PlayerSlot p){} }
 internal static class PvpCharms {internal static bool Active,Allowed;}
 internal sealed class PlayerContext:IDisposable {
  internal static PlayerSlot Current;readonly CoopSession s;readonly PlayerSlot p,old;
  internal static PlayerContext Enter(PlayerSlot p)=>new(p);
  PlayerContext(PlayerSlot p){s=Plugin.Self.Session;this.p=p;old=Current??s.Primary;old.Capture(s.Data);Current=p;p.Apply(s.Data);}
  public void Dispose(){p.Capture(s.Data);Current=old==s.Primary?null:old;old.Apply(s.Data);}
 }
 internal static partial class Charms {internal static int Saves;internal static bool CanEdit(PlayerSlot p)=>p.Editable;internal static void Save(CoopSession s){Saves++;} }
}

namespace KO.HollowKnight8 {internal static class UiLocalization {internal static string Get(string key)=>key;}}
public static class BossSequenceController {public static bool BoundCharms;}

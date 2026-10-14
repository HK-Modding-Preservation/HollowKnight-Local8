using System;
using System.Collections.Generic;
namespace UnityEngine {
 public class Object {public bool destroyed;public static implicit operator bool(Object o)=>o!=null&&!o.destroyed;}
 public class Transform {public Vector3 position;}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y=0,float z=0){this.x=x;this.y=y;this.z=z;}}
 public static class Time {public static float time,unscaledTime;}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static int CeilToInt(float n)=>(int)Math.Ceiling(n);}
}
public class HeroController:UnityEngine.Object {public UnityEngine.Transform transform=new();public State cState=new();public sealed class State {public bool dead,transitioning;}}
namespace KO.HollowKnight8 {
 internal static class AdventureStats {internal static void Down(PlayerSlot p) {}}
 internal class Vitals {internal int Health=5,Blue;}
 internal class PlayerSlot {
  internal int Index;internal HeroController Hero=new();internal bool Down,Hazard,Ready=true,Connected=true,Retiring,SpawnPending,SpawnAsDown,Faulted;
  internal Vitals Vitals=new();internal float DownAt,NextTimedRespawnAttempt,HazardUntil,RetryAt,LifeStartedAt;internal UnityEngine.Vector3 LastHazardPosition;
  internal bool Alive=>Hero&&!Down&&!Hazard&&!Retiring;
 }
 internal class Setting<T> {internal T Value;internal Setting(T value){Value=value;}}
 internal class Local8Runtime {internal Setting<bool> TimedRespawn=new(true);internal Setting<float> RespawnSeconds=new(15);internal List<string> Notices=new();internal void Notice(string n)=>Notices.Add(n);}
 internal partial class CoopSession {
  internal bool Active=true,Gameplay=true,TeamWipe,ThrowOnHide;
  internal List<PlayerSlot> Players=new();internal Local8Runtime plugin=new();internal readonly List<int> Searches=new(),Restored=new();
  internal readonly HashSet<int> Unsafe=new(),Refused=new();internal int HazardRecoveries,DeathCommits,HideCalls;
  internal void Commit(PlayerSlot p){DeathCommits++;}
  private void Hide(PlayerSlot p){HideCalls++;if(ThrowOnHide)throw new InvalidOperationException("hide");}
  internal bool NearAlly(PlayerSlot ally,PlayerSlot arriving,out UnityEngine.Vector3 at){Searches.Add(ally.Index);at=new(ally.Index+100);return !Unsafe.Contains(ally.Index);}
  internal void Recover(PlayerSlot p,bool respawn,UnityEngine.Vector3? reviveAt=null){
   if(!respawn){HazardRecoveries++;p.Hazard=p.Down=false;return;}
   int target=reviveAt.HasValue?(int)reviveAt.Value.x-100:-1;Restored.Add(target);
   if(Refused.Contains(target))return;
   p.Down=p.Hazard=false;p.Vitals.Health=3;p.Hero.cState.dead=false;
  }
 }
 internal static class PvpMatch {internal static bool Running;}
 internal static class PvpArena {internal static bool Active;}
 internal static class EmergencyWarp {internal static readonly HashSet<PlayerSlot> Trips=new();internal static bool Active(PlayerSlot p)=>Trips.Contains(p);internal static void Cancel(PlayerSlot p)=>Trips.Remove(p);}
 internal static class TransitionVote {internal static readonly HashSet<PlayerSlot> Entrants=new();internal static bool Holding(PlayerSlot p)=>Entrants.Contains(p);}
 internal static class RadianceAscentCheckpoint {
  internal static bool Refuse;internal static UnityEngine.Vector3? Highest;
  internal static bool BeforeRecovery(CoopSession s,PlayerSlot p,ref UnityEngine.Vector3? at){if(Refuse)return false;if(Highest.HasValue)at=Highest;return true;}
 }
 internal static class CrystalDashCoop {internal static void StopPlayer(PlayerSlot p){}}
 internal static class BenchSeats {internal static void Leave(PlayerSlot p){}}
 internal static class Revival {internal static void End(PlayerSlot p){}}
 internal static class BenchPoseRecovery {internal static void Downed(PlayerSlot p){}}
 internal static class ArenaVisualRecovery {internal static void Downed(PlayerSlot p){}}
 internal static class PvpCombat {internal static int Deaths;internal static void Down(PlayerSlot p){Deaths++;}}
 internal static class CoopShades {internal static int Spawned;internal static void Spawn(CoopSession s,PlayerSlot p){Spawned++;}}
 internal static class DreamSequence {internal static bool InStoryDream;}
 internal static class Diagnostics {internal static List<string> Messages=new();internal static void Write(string s)=>Messages.Add(s);}
 internal static class MenuInput {internal static string Label=>"F8";}
 internal static class CoopEnding {internal static bool NeedsFocus(PlayerSlot p)=>false;}
 internal static class UiLocalization {internal static string Get(string key)=>key;}
 internal static class Charms {internal static string SanitizeLocalized(string key,string sheet,string fallback)=>key;}
}

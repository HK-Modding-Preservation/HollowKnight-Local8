using System;using System.Collections.Generic;using System.Linq;using UnityEngine;using UnityEngine.Audio;
namespace UnityEngine {
 public class Object {public string name;public bool Exists=true;public static readonly List<Object> All=new();public Object()=>All.Add(this);public static implicit operator bool(Object o)=>!ReferenceEquals(o,null)&&o.Exists;public static bool operator ==(Object a,Object b)=>ReferenceEquals(a,b);public static bool operator !=(Object a,Object b)=>!ReferenceEquals(a,b);public override bool Equals(object o)=>ReferenceEquals(this,o);public override int GetHashCode()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);public static T[] FindObjectsOfType<T>()where T:Object=>All.OfType<T>().Where(x=>x).ToArray();}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.Components.OfType<T>().FirstOrDefault(x=>x);}
 public class GameObject:Object {public bool activeInHierarchy=true;public readonly List<Component> Components=new();public Transform transform;public GameObject(string name){this.name=name;transform=new(){gameObject=this};}public T Add<T>()where T:Component,new(){var c=new T{gameObject=this,name=name};Components.Add(c);return c;}}
 public class Transform:Component {public Vector3 position;}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static implicit operator Vector2(Vector3 v)=>new(v.x,v.y);public static float Distance(Vector2 a,Vector2 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));}
 public class AudioClip:Object {public int samples=441000;}
 public class AudioSource:Component {public bool isActiveAndEnabled=true,isPlaying=true;public AudioClip clip;public float volume=1;public int timeSamples=1000,Plays;public void Play(){isPlaying=true;Plays++;}public void Stop()=>isPlaying=false;}
 public static class Time {public static float unscaledTime=100;}
 public static class Application {public static bool isFocused=true;}
}
namespace UnityEngine.Audio {public class AudioMixer:UnityEngine.Object {public AudioMixerSnapshot Current;}public class AudioMixerSnapshot:UnityEngine.Object {public AudioMixer audioMixer{get;set;}public int Transitions;public float LastDuration;public void TransitionTo(float time){Transitions++;LastDuration=time;if(audioMixer)audioMixer.Current=this;}}}
namespace UnityEngine.SceneManagement {public static class SceneManager {public static string Room="Fungus1_04";public static Scene GetActiveScene()=>new(){name=Room};}public struct Scene {public string name;}}
public enum MusicChannels {Main,MainAlt,Action,Sub,Tension,Extra}
public class PlayerData {}
public class MusicCue:UnityEngine.Object {public string OriginalMusicEventName="BossHornet";public AudioMixerSnapshot Snapshot;public MusicCue Alternative;public MusicCue ResolveAlternatives(PlayerData data)=>Alternative??this;public readonly MusicChannelInfo[] Channels=new MusicChannelInfo[6];public MusicChannelInfo GetChannelInfo(MusicChannels ch)=>Channels[(int)ch];public class MusicChannelInfo {public AudioClip Clip;}}
public class AudioManager:UnityEngine.Object {private readonly AudioSource[] musicSources;public AudioSource[] Sources=>musicSources;public MusicCue CurrentMusicCue{get;set;}public int CueCalls,SnapshotCalls;public AudioManager(AudioSource[] sources)=>musicSources=sources;public void NativeCue(MusicCue cue){CueCalls++;CurrentMusicCue=cue.ResolveAlternatives(GameManager.instance.playerData);for(int i=0;i<musicSources.Length;i++){var s=musicSources[i];s.clip=CurrentMusicCue.GetChannelInfo((MusicChannels)i)?.Clip;s.isPlaying=s.clip;s.volume=1;}}public void ApplyMusicSnapshot(AudioMixerSnapshot snapshot,float delay,float transition)=>On.AudioManager.Mix(this,snapshot,delay,transition);}
public class GameManager:UnityEngine.Object {public static GameManager instance;public AudioManager AudioManager;public MusicCue noMusicCue;public AudioMixerSnapshot noMusicSnapshot;public PlayerData playerData=new();public bool isPaused,IsLoadingSceneTransition,Gameplay=true;public bool IsGameplayScene()=>Gameplay;}
public class HeroController:Component {}
public class HealthManager:Component {public int hp=100;public bool Dead,BossFlag=true;public bool GetIsDead()=>Dead;}
public static class BossSceneController {public static bool IsBossScene;}
namespace HutongGames.PlayMaker {public class FsmObject {public UnityEngine.Object Value{get;set;}}public class FsmFloat {public float Value{get;set;}}public class FsmStateAction {public int Finishes;public void Finish()=>Finishes++;}}
namespace HutongGames.PlayMaker.Actions {public class TransitionToAudioSnapshot:HutongGames.PlayMaker.FsmStateAction {public HutongGames.PlayMaker.FsmObject snapshot;public HutongGames.PlayMaker.FsmFloat transitionTime=new(){Value=1};public void OnEnter()=>On.HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot.Enter(this);public void Native_OnEnter()=>throw new NotImplementedException();}}
namespace On {
 public static class AudioManager {
  public delegate void orig_ApplyMusicCue(global::AudioManager a,MusicCue cue,float delay,float transition,bool snapshot);
  public delegate void orig_ApplyMusicSnapshot(global::AudioManager a,AudioMixerSnapshot snapshot,float delay,float transition);public delegate void hook_ApplyMusicSnapshot(orig_ApplyMusicSnapshot orig,global::AudioManager a,AudioMixerSnapshot snapshot,float delay,float transition);public static event hook_ApplyMusicSnapshot ApplyMusicSnapshot;
  public static int Count=>ApplyMusicSnapshot?.GetInvocationList().Length??0;public static void Mix(global::AudioManager a,AudioMixerSnapshot s,float d,float t){orig_ApplyMusicSnapshot next=(a,s,d,t)=>{a.SnapshotCalls++;s?.TransitionTo(t);};if(ApplyMusicSnapshot!=null)foreach(hook_ApplyMusicSnapshot h in ApplyMusicSnapshot.GetInvocationList()){var old=next;next=(a,s,d,t)=>h(old,a,s,d,t);}next(a,s,d,t);}
 }
}
namespace On.HutongGames.PlayMaker.Actions {public static class TransitionToAudioSnapshot {public delegate void orig_OnEnter(global::HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot action);public delegate void hook_OnEnter(orig_OnEnter orig,global::HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot action);public static event hook_OnEnter OnEnter;public static int Count=>OnEnter?.GetInvocationList().Length??0;public static void Enter(global::HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot a){orig_OnEnter next=a=>a.Native_OnEnter();if(OnEnter!=null)foreach(hook_OnEnter h in OnEnter.GetInvocationList()){var old=next;next=a=>h(old,a);}next(a);}}}
namespace KO.HollowKnight8 {
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal bool Alive=true,Ready=true;}
 internal class CoopSession {internal bool Active=true,TeamWipe,AllowVanillaDeath;internal PlayerData Data=new();internal readonly List<PlayerSlot> Players=new();}
 internal class Runtime:UnityEngine.Object {internal CoopSession Session;}
 internal static class Plugin {internal static Runtime Self;}
 internal class LocalShade:Component {}
 internal static class ArenaGather {internal static readonly HashSet<HealthManager> engagedBosses=new();internal static HealthManager encounterTarget;internal static bool Boss(HealthManager boss)=>boss.BossFlag;}
 internal static class PartySoul {internal static bool IndividualDeathAudio;}
 internal static class PvpArena {internal static bool KeepsMusic;}
 internal static class Diagnostics {internal static readonly List<string> Messages=new();internal static void Write(string text)=>Messages.Add(text);internal static void Throttled(string text,Exception e)=>Messages.Add(text+": "+e.Message);}
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine {
 public class Object {public bool Exists=true;public static implicit operator bool(Object o)=>o!=null&&o.Exists;}
 public class Transform {public Vector3 position;}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y=0,float z=0){this.x=x;this.y=y;this.z=z;}}
 public class GameObject:Object {public string name;public bool activeInHierarchy=true;public Transform transform=new();public GameObject(string name)=>this.name=name;}
 public static class Time {public static float time,unscaledTime;}
 public static class Mathf {public static float Abs(float value)=>Math.Abs(value);}
}
public class HeroController:UnityEngine.Object {public bool controlReqlinquished;}
public class GameManager:UnityEngine.Object {public static GameManager instance=new();public string sceneName="Fungus1_04";public bool isPaused,IsLoadingSceneTransition;}
namespace HutongGames.PlayMaker {
 public class FsmFloat {public string Name;public bool UseVariable;public float Value {get;set;}}
 public class FsmEvent {public string Name;public static FsmEvent GetFsmEvent(string name)=>new(){Name=name};}
 public class FsmTransition {public string EventName,ToState;}
 public class FsmStateAction {public Fsm Fsm {get;set;}public bool Enabled=true,Finished;public void Finish()=>Finished=true;}
 public class FsmState {public Fsm Fsm;public string Name;public FsmStateAction[] Actions=Array.Empty<FsmStateAction>();public FsmTransition[] Transitions=Array.Empty<FsmTransition>();}
 public class Fsm {
  public string Name;public UnityEngine.GameObject GameObject;public FsmState ActiveState;public string ActiveStateName=>ActiveState?.Name;
  public List<FsmState> States=new();public Action<string> Delivered;public readonly List<string> Events=new();
  public FsmState GetState(string name)=>States.FirstOrDefault(s=>s.Name==name);
  public void Event(FsmEvent evt){if(evt==null)return;Events.Add(evt.Name);var t=ActiveState.Transitions.FirstOrDefault(t=>t.EventName==evt.Name);if(t!=null)ActiveState=GetState(t.ToState);Delivered?.Invoke(evt.Name);}
 }
}
namespace HutongGames.PlayMaker.Actions {
 public class GetPosition:HutongGames.PlayMaker.FsmStateAction {}
 public class FloatCompare:HutongGames.PlayMaker.FsmStateAction {
  public HutongGames.PlayMaker.FsmFloat float1=new(){Name="X Pos",UseVariable=true},float2=new(){Value=27.3f},tolerance=new();
  public HutongGames.PlayMaker.FsmEvent equal,lessThan,greaterThan;public bool everyFrame;
  public void OnEnter()=>Native_OnEnter();public void DoCompare()=>Native_DoCompare();
  public void Native_OnEnter()=>throw new NotImplementedException();public void Native_DoCompare()=>throw new NotImplementedException();
 }
}
namespace KO.HollowKnight8 {
 internal class Data {internal bool hornet1Defeated,hasDash;}
 internal class PlayerSlot {internal int Index;internal HeroController Hero=new();internal bool Ready=true,Alive=true,Connected=true,Retiring;}
 internal class CoopSession {internal bool Active=true,Gameplay=true,TeamWipe;internal Data Data=new();internal List<PlayerSlot> Players=new();}
 internal class Runtime:UnityEngine.Object {internal CoopSession Session;}
 internal static class Plugin {internal static Runtime Self;}
 internal static class Diagnostics {internal static List<string> Lines=new();internal static void Write(string text)=>Lines.Add(text);}
}

namespace KO.HollowKnight8 {internal static class ArenaGather {internal static readonly List<string> Entries=new();internal static void Encounter(HutongGames.PlayMaker.Fsm f,PlayerSlot p)=>Entries.Add(f.ActiveStateName);}}

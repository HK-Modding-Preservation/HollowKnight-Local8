using System;using System.Collections.Generic;
namespace UnityEngine {
 public class Object {
  static int next;public readonly int Id=++next;public bool Alive=true;
  public static implicit operator bool(Object o)=>!ReferenceEquals(o,null)&&o.Alive;
 }
 public class GameObject:Object {
  public string name;public readonly Transform transform;
  public GameObject(string name,Transform parent=null){this.name=name;transform=new(){gameObject=this,parent=parent};parent?.Children.Add(transform);}
 }
 public class Transform:Object{
  public GameObject gameObject;public Transform parent;public readonly List<Transform>Children=new();public int childCount=>Children.Count;public Transform GetChild(int i)=>Children[i];
 }
 public static class Time{public static float unscaledTime,timeScale=1;public static int frameCount;}
}
public class PlayMakerFSM:UnityEngine.Object{
 public static List<PlayMakerFSM> FsmList{get;set;}=new();
 public static long ObjectReads;private UnityEngine.GameObject root;
 public UnityEngine.GameObject gameObject{get{ObjectReads++;return root;}set{root=value;}}
 private HutongGames.PlayMaker.Fsm fsm;
 public HutongGames.PlayMaker.Fsm Fsm{get{if(fsm!=null)fsm.Owner=this;return fsm;}set{fsm=value;}}
}
namespace HutongGames.PlayMaker{
 public class FsmEvent{public string Name;}
 public class FsmEventData{public Fsm SentByFsm;public UnityEngine.GameObject SentByGameObject;}
 public class Fsm{public string Name;public PlayMakerFSM Owner;public Action<Fsm,FsmEvent,FsmEventData> Receive;public void ProcessEvent(FsmEvent evt,FsmEventData data)=>Receive?.Invoke(this,evt,data);}
}
namespace On.HutongGames.PlayMaker{
 public static class Fsm{
  public delegate void orig_BroadcastEventToGameObject_GameObject_FsmEvent_FsmEventData_bool_bool(global::HutongGames.PlayMaker.Fsm f,UnityEngine.GameObject g,global::HutongGames.PlayMaker.FsmEvent e,global::HutongGames.PlayMaker.FsmEventData d,bool children,bool exclude);
  public delegate void hook_Object(orig_BroadcastEventToGameObject_GameObject_FsmEvent_FsmEventData_bool_bool orig,global::HutongGames.PlayMaker.Fsm f,UnityEngine.GameObject g,global::HutongGames.PlayMaker.FsmEvent e,global::HutongGames.PlayMaker.FsmEventData d,bool children,bool exclude);
  public static event hook_Object BroadcastEventToGameObject_GameObject_FsmEvent_FsmEventData_bool_bool;
  public delegate void orig_BroadcastEvent_FsmEvent_bool(global::HutongGames.PlayMaker.Fsm f,global::HutongGames.PlayMaker.FsmEvent e,bool exclude);
  public delegate void hook_Global(orig_BroadcastEvent_FsmEvent_bool orig,global::HutongGames.PlayMaker.Fsm f,global::HutongGames.PlayMaker.FsmEvent e,bool exclude);
  public static event hook_Global BroadcastEvent_FsmEvent_bool;
 }
}
namespace KO.HollowKnight8{
 internal class CoopSession{internal bool Active=true;}
 internal class Runtime:UnityEngine.Object{internal CoopSession Session=new();}
 internal static class Plugin{internal static Runtime Self=new();}
 internal class PlayerSlot{internal int Index;}
 internal static class ReferenceField{internal static int Compiled=4,Fallback;}
 internal static class Diagnostics{internal static readonly List<string>Messages=new();internal static void Write(string s)=>Messages.Add(s);internal static void Throttled(string s,Exception e)=>Messages.Add(s+e);}
}

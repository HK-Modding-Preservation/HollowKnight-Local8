using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using HutongGames.PlayMaker;
namespace UnityEngine {
 public class Object {public string name {get;set;}="Object";public bool Exists=true;public static implicit operator bool(Object o)=>!ReferenceEquals(o,null)&&o.Exists;public static bool operator ==(Object a,Object b)=>ReferenceEquals(a,b);public static bool operator !=(Object a,Object b)=>!ReferenceEquals(a,b);public override bool Equals(object o)=>ReferenceEquals(this,o);public override int GetHashCode()=>base.GetHashCode();}
 public class Component:Object {public GameObject gameObject {get;set;}public Transform transform=>gameObject.transform;public T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();public T[] GetComponents<T>()where T:Component=>gameObject.GetComponents<T>();public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>gameObject.GetComponentsInChildren<T>(inactive);}
 public class GameObject:Object {public bool activeInHierarchy=true;public Transform transform {get;private set;}public GameObject(){transform=new(){gameObject=this};}public readonly List<Component> Components=new();public T Add<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}public T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault(c=>c);public T[] GetComponents<T>()where T:Component=>Components.OfType<T>().Where(c=>c).ToArray();public T[] GetComponentsInChildren<T>(bool inactive)where T:Component=>GetComponents<T>().Concat(transform.Children.Values.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>(inactive))).ToArray();}
 public class Transform:Component {public Vector3 position;public int childCount=>Children.Count;public Transform GetChild(int i)=>Children.Values.ElementAt(i);public readonly Dictionary<string,Transform> Children=new();public Transform Find(string child)=>Children.TryGetValue(child,out var value)?value:null;}
 public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z=0){this.x=x;this.y=y;this.z=z;}public static float Distance(Vector3 a,Vector3 b)=>(float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));}
 public struct Bounds {public Vector3 center,size;public bool IntersectsXY(Bounds b)=>Math.Abs(center.x-b.center.x)<=(size.x+b.size.x)/2&&Math.Abs(center.y-b.center.y)<=(size.y+b.size.y)/2;public bool Intersects(Bounds b)=>IntersectsXY(b)&&Math.Abs(center.z-b.center.z)<=(size.z+b.size.z)/2;}
 public struct ColliderDistance2D {public bool isOverlapped;}
 public class Collider2D:Component {public bool enabled=true;public Vector3 Size=new(1,2);public Bounds bounds=>new(){center=transform.position,size=Size};public ColliderDistance2D Distance(Collider2D b)=>new(){isOverlapped=bounds.IntersectsXY(b.bounds)};}
 public static class Mathf {public static float Abs(float f)=>Math.Abs(f);}
}
namespace HutongGames.PlayMaker {
 public class FsmFloat {public float Value;public bool UseVariable;public string Name;}
 public class FsmGameObject {public GameObject Value;}
 public class FsmObject {public UnityEngine.Object Value;}
 public class FsmBool {public bool Value {get;set;}public bool UseVariable;public string Name;}
 public class FsmOwnerDefault {public bool UseOwner=true;public FsmGameObject GameObject=new();}
 public class FsmEvent {public string Name;public FsmEvent(string name)=>Name=name;}
 public class FsmStateAction {public Fsm Fsm {get;set;}public bool Finished;public void Finish(){Finished=true;}public bool Enabled=true;public GameObject Owner=>Fsm?.GameObject;}
 public class FsmState {public string Name="Closed";public FsmStateAction[] Actions=Array.Empty<FsmStateAction>();}
 public class Fsm {public string Name="Blocker Control";public GameObject GameObject;public FsmState ActiveState=new();public FsmEvent LastEvent;public void Event(FsmEvent e){LastEvent=e;}public GameObject GetOwnerDefaultTarget(FsmOwnerDefault source)=>source.UseOwner?GameObject:source.GameObject.Value;}
}
namespace HutongGames.PlayMaker.Actions {
 public class GetDistance:FsmStateAction {public FsmOwnerDefault gameObject=new();public FsmGameObject target=new();public FsmFloat storeResult=new();public int NativeCalls;public void Run()=>On.HutongGames.PlayMaker.Actions.GetDistance.Run_DoGetDistance(this);public void Native(){NativeCalls++;storeResult.Value=Vector3.Distance(Fsm.GetOwnerDefaultTarget(gameObject).transform.position,target.Value.transform.position);}}
 public class GetXDistance:FsmStateAction {public FsmOwnerDefault gameObject=new();public FsmGameObject target=new();public FsmFloat storeResult=new();public int NativeCalls;public void Run()=>On.HutongGames.PlayMaker.Actions.GetXDistance.Run_DoGetDistance(this);public void Native(){NativeCalls++;storeResult.Value=Mathf.Abs(Fsm.GetOwnerDefaultTarget(gameObject).transform.position.x-target.Value.transform.position.x);}}
 public class FloatCompare:FsmStateAction {public FsmFloat float1,float2=new(){Value=5},tolerance=new();public FsmEvent lessThan=new("NEAR"),equal=new("FAR"),greaterThan=new("FAR"),Selected;public void Run()=>On.HutongGames.PlayMaker.Actions.FloatCompare.Run_DoCompare(this);public void Native(){float a=float1.Value,b=float2.Value;Selected=Math.Abs(a-b)<=tolerance.Value?equal:a<b?lessThan:greaterThan;}}
 public class BoolTest:FsmStateAction {public FsmBool boolVariable;public FsmEvent isTrue,isFalse;public FsmEvent Selected=>Fsm.LastEvent;public bool everyFrame;public void Run()=>On.HutongGames.PlayMaker.Actions.BoolTest.Run_OnEnter(this);public void Update()=>On.HutongGames.PlayMaker.Actions.BoolTest.Run_OnUpdate(this);public void Native_OnEnter()=>throw new NotImplementedException();public void Native_OnUpdate()=>throw new NotImplementedException();}
}
public class HeroController:Component {}
public class HeroBox:Component {}
public class HealthManager:Component {public bool Dead;public bool GetIsDead()=>Dead;}
public class AlertRange:Component {public bool enabled=true;public bool IsHeroInRange {get;set;}public static AlertRange Find(GameObject owner,string childName)=>Native_Find(owner,childName);public static AlertRange Native_Find(GameObject owner,string childName)=>throw new NotImplementedException();}
public class CheckAlertRange:FsmStateAction {public FsmObject alertRange=new();public FsmBool storeResult=new();public void Run()=>On.CheckAlertRange.Run_Apply(this);public void Native()=>storeResult.Value=(alertRange.Value as AlertRange)?.IsHeroInRange??false;}
public class CheckAlertRangeByName:FsmStateAction {public string childName="Range";public FsmBool storeResult=new();public bool everyFrame;private AlertRange source;public void Run()=>Native_OnEnter();public void Update()=>Native_OnUpdate();public void Apply()=>On.CheckAlertRangeByName.Run_Apply(this);public void Native_OnEnter()=>throw new NotImplementedException();public void Native_OnUpdate()=>throw new NotImplementedException();public void Native_Apply()=>throw new NotImplementedException();}
namespace KO.HollowKnight8 {
 internal class Vitals {internal int Health=5;}
 internal class PlayerSlot {internal int Index;internal HeroController Hero;internal Vitals Vitals=new();internal bool Ready=true,Connected=true,Down,Hazard,Retiring,ArenaTransfer,Warp,Holding;internal bool Alive=>Hero&&!Down&&!Hazard&&!Retiring&&Hero.gameObject.activeInHierarchy;}
 internal class CoopSession {internal bool Active=true;internal List<PlayerSlot> Players=new();internal Dictionary<Component,PlayerSlot> Owned=new();internal PlayerSlot Resolve(Component c)=>Players.FirstOrDefault(p=>p.Hero==c)??(Owned.TryGetValue(c,out var p)?p:null);}
 internal class Runtime {internal CoopSession Session;}
 internal static class Plugin {internal static Runtime Self;}
 internal static class EmergencyWarp {internal static bool Active(PlayerSlot p)=>p.Warp;}
 internal static class TransitionVote {internal static bool Holding(PlayerSlot p)=>p.Holding;}
 internal static class Diagnostics {internal static void Write(string message){}}
}

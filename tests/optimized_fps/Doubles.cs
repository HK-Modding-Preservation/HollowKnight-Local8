using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine {
 internal class Object {
  static int next; internal readonly int Id=++next; internal bool Alive=true;
  internal static int IdReads,TransformReads,HierarchyReads,ProjectionReads;
  public int GetInstanceID(){IdReads++;return Id;}
  public static implicit operator bool(Object o)=>!ReferenceEquals(o,null)&&o.Alive;
 }
 internal class Component:Object {
  internal GameObject gameObject; internal Transform transform=>gameObject.transform;
  internal T GetComponentInParent<T>()where T:Component {
   HierarchyReads++;for(var t=gameObject.Node;t!=null;t=t.parent){var v=t.gameObject.GetComponent<T>();if(v)return v;}return null;
  }
  internal T GetComponent<T>()where T:Component=>gameObject.GetComponent<T>();
  internal T[] GetComponentsInChildren<T>(bool includeInactive)where T:Component=>gameObject.GetComponentsInChildren<T>(includeInactive);
 }
 internal class MonoBehaviour:Component{}
 internal class GameObject:Object {
  internal string name="Object";internal bool activeInHierarchy=true; internal readonly Transform Node; internal readonly List<Component> Components=new();
  internal Transform transform{get{TransformReads++;return Node;}}
  internal GameObject(Transform parent=null){Node=new Transform{gameObject=this,parent=parent};Components.Add(Node);if(parent!=null)parent.Children.Add(Node);}
  internal T AddComponent<T>()where T:Component,new(){var c=new T{gameObject=this};Components.Add(c);return c;}
  internal T GetComponent<T>()where T:Component=>Components.OfType<T>().FirstOrDefault(c=>c.Alive);
  internal T[] GetComponentsInChildren<T>(bool includeInactive)where T:Component{
   var l=new List<T>();void Walk(Transform t){l.AddRange(t.gameObject.Components.OfType<T>());foreach(var x in t.Children)Walk(x);}Walk(Node);return l.ToArray();
  }
 }
 internal class Transform:Component {
  internal Transform parent;internal readonly List<Transform>Children=new(); internal Vector3 position;
  internal float SX=1,SY=1,Shear=.17f;
  internal Vector3 TransformPoint(Vector3 v){ProjectionReads++;return new Vector3(v.x*SX+v.y*Shear+position.x,v.y*SY+v.x*.11f+position.y,v.z+position.z);}
 }
 internal struct Vector2 {
  internal float x,y;internal Vector2(float x,float y){this.x=x;this.y=y;}
  internal float sqrMagnitude=>x*x+y*y;
  public static Vector2 operator-(Vector2 a,Vector2 b)=>new(a.x-b.x,a.y-b.y);
  public static implicit operator Vector3(Vector2 a)=>new(a.x,a.y,0);
  public static implicit operator Vector2(Vector3 a)=>new(a.x,a.y);
 }
 internal struct Vector3 {internal float x,y,z;internal Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public override string ToString()=>x+","+y+","+z;}
 internal class Camera:Component {
  internal float Scale=.2f,OX=.5f,OY=.5f,Perspective=.013f;
  internal Vector3 WorldToViewportPoint(Vector3 v){ProjectionReads++;float d=1+v.z*Perspective;return new Vector3(v.x*Scale/d+OX,v.y*Scale/d+OY,v.z);}
 }
 internal static class Time {internal static int frameCount;internal static float unscaledTime;}
}
namespace HutongGames.PlayMaker {
 internal class Fsm{internal object Owner;internal string Name="Bench Control";internal UnityEngine.GameObject GameObject=>Owner as UnityEngine.GameObject;internal State ActiveState;}
 internal class State{internal string Name="Idle";}
 internal class FsmStateAction{internal Fsm Fsm;}
}
namespace KO.HollowKnight8 {
 internal class HeroController:UnityEngine.MonoBehaviour{internal bool controlReqlinquished;}
 internal class Vitals{internal bool AtBench;}
 internal class PlayMakerFSM:UnityEngine.Component{internal bool isActiveAndEnabled=true;internal string ActiveStateName;}
 internal class GameManager:UnityEngine.Object{internal static GameManager instance;internal PlayMakerFSM inventoryFSM;}
 internal static class BenchSeats{internal static bool Seated(PlayerSlot p)=>p.SeatOccupied;}
 internal class OwnerTag:UnityEngine.MonoBehaviour{internal PlayerSlot Player;}
 internal class Button{internal bool IsPressed,WasPressed;}
 internal class Actions{internal readonly Button up=new(),down=new();}
 internal class PlayerSlot{
  internal int Index;internal HeroController Hero;internal bool Retiring,Valid=true,Eligible=true,Inside=true,Seat=true,Competing,BlockExit;
  internal Actions Actions=new();internal Vitals Vitals=new();internal bool SeatOccupied;
 }
 internal static class PvpCombat{internal static void Track(UnityEngine.GameObject root,PlayerSlot p,bool reset){}}
 internal static class Plugin{internal static readonly Runtime Self=new();}
 internal class Runtime{internal CoopSession Session;}
 internal static class PvpMatch{internal static bool BlockExit(PlayerSlot p)=>p.BlockExit;}
 internal static class Diagnostics{internal static void Write(string text){}}
 internal static class StagMenuRouting{internal static int Notes;internal static void NoteInteraction(PlayerSlot p){Notes++;}}
 internal class Entry{
  internal HutongGames.PlayMaker.Fsm Fsm;internal PlayerSlot Owner;internal bool Busy,Bench,Up,Down,Scene;
  internal float LastTouch,RequestedAt=-100,NextAllowed;
 }
}

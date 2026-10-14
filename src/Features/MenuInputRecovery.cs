using System.Collections.Generic;
using InControl;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KO.HollowKnight8 {
// Only repair references to the charm menu's private, neutral input view.
// A shop or another mod's legitimate bindings are never replaced by this guard.
internal static class MenuInputRecovery {
 static readonly HashSet<PlayerAction> neutral=new HashSet<PlayerAction>();
 static readonly HashSet<PlayerTwoAxisAction> vectors=new HashSet<PlayerTwoAxisAction>();
 static readonly HashSet<HeroActions> views=new HashSet<HeroActions>();
 static CoopSession owner;static float nextScan;
 internal static void CaptureCreated(HeroActions view){
  Capture();if(view==null)return;
  // Capture private actions before CharmNativeUi aliases selected fields to
  // P1. Shared P1 actions must never be classified as disposable neutral ones.
  foreach(var action in new[]{view.menuSubmit,view.menuCancel,view.jump,view.attack,view.cast,view.pause,view.openInventory,view.paneLeft,view.paneRight})if(action!=null)neutral.Add(action);
  vectors.Add(view.moveVector);views.Add(view);
 }
 internal static void Flush(){nextScan=0;Tick();}
 internal static void Capture(){
  var s=Plugin.Self==null?null:Plugin.Self.Session;
  if(owner!=s){Reset();owner=s;}
  var view=CharmNativeUi.view;if(view==null)return;
  views.Add(view);neutral.Add(view.menuSubmit);vectors.Add(view.moveVector);
 }
 static PlayerAction Repair(PlayerAction value,PlayerAction real){return value!=null&&neutral.Contains(value)?real:value;}
 static PlayerTwoAxisAction Repair(PlayerTwoAxisAction value,PlayerTwoAxisAction real){return value!=null&&vectors.Contains(value)?real:value;}
 internal static void Tick(){
  var s=Plugin.Self==null?null:Plugin.Self.Session;
  if(s!=owner){Reset();owner=s;}
  if(s==null||!s.Active||s.Primary==null||s.Primary.Actions==null||views.Count==0||Time.unscaledTime<nextScan)return;
  nextScan=Time.unscaledTime+.25f;
  if(Charms.NativeMenuOpen||ShopMenuRouting.Buyer!=null&&ShopMenuRouting.MenuVisible||StagMenuRouting.HasOwner||InteractionRouter.ActivePlayer!=null&&!InteractionRouter.ActiveIsBench)return;
  var a=s.Primary.Actions;var gm=GameManager.instance;
  if(gm&&gm.inputHandler&&views.Contains(gm.inputHandler.inputActions))gm.inputHandler.inputActions=a;
  // Both native module types derive from BaseInputModule. One fresh world
  // query retains disabled components on active objects and discovers modules
  // added by mods immediately; it is deliberately not a stale frame cache.
  var modules=Object.FindObjectsOfType<BaseInputModule>();
  foreach(var candidate in modules){var module=candidate as InControlInputModule;if(module){
   module.MoveAction=Repair(module.MoveAction,a.moveVector);module.SubmitAction=Repair(module.SubmitAction,a.menuSubmit);module.CancelAction=Repair(module.CancelAction,a.menuCancel);
  }}
  foreach(var candidate in modules){var module=candidate as HollowKnightInputModule;if(module){
   module.MoveAction=Repair(module.MoveAction,a.moveVector);module.SubmitAction=Repair(module.SubmitAction,a.menuSubmit);module.CancelAction=Repair(module.CancelAction,a.menuCancel);
   module.JumpAction=Repair(module.JumpAction,a.jump);module.AttackAction=Repair(module.AttackAction,a.attack);module.CastAction=Repair(module.CastAction,a.cast);
  }}
 }
 internal static void Reset(){neutral.Clear();vectors.Clear();views.Clear();owner=null;nextScan=0;}
}
}

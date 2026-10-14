using InControl;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KO.HollowKnight8 {
internal static class CharmMenuExit {
 static float until;
 static bool pending;
 // The inventory pane can still be active and inside the viewport during a
 // transition to another UI.  Its position alone must not own menu input.
 internal static bool NativeActive(bool visible) {
  if(!visible)return CharmMenuDiagnostics.Report(false,Charms.NativeProbeReason);
  var gm=GameManager.instance;
  if(gm&&(gm.isPaused||gm.IsLoadingSceneTransition))return CharmMenuDiagnostics.Report(false,gm.isPaused?"paused":"loading");
  var interacting=InteractionRouter.ActivePlayer;
  if(ShopMenuRouting.Buyer!=null&&ShopMenuRouting.MenuVisible)return CharmMenuDiagnostics.Report(false,"shop");
  if(StagMenuRouting.HasOwner)return CharmMenuDiagnostics.Report(false,"stag");
  if(interacting!=null&&!InteractionRouter.ActiveIsBench)return CharmMenuDiagnostics.Report(false,"dialogue");
  if(PickupCard.Owner!=null)return CharmMenuDiagnostics.Report(false,"pickup");
  if(gm&&gm.inventoryFSM){
   var inventory=gm.inventoryFSM;
   if(!inventory.isActiveAndEnabled||!inventory.gameObject.activeInHierarchy)return CharmMenuDiagnostics.Report(false,"inventory-disabled");
   string state=inventory.ActiveStateName;
   if(string.Equals(state,"Closed",System.StringComparison.OrdinalIgnoreCase)||string.Equals(state,"Close",System.StringComparison.OrdinalIgnoreCase)||string.Equals(state,"Inactive",System.StringComparison.OrdinalIgnoreCase))return CharmMenuDiagnostics.Report(false,"inventory-closed");
  }
  GameObject pane=Charms.NativePane;
  // Inventory navigation, selected backboards and stale EventSystem focus need
  // not be children of the Charms Pane. A live inventory that actually owns
  // the visible pane is stronger evidence than that unrelated focus pointer.
  var nativeInventory=gm?gm.inventoryFSM:null;
  if(pane&&nativeInventory&&(pane.transform==nativeInventory.transform||pane.transform.IsChildOf(nativeInventory.transform)))return CharmMenuDiagnostics.Report(true,"native-inventory");
  EventSystem events=EventSystem.current;
  GameObject selected=events==null?null:events.currentSelectedGameObject;
  if(selected==null||!selected.activeInHierarchy||pane==null)return CharmMenuDiagnostics.Report(true,"visible-pane");
  bool owned=selected.transform==pane.transform||selected.transform.IsChildOf(pane.transform)
   ||nativeInventory&&(selected.transform==nativeInventory.transform||selected.transform.IsChildOf(nativeInventory.transform));
  return CharmMenuDiagnostics.Report(owned,owned?"inventory-focus":"unrelated-ui");
 }
 internal static void OnExit() {pending=true;until=Time.unscaledTime+20f;Tick();}
 internal static void Tick() {
  if(!pending)return;
  if(Time.unscaledTime>until){pending=false;return;}
  CoopSession session=Plugin.Self==null?null:Plugin.Self.Session;
  if(session==null||!session.Active||session.Primary==null||session.Primary.Actions==null||Charms.NativeMenuOpen||ShopMenuRouting.Buyer!=null||StagMenuRouting.HasOwner||InteractionRouter.ActivePlayer!=null)return;
  MenuInputRecovery.Flush();
  pending=false;
 }
}
}

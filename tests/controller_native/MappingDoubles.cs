using System;
using System.Linq;
using InControl;
using GlobalEnums;

namespace InControl {
 public class PlayerActionSet {
  public System.Collections.Generic.List<PlayerAction> Actions=new();
  public void Reset(){foreach(var action in Actions)action.ResetBindings();}
  public string Save()=>BindingStore.Save(Actions);
  public void LoadData(byte[] data)=>BindingStore.Load(Actions,data);
  public void Load(string data){throw new Exception("native Load IL was not transplanted");}
 }
}
public class ControllerMapping {
 public InputControlType jump=InputControlType.Action1,attack=InputControlType.Action3,dash=InputControlType.RightTrigger,
 cast=InputControlType.Action2,superDash=InputControlType.LeftTrigger,dreamNail=InputControlType.Action4,
 quickMap=InputControlType.LeftBumper,quickCast=InputControlType.RightBumper;
}
public class GameSettings {
 public ControllerMapping controllerMapping=new();
 public string jumpKey,attackKey,dashKey,castKey,superDashKey,dreamNailKey,quickMapKey,inventoryKey,quickCastKey,upKey,downKey,leftKey,rightKey;
 public bool LoadGamepadSettings(GamepadType type)=>true;
 public void ResetGamepadSettings(GamepadType type){}
 public void SaveGamepadSettings(GamepadType type){}
 public void SaveKeyboardSettings(){}
}
public sealed partial class InputHandler {
 public struct KeyOrMouseBinding {public Key Key;public KeyOrMouseBinding(Key key){Key=key;}}
 public GameSettings gs=new();public GamepadType activeGamepadType;public int Mapped;
 public bool verboseMode;public GamepadState gamepadState;
 public InputDevice gameController=InputDevice.Null;
 public void Native_MapControllerButtons(GamepadType type){throw new Exception("native IL was not transplanted");}
 public void Native_ResetDefaultControllerButtonBindings(){throw new Exception("native reset IL was not transplanted");}
 public void Native_ResetDefaultKeyBindings(){throw new Exception("native keyboard reset IL was not transplanted");}
 public void MapDefaultKeyboardLayout(){throw new Exception("native default layout IL was not transplanted");}
 public void Native_RemapUIButtons(){throw new Exception("native UI reset IL was not transplanted");}
 public void MapControllerButtons(GamepadType type)=>On.InputHandler.Run(this,type);
 public void SetActiveGamepadType(InputDevice device)=>On.InputHandler.Run_SetActiveGamepadType(this,device,(self,pad)=>self.MapControllerButtons(self.activeGamepadType));
 // File/settings IO is controlled. The remapping body itself is native IL.
 public void MapKeyboardLayoutFromGameSettings(){
  Mapped++;
  foreach(var action in new[]{inputActions.left,inputActions.right,inputActions.up,inputActions.down,
     inputActions.jump,inputActions.attack,inputActions.dash,inputActions.cast,inputActions.superDash,
     inputActions.dreamNail,inputActions.quickMap,inputActions.openInventory,inputActions.quickCast})
    action.AddBinding(new KeyBindingSource(Key.Return));
 }
}
namespace On {
 public static partial class InputHandler {
  public delegate void orig_MapControllerButtons(global::InputHandler self,GamepadType type);
  public delegate void hook_MapControllerButtons(orig_MapControllerButtons orig,global::InputHandler self,GamepadType type);
  public static event hook_MapControllerButtons MapControllerButtons;
  public static int Count=>MapControllerButtons?.GetInvocationList().Length??0;
  public static void Run(global::InputHandler self,GamepadType type,orig_MapControllerButtons action=null){
   orig_MapControllerButtons next=action??((input,kind)=>input.Native_MapControllerButtons(kind));
   if(MapControllerButtons!=null)foreach(hook_MapControllerButtons hook in MapControllerButtons.GetInvocationList()){
    var previous=next;next=(input,kind)=>hook(previous,input,kind);
   }
   next(self,type);
  }
 }
}

namespace InControl {public static class Logger {public static int Errors;public static void LogError(string text){Errors++;}}}
namespace Modding {public class Mod {public static int Writes;protected void SaveGlobalSettings(){Writes++;}}}
namespace KO.HollowKnight8 {
 internal class Local8Mod:Modding.Mod {internal static Local8Mod Instance=new();internal static SettingsData Settings=new();}
 internal class SettingsData {public string[] ExtraKeyboardBindings=new string[8];}
 internal static class Hud {internal static int selected;}
}

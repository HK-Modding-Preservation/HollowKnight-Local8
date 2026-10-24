using System;
using System.Linq;
using InControl;
using GlobalEnums;
using KO.HollowKnight8;

static partial class Test {
 static readonly InputControlType[][] directions={
  new[]{InputControlType.DPadLeft,InputControlType.LeftStickLeft},
  new[]{InputControlType.DPadRight,InputControlType.LeftStickRight},
  new[]{InputControlType.DPadUp,InputControlType.LeftStickUp},
  new[]{InputControlType.DPadDown,InputControlType.LeftStickDown}
 };
 static PlayerAction[] Directions(HeroActions a)=>new[]{a.left,a.right,a.up,a.down};
 static CoopSession NativeStart(string preference,params InputDevice[] pads){
  var s=Start(preference,pads);s.Active=true;
  var axes=Directions(s.originalActions);
  for(int i=0;i<4;i++){
   axes[i].ClearBindings();
   axes[i].AddDefaultBinding(new KeyBindingSource(new[]{Key.LeftArrow,Key.RightArrow,Key.UpArrow,Key.DownArrow}[i]));
   foreach(var control in directions[i])axes[i].AddDefaultBinding(new DeviceBindingSource(control));
  }
  Controls.RebindPrimary(s.Primary,s.originalActions,s.Primary.Device);
  GameManager.instance.inputHandler.inputActions=s.Primary.Actions;
  return s;
 }
 static void NativeMappings(){
  ControllerMappingIsolation.Uninstall();
  // Negative control: execute the game's actual reset/remap body on filtered
  // P1 controls. Buttons return, but the pad directions really disappear.
  var pads=Enumerable.Range(0,8).Select(i=>Pad("DualSense "+i)).ToArray();
  var s=NativeStart("Auto",pads);
  var input=GameManager.instance.inputHandler;
  input.Native_MapControllerButtons(GamepadType.PS4);
  Check(Directions(s.Primary.Actions).All(a=>!a.Bindings.OfType<DeviceBindingSource>().Any()),"negative control did not reproduce loss of P1 pad directions");
  Check(s.Primary.Actions.jump.Bindings.OfType<DeviceBindingSource>().Any(),"negative control did not retain P1 buttons");
  End(s);

  ControllerMappingIsolation.Install();ControllerMappingIsolation.Install();
  Check(On.InputHandler.Count==1,"mapping isolation installed twice");
  foreach(var preference in new[]{"Auto","Keyboard"}){
   s=NativeStart(preference,pads);input=GameManager.instance.inputHandler;
   for(int i=preference=="Auto"?1:0;i<7;i++)Press(s,pads[i]);
   s.Refresh();
   foreach(var kind in Enum.GetValues<GamepadType>()){
    input.activeGamepadType=kind;
    foreach(var active in pads){
     InputManager.ActiveDevice=active;
     var signatures=s.Players.Select(p=>Controls.Signature(p.Actions)).ToArray();
     var actions=s.Players.Select(p=>p.Actions).ToArray();
     On.InputHandler.Run(input,kind);
     Check(input.inputActions==s.Primary.Actions,"native remap leaves wrong action set attached");
     for(int i=0;i<s.Players.Count;i++){
      var p=s.Players[i];
      Check(p.Actions==actions[i]&&Controls.Signature(p.Actions)==signatures[i],"another active pad changed a player's controls");
      BindingKind(p,Controls.IsKeyboard(p.Device));
      if(!Controls.IsKeyboard(p.Device))for(int axis=0;axis<4;axis++)foreach(var control in directions[axis]){
       p.Device.GetControl(control).Value=.8f;
       Check(Directions(p.Actions)[axis].Read(p.Device)>.7f,"D-pad/stick input lost after native callback");
       p.Device.GetControl(control).Value=0;
       Check(Directions(p.Actions)[axis].Read(p.Device)==0,"direction sticks after release");
      }
     }
     // Also execute the real periodic refresh after the native remap, rather
     // than testing only the callback in isolation.
     var devices=s.Players.Select(p=>p.Device).ToArray();s.Refresh();
     var refreshed=s.Players.Select(p=>p.Actions).ToArray();s.Refresh();
     for(int i=0;i<s.Players.Count;i++){
      var p=s.Players[i];Check(p.Device==devices[i]&&p.Actions==refreshed[i]&&!p.Actions.IsDestroyed,"settings refresh loses device or recreates unchanged controls");
      if(!Controls.IsKeyboard(p.Device))foreach(var direction in directions){p.Device.GetControl(direction[0]).Value=.9f;Check(Directions(p.Actions)[Array.IndexOf(directions,direction)].Read(p.Device)>.8f,"d-pad stops responding after periodic refresh");p.Device.GetControl(direction[0]).Value=0;}
     }
    }
   }
   // A native callback can happen while the router is serving a guest.
   var guest=s.Players[1];input.inputActions=guest.Actions;
   On.InputHandler.Run(input,GamepadType.PS4);Check(input.inputActions==guest.Actions,"guest context was replaced by P1");
   bool threw=false;try{On.InputHandler.Run(input,GamepadType.PS4,(self,kind)=>{Check(self.inputActions==s.OriginalActions,"callback did not receive native template");throw new InvalidOperationException();});}catch(InvalidOperationException){threw=true;}
   Check(threw&&input.inputActions==guest.Actions,"mapping exception corrupts the active context");
   var foreign=new InputHandler{inputActions=new HeroActions()};
   On.InputHandler.Run(foreign,GamepadType.PS4,(self,kind)=>Check(self.inputActions==foreign.inputActions&&self.inputActions!=s.OriginalActions,"foreign input handler was routed into the game template"));
   foreign.inputActions.Destroy();
   s.Active=false;int before=input.Mapped;
   string solo=Controls.Signature(input.inputActions);
   On.InputHandler.Run(input,GamepadType.PS4);Check(input.Mapped==before+1,"inactive session suppresses native remapping");
   Check(Controls.Signature(input.inputActions)==solo,"single-player filtered controls were reset by native remapping");
   End(s);
  }
  ControllerMappingIsolation.Uninstall();ControllerMappingIsolation.Uninstall();
  Check(On.InputHandler.Count==0,"mapping hook survives uninstall");
  Console.WriteLine("PASS native MapControllerButtons negative/positive controls; D-pad and stick on eight simulated pads, every native gamepad layout, keyboard P1, context and exception restoration.");
 }
}

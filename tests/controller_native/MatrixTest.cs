using System;using System.Linq;using InControl;using UnityEngine;using GlobalEnums;using KO.HollowKnight8;

static partial class Test {
 static UnityInputDevice Port(int id)=>new(){Name="Steam Virtual Gamepad",Meta="XInput",JoystickId=id};
 static void ReadMatrix(CoopSession s){
  foreach(var owner in s.Players.Where(p=>!Controls.IsKeyboard(p.Device)))
   for(int axis=0;axis<4;axis++)foreach(var physical in directions[axis]){
    owner.Device.GetControl(physical).Value=.85f;
    foreach(var player in s.Players){
     Check(player.Actions.Device==player.Device,"action set followed active pad instead of assigned pad");
     for(int observed=0;observed<4;observed++)Check(Directions(player.Actions)[observed].Read(player.Actions.Device)==(player==owner&&observed==axis?.85f:0),"one player's direction moved a different actor/axis");
    }
    owner.Device.GetControl(physical).Value=0;
    foreach(var player in s.Players)Check(Directions(player.Actions).All(a=>a.Read(player.Actions.Device)==0),"released direction remained held");
   }
  foreach(var owner in s.Players.Where(p=>!Controls.IsKeyboard(p.Device))){
   var control=owner.Actions.jump.Bindings.OfType<DeviceBindingSource>().First().Control;owner.Device.GetControl(control).Value=1;
   foreach(var player in s.Players)Check(player.Actions.jump.Read(player.Actions.Device)==(player==owner?1:0),"jump reached a different assigned device");
   owner.Device.GetControl(control).Value=0;
  }
 }
 static void ControllerMatrix(){
  ControllerMappingIsolation.Install();
  foreach(int count in new[]{2,4,8}){
   var pads=Enumerable.Range(1,count).Select(Port).ToArray();var s=NativeStart("Auto",pads);var input=GameManager.instance.inputHandler;
   foreach(var pad in pads.Skip(1).Reverse())Press(s,pad);
   Check(s.Players.Count==count&&s.Players.All(p=>!Controls.IsKeyboard(p.Device)),"controller-only roster required a keyboard");
   s.Refresh();
   for(int cycle=0;cycle<12;cycle++){
    foreach(var active in pads){InputManager.ActiveDevice=active;On.InputHandler.Run(input,Enum.GetValues<GamepadType>()[cycle%Enum.GetValues<GamepadType>().Length]);}
    On.InputHandler.Run_ResetDefaultControllerButtonBindings(input,self=>self.Native_ResetDefaultControllerButtonBindings());
    On.InputHandler.Run_RemapUIButtons(input,self=>self.Native_RemapUIButtons());
    s.Refresh();ReadMatrix(s);
    var retained=s.Players.Select(p=>p.Actions).ToArray();var signature=retained.Select(Controls.Signature).ToArray();
    for(int frame=0;frame<60;frame++)foreach(var p in s.Players)Controls.Pump(p,frame<30&&p.Index==cycle%count);
    for(int i=0;i<count;i++)Check(s.Players[i].Actions==retained[i]&&Controls.Signature(retained[i])==signature[i],"temporarily holding an actor changed device/bindings");
    foreach(var p in s.Players)Controls.Pump(p,false);
   }
   for(int index=0;index<count;index++){
    var p=s.Players[index];var disconnected=(UnityInputDevice)p.Device;var old=p.Actions;var other=s.Players.Where(q=>q!=p).Select(q=>(q,q.Device,q.Actions)).ToArray();
    disconnected.IsAttached=false;InputManager.Devices.Remove(disconnected);foreach(var player in s.Players)Controls.Pump(player,false);
    Check(!p.Connected&&p.InputBlocked,"disconnected player did not block its own input");
    foreach(var tuple in other)Check(tuple.q.Connected&&!tuple.q.InputBlocked,"one disconnected pad blocked another player");
    InputManager.Devices.Reverse();var replacement=Port(disconnected.JoystickId);InputManager.Devices.Insert(0,replacement);s.Reconnect();Controls.Pump(p,false);
    Check(p.Device==replacement&&p.Actions.Device==replacement&&p.Connected&&!p.InputBlocked,"a slot failed to reconnect to its original port");
    foreach(var tuple in other)Check(tuple.q.Device==tuple.Device&&tuple.q.Actions==tuple.Actions,"reconnect reassigned another slot's pad/actions");
    Check(index==0?p.Actions!=old&&old.IsDestroyed:p.Actions==old&&!old.IsDestroyed,"reconnect invalidated a guest's action references");
    Check(GameManager.instance.inputHandler.inputActions==s.Primary.Actions,"reconnect left stale native P1 actions");ReadMatrix(s);
   }
   // Lose/recreate every device together, with a different enumeration order.
   var ports=s.Players.Select(p=>((UnityInputDevice)p.Device).JoystickId).ToArray();
   foreach(var p in s.Players){p.Device.IsAttached=false;Controls.Pump(p,false);}
   InputManager.Devices.Clear();foreach(int port in ports.Reverse())InputManager.Devices.Add(Port(port));s.Reconnect();
   for(int i=0;i<count;i++){Controls.Pump(s.Players[i],false);Check(((UnityInputDevice)s.Players[i].Device).JoystickId==ports[i]&&s.Players[i].Connected&&!s.Players[i].InputBlocked,"simultaneous reconnect merged/reordered matching pad names");}
   ReadMatrix(s);
   for(int round=0;round<3;round++)foreach(var target in s.Players.Skip(1).ToArray()){
    var p1=s.Primary;var targetPad=target.Device;var primaryPad=p1.Device;int health=target.Health;var unrelated=s.Players.Where(p=>p!=p1&&p!=target).Select(p=>(p,p.Device,p.Actions)).ToArray();
    Check(s.AssignPrimary(targetPad)&&p1.Device==targetPad&&target.Device==primaryPad&&target.Health==health,"P1 swap failed or moved character state");
    foreach(var tuple in unrelated)Check(tuple.p.Device==tuple.Device&&tuple.p.Actions==tuple.Actions,"pair swap rebuilt an unrelated player's controls");
    Check(s.Players.Select(p=>p.Device).Distinct().Count()==count&&s.Players.Select(p=>p.Actions).Distinct().Count()==count,"P1 swap shared a pad/action set");ReadMatrix(s);
   }
   var freed=s.Primary.Device;Check(s.AssignPrimary(InputDevice.Null),"P1 could not switch to keyboard in a full party");
   Check(s.Primary.Actions.left.Bindings.OfType<KeyBindingSource>().Any()&&s.Primary.Actions.jump.Bindings.OfType<KeyBindingSource>().Any(),"keyboard P1 lost movement/jump");ReadMatrix(s);
   Check(s.AssignPrimary(freed),"P1 could not return to its controller");ReadMatrix(s);
   var clears=s.Players.Select(p=>p.Actions.Clears).ToArray();Application.isFocused=false;
   foreach(var p in s.Players)Controls.Pump(p,false);
   for(int i=0;i<count;i++)Check(s.Players[i].InputBlocked&&s.Players[i].Actions.Clears>clears[i],"overlay/focus loss left a slot's input held");
   Application.isFocused=true;foreach(var p in s.Players)Controls.Pump(p,false);
   Check(s.Players.All(p=>p.Connected&&!p.InputBlocked),"overlay return left a player blocked");ReadMatrix(s);End(s);
  }
  ControllerMappingIsolation.Uninstall();
  Console.WriteLine("PASS sustained 2/4/8-pad input isolation matrix: every actor's D-pad/stick/jump press/release, native resets, every-slot and simultaneous port reconnect, full-roster P1 swaps/keyboard return, waiting holds and focus recovery. Devices and InControl IO are simulated.");
 }
}

using System;
using System.Collections.Generic;
using System.Linq;
using InControl;

namespace UnityEngine {
 public enum RuntimePlatform { WindowsPlayer, LinuxPlayer }
 public static class Application { public static bool isFocused=true; public static RuntimePlatform platform=RuntimePlatform.LinuxPlayer; }
 public static class Time { public static float unscaledTime; }
 public static class Input { public static string[] GetJoystickNames()=>Array.Empty<string>(); }
}
namespace InControl {
 public enum InputRangeType { ZeroToMinusOne, ZeroToOne }
 public enum BindingSourceType { KeyBindingSource, MouseBindingSource, DeviceBindingSource, UnknownDeviceBindingSource }
 public abstract class BindingSource {public abstract BindingSourceType BindingSourceType{get;}public virtual string Name=>BindingSourceType.ToString();}
 public sealed class KeyBindingSource:BindingSource {public Key Control;public KeyBindingSource(Key c){Control=c;}public override BindingSourceType BindingSourceType=>BindingSourceType.KeyBindingSource;public override string Name=>Control.ToString();}
 public sealed class MouseBindingSource:BindingSource {public int Control;public MouseBindingSource(int c){Control=c;}public override BindingSourceType BindingSourceType=>BindingSourceType.MouseBindingSource;}
 public sealed class DeviceBindingSource:BindingSource {public InputControlType Control;public DeviceBindingSource(InputControlType c){Control=c;}public override BindingSourceType BindingSourceType=>BindingSourceType.DeviceBindingSource;public override string Name=>Control.ToString();}
 public struct UnknownDeviceControl {public InputControlType Control;public InputRangeType Range;public UnknownDeviceControl(InputControlType c,InputRangeType r){Control=c;Range=r;}}
 public sealed class UnknownDeviceBindingSource:BindingSource {public UnknownDeviceControl Control;public UnknownDeviceBindingSource(UnknownDeviceControl c){Control=c;}public override BindingSourceType BindingSourceType=>BindingSourceType.UnknownDeviceBindingSource;}
 public sealed class PlayerAction {public string Name;public List<BindingSource> Bindings=new();public PlayerAction(string n){Name=n;}public void AddBinding(BindingSource b){Bindings.Add(b);}public void ClearBindings(){Bindings.Clear();}}
 public class InputControl {public bool WasPressed;}
 public class InputDevice {
  public static readonly InputDevice Null=new InputDevice{Name="Keyboard",IsAttached=false};
  public string Name="Pad";public Guid GUID=Guid.NewGuid();public bool IsAttached=true,Passive,IsUnknown,CommandWasPressed;public int NumUnknownButtons=12,NumUnknownAnalogs=2;
  readonly Dictionary<InputControlType,InputControl> controls=new();public InputControl GetControl(InputControlType c){if(!controls.TryGetValue(c,out var v))controls[c]=v=new InputControl();return v;}
 }
 public sealed class NativeInfo {public string name="vJoy",location="location";}
 public class NativeInputDevice:InputDevice {public NativeInfo Info=new();}
 public class UnityInputDevice:InputDevice {public string Meta="vJoy";public int JoystickId;}
 public class NativeInputDeviceManager {public static bool CheckPlatformSupport(List<string> messages)=>true;}
 public class UnityInputDeviceManager {}
 public static class InputManager {public static bool IsSetup=true;public static List<InputDevice> Devices=new();public static InputDevice ActiveDevice=InputDevice.Null;public static bool HasDeviceManager<T>()=>false;public static void AddDeviceManager<T>(){}}
}
public sealed partial class HeroActions {
 public static int Created,Destroyed;public bool IsDestroyed;public int Clears;public InputDevice Device;
 public List<PlayerAction> Actions=new();
 PlayerAction Add(string name){var a=new PlayerAction(name);Actions.Add(a);return a;}
 public PlayerAction GetPlayerActionByName(string name)=>Actions.FirstOrDefault(a=>a.Name==name);
 public void ClearInputState(){Clears++;}
 public void Destroy(){if(IsDestroyed)throw new Exception("double destroy");IsDestroyed=true;Destroyed++;}
}
public sealed class InputHandler {public HeroActions inputActions;public static implicit operator bool(InputHandler value)=>value!=null;}
public sealed class GameManager {public static GameManager instance;public InputHandler inputHandler=new();public static implicit operator bool(GameManager value)=>value!=null;}
namespace KO.HollowKnight8 {
 internal sealed class Setting<T>{internal T Value;internal Setting(T value){Value=value;}}
 internal sealed class PlayerSlot {internal int Index;internal HeroActions Actions;internal bool OwnsActions,Connected,InputBlocked,ArenaTransfer;internal InputDevice Device;internal string DeviceKey,DeviceName,ProfileId;internal int Health=5;}
 internal sealed class Local8Runtime {internal static Local8Runtime Self;internal CoopSession Session;internal Setting<string> PrimaryDevice=new(PrimaryController.Automatic);internal bool Panel;internal string Message;internal void Notice(string text){Message=text;}}
 internal static class Plugin {internal static Local8Runtime Self=>Local8Runtime.Self;}
 internal static class Diagnostics {internal static void Write(string text){}internal static void Throttled(string text,Exception error){}}
 internal static class KeypadInput {internal static void Defaults(PlayerSlot p){}internal static void RepairMenus(PlayerSlot p){}}
 internal static class KeyboardMemory {internal static Dictionary<int,Key> Saved=new();internal static void Restore(PlayerSlot p){if(Saved.TryGetValue(p.Index,out var key)){p.Actions.attack.ClearBindings();p.Actions.attack.AddBinding(new KeyBindingSource(key));}}}
 internal static class PvpSpellOnly {internal static void Input(PlayerSlot p){}}
}

using System;
using System.Linq;
using InControl;
using KO.HollowKnight8;

static partial class Test {
 static void KeyboardData(){
  var s=Start("Auto",Pad());s.Join(InputDevice.Null);var p=s.Players[1];var actions=p.Actions;var primary=s.Primary.Actions;
  // The native Load method catches malformed base64 and silently resets the
  // regular action bindings. Its exact exception region runs in this host.
  int errors=Logger.Errors;actions.Load("!bad-base64!");
  Check(Logger.Errors==errors+1&&actions.left.Bindings.Count==0,"native Load negative control did not silently reset keyboard input");
  Controls.Dispose(p);Controls.InitSecondary(p,s.originalActions,InputDevice.Null);actions=p.Actions;string healthy=Controls.Signature(actions);
  Local8Mod.Settings=new SettingsData();Modding.Mod.Writes=0;
  KeyboardMemoryUnderTest.Save(p);string stored=Local8Mod.Settings.ExtraKeyboardBindings[p.Index];
  Check(!string.IsNullOrEmpty(stored)&&Modding.Mod.Writes==1,"keyboard save did not persist bindings");
  KeyboardMemoryUnderTest.Save(p);Check(Modding.Mod.Writes==1,"unchanged keyboard settings written repeatedly");
  foreach(string bad in new[]{"!bad-base64!",Convert.ToBase64String(new byte[]{1,2,3,4})}){
   Local8Mod.Settings.ExtraKeyboardBindings[p.Index]=bad;KeyboardMemoryUnderTest.Restore(p);
   Check(p.Actions==actions&&Controls.Signature(actions)==healthy&&s.Primary.Actions==primary,"silent native load failure disabled a player or changed another player");
   Check(!ProfileKeyboardUnderTest.CanApply(p,bad,s),"profile validation accepted a silently reset action set");
   bool threw=false;try{ProfileKeyboardUnderTest.Apply(p,bad);}catch(FormatException){threw=true;}
   Check(threw&&Controls.Signature(actions)==healthy&&Local8Mod.Settings.ExtraKeyboardBindings[p.Index]==bad,"invalid profile application changed controls or stored data");
  }
  Local8Mod.Settings.ExtraKeyboardBindings[p.Index]=stored;actions.attack.ClearBindings();actions.attack.AddBinding(new KeyBindingSource(Key.Return));
  KeyboardMemoryUnderTest.Restore(p);Check(Controls.Signature(actions)==healthy,"valid keyboard settings no longer restore");
  Hud.selected=p.Index;ActionKeys.Capturing=15;KeyboardMemoryUnderTest.BeforeCapture();
  actions.attack.ClearBindings();actions.attack.AddBinding(new KeyBindingSource(Key.Return));ActionKeys.Capturing=0;KeyboardMemoryUnderTest.AfterCapture(s);
  Check(Modding.Mod.Writes==2&&Local8Mod.Settings.ExtraKeyboardBindings[p.Index]!=stored,"completed capture did not save the selected player's controls");
  string newBindings=Local8Mod.Settings.ExtraKeyboardBindings[p.Index];
  Check(ProfileKeyboardUnderTest.CanApply(p,newBindings,s),"valid keyboard profile rejected");
  Check(Controls.ResetKeyboard(p,s.originalActions)&&p.Actions==actions,"reset lost action identity");
  KeyboardMemoryUnderTest.Save(p);KeyboardMemoryUnderTest.Restore(p);
  Check(p.Actions.attack.Bindings.OfType<KeyBindingSource>().Single().Control==Key.Q&&Modding.Mod.Writes==3,"default bindings did not persist after reset and restore");
  End(s);
  Console.WriteLine("PASS exact native silent Load failure, fallback controls, corrupt/valid profiles, real keyboard settings save/capture/reset persistence. Snapshot byte storage is controlled.");
 }
}

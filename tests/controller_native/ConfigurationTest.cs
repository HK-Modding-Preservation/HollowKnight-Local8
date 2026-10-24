using System;
using System.Collections;
using System.Linq;
using InControl;
using GlobalEnums;
using KO.HollowKnight8;

static partial class Test {
 static IEnumerator UiSetup(InputHandler input,HeroActions template,Action disposed,bool fail=false){
  try{
   Check(input.inputActions==template,"UI setup entered with player actions");
   yield return "native wait";
   Check(input.inputActions==template,"delayed UI setup resumed with player actions");
   if(fail)throw new InvalidOperationException("delayed setup failure");
   input.inputActions.menuSubmit.AddDefaultBinding(new DeviceBindingSource(InputControlType.Action1));
  }finally{Check(input.inputActions==template,"UI setup disposed outside template context");disposed();}
 }
 static void ConfigurationPaths(){
  var pads=Enumerable.Range(0,8).Select(i=>Pad("Config pad "+i)).ToArray();
  ControllerMappingIsolation.Install();
  // Negative control models test5: only the inner MapControllerButtons is
  // isolated. Execute the exact native outer reset without the new outer hook.
  var s=NativeStart("Auto",pads);var input=GameManager.instance.inputHandler;
  input.activeGamepadType=GamepadType.PS4;
  s.Refresh();On.InputHandler.Run(input,GamepadType.PS4);s.Refresh();
  string template=Controls.Signature(s.OriginalActions);
  input.Native_ResetDefaultControllerButtonBindings();s.Refresh();
  Check(Controls.Signature(s.OriginalActions)==template&&!s.Primary.Actions.jump.Bindings.OfType<DeviceBindingSource>().Any(),"negative control did not retain the lost P1 button after a full settings refresh");End(s);
  s=NativeStart("Auto",pads);input=GameManager.instance.inputHandler;input.Native_RemapUIButtons();
  Check(s.Primary.Actions.menuSubmit.Bindings.Count==0&&s.Primary.Actions.paneRight.Bindings.Count==0,"native UI reset negative control did not clear filtered menu bindings");End(s);

  foreach(string preference in new[]{"Auto","Keyboard"}){
   s=NativeStart(preference,pads);input=GameManager.instance.inputHandler;
   for(int i=preference=="Auto"?1:0;i<7;i++)Press(s,pads[i]);
   s.Refresh();On.InputHandler.Run(input,GamepadType.PS4);s.Refresh();
   var actions=s.Players.Select(p=>p.Actions).ToArray();var signatures=s.Players.Select(p=>Controls.Signature(p.Actions)).ToArray();
   for(int repeat=0;repeat<20;repeat++){
    On.InputHandler.Run_ResetDefaultControllerButtonBindings(input,self=>self.Native_ResetDefaultControllerButtonBindings());
    On.InputHandler.Run_RemapUIButtons(input,self=>self.Native_RemapUIButtons());
    On.InputHandler.Run_ResetDefaultKeyBindings(input,self=>self.Native_ResetDefaultKeyBindings());
    for(int i=0;i<s.Players.Count;i++)Check(s.Players[i].Actions==actions[i]&&Controls.Signature(actions[i])==signatures[i],"native settings reset mutates a player's live bindings");
   }
   var selected=On.InputHandler.Run_ActionButtonToPlayerAction(input,HeroActionButton.JUMP,(self,button)=>self.inputActions.jump);
   Check(selected==s.OriginalActions.jump,"native rebind screen cached the filtered player action");
   On.InputHandler.Run_GetKeyBindingForAction(input,s.Primary.Actions.jump,(self,action)=>{Check(self.inputActions==s.OriginalActions&&action==selected,"key label reads a filtered action or a different set");return new InputHandler.KeyOrMouseBinding(Key.Return);});
   On.InputHandler.Run_GetButtonBindingForAction(input,s.Primary.Actions.jump,(self,action)=>{Check(self.inputActions==s.OriginalActions&&action==selected,"button label reads a filtered action or a different set");return InputControlType.Action1;});
   foreach(var save in new[]{false,true}){
    if(save)On.InputHandler.Run_SendKeyBindingsToGameSettings(input,self=>Check(self.inputActions==s.OriginalActions,"saving keyboard settings reads the filtered controller"));
    else On.InputHandler.Run_SendButtonBindingsToGameSettings(input,self=>Check(self.inputActions==s.OriginalActions,"saving controller settings reads the filtered keyboard"));
   }
   On.InputHandler.Run_SetActiveGamepadType(input,pads.Last(),(self,pad)=>Check(self.inputActions==s.OriginalActions&&pad==pads.Last(),"native activation setup cached player actions"));
   On.InputHandler.Run_RemoveGamepadUIInputActions(input,self=>Check(self.inputActions==s.OriginalActions,"detach removed a player's menu bindings"));
   int disposed=0;var body=On.InputHandler.Run_SetupGamepadUIInputActions(input,self=>UiSetup(self,s.OriginalActions,()=>disposed++));
   var previous=input.inputActions;Check(body.MoveNext()&&Equals(body.Current,"native wait")&&input.inputActions==previous,"UI setup retained a global template across yield");
   input.inputActions=s.Players[1].Actions;previous=input.inputActions;
   Check(!body.MoveNext()&&disposed==1&&input.inputActions==previous,"UI setup completion leaks context or skips disposal");
   body=On.InputHandler.Run_SetupGamepadUIInputActions(input,self=>UiSetup(self,s.OriginalActions,()=>disposed++));
   Check(body.MoveNext()&&input.inputActions==previous,"UI cancellation changed current guest input");((IDisposable)body).Dispose();
   Check(disposed==2&&input.inputActions==previous,"UI cancellation leaked context or skipped disposal");
   body=On.InputHandler.Run_SetupGamepadUIInputActions(input,self=>UiSetup(self,s.OriginalActions,()=>disposed++,true));body.MoveNext();
   bool threw=false;try{body.MoveNext();}catch(InvalidOperationException){threw=true;}
   Check(threw&&disposed==3&&input.inputActions==previous,"delayed UI exception corrupts input routing");
   threw=false;try{On.InputHandler.Run_ResetDefaultControllerButtonBindings(input,self=>throw new InvalidOperationException());}catch(InvalidOperationException){threw=true;}
   Check(threw&&input.inputActions==previous,"reset exception corrupts guest context");
   for(int i=0;i<s.Players.Count;i++){Check(s.Players[i].Actions==actions[i]&&Controls.Signature(actions[i])==signatures[i],"configuration coroutine changed a player's bindings");BindingKind(s.Players[i],Controls.IsKeyboard(s.Players[i].Device));}
   s.Refresh();Check(s.Players.Select(p=>p.Device).Distinct().Count()==s.Players.Count,"configuration refresh merges devices");
   var foreign=new InputHandler{inputActions=new HeroActions()};var foreignActions=foreign.inputActions;
   On.InputHandler.Run_ResetDefaultKeyBindings(foreign,self=>Check(self.inputActions==foreignActions,"foreign input configuration was redirected"));foreignActions.Destroy();
   End(s);
  }
  ControllerMappingIsolation.Install();Check(On.InputHandler.TotalCount==12,"configuration hooks installed twice or missing");
  ControllerMappingIsolation.Uninstall();Check(On.InputHandler.TotalCount==0,"configuration hooks survived uninstall");
  Console.WriteLine("PASS full native reset negative/positive controls, UI resets, configuration action lookup/save, delayed setup/yield/cancel/exception and foreign-handler isolation.");
 }
}

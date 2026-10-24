#!/usr/bin/env python3
"""Execute the real native controller remapping body through the production hook."""
from pathlib import Path
import argparse, json, os, re, shutil, subprocess, tempfile

here=Path(__file__).resolve().parent;root=here.parents[1];old=root/'tests/primary_controller'
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--managed',type=Path,required=True)
p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));a=p.parse_args()
dotnet=shutil.which(a.dotnet) or str(Path(a.dotnet).resolve());managed=a.managed.resolve()
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
controls=(root/'src/Core/Controls.cs').read_text();session=(root/'src/Core/CoopSession.cs').read_text()
keypad=(root/'src/Features/KeypadInput.cs').read_text()
def keypad_method(marker):
 start=keypad.index(marker);opening=keypad.index('{',start);depth=1;end=opening+1
 while depth:depth+=(keypad[end]=='{')-(keypad[end]=='}');end+=1
 return keypad[start:end]
keypad_defaults='using InControl;namespace KO.HollowKnight8 {internal static partial class KeypadInput {'+keypad_method('internal static void Defaults(')+keypad_method('static void Bind(')+'}}'
def method(name):
 start=re.search(r'\t(?:internal|private) [^\n]+\b'+name+r'\(',session).start()
 opening=session.index('{',start);depth=1;end=opening+1
 while depth:depth+=(session[end]=='{')-(session[end]=='}');end+=1
 return session[start:end]
with tempfile.TemporaryDirectory(prefix='local8-native-controller-') as directory:
 out=Path(directory)
 def compile(name,files,extra=()):
  options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/(name+'.dll'))]
  options+=['-r:'+str(f) for f in refs.glob('*.dll')]+list(extra)+list(map(str,files))
  (out/(name+'.rsp')).write_text('\n'.join('"'+s+'"' for s in options))
  (out/(name+'.runtimeconfig.json')).write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
  subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/(name+'.rsp'))],check=True)
 compile('Transplant',[here/'Transplant.cs'],['-r:'+str(managed/'Mono.Cecil.dll')])
 shutil.copy2(managed/'Mono.Cecil.dll',out/'Mono.Cecil.dll')
 native=managed/'Assembly-CSharp.dll'
 (out/'NativeEnums.cs').write_text(subprocess.check_output([dotnet,str(out/'Transplant.dll'),str(native),'--enums'],text=True))
 keys=sorted(set(re.findall(r'InControl\.Key\.(\w+)',controls))|set(re.findall(r'\bKey\.(\w+)',keypad_defaults))|{'Return'})
 actions=['left','right','up','down','jump','attack','dash','cast','focus','quickCast','dreamNail','superDash','pause','openInventory','quickMap','menuSubmit','menuCancel','textSpeedup','skipCutscene','paneLeft','paneRight']
 contract=''
 contract+='public sealed partial class HeroActions {'+' '.join('public InControl.PlayerAction '+n+';' for n in actions)+' public HeroActions(){Created++;'+''.join(n+'=Add("'+n+'");' for n in actions)+'}}'
 (out/'Contract.cs').write_text(contract)
 (out/'KeypadDefaults.cs').write_text(keypad_defaults)
 doubles=(old/'Doubles.cs').read_text().replace('public sealed class InputHandler {','public sealed partial class InputHandler {')
 doubles=doubles.replace('public sealed partial class HeroActions {','public sealed partial class HeroActions : InControl.PlayerActionSet {')
 doubles=doubles.replace(' public List<PlayerAction> Actions=new();','')
 for line in [
  ' public string Save()=>BindingStore.Save(Actions);',
  ' public void LoadData(byte[] data)=>BindingStore.Load(Actions,data);',
  ' public void Load(string data){if(data==null)return;try{LoadData(Convert.FromBase64String(data));}catch{foreach(var a in Actions)a.ClearBindings();}}']:
  doubles=doubles.replace(line,'')
 doubles=doubles.replace('public class InputControl {public bool WasPressed;}','public class InputControl {public bool WasPressed;public float Value;}')
 doubles=doubles.replace('public string Name="Pad";','public string get_Name()=>Name;public string Name="Pad";')
 doubles=doubles.replace('public KeyBindingSource(Key c){Control=c;}','public KeyBindingSource(Key c){Control=c;}public KeyBindingSource(Key[] keys){Control=keys.Single();}')
 doubles=doubles.replace('public static class InputManager {','public static class InputManager {public static InputDevice get_ActiveDevice()=>ActiveDevice;')
 start=doubles.index(' public sealed class PlayerAction {');end=doubles.index('\n public class InputControl',start)
 doubles=doubles[:start]+''' public sealed class PlayerAction {
  public string Name;public List<BindingSource> Bindings=new(),Defaults=new();public PlayerAction(string name){Name=name;}
  static bool Same(BindingSource a,BindingSource b)=>a.BindingSourceType==b.BindingSourceType&&a.Name==b.Name;
  public bool AddBinding(BindingSource b){if(Bindings.Any(x=>Same(x,b)))return false;Bindings.Add(b);return true;}
  public void AddDefaultBinding(BindingSource b){if(!Defaults.Any(x=>Same(x,b)))Defaults.Add(b);AddBinding(b);}
  public void ClearBindings(){Bindings.Clear();}
  public void ResetBindings(){Bindings.Clear();Bindings.AddRange(Defaults);}
  public float Read(InputDevice d)=>Bindings.OfType<DeviceBindingSource>().Select(b=>d.GetControl(b.Control).Value).DefaultIfEmpty().Max();
 }
'''+doubles[end:]
 (out/'Doubles.cs').write_text(doubles)
 hook_specs=[
  ('SetActiveGamepadType','void',[('InControl.InputDevice','device')]),
  *[(name,'void',[]) for name in ['ResetDefaultKeyBindings','ResetDefaultControllerButtonBindings','RemapUIButtons','SendKeyBindingsToGameSettings','SendButtonBindingsToGameSettings','RemoveGamepadUIInputActions']],
  ('SetupGamepadUIInputActions','System.Collections.IEnumerator',[]),
  ('ActionButtonToPlayerAction','InControl.PlayerAction',[('GlobalEnums.HeroActionButton','button')]),
  ('GetKeyBindingForAction','global::InputHandler.KeyOrMouseBinding',[('InControl.PlayerAction','actionValue')]),
  ('GetButtonBindingForAction','InControl.InputControlType',[('InControl.PlayerAction','actionValue')])]
 hooks='namespace UnityEngine {public static class Debug {public static void LogFormat(string text,params object[] args){}}}namespace On {public static partial class InputHandler {'
 counts=['Count']
 for name,result,parameters in hook_specs:
  signature=', '.join(['global::InputHandler self']+[kind+' '+value for kind,value in parameters]);arguments=', '.join(['self']+[value for _,value in parameters]);ret='' if result=='void' else 'return '
  hooks+=f'public delegate {result} orig_{name}({signature});public delegate {result} hook_{name}(orig_{name} orig,{signature});public static event hook_{name} {name};'
  hooks+=f'public static {result} Run_{name}({signature},orig_{name} action){{orig_{name} next=action;if({name}!=null)foreach(hook_{name} hook in {name}.GetInvocationList()){{var previous=next;next=({arguments})=>hook(previous,{arguments});}}{ret}next({arguments});}}'
  counts.append(f'({name}?.GetInvocationList().Length??0)')
 hooks+='public static int TotalCount=>'+'+'.join(counts)+';}}'
 (out/'OnContracts.cs').write_text(hooks)
 host='''using System;using System.Linq;using System.Collections.Generic;using InControl;
 namespace KO.HollowKnight8 {internal sealed class CoopSession {
 internal List<PlayerSlot> Players=new();internal Local8Runtime plugin;internal HeroActions originalActions;internal bool CanJoin=true,Active;private string bindingSignature;
 internal HeroActions OriginalActions=>originalActions;
 internal PlayerSlot Primary=>Players.FirstOrDefault();
 internal void Initialize(PlayerSlot p){InitializePrimaryInput(p);Players.Add(p);}
 internal void Reconnect(){ReconnectControllers();}
 internal void Refresh(){RefreshBindings();}
 internal void Press(InputDevice d){HandleControllerJoin(d);}
 internal void Join(InputDevice d){if(Players.Count>=8)return;var p=new PlayerSlot{Index=Players.Count};Controls.InitSecondary(p,originalActions,d);Players.Add(p);}
 '''+'\n'.join(method(n) for n in ['InitializePrimaryInput','ReconnectControllers','HandleControllerJoin','AssignPrimaryAutomatic','AssignPrimary','RefreshBindings'])+'}}'
 (out/'Session.cs').write_text(host)
 test=(old/'Test.cs').read_text().replace('static class Test {','static partial class Test {').replace('static void Main(){','static void Main(){NativeMappings();ConfigurationPaths();KeyboardData();ControllerMatrix();')
 (out/'ExistingTest.cs').write_text(test)
 (out/'KeyboardMemoryUnderTest.cs').write_text((root/'src/Features/KeyboardMemory.cs').read_text().replace('internal static class KeyboardMemory {','internal static class KeyboardMemoryUnderTest {'))
 (out/'ProfileKeyboardUnderTest.cs').write_text((root/'src/Features/ProfileKeyboard.cs').read_text().replace('internal static class ProfileKeyboard','internal static class ProfileKeyboardUnderTest'))
 compile('Test',[out/'Doubles.cs',out/'Contract.cs',out/'KeypadDefaults.cs',out/'NativeEnums.cs',out/'OnContracts.cs',out/'Session.cs',out/'ExistingTest.cs',here/'MappingDoubles.cs',here/'Test.cs',here/'ConfigurationTest.cs',here/'KeyboardDataTest.cs',here/'MatrixTest.cs',out/'KeyboardMemoryUnderTest.cs',out/'ProfileKeyboardUnderTest.cs',root/'src/Core/Controls.cs',root/'src/Core/PrimaryController.cs',root/'src/Core/LobbyRoster.cs',root/'src/Features/HealthChangeFeedback.cs',root/'src/Features/ControllerMappingIsolation.cs',root/'src/Features/KeyboardBindingData.cs'])
 subprocess.run([dotnet,str(out/'Transplant.dll'),str(native),str(out/'Test.dll'),str(out/'Patched.dll')],check=True)
 shutil.copy2(out/'Patched.dll',out/'Test.dll')
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

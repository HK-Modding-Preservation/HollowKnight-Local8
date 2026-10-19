#!/usr/bin/env python3
"""Run real input assignment, bindings and reconnect methods on an explicit host."""
from pathlib import Path
import argparse, json, os, re, shutil, subprocess, tempfile
here=Path(__file__).resolve().parent; root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));a=p.parse_args()
dotnet=shutil.which(a.dotnet) or str(Path(a.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
controls=(root/'src/Core/Controls.cs').read_text();session=(root/'src/Core/CoopSession.cs').read_text()
def method(name):
 start=re.search(r'\t(?:internal|private) [^\n]+\b'+name+r'\(',session).start()
 opening=session.index('{',start);depth=1;end=opening+1
 while depth:
  depth+=(session[end]=='{')-(session[end]=='}');end+=1
 return session[start:end]
with tempfile.TemporaryDirectory(prefix='local8-primary-controller-') as directory:
 out=Path(directory)
 # Contract declarations only. All input selection/filtering remains production code.
 keys=sorted(set(re.findall(r'InControl\.Key\.(\w+)',controls))|{'Return'})
 enums=sorted(set(re.findall(r'InputControlType\.(\w+)',controls)))
 actions=['left','right','up','down','jump','attack','dash','cast','focus','quickCast','dreamNail','superDash','pause','openInventory','quickMap','menuSubmit','menuCancel','textSpeedup','skipCutscene','paneLeft','paneRight']
 contract='namespace InControl {public enum Key{'+','.join(keys)+'} public enum InputControlType{'+','.join(enums)+'}}\n'
 contract+='public sealed partial class HeroActions {'+' '.join('public InControl.PlayerAction '+n+';' for n in actions)+' public HeroActions(){Created++;' + ''.join(n+'=Add("'+n+'");' for n in actions)+'}}'
 (out/'Contract.cs').write_text(contract)
 host='''using System;using System.Linq;using System.Collections.Generic;using InControl;
 namespace KO.HollowKnight8 {internal sealed class CoopSession {
 internal List<PlayerSlot> Players=new();internal Local8Runtime plugin;internal HeroActions originalActions;internal bool CanJoin=true;
 internal PlayerSlot Primary=>Players.FirstOrDefault();
 internal void Initialize(PlayerSlot p){InitializePrimaryInput(p);Players.Add(p);}
 internal void Reconnect(){ReconnectControllers();}
 internal void Press(InputDevice d){HandleControllerJoin(d);}
 internal void Join(InputDevice d){if(Players.Count>=8)return;var p=new PlayerSlot{Index=Players.Count};Controls.InitSecondary(p,originalActions,d);Players.Add(p);}
 '''+'\n'.join(method(n) for n in ['InitializePrimaryInput','ReconnectControllers','HandleControllerJoin','AssignPrimaryAutomatic','AssignPrimary'])+'}}'
 (out/'Session.cs').write_text(host)
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(f) for f in refs.glob('*.dll')]
 options +=[str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'Contract.cs'),str(out/'Session.cs'),str(root/'src/Core/Controls.cs'),str(root/'src/Core/PrimaryController.cs')]
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
 (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

#!/usr/bin/env python3
"""Execute production ElderBaldurRange with controlled action/event/geometry IO."""
from pathlib import Path
import argparse,json,subprocess,tempfile,shutil
here=Path(__file__).resolve().parent;repo=here.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--dotnet',required=True);p.add_argument('--managed',type=Path,required=True);a=p.parse_args()
dotnet=str(Path(a.dotnet).resolve());version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
hooks=''
for cls,method in [('HutongGames.PlayMaker.Actions.GetDistance','DoGetDistance'),('HutongGames.PlayMaker.Actions.GetXDistance','DoGetDistance'),('HutongGames.PlayMaker.Actions.FloatCompare','DoCompare'),('HutongGames.PlayMaker.Actions.BoolTest','OnEnter'),('HutongGames.PlayMaker.Actions.BoolTest','OnUpdate'),('CheckAlertRange','Apply'),('CheckAlertRangeByName','Apply')]:
 parts=cls.rsplit('.',1);ns='On.'+parts[0] if len(parts)==2 else 'On';name=parts[-1]
 hooks+='namespace '+ns+'{public static partial class '+name+'{'
 hooks+=f'public delegate void orig_{method}(global::{cls} self);public delegate void hook_{method}(orig_{method} orig,global::{cls} self);public static event hook_{method} {method};public static int Count_{method}=>{method}?.GetInvocationList().Length??0;'
 hooks+=f'public static void Run_{method}(global::{cls} self)'+'{'+f'orig_{method} next=x=>x.{('Native_'+method if name in ['BoolTest','CheckAlertRangeByName'] else 'Native')}();if({method}!=null)foreach(hook_{method} hook in {method}.GetInvocationList())'+'{var before=next;next=x=>hook(before,x);}next(self);}}}'
with tempfile.TemporaryDirectory(prefix='local8-baldur-') as folder:
 out=Path(folder);(out/'Hooks.cs').write_text(hooks)
 files=[repo/'src/Features/ColliderGeometry.cs',here/'Doubles.cs',here/'Test.cs',here/'Encounter.cs',out/'Hooks.cs',repo/'src/Features/ElderBaldurRange.cs']
 args=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(f) for f in refs.glob('*.dll')]+list(map(str,files))
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in args));(out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 transplant_args=['-nologo','-target:exe','-langversion:latest','-out:'+str(out/'Transplant.dll')]+['-r:'+str(f) for f in refs.glob('*.dll')]+['-r:'+str(a.managed/'Mono.Cecil.dll'),str(here/'Transplant.cs')]
 (out/'transplant.rsp').write_text('\n'.join('"'+s+'"' for s in transplant_args));shutil.copy2(a.managed/'Mono.Cecil.dll',out/'Mono.Cecil.dll');shutil.copy2(out/'Test.runtimeconfig.json',out/'Transplant.runtimeconfig.json')
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'transplant.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Transplant.dll'),str(a.managed/'Assembly-CSharp.dll'),str(out/'Test.dll'),str(out/'NativeTest.dll')],check=True)
 shutil.copy2(out/'NativeTest.dll',out/'Test.dll')
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

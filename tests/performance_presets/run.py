#!/usr/bin/env python3
from pathlib import Path
import argparse,json,os,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
p=argparse.ArgumentParser();p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));a=p.parse_args();dotnet=shutil.which(a.dotnet) or str(Path(a.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1);sdk=Path(location.rstrip(']'));framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((framework/'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='local8-presets-') as folder:
 out=Path(folder);options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(p) for p in refs.glob('*.dll')]
 options += [str(here/'Doubles.cs'),str(here/'Test.cs')]
 options += [str(root/'src'/p) for p in ['Core/PerformanceConfiguration.cs','Core/PerformancePresets.cs','Core/NativeGraphicsOptions.cs','Core/NativeParticleCopy.cs','Core/Reflect.cs','Core/PerformanceTuning.cs','Core/Local8Settings.cs','Features/PerformanceMenu.cs']]
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options));(out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True);subprocess.run([dotnet,str(out/'Test.dll')],check=True)

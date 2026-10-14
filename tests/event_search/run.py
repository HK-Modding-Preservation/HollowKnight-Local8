#!/usr/bin/env python3
"""Check the live production registry index and its patch on supplied native IL."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
p.add_argument('--managed',required=True);a=p.parse_args()
dotnet=shutil.which(a.dotnet)or str(Path(a.dotnet).resolve());managed=Path(a.managed).resolve()
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((framework/'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='local8-event-search-')as folder:
 out=Path(folder)
 deps=['Mono.Cecil.dll','MonoMod.Utils.dll','MonoMod.RuntimeDetour.dll']
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
 options+=['-r:'+str(f)for f in refs.glob('*.dll')]+['-r:'+str(managed/f)for f in deps]
 options+=[str(here/'Doubles.cs'),str(here/'Test.cs')]+[str(root/'src/Core'/(f+'.cs'))for f in ['FsmRegistryIndex','NativeEventSearch','ImpactMetrics','DamageEventMetrics']]
 (out/'test.rsp').write_text('\n'.join('"'+s+'"'for s in options));(out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
 for f in deps:shutil.copyfile(managed/f,out/f)
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll'),str(managed/'PlayMaker.dll')],check=True)

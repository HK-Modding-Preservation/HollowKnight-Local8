#!/usr/bin/env python3
"""Execute real new-save hint, rescue/join priority and persisted state with controlled Unity UI."""
from pathlib import Path
import argparse,json,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--dotnet',required=True);a=p.parse_args()
dotnet=str(Path(a.dotnet).resolve());version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='local8-setup-hint-') as folder:
 out=Path(folder)
 files=[here/'Doubles.cs',here/'Test.cs']+[root/'src'/name for name in ['Core/Local8SaveData.cs','Core/RescueHint.cs','Features/CoopSetupHint.cs','Features/JoinHint.cs','Features/RoleDefinition.cs','Features/UiLocalization.cs']]
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(f) for f in refs.glob('*.dll')]+list(map(str,files))+['-resource:'+str(root/'assets'/n)+','+n for n in ['Local8.UI.Text','Local8.Roles.Text']]
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options));(out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True);subprocess.run([dotnet,str(out/'Test.dll')],check=True)

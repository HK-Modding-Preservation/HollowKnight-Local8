#!/usr/bin/env python3
"""Exercise actual profile/stat/audio services on a controlled game host, not Unity."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent; root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));a=p.parse_args()
dotnet=shutil.which(a.dotnet) or str(Path(a.dotnet).resolve())
v,loc=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(loc.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='hk8-profiles-') as folder:
 out=Path(folder)
 # These fields are native serialized data, not a second implementation of charms.
 (out/'NativeFields.cs').write_text('public partial class PlayerData {'+' '.join('public bool equippedCharm_'+str(i)+';' for i in range(1,41))+'}')
 production=['Features/PlayerProfileData.cs','Features/PlayerProfiles.cs','Features/ProfileKeyboard.cs','Features/AdventureStats.cs','Features/AdventureStatsUI.cs','Features/DamageAudioGuard.cs','Core/CharmLoadout.cs','Core/CoopRules.cs','Core/Vitals.cs']
 opts=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(x) for x in refs.glob('*.dll')]
 opts+=[str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'NativeFields.cs')]+[str(root/'src'/x) for x in production]
 (out/'test.rsp').write_text('\n'.join('"'+x+'"' for x in opts));(out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
 subprocess.run([dotnet,str(sdk/v/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

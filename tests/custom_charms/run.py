#!/usr/bin/env python3
"""Execute production charm rules/loadouts and extracted toggle/copy bodies."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent
source=here.parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
args=parser.parse_args()
dotnet=shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'))
framework=sorted((sdk.parent/'packs'/'Microsoft.NETCore.App.Ref').iterdir(),key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
refs=next((framework/'ref').iterdir())
def extract(text,marker):
 start=text.index(marker);opening=text.index('{',start);depth=1;end=opening+1
 while depth:
  depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
charms=(source/'src/Core/Charms.cs').read_text()
pvp=(source/'src/Features/PvpCharms.cs').read_text()
shim='using System;using System.Collections.Generic;namespace KO.HollowKnight8 {internal static partial class Charms {'
for name in ['Toggle','FirstOwned','EditorNeighbor']:
 result='void' if name=='Toggle' else 'int'
 shim+=extract(charms,'internal static '+result+' '+name+'(')
shim+='}internal static class PvpCopy {'+extract(pvp,'static CharmLoadout Copy(')+'internal static CharmLoadout Run(CharmLoadout source)=>Copy(source);}}'
with tempfile.TemporaryDirectory(prefix='local8-custom-charms-') as folder:
 out=Path(folder);(out/'Extracted.cs').write_text(shim)
 files=[here/'Doubles.cs',here/'LegacyCharmGridRules.cs',here/'Test.cs',out/'Extracted.cs',source/'src/Features/VoidHeartRules.cs']+[source/'src/Core'/name for name in ['CharmGridRules.cs','CharmCell.cs','CharmLoadout.cs','CharmExtensions.cs','CoopRules.cs']]
 options=['-nologo','-target:exe','-langversion:latest','-out:'+str(out/'Test.dll')]+['-r:'+str(p) for p in sorted(refs.glob('*.dll'))]+list(map(str,files))
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
 (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

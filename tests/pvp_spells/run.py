#!/usr/bin/env python3
"""Exercise the production spell-duel service, settings and owner-scoped input pump."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));args=ap.parse_args()
dotnet=shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((framework/'ref').iterdir())
def method(path,signature):
    text=(root/path).read_text();start=text.index(signature);opening=text.index('{',start);depth=1;end=opening+1
    while depth:depth+=(text[end]=='{')-(text[end]=='}');end+=1
    return text[start:end]
host='using System;using UnityEngine;using InControl;namespace KO.HollowKnight8 {internal static class Controls{'+method('src/Core/Controls.cs','internal static void Pump(')+'}internal partial class CoopSession{'+method('src/Core/CoopSession.cs','internal void Commit(')+'}}'
with tempfile.TemporaryDirectory(prefix='hk8-pvp-spells-') as folder:
    out=Path(folder);(out/'Host.cs').write_text(host)
    production=['Features/PvpSpellOnly.cs','Core/Local8Settings.cs','Core/PerformanceConfiguration.cs','Core/PvpKind.cs','Core/PvpRules.cs','Features/PvpArenaRules.cs','Features/PlayerProfileData.cs']
    options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(p) for p in refs.glob('*.dll')]
    options += [str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'Host.cs')]+[str(root/'src'/name) for name in production]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
    subprocess.run([dotnet,str(out/'Test.dll')],check=True)

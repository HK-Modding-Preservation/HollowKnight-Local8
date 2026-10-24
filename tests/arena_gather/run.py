#!/usr/bin/env python3
"""Execute the complete production arena gather, Hornet intro and safe placement with controlled Unity IO."""
from pathlib import Path
import argparse,json,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--dotnet',required=True);a=p.parse_args()
dotnet=str(Path(a.dotnet).resolve());version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
def method(text,marker):
 start=text.index(marker);opening=text.index('{',start);depth=1;end=opening+1
 while depth:depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
session=(root/'src/Core/CoopSession.cs').read_text()
host='using System;using UnityEngine;namespace KO.HollowKnight8 {internal sealed partial class CoopSession {'
for marker in ('internal bool IsSafe(','internal bool InRoom(','internal bool FindSafePosition(','internal void Commit('):host+=method(session,marker)
host+='}}'
with tempfile.TemporaryDirectory(prefix='local8-arena-gather-') as folder:
 out=Path(folder);(out/'Host.cs').write_text(host)
 files=[here/'Doubles.cs',here/'Test.cs',out/'Host.cs']+[root/'src'/name for name in ['Core/ArenaGather.cs','Core/ArenaRules.cs','Core/ArenaPoint.cs','Core/ArenaGate.cs','Core/ArenaZone.cs','Core/ArenaLanding.cs','Core/DuelGround.cs','Core/RecoveryRules.cs','Core/PartyRules.cs','Features/SpawnSafety.cs','Features/HornetEncounterFlow.cs']]
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(f) for f in refs.glob('*.dll')]+list(map(str,files))
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options));(out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

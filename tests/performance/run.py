#!/usr/bin/env python3
"""Check production caches, shared-light geometry and joining work with controlled doubles."""
from pathlib import Path
import argparse, json, os, shutil, subprocess, tempfile

here = Path(__file__).resolve().parent
source = here.parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
args = parser.parse_args()
dotnet = shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version, location = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()[-1].rsplit(' [', 1)
sdk = Path(location.rstrip(']'))
framework = sorted((sdk.parent/'packs'/'Microsoft.NETCore.App.Ref').iterdir(), key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
refs = next((framework/'ref').iterdir())

def extract(text, marker):
    start = text.index(marker); opening = text.index('{', start); depth = 1; end = opening + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    return text[start:end]

summons = (source/'src/Core/SummonRouting.cs').read_text()
extracted = 'using System;using System.Collections.Generic;using UnityEngine;namespace KO.HollowKnight8 {internal static class SummonUnderTest {'
for name in ['private static int Kind(', 'private static int CountOwned(']:
    extracted += extract(summons, name).replace('private static', 'internal static')
extracted += '}'
# Execute the actual collision setup block. It must keep every pair while
# looking up each other actor's hierarchy only once, including inactive nodes.
session = (source/'src/Core/CoopSession.cs').read_text()
block = extract(session[session.index('s.Renderers = gameObject2.GetComponentsInChildren<Renderer>'):], 'foreach (PlayerSlot player in Players)')
extracted += 'internal sealed class CollisionUnderTest {internal readonly List<PlayerSlot> Players=new();internal void Join(PlayerSlot s){'+block+'}}}'
production = ['Core/Reflect', 'Core/FramePlayers', 'Core/PerformanceTuning', 'Core/PerformanceConfiguration', 'Core/LightPoint', 'Core/LightVertex', 'Core/LightGeometry', 'Core/LightBlendRules', 'Core/LightBlendGrid', 'Core/LightEdgeFade']
with tempfile.TemporaryDirectory(prefix='local8-performance-') as folder:
    out = Path(folder); (out/'Extracted.cs').write_text(extracted)
    options = ['-nologo', '-target:exe', '-langversion:latest', '-optimize+', '-out:'+str(out/'Test.dll')]
    options += ['-r:'+str(p) for p in sorted(refs.glob('*.dll'))]
    options += list(map(str, [here/'Doubles.cs', here/'Test.cs', here/'Alpha196LightEdgeFade.cs', here/'Alpha196Reflect.cs', out/'Extracted.cs']))
    options += [str(source/'src'/(p+'.cs')) for p in production]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name, 'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet, str(sdk/version/'Roslyn/bincore/csc.dll'), '@'+str(out/'test.rsp')], check=True)
    subprocess.run([dotnet, str(out/'Test.dll')], check=True)

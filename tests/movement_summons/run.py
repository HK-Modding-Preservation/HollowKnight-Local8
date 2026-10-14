#!/usr/bin/env python3
"""Run the production walk routing, spider lifetime, dash reset/transit and FX code."""
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
hooks = (source/'src/Core/Hooks.cs').read_text()
extracted = 'using System;using System.Collections.Generic;using UnityEngine;namespace KO.HollowKnight8 {internal static partial class Hooks {'
extracted += 'private static readonly Dictionary<PlayerSlot,HashSet<WalkArea>> walkAreas=new();'
for name in ['private static bool ExtraWalk(', 'private static void WalkEnter(', 'private static void WalkStay(', 'private static void WalkExit(', 'private static void WalkTouch(', 'private static void RefreshWalk(']:
    extracted += extract(hooks, name)
extracted += '}}'
production = ['src/Core/ActorRecovery.cs', 'src/Features/WeaverlingLifetime.cs', 'src/Features/NativeDashEffects.cs', 'src/Features/CrystalDashTransit.cs', 'src/Features/CrystalDashRules.cs', 'src/Features/CrystalParticleDensity.cs']
with tempfile.TemporaryDirectory(prefix='local8-movement-summons-') as folder:
    out = Path(folder); (out/'Extracted.cs').write_text(extracted)
    options = ['-nologo', '-target:exe', '-langversion:latest', '-out:'+str(out/'Test.dll')]
    options += ['-r:'+str(p) for p in sorted(refs.glob('*.dll'))]
    options += list(map(str, [here/'Doubles.cs', here/'Test.cs', out/'Extracted.cs']))
    options += [str(source/p) for p in production]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name, 'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet, str(sdk/version/'Roslyn/bincore/csc.dll'), '@'+str(out/'test.rsp')], check=True)
    subprocess.run([dotnet, str(out/'Test.dll')], check=True)

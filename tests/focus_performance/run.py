#!/usr/bin/env python3
"""Exercise production Focus mask callbacks and interaction hierarchy indexing."""
from pathlib import Path
import argparse, json, os, re, shutil, subprocess, tempfile
here = Path(__file__).resolve().parent
source = here.parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
args = parser.parse_args()
dotnet = shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version, location = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()[-1].rsplit(' [', 1)
sdk = Path(location.rstrip(']'))
framework = sorted((sdk.parent/'packs'/'Microsoft.NETCore.App.Ref').iterdir(), key=lambda p: tuple(map(int, p.name.split('.'))))[-1]
refs = next((framework/'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='local8-focus-perf-') as folder:
    out = Path(folder)
    hooks = (source/'src/Core/Hooks.cs').read_text()
    methods = []
    for name in ['SpellPoolAction', 'SpellCreateAction']:
        start = re.search(r'\tprivate static void '+name+r'\(', hooks).start()
        opening = hooks.index('{', start); end = opening + 1; depth = 1
        while depth:
            depth += (hooks[end] == '{') - (hooks[end] == '}'); end += 1
        methods.append(hooks[start:end].replace('private static void', 'internal static void', 1))
    (out/'SpawnHookHost.cs').write_text('using UnityEngine;namespace KO.HollowKnight8 {internal static class SpawnHookHost {static CoopSession Session=>Plugin.Self?.Session;'+ '\n'.join(methods) + '}}')
    options = ['-nologo', '-target:exe', '-langversion:latest', '-optimize+', '-out:' + str(out/'Test.dll')]
    options += ['-r:' + str(p) for p in sorted(refs.glob('*.dll'))]
    options += [str(here/'Doubles.cs'), str(here/'Test.cs'), str(here/'SpawnDoubles.cs'), str(out/'SpawnHookHost.cs')]
    options += [str(source/'src/Core'/(p+'.cs')) for p in ['FocusBackdropGuard', 'PlayerLighting', 'InteractionAncestors', 'NativeBackground', 'NativeRenderBudget', 'PerformanceTuning', 'PerformanceConfiguration', 'Reflect', 'VanillaHud', 'ReferenceField']]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': refs.name, 'framework': {'name': 'Microsoft.NETCore.App', 'version': framework.name}}}))
    subprocess.run([dotnet, str(sdk/version/'Roslyn/bincore/csc.dll'), '@'+str(out/'test.rsp')], check=True)
    subprocess.run([dotnet, str(out/'Test.dll')], check=True)

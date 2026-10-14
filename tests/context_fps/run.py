#!/usr/bin/env python3
"""Compare actual context/binding code with alpha218, on controlled .NET doubles."""
from pathlib import Path
import argparse, json, os, shutil, subprocess, tempfile
here = Path(__file__).resolve().parent
root = here.parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
args = parser.parse_args()
dotnet = shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version, location = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()[-1].rsplit(' [', 1)
sdk = Path(location.rstrip(']'))
framework = sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1]
refs = next((framework/'ref').iterdir())

def extract(text, marker):
    start = text.index(marker); opening = text.index('{', start); depth = 1; end = opening + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    return text[start:end]

hooks = (root/'src/Core/Hooks.cs').read_text()
shim = 'using System;using System.Reflection;using System.Collections.Generic;using UnityEngine;using HutongGames.PlayMaker;namespace KO.HollowKnight8 {'
for class_name, fast in [('Hooks', True), ('LegacyHooks', False)]:
    shim += 'internal static class '+class_name+' {internal static CoopSession Session=>Plugin.Self.Session;'
    shim += 'private static readonly Dictionary<Type,FieldInfo[]> referenceFields=new();private static readonly Dictionary<Type,ReferenceField[]> referenceAccesses=new();'
    for marker in ['internal static FieldInfo[] GetReferenceFields(', 'internal static UnityEngine.Object Mapped(', 'internal static void RebindObject(']:
        method = extract(hooks, marker)
        if not fast:
            if marker == 'internal static void RebindObject(': method = (here/'LegacyRebind.cs.txt').read_text()
            method = method.replace('ReferenceField[] array = GetReferenceAccesses(obj.GetType());', 'FieldInfo[] array = GetReferenceFields(obj.GetType());').replace('foreach (ReferenceField fieldInfo in array)', 'foreach (FieldInfo fieldInfo in array)')
        shim += method
    if fast: shim += extract(hooks, 'internal static ReferenceField[] GetReferenceAccesses(')
    shim += '}'
shim += '}'
context = (root/'src/Core/PlayerContext.cs').read_text()
legacy = context.replace('PlayerContext', 'LegacyPlayerContext')
import re
legacy = re.sub(r'private static readonly ReferenceField (\w+) = ReferenceField.For\((Reflect.Field\([^;]+)\)\);', r'private static readonly FieldInfo \1 = \2);', legacy)
# Require the legacy version to contain no executable accessors.
assert 'ReferenceField.For' not in legacy
with tempfile.TemporaryDirectory(prefix='local8-context-fps-') as folder:
    out = Path(folder)
    (out/'Extracted.cs').write_text(shim); (out/'LegacyPlayerContext.cs').write_text(legacy)
    fields = 'public partial class PlayerData {'+' '.join('public bool equippedCharm_'+str(i)+';' for i in range(1,41))+'}'
    (out/'NativeFields.cs').write_text(fields)
    files = [here/'Doubles.cs', here/'Test.cs', out/'Extracted.cs', out/'LegacyPlayerContext.cs', out/'NativeFields.cs']
    files += [root/'src/Core'/(name+'.cs') for name in ['ReferenceField','PlayerContext','Reflect','CharmLoadout','CharmExtensions','Vitals','CoopRules','FsmTextCache','ImpactMetrics','DamageEventMetrics','InvariantText']]
    options = ['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
    options += ['-r:'+str(p) for p in refs.glob('*.dll')]+[str(p) for p in files]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet, str(sdk/version/'Roslyn/bincore/csc.dll'), '@'+str(out/'test.rsp')], check=True)
    subprocess.run([dotnet, str(out/'Test.dll')], check=True)

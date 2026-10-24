#!/usr/bin/env python3
"""Exercise production timed-respawn, death/restoration and HUD code with controlled API doubles."""
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
framework = sorted((sdk.parent / 'packs/Microsoft.NETCore.App.Ref').iterdir(),
                   key=lambda p: tuple(map(int, p.name.split('.'))))[-1]
refs = next((framework / 'ref').iterdir())
session = (source / 'src/Core/CoopSession.cs').read_text()
hud = (source / 'src/Core/Hud.cs').read_text()

def block(text, signature):
    start = text.index(signature)
    opening = text.index('{', start)
    depth, end = 1, opening + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]

# Extract the actual scene-rebuild branch and recovery dispatch rather than
# duplicating their behavior in the test. A clock reset regresses these tests.
restore = block(session, 'if (s.SpawnAsDown)')
recovery = session[session.index('if (p2.Hazard &&'):session.index('if (!p2.Alive)', session.index('if (p2.Hazard &&'))]
extracted = '''using System;using UnityEngine;using GlobalEnums;namespace KO.HollowKnight8 {
internal partial class CoopSession {
''' + block(session, 'internal void Down(') + '\n' + block(session, 'internal void Hazard(') + '''
internal void CompleteRoomRebuild(PlayerSlot s) {var coopSession=this;
''' + restore + '''
}
internal void Dispatch(PlayerSlot p2) {
''' + recovery + '''
}
}
internal static class ProductionHud {
''' + block(hud, 'private static string Status(') + '''
internal static string Label(PlayerSlot p,Local8Runtime plugin)=>Status(p,plugin);
}}
'''
with tempfile.TemporaryDirectory(prefix='local8-respawn-tests-') as temporary:
    out = Path(temporary)
    (out / 'Extracted.cs').write_text(extracted)
    options = ['-nologo', '-target:exe', '-langversion:latest', '-out:' + str(out / 'Test.dll')]
    options += ['-r:' + str(p) for p in refs.glob('*.dll')]
    options += [str(here / 'Doubles.cs'), str(here / 'Test.cs'), str(out / 'Extracted.cs'),
                str(source / 'src/Features/TimedRespawnRecovery.cs')]
    (out / 'test.rsp').write_text('\n'.join('"' + o + '"' for o in options))
    (out / 'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {
        'tfm': refs.name, 'framework': {'name': 'Microsoft.NETCore.App', 'version': framework.name}}}))
    subprocess.run([dotnet, str(sdk / version / 'Roslyn/bincore/csc.dll'), '@' + str(out / 'test.rsp')], check=True)
    subprocess.run([dotnet, str(out / 'Test.dll')], check=True)

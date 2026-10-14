#!/usr/bin/env python3
"""Execute production hurt FX, intro routing, gravity recovery and warp detection."""
from pathlib import Path
import argparse, json, os, shutil, subprocess, tempfile
here = Path(__file__).resolve().parent
root = here.parents[1]
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
a = p.parse_args()
dotnet = shutil.which(a.dotnet) or str(Path(a.dotnet).resolve())
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
header = 'using System;using GlobalEnums;using UnityEngine;using HutongGames.PlayMaker;namespace KO.HollowKnight8 {'
common = '''internal static CoopSession Session=>Plugin.Self.Session;
private static PlayerSlot HeroPlayer(HeroController h)=>Session?.Resolve(h);
private static void RefreshWalk(PlayerSlot p){}
internal static bool Frame(HeroController h,out PlayerSlot p)=>HeroFrame(h,out p);
'''
code = header+'internal static class Hooks {'+common+extract(hooks,'private static bool HeroFrame(')+'}'
code += 'internal static class LegacyHooks {'+common+(here/'LegacyHeroFrame.cs.txt').read_text()+'}'
world = (root/'src/Core/WorldRouting.cs').read_text()
code += '''internal static class WorldRouting {internal static Fsm Current;internal static PlayerSlot warp;internal static Vector3 warpAt;internal static float warpTime;
internal struct Call {internal Fsm Previous;internal PlayerSlot Player;internal Vector3 Position;internal int RoarScope;}
'''+extract(world,'internal static Call Begin(')+extract(world,'internal static void End(')+(here/'LegacyWorldEnd.cs.txt').read_text()+'}'
session = (root/'src/Core/CoopSession.cs').read_text()
code += 'internal static class PulseUnderTest {internal static void New(PlayerSlot p2,GameManager gm){'+extract(session,'if (Time.unscaledTime - p2.LastDamageAt > 4f')+'}internal static void Old(PlayerSlot p2,GameManager gm){'+(here/'LegacyPulse.cs.txt').read_text()+'}}}'
with tempfile.TemporaryDirectory(prefix='local8-impact-fps-') as folder:
    out = Path(folder); (out/'Extracted.cs').write_text(code)
    (out/'Alpha222Roar.cs').write_text((here/'Alpha222Roar.cs.txt').read_text())
    options = ['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
    options += ['-r:'+str(p) for p in refs.glob('*.dll')]
    options += [str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'Extracted.cs'),str(out/'Alpha222Roar.cs')]
    options += [str(root/'src'/p) for p in ['Core/Reflect.cs','Core/CoopRules.cs','Features/DamageParticleBudget.cs','Features/PhysicsGuard.cs','Features/BossIntroMotion.cs','Features/RoarMotionRecovery.cs']]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"'for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
    subprocess.run([dotnet,str(out/'Test.dll')],check=True)

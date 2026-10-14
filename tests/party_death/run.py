#!/usr/bin/env python3
"""Run the real party-death module and hook bodies against controlled API doubles."""
from pathlib import Path
import argparse, json, os, shutil, subprocess, tempfile

here = Path(__file__).resolve().parent
source = here.parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'), help='.NET SDK executable (8 or later)')
args = parser.parse_args()
dotnet = shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
sdks = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()
if not sdks:
    parser.error('A .NET SDK is required.')
version, location = sdks[-1].rsplit(' [', 1)
sdk_root = Path(location.rstrip(']'))
csc = sdk_root / version / 'Roslyn' / 'bincore' / 'csc.dll'
packs = sorted((sdk_root.parent / 'packs' / 'Microsoft.NETCore.App.Ref').iterdir(),
               key=lambda p: tuple(int(n) for n in p.name.split('.')))
framework = packs[-1]
ref_dir = next((framework / 'ref').iterdir())
refs = sorted(ref_dir.glob('*.dll'))
hooks = (source / 'src/Core/Hooks.cs').read_text()

def method(name):
    result_type = 'void' if name == 'FsmEvent' else 'IEnumerator'
    start = hooks.index('\tprivate static ' + result_type + ' ' + name + '(')
    first = hooks.index('{', start)
    depth, end = 1, first + 1
    while depth:
        depth += (hooks[end] == '{') - (hooks[end] == '}')
        end += 1
    return hooks[start:end]

shim = '''using System;using System.Collections;using System.Collections.Generic;using HutongGames.PlayMaker;namespace KO.HollowKnight8 {internal static class DeathHooks {
private static CoopSession Session=>Plugin.Self?.Session;
private static PlayerSlot HeroPlayer(HeroController h)=>Session?.Resolve(h);
private static IEnumerator Empty(){yield break;}
private static readonly HashSet<string> localEvents=new(){"HERO DAMAGED","HERO HEALED","HERO LANDED","HERO DEATH","HERO DEAD","FOCUS COMPLETED","HERO RECOIL","HERO DASH"};
private static void ShareRoar(Fsm f,FsmEvent e){}
private static bool IsVitalsHud(Fsm f)=>false;
private interface IFsmCall {void Invoke(Fsm f);}
private struct EventCall : IFsmCall {internal On.HutongGames.PlayMaker.Fsm.orig_ProcessEvent Original;internal FsmEvent Event;internal FsmEventData Data;public void Invoke(Fsm f)=>Original(f,Event,Data);}
private static void RunFsm<T>(T a,Fsm f,bool frame,ref DamageEventMetrics.Trace trace)where T:struct,IFsmCall=>a.Invoke(f);
private static bool SuppressedHudEvent(Fsm f,FsmEvent evt)=>false;
''' + '\n'.join(method(n) for n in ['Die', 'PlayerDead', 'PlayerDeadHazard', 'FsmEvent']) + '''
internal static IEnumerator Hero(On.HeroController.orig_Die orig,HeroController h)=>Die(orig,h);
internal static IEnumerator Manager(On.GameManager.orig_PlayerDead orig,GameManager g,float wait)=>PlayerDead(orig,g,wait);
internal static IEnumerator Hazard(On.GameManager.orig_PlayerDeadFromHazard orig,GameManager g,float wait)=>PlayerDeadHazard(orig,g,wait);
internal static void Event(On.HutongGames.PlayMaker.Fsm.orig_ProcessEvent orig,Fsm f,FsmEvent e,FsmEventData d)=>FsmEvent(orig,f,e,d);
} internal static class DamageEventMetrics {internal enum Phase{Guard}internal struct Trace:System.IDisposable {internal void Mark(Phase phase){}public void Dispose(){}}internal static Trace Begin(Fsm f)=>default;} }'''

with tempfile.TemporaryDirectory(prefix='local8-death-tests-') as temporary:
    output = Path(temporary)
    (output / 'ExtractedHooks.cs').write_text(shim)
    options = ['-nologo', '-target:exe', '-langversion:latest', '-out:' + str(output / 'Test.dll')]
    options += ['-r:' + str(p) for p in refs]
    options += [str(here / 'Doubles.cs'), str(here / 'Test.cs'), str(output / 'ExtractedHooks.cs'),
                str(source / 'src/Features/PartyDeath.cs')]
    (output / 'test.rsp').write_text('\n'.join('"' + option + '"' for option in options))
    (output / 'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {
        'tfm': ref_dir.name, 'framework': {'name': 'Microsoft.NETCore.App', 'version': framework.name}}}))
    subprocess.run([dotnet, str(csc), '@' + str(output / 'test.rsp')], check=True)
    subprocess.run([dotnet, str(output / 'Test.dll')], check=True)

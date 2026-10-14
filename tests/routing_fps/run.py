#!/usr/bin/env python3
"""Exercise actual event wrappers, negative source probes and input recovery against alpha220."""
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
    start = text.index(marker); opening = text.index('{', start)
    arrow = text.find('=>', start, opening)
    if arrow >= 0: return text[start:text.index(';', arrow)+1]
    depth = 1; end = opening + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    return text[start:end]
hooks = (root/'src/Core/Hooks.cs').read_text()
header = 'using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using HutongGames.PlayMaker;using HutongGames.PlayMaker.Actions;using Scenes=UnityEngine.SceneManagement.SceneManager;namespace KO.HollowKnight8 {'
common = '''internal static CoopSession Session=>Plugin.Self.Session;
private static readonly HashSet<string> localEvents=new(){"HERO DAMAGED","HERO DEATH","HERO DEAD","HERO HEALED","HERO LANDED","FOCUS COMPLETED","HERO RECOIL","HERO DASH"};
private static PlayerSlot FsmPlayer(Fsm f,out bool enemy){enemy=f.Enemy;Recorder.Add("route:"+f.Name);Recorder.ThrowAt("route");return f.Player;}
private static void Retarget(Fsm f,PlayerSlot p){Recorder.Add("bind:"+f.Name);Recorder.ThrowAt("bind");}
private static bool IsVitalsHud(Fsm f)=>f.Hud;
private static void ShareRoar(Fsm f,FsmEvent e)=>Recorder.Add("roar");
internal static void Event(On.HutongGames.PlayMaker.Fsm.orig_ProcessEvent o,Fsm f,FsmEvent e,FsmEventData d)=>FsmEvent(o,f,e,d);
internal static void Awake(On.HutongGames.PlayMaker.Fsm.orig_Awake o,Fsm f)=>FsmAwake(o,f);
internal static void Enable(On.HutongGames.PlayMaker.Fsm.orig_OnEnable o,Fsm f)=>FsmEnable(o,f);
internal static void Start(On.HutongGames.PlayMaker.Fsm.orig_Start o,Fsm f)=>FsmStart(o,f);
'''
code = header+'internal static class Hooks {'+common
for marker in ['private interface IFsmCall', 'private struct AwakeCall', 'private struct EnableCall', 'private struct StartCall', 'private struct EventCall', 'private static void RunFsm<T>(', 'private static void FsmAwake(', 'private static void FsmEnable(', 'private static void FsmStart(', 'private static void FsmEvent(', 'private static bool SuppressedHudEvent(']:
    code += extract(hooks, marker)
code += '} internal static class LegacyHooks {'+common+(here/'LegacyHooks.cs.txt').read_text()+'}}'
combat = (root/'src/Core/PvpCombat.cs').read_text()
code += 'namespace KO.HollowKnight8 {internal static partial class PvpCombat {internal static readonly float[] lastCast=new float[8];internal sealed class Attack{internal PvpKind Kind;internal Collider2D Collider;internal PlayerSlot Owner;internal readonly PvpSwing Swing=new();}internal static void Correct(CoopSession s,Attack a,bool legacy){if(legacy)LegacyCorrectSpellOwner(s,a);else CorrectSpellOwner(s,a);}'
code += extract(combat,'private static void CorrectSpellOwner(')+(here/'LegacySpellOwner.cs.txt').read_text()+'}}'
source = (root/'src/Features/SharedAcquisition.cs').read_text()
acquisition_common = '''private sealed class Identity {internal string Key;internal int Value;internal GameObject Pickup;}
private sealed class Miss {internal FsmState[] States;internal float Until;}
private static readonly Dictionary<Fsm,Identity> sources=new();private static readonly Dictionary<Fsm,Miss> misses=new();
internal static bool Replaying;private static CoopSession Current=>Plugin.Self.Session;
internal static void Reset(){sources.Clear();misses.Clear();Replaying=false;}
internal static void Prime(Fsm f)=>Kind(f);
private static bool Valid(PlayerSlot p)=>p!=null&&p.Hero;
private static bool Starts(FsmState state,Identity identity)=>state!=null&&state.Name=="Acquire";
private static PlayerSlot Route(PlayerSlot p,Fsm f)=>p;
private static PlayerSlot Collector(Fsm f,string key)=>Current.Primary;
private static void Early(Fsm f,PlayerSlot p,Identity i,bool proof)=>Recorder.Add("acquire:"+i.Key+":"+p?.Index);
'''
for class_name, legacy in [('AcquisitionUnderTest',False),('LegacyAcquisition',True)]:
    code += 'namespace KO.HollowKnight8 {internal static class '+class_name+' {'+acquisition_common
    for marker in ['private static Identity NativePickup(', 'private static Identity Kind(', 'private static bool WorldSource(']: code += extract(source,marker)
    code += (here/'LegacyBeforeEvent.cs.txt').read_text() if legacy else extract(source,'internal static void BeforeEvent(')
    code += '}}'
shade = (root/'src/Features/ShadeCloakRitual.cs').read_text()
for class_name, legacy in [('ShadeUnderTest',False),('LegacyShade',True)]:
    code += 'namespace KO.HollowKnight8 {internal static class '+class_name+' {'
    code += 'sealed class Probe {internal FsmState[] States;internal float Next;}static readonly Dictionary<Fsm,string> kinds=new();static readonly Dictionary<Fsm,Probe> unrecognized=new();internal static string Describe(Fsm f)=>Kind(f);internal static void Reset(){kinds.Clear();unrecognized.Clear();}static string Spell(string key)=>key=="fireballLevel"?"Shade Soul":key=="quakeLevel"?"Descending Dark":key=="screamLevel"?"Abyss Shriek":null;'
    code += extract(shade,'static string Acquisition(')
    code += (here/'LegacyShadeKind.cs.txt').read_text() if legacy else extract(shade,'static string Kind(')
    code += '}}'
menu = (root/'src/Features/MenuInputRecovery.cs').read_text()
legacy = menu.replace('internal static class MenuInputRecovery', 'internal static class LegacyMenuInputRecovery')
start = legacy.index('  // Both native module types');end = legacy.index('\n }\n internal static void Reset()',start)
legacy = legacy[:start]+'''  foreach(var module in Object.FindObjectsOfType<InControlInputModule>())if(module){
   module.MoveAction=Repair(module.MoveAction,a.moveVector);module.SubmitAction=Repair(module.SubmitAction,a.menuSubmit);module.CancelAction=Repair(module.CancelAction,a.menuCancel);
  }
  foreach(var module in Object.FindObjectsOfType<HollowKnightInputModule>())if(module){
   module.MoveAction=Repair(module.MoveAction,a.moveVector);module.SubmitAction=Repair(module.SubmitAction,a.menuSubmit);module.CancelAction=Repair(module.CancelAction,a.menuCancel);
   module.JumpAction=Repair(module.JumpAction,a.jump);module.AttackAction=Repair(module.AttackAction,a.attack);module.CastAction=Repair(module.CastAction,a.cast);
  }'''+legacy[end:]
with tempfile.TemporaryDirectory(prefix='local8-routing-fps-') as folder:
    out = Path(folder); (out/'Extracted.cs').write_text(code); (out/'LegacyMenu.cs').write_text(legacy)
    options = ['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
    options += ['-r:'+str(p) for p in refs.glob('*.dll')]
    options += [str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'Extracted.cs'),str(out/'LegacyMenu.cs')]
    options += [str(root/'src'/p) for p in ['Core/InvariantText.cs','Core/DamageEventMetrics.cs','Core/ImpactMetrics.cs','Core/SpawnMetrics.cs','Core/PvpKind.cs','Features/MenuInputRecovery.cs']]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"'for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
    subprocess.run([dotnet,str(out/'Test.dll')],check=True)

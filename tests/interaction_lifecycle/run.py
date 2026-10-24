#!/usr/bin/env python3
"""Run production interaction ownership, control hooks, charm gating and recovery.

The native .NET host cannot execute Unity; it exercises production methods
with explicit scene/input/body doubles. Frozen alpha226 methods demonstrate
the inventory/bench false-ownership regression before the new checks.
"""
from pathlib import Path
import argparse, json, subprocess, tempfile

here = Path(__file__).resolve().parent
root = here.parents[1]
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--dotnet', required=True)
a = p.parse_args()
dotnet = str(Path(a.dotnet).resolve())
version, location = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()[-1].rsplit(' [', 1)
sdk = Path(location.rstrip(']'))
framework = sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1]
refs = next((framework/'ref').iterdir())

def extract(text, marker):
    start = text.index(marker)
    opening = text.index('{', start)
    arrow = text.find('=>', start, opening)
    if arrow >= 0:
        return text[start:text.index(';', arrow)+1]
    depth, end = 1, opening+1
    while depth:
        depth += (text[end]=='{')-(text[end]=='}')
        end += 1
    return text[start:end]

router = (root/'src/Core/InteractionRouter.cs').read_text()
legacy = (here/'LegacyRouter.cs.txt').read_text()
header = 'using System;using System.Collections.Generic;using UnityEngine;using UnityEngine.EventSystems;using GlobalEnums;using HutongGames.PlayMaker;using HutongGames.PlayMaker.Actions;namespace KO.HollowKnight8 {'
code = header
for name, text in [('InteractionRouter', router), ('LegacyRouter', legacy)]:
    code += 'internal static class '+name+' {'+extract(text,'private sealed class Entry')
    code += '''
    static readonly Dictionary<Fsm,Entry> entries=new();static Entry active;
    internal static bool ActiveIsBench=>active!=null&&active.Busy&&active.Bench;
    internal static void Seed(Fsm f,PlayerSlot p,bool bench=false){entries[f]=new Entry{Fsm=f,Owner=p,Bench=bench,Up=true,Triggers=Array.Empty<Collider2D>(),Subscribed=Array.Empty<TriggerEnterEvent>(),RequestedAt=Time.unscaledTime};}
    internal static void Clear(){entries.Clear();active=null;}
    internal static PlayerSlot Owner(Fsm f)=>entries[f].Owner;
    internal static void Select(Fsm f)=>Choose(entries[f]);
    internal static PlayerSlot Resolve(Fsm f){var e=Describe(f);if(e==null)return null;Choose(e);return e.Owner;}
    static Entry Describe(Fsm f)=>f!=null&&!f.Ui&&!f.Actor&&entries.TryGetValue(f,out var e)?e:null;
    static string Kind(Fsm f)=>(f.Name+" "+f.GameObject.name).ToLowerInvariant();
    static bool Inside(Entry e,PlayerSlot p)=>p!=null&&e.Fsm.Range.Contains(p);
    static bool Eligible(Entry e,PlayerSlot p)=>Inside(e,p);
    static bool BenchSeatNear(Fsm f,PlayerSlot p)=>f.Range.Contains(p);
    static bool CompetingPrompt(PlayerSlot p,Entry e)=>false;
    static Vector2 PromptAnchor(Entry e,Vector2 at)=>new();
    '''
    for marker in ['internal static PlayerSlot ActivePlayer', 'private static bool Valid(', 'private static bool Request(', 'private static void Choose(', 'private static void Refresh(']:
        code += extract(text, marker)
    if name == 'InteractionRouter':
        for marker in ['internal static bool Holds(', 'internal static string ActiveSummary', 'internal static void ControlClaimed(', 'internal static void ControlReturned(', 'private static bool WaitingForRequest(', 'private static void End(']:
            code += extract(router, marker)
    code += '}'
hooks=(root/'src/Core/Hooks.cs').read_text()
code+='internal static class Hooks {internal static CoopSession Session=>Plugin.Self.Session;static PlayerSlot FsmPlayer(Fsm f,out bool enemy){enemy=false;return HornetEncounterFlow.Resolve(f)??InteractionRouter.Resolve(f)??WorldRouting.Resolve(f);}static void RebindObject(object action,PlayerSlot p){}internal static void Call(On.HutongGames.PlayMaker.Actions.CallMethodProper.orig_OnEnter o,CallMethodProper action)=>InteractionCall(o,action);internal static void State(On.HutongGames.PlayMaker.FsmState.orig_OnEnter o,FsmState state)=>StateEnter(o,state);'
for marker in ['private static void InteractionCall(', 'private static void StateEnter(']:code+=extract(hooks,marker)
code+='}'
code+='internal partial class CoopSession {readonly Dictionary<PlayerSlot,float> stuckSince=new();readonly Dictionary<PlayerSlot,Vector3> stuckPosition=new();internal int VisualRestores;internal static void RestoreLivingVisuals(PlayerSlot p)=>Plugin.Self.Session.VisualRestores++;internal void Check(PlayerSlot p)=>CheckStuckControl(p);'
code+=extract((root/'src/Core/CoopSession.cs').read_text(),'private void CheckStuckControl(')+'}'
code+='internal static class CharmMenuExit {'+extract((root/'src/Features/CharmMenuExit.cs').read_text(),'internal static bool NativeActive(')+'internal static void OnExit(){} }'
charms=(root/'src/Core/Charms.cs').read_text()
code+='''internal static class Charms {
 static readonly Rigidbody2D[] frozen=new Rigidbody2D[8];static readonly bool[] wasKinematic=new bool[8];
 static bool[] submitHeld=new bool[8];internal static bool wasNative;static int nativeFrame=-1;static GameObject nativePane;static InvCharmBackboard[] nativeBoards=Array.Empty<InvCharmBackboard>();static float nextBoardProbe;static bool nativeVisible;
 internal static bool NativeMenuOpen=>nativeVisible&&CharmMenuExit.NativeActive(true);internal static string NativeProbeReason="hidden";internal static GameObject NativePane=>nativePane;
 internal static void Visible(GameObject pane){nativePane=pane;nativeVisible=pane;}
'''
for marker in ['internal static void Freeze(', 'private static void Release(', 'internal static void ReleaseNative(']:code+=extract(charms,marker)
code+='}}'
with tempfile.TemporaryDirectory(prefix='local8-interaction-lifecycle-') as folder:
    out=Path(folder)
    (out/'Host.cs').write_text(code)
    options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
    options+=['-r:'+str(p) for p in refs.glob('*.dll')]
    options += [str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'Host.cs'),str(root/'src/Features/InteractionControlLifecycle.cs'),str(root/'src/Features/HornetEncounterFlow.cs')]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
    subprocess.run([dotnet,str(out/'Test.dll')],check=True)

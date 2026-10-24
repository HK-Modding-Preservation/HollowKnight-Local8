#!/usr/bin/env python3
"""Exercise complete production voting/visibility and actual door selection logic.

Unity rendering/physics and the native loading boundary are explicit doubles.
The vote, fade hooks, door eligibility and targeted interaction release are real.
"""
from pathlib import Path
import argparse, json, subprocess, tempfile

here=Path(__file__).resolve().parent; root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__); p.add_argument('--dotnet',required=True); a=p.parse_args()
dotnet=str(Path(a.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']')); framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1]
refs=next((framework/'ref').iterdir())
def extract(text,marker):
 start=text.index(marker); opening=text.index('{',start); depth=1; end=opening+1
 while depth: depth+=(text[end]=='{')-(text[end]=='}'); end+=1
 return text[start:end]
door=(root/'src/Core/Doorways.cs').read_text(); router=(root/'src/Core/InteractionRouter.cs').read_text()
host='''using System;using System.Collections.Generic;using UnityEngine;using GlobalEnums;using HutongGames.PlayMaker;using HutongGames.PlayMaker.Actions;
namespace KO.HollowKnight8 {internal static class Doorways {
'''+extract(door,'private sealed class Door')+'''
 static readonly List<Door> doors=new();static readonly bool[] held=new bool[8];internal static float scanAt;
 internal static bool Prompt;
 static bool EntryPromptVisible()=>Prompt;
 static void Scan(CoopSession s){scanAt=Time.unscaledTime+2;}
 internal static void Reset(){doors.Clear();Array.Clear(held,0,held.Length);scanAt=0;Prompt=false;}
 internal static void Seed(PlayMakerFSM f){doors.Add(new Door{Machine=f,Colliders=Array.Empty<Collider2D>()});}
'''
for marker in ['internal static GameObject WaitSource(', 'internal static bool Tick(', 'internal static bool Destination(', 'private static bool Near(Door ', 'private static bool Near(TransitionPoint ', 'private static bool LockedNearby(', 'private static bool PrerequisiteMet(', 'private static bool Ready(']:
 host+=extract(door,marker)
host+='}internal static class InteractionRouter {'+extract(router,'private sealed class Entry')+'''
 static readonly Dictionary<Fsm,Entry> entries=new();static Entry active;
 internal static PlayerSlot ActivePlayer=>active!=null&&active.Busy?active.Owner:null;
 internal static void Reset(){entries.Clear();active=null;}
 internal static void Seed(Fsm f,PlayerSlot p,bool scene){var e=new Entry{Fsm=f,Owner=p,Scene=scene,Busy=true,ControlHero=p.Hero,ControlState=f.ActiveState};entries[f]=e;active=e;}
 internal static bool Busy(Fsm f)=>entries[f].Busy;
'''+extract(router,'internal static Fsm CompleteRoomEntry(')+extract(router,'private static void End(')+'}}'
with tempfile.TemporaryDirectory(prefix='local8-room-wait-') as folder:
 out=Path(folder);(out/'Host.cs').write_text(host)
 (out/'LegacyVote.cs').write_text((here/'LegacyVote.cs.txt').read_text().replace('internal static class TransitionVote','internal static class LegacyVote'))
 files=[here/'Doubles.cs',here/'Test.cs',out/'Host.cs',out/'LegacyVote.cs']+[root/'src'/f for f in ['Core/TransitionVote.cs','Features/RoomWaitVisibility.cs','Features/StagTravelRules.cs']]
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(f) for f in refs.glob('*.dll')]+list(map(str,files))
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
 (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

#!/usr/bin/env python3
"""Execute production spawn geometry, joining, recovery and finish-spawn lifecycle.

Unity queries run against a controlled rectangular collision world. Native
initialization, rendering and scene activation remain explicit boundaries.
"""
from pathlib import Path
import argparse,json,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--dotnet',required=True);a=p.parse_args()
dotnet=str(Path(a.dotnet).resolve());version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
def method(text,marker):
 start=text.index(marker);o=text.index('{',start);depth=1;end=o+1
 while depth:depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
session=(root/'src/Core/CoopSession.cs').read_text();spawn=method(session,'private void Spawn(')
guard=spawn[spawn.index('{')+1:spawn.index('\n\t\ts.SpawnPending = true;')]
host='''using System;using System.Linq;using System.Collections;using System.Collections.Generic;using System.Runtime.CompilerServices;using System.Diagnostics;using UnityEngine;using GlobalEnums;using Object=UnityEngine.Object;
namespace KO.HollowKnight8 {internal sealed partial class CoopSession {
 internal int sceneEpoch,NativeCreates;internal PlayerSlot Cloning;internal string scene="Test";internal Runtime plugin=new();
 internal IEnumerator Finish(PlayerSlot p)=>FinishSpawn(p,sceneEpoch);
 internal void Resume(PlayerSlot p)=>ResumeParked(p);
 internal void InitialGuard(PlayerSlot s){'''+guard+'''NativeCreates++;}
 internal void PlayerFault(PlayerSlot p,string context,Exception e){throw new Exception(context,e);}
 internal void RegisterHierarchy(PlayerSlot p){}
'''
for marker in ['private sealed class __iterator__FinishSpawn_d__62', 'private IEnumerator FinishSpawn(', 'private void Park(', 'private void ResumeParked(', 'private void DelaySpawn(', 'private bool TryJoinDestination(', 'internal bool IsSafe(', 'internal bool InRoom(', 'internal bool FindSafePosition(', 'internal PlayerSlot LongestLiving(', 'internal void Recover(', 'private bool GameCheckpoint(', 'private static bool AwayFromHazard(', 'internal void Commit(']:host+=method(session,marker)
host+=method((here/'LegacyJoin.cs.txt').read_text(),'private Vector3 JoinDestination(').replace('private Vector3 JoinDestination(','internal Vector3 LegacyJoinDestination(')
host+='}internal static class LegacyPlacement {static bool ClearAt(PlayerSlot p,Vector3 at)=>SpawnSafety.ClearAt(p,at);static bool NearAlly(CoopSession s,PlayerSlot a,PlayerSlot p,out Vector3 at)=>SpawnSafety.NearAlly(s,a,p,out at);'
host+=method((here/'LegacyRecovery.cs.txt').read_text(),'internal static bool Recovery(')+'}'
host+='internal static class GroupWarp {internal static PlayerSlot warp;internal static float warpTime;internal static Vector3 warpAt;'+method((root/'src/Core/WorldRouting.cs').read_text(),'internal static void Tick(')+'}'
host+='internal static class ShrineGather {static bool gathered;static float nextGather;internal static void Run(CoopSession s){gathered=false;nextGather=0;GatherAtShrine(s);}'+method((root/'src/Core/DreamSequence.cs').read_text(),'private static void GatherAtShrine(')+'}'
host+='internal static class InteractionRouter {'+method((root/'src/Core/InteractionRouter.cs').read_text(),'internal static bool DoorFallbackTick(')+'}'
host+='internal static partial class EmergencyWarp {'+method((root/'src/Core/EmergencyWarp.cs').read_text(),'internal static void Restore(')+'}}'
with tempfile.TemporaryDirectory(prefix='local8-spawn-safety-') as folder:
 out=Path(folder);(out/'Host.cs').write_text(host)
 files=[here/'Doubles.cs',here/'Test.cs',here/'MotionTest.cs',here/'CrushTest.cs',out/'Host.cs']+[root/'src'/name for name in ['Features/FallingPlatformHazard.cs','Features/SpawnSafety.cs','Features/EntryGravityRecovery.cs','Features/TerrainEmbedRecovery.cs','Core/DuelGround.cs','Core/RecoveryRules.cs','Core/PartyRules.cs','Core/ArenaLanding.cs']]
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(f) for f in refs.glob('*.dll')]+list(map(str,files))
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options));(out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

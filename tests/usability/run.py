#!/usr/bin/env python3
"""Exercise real alpha225 PvP entry/restoration, Escape and neutral-input repairs."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
p=argparse.ArgumentParser();p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));a=p.parse_args();dotnet=shutil.which(a.dotnet) or str(Path(a.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1);sdk=Path(location.rstrip(']'));framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((framework/'ref').iterdir())
def method(text,marker):
 start=text.index(marker);opening=text.index('{',start);depth=1;end=opening+1
 while depth:depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
match=(root/'src/Core/PvpMatch.cs').read_text();hooks=(root/'src/Core/Hooks.cs').read_text();arena=(root/'src/Features/PvpArena.cs').read_text()
code='using System;using System.Collections;using System.Collections.Generic;using System.Reflection;using UnityEngine;using GlobalEnums;namespace KO.HollowKnight8 {'
code+='internal static class PvpMatch {'+method(match,'internal sealed class Before')
code+='internal static readonly List<Before> roster=new();static int[] sides=new int[8];static bool[] alive=new bool[8];internal static int state,round;static int bestOf;static float remaining;static bool requested;static Dictionary<int,int> seriesWins=new();internal static string StartIssue="",Result="";internal static bool Running=>state!=0;internal static bool TestBegin(CoopSession s)=>Begin(s);'
for marker in ['private static bool Reject(','private static bool Begin(','private static void NextRound(','internal static Before Saved(','internal static void Stop(']:code+=method(match,marker)
code+='}internal static class Hooks {internal static CoopSession Session=>Plugin.Self.Session;static float lastPauseModalAt;static bool pauseRecoveryCandidate;static float nextPauseControlSnapshot;static IEnumerator Empty(){yield break;}internal static bool TestFree(CoopSession s,GameManager gm,InputHandler input)=>FreeToPause(s,gm,input);internal static IEnumerator TestToggle(On.GameManager.orig_PauseGameToggle orig,GameManager gm)=>PauseToggle(orig,gm);internal static void TestInput(On.InputHandler.orig_Update orig,InputHandler input)=>InputUpdate(orig,input);'
for marker in ['private static IEnumerator PauseToggle(','private static bool FreeToPause(','private static void InputUpdate(']:code+=method(hooks,marker)
code+='}internal static class ArenaUnderTest {'+method(arena,'sealed class Before')
code+='enum Stage {None,Loading,Returning}static Stage stage;internal static bool Active=>stage!=Stage.None;static PvpArenaOptions Options=>PvpArena.Options;static CoopSession Current=>Plugin.Self.Session;static List<Before> roster=new();static Dictionary<FieldInfo,object> world=new();static Dictionary<int,int> wins=new();static int[] sides=new int[8];static bool[] alive=new bool[8];static int round,bestOf,oldMode;static float deadline;static CoopSession session;static string sourceRoom,arenaRoom;static PvpArenaOptions rules;static FieldInfo[] vitalFields=typeof(Vitals).GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);static void Back(){}static void Issue(string key){PvpMatch.StartIssue=key;}static void Travel(string room){PvpArena.Travels++;}static void Place(PlayerSlot p,Vector3 at){session.Recover(p,true,at);}internal static void TestRestore(){RestoreAll(true);}static void Clear(){stage=Stage.None;}internal static Vector3 Saved(PlayerSlot p)=>roster.Find(x=>x.Player==p).Position;internal static void Reset(){stage=Stage.None;roster.Clear();}'
for marker in ['internal static bool StartRequest(','static object CopyValue(','static Vitals CopyVitals(Vitals from)','static void CopyVitals(Vitals from,Vitals to)','static void RestoreWorld(','static void RestoreAll(']:code+=method(arena,marker)
code+='}}'
code=code.replace('UnitySceneManager.GetActiveScene()','UnityEngine.SceneManagement.SceneManager.GetActiveScene()')
with tempfile.TemporaryDirectory(prefix='local8-usability-') as folder:
 out=Path(folder);(out/'Host.cs').write_text(code)
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]+['-r:'+str(p) for p in refs.glob('*.dll')]
 options += [str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'Host.cs')]+[str(root/'src'/p) for p in ['Features/PvpStartPosition.cs','Features/MenuInputRecovery.cs','Core/MenuInput.cs','Core/PvpRules.cs','Features/JumpEffectLifetime.cs','Core/Reflect.cs','Core/ReferenceField.cs']]
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options));(out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True);subprocess.run([dotnet,str(out/'Test.dll')],check=True)

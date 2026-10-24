#!/usr/bin/env python3
"""Run production Fury, hazard and shared stag-route code with controlled Unity IO."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));a=p.parse_args()
dotnet=shutil.which(a.dotnet) or str(Path(a.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
def method(text,signature):
 start=text.index(signature);opening=text.index('{',start);depth=1;end=opening+1
 while depth:depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
session=(root/'src/Core/CoopSession.cs').read_text();hooks=(root/'src/Core/Hooks.cs').read_text();votes=(root/'src/Core/TransitionVote.cs').read_text()
legacy=(here/'LegacyDirect.cs.txt').read_text()
code='''using System;using System.Collections;using UnityEngine;using GlobalEnums;
namespace KO.HollowKnight8 {
internal partial class CoopSession {
'''+method(session,'internal void Hazard(')+'''}
internal static class ReportHooks {
 private static CoopSession Session=>Plugin.Self.Session;
 private static PlayerSlot HeroPlayer(HeroController hero)=>Session.Resolve(hero);
 private static IEnumerator Empty(){yield break;}
 internal static IEnumerator Run(On.HeroController.orig_DieFromHazard orig,HeroController hero,HazardType kind,float angle)=>Hazard(orig,hero,kind,angle);
'''+method(hooks,'private static IEnumerator Hazard(')+'''}
internal static class VoteProbe {
 internal static int Queued;
 internal static void Reset(){Queued=0;}
 private static bool Holding(PlayerSlot p)=>false;
 private static bool CanDefer(CoopSession s,PlayerSlot p)=>s.Active;
 private static void Add(PlayerSlot p,string scene,Action commit,string entryGate=null,GameObject door=null){Queued++;}
'''+method(votes,'internal static bool Direct(')+method(votes,'internal static bool Gate(')+method(votes,'private static bool BossFlow(')+method(votes,'private static bool DreamFlow(')+method(legacy,'internal static bool Direct(').replace('bool Direct(','bool LegacyDirect(')+'}}'
with tempfile.TemporaryDirectory(prefix='local8-community-report-') as directory:
 out=Path(directory);(out/'Extracted.cs').write_text(code)
 production=['Features/FuryRecovery.cs','Features/HazardFeedback.cs','Features/StagTravelRules.cs','Core/Reflect.cs','Core/PerformanceConfiguration.cs']
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
 options+=['-r:'+str(f) for f in refs.glob('*.dll')]
 options+=list(map(str,[here/'Doubles.cs',here/'Test.cs',out/'Extracted.cs']+[root/'src'/name for name in production]))
 (out/'test.rsp').write_text('\n'.join('"'+option+'"' for option in options))
 (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

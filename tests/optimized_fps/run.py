#!/usr/bin/env python3
"""Run the production resolver, interaction selection and vertex projector."""
from pathlib import Path
import argparse, json, os, shutil, subprocess, tempfile
here=Path(__file__).resolve().parent
source=here.parents[1]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
a=p.parse_args();dotnet=shutil.which(a.dotnet)or str(Path(a.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'))
framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir(),key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
refs=next((framework/'ref').iterdir())
def extract(text,marker):
 start=text.index(marker);opening=text.index('{',start);depth=1;end=opening+1
 while depth:
  depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
session=(source/'src/Core/CoopSession.cs').read_text()
code='using System;using System.Linq;using System.Collections.Generic;using UnityEngine;using HutongGames.PlayMaker;namespace KO.HollowKnight8 {'
code+='internal sealed class CoopSession {internal readonly List<PlayerSlot> Players=new();internal PlayerSlot Primary,Cloning;private readonly Dictionary<int,PlayerSlot>owners=new();private readonly HashSet<int>noOwnerThisFrame=new();private int ownerFrame=-1;private readonly OwnerQueryCache ownerQueries=new();internal PlayerSlot Raw(object obj)=>ResolveUncached(obj);'
for marker in ['internal void RegisterHierarchy(', 'internal void RefreshOwnership(', 'internal PlayerSlot Resolve(', 'private PlayerSlot ResolveUncached(']:code+=extract(session,marker)
code+='}'
interaction=(source/'src/Core/InteractionRouter.cs').read_text()
choose=extract(interaction,'private static void Choose(')
legacy=choose.replace('Valid(player) && Request(e, player) && (Eligible(e, player) || player == touched)','Valid(player) && (Eligible(e, player) || player == touched) && Request(e, player)').replace('private static void Choose(','private static void ChooseLegacy(')
code+='internal static class InteractionUnderTest {internal static Entry active;internal static int EligibleCalls;internal static void Run(Entry e,PlayerSlot touched)=>Choose(e,touched);internal static void RunLegacy(Entry e,PlayerSlot touched)=>ChooseLegacy(e,touched);private static bool Valid(PlayerSlot p)=>p!=null&&p.Valid;private static bool Eligible(Entry e,PlayerSlot p){EligibleCalls++;return p.Eligible;}private static bool Inside(Entry e,PlayerSlot p)=>Valid(p)&&p.Inside;private static bool BenchSeatNear(Fsm f,PlayerSlot p)=>p.Seat;private static bool CompetingPrompt(PlayerSlot p,Entry e)=>p.Competing;private static Vector2 PromptAnchor(Entry e,Vector2 at)=>new Vector2(0,0);private static string Kind(Fsm f)=>(f.Name+" "+f.GameObject.name).ToLowerInvariant();private static void Refresh(Entry e){}'
code+=extract(interaction,'private static bool Request(')+choose+legacy+'}}'
with tempfile.TemporaryDirectory(prefix='local8-optimized-fps-')as folder:
 out=Path(folder);(out/'Extracted.cs').write_text(code)
 options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
 options+=['-r:'+str(p)for p in refs.glob('*.dll')]
 options+=[str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'Extracted.cs')]
 options+=[str(source/'src/Core'/(p+'.cs'))for p in ['OwnerQueryCache','LightProjection','LightVertex']]
 (out/'test.rsp').write_text('\n'.join('"'+s+'"'for s in options))
 (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)

#!/usr/bin/env python3
"""Exercise production UI ownership, Pale Court guards/callback repair and emitted IL."""
from pathlib import Path
import argparse,json,os,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent
source=here.parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
parser.add_argument('--managed',required=True)
args=parser.parse_args()
dotnet=shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'))
framework=sorted((sdk.parent/'packs'/'Microsoft.NETCore.App.Ref').iterdir(),key=lambda p:tuple(map(int,p.name.split('.'))))[-1]
refs=next((framework/'ref').iterdir())
managed=Path(args.managed).resolve()
def extract(text,marker):
 start=text.index(marker);opening=text.index('{',start);depth=1;end=opening+1
 while depth:
  depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
shop=(source/'src/Core/ShopMenuRouting.cs').read_text()
exit=(source/'src/Features/CharmMenuExit.cs').read_text()
pale=(source/'src/Features/PaleCourtInterop.cs').read_text()
shim='using System;using UnityEngine;using UnityEngine.EventSystems;using GlobalEnums;using HutongGames.PlayMaker;using MonoMod.Cil;using Mono.Cecil.Cil;namespace KO.HollowKnight8 { '
shim+='internal static partial class ShopMenuRouting {'+extract(shop,'internal static bool IsShopList(')+extract(shop,'internal static void Observe(')+'}'
shim+='internal static class CharmMenuExit {'+extract(exit,'internal static bool NativeActive(')+'}'
# Test the exact IL manipulator bodies without installing native detours on a
# foreign .NET runtime. Unity's Mono runtime is the deployment target.
shim+='internal static class Patches {'
for local,name in [('start','Start'),('check','Stage'),('level','Bloom'),('enable','Boon')]:
 body=extract(pale,'hooks.Add(new ILHook('+local+', il =>')
 body=body[body.index('{'):]
 for guard in ('ExtraHero','WrongActor','UninitializedBloom','DiscardBoonTemplate'):body=body.replace('nameof('+guard+')','"'+guard+'"')
 shim+='internal static void '+name+'(ILContext il)'+body
shim+='static System.Reflection.MethodInfo Method(string name)=>typeof(PaleCourtInterop).GetMethod(name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);static System.Reflection.FieldInfo slashes=typeof(FiveKnights.AbyssalBloom).GetField("_nailSlashes",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);}}'
with tempfile.TemporaryDirectory(prefix='local8-charm-regressions-') as folder:
 out=Path(folder);(out/'Extracted.cs').write_text(shim)
 deps=[managed/name for name in ['Mono.Cecil.dll','MonoMod.Utils.dll','MonoMod.RuntimeDetour.dll']]
 options=['-nologo','-target:exe','-langversion:latest','-out:'+str(out/'Test.dll')]+['-r:'+str(p) for p in sorted(refs.glob('*.dll'))+deps]+list(map(str,[here/'Doubles.cs',here/'Test.cs',out/'Extracted.cs',source/'src/Features/PaleCourtInterop.cs']))
 (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
 (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
 for dep in deps:shutil.copy2(dep,out/dep.name)
 subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
 subprocess.run([dotnet,str(out/'Test.dll')],check=True)
 # Keep the fixture's assembly name and filename consistent; otherwise an
 # imported self-reference may load the unmodified Test.dll a second time.
 os.replace(out/'PatchedTest.dll',out/'Test.dll')
 subprocess.run([dotnet,str(out/'Test.dll'),'--patched'],check=True)

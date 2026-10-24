#!/usr/bin/env python3
"""Run production party rewards with exact native SoulOrb coroutine IL; Unity is simulated."""
from pathlib import Path
import subprocess,json,shutil,argparse,tempfile,os
root=Path(__file__).resolve().parent;source=root.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--managed',type=Path,required=True);p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));p.add_argument('--scuttler-source',type=Path);a=p.parse_args()
dotnet=shutil.which(a.dotnet) or str(Path(a.dotnet).resolve());managed=a.managed.resolve()
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1);sdk=Path(location.rstrip(']'));framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((framework/'ref').iterdir())
hooks={'SoulOrb':['OnEnable','OnDisable','Start','Zoom'],'HealthManager':['OnEnable'],'ScuttlerControl':['Hit','Heal'],'HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool':['OnEnter']}
def generate_hooks():
 text='using System;'
 for cls,methods in hooks.items():
  pieces=cls.rsplit('.',1);ns='On.'+pieces[0] if len(pieces)>1 else 'On';name=pieces[-1]
  text+='namespace '+ns+' {public static class '+name+' {'
  for method in methods:
   ret='System.Collections.IEnumerator' if method in ('Zoom','Heal') else 'void'
   args=['global::'+cls+' self'];names=['self']
   if method=='Zoom':args+=['bool doZoom'];names+=['doZoom']
   if method=='Hit':args+=['HitInstance damage'];names+=['damage']
   args=','.join(args);names=','.join(names);call='self.Native_'+method+'('+('doZoom' if method=='Zoom' else 'damage' if method=='Hit' else '')+')'
   text+=f'public delegate {ret} orig_{method}({args});public delegate {ret} hook_{method}(orig_{method} orig,{args});public static event hook_{method} {method};public static int Count_{method}=>{method}?.GetInvocationList().Length??0;'
   if method=='Heal':
    text+='public static bool Deferred_Heal;public static System.Collections.IEnumerator Run_Heal(global::ScuttlerControl self)=>Deferred_Heal?Deferred(self):Invoke_Heal(self);'
    text+='static System.Collections.IEnumerator Deferred(global::ScuttlerControl self){yield return null;var inner=Invoke_Heal(self);try{while(inner.MoveNext())yield return inner.Current;}finally{(inner as IDisposable)?.Dispose();}}'
   entry='Invoke_Heal' if method=='Heal' else 'Run_'+method
   text+=f'public static {ret} {entry}({args})'+'{'+f'orig_{method} next=({names})=>{call};if({method}!=null)foreach(hook_{method} hook in {method}.GetInvocationList())'+'{var previous=next;next=('+names+')=>hook(previous,'+names+');}'+('return ' if method in ('Zoom','Heal') else '')+'next('+names+');}'
  text+='}}'
 return text

with tempfile.TemporaryDirectory(prefix='local8-rewards-native-') as folder:
 out=Path(folder);(out/'Hooks.cs').write_text(generate_hooks())
 def compile(name,files,extra=[]):
  args=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/(name+'.dll'))]+['-r:'+str(p) for p in refs.glob('*.dll')]+extra+list(map(str,files))
  (out/(name+'.rsp')).write_text('\n'.join('"'+s+'"' for s in args));(out/(name+'.runtimeconfig.json')).write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
  subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/(name+'.rsp'))],check=True)
 production=['Features/SoulTotemSharing.cs','Features/SoulOrbRouting.cs','Core/Lifeblood.cs','Features/LifebloodScuttlers.cs','Core/PlayerContext.cs','Core/Reflect.cs','Core/ReferenceField.cs']
 compile('NativeProbe',[root/'Doubles.cs',root/'Test.cs',root/'TotemTest.cs',root/'ScuttlerDoubles.cs',root/'ScuttlerTest.cs',out/'Hooks.cs']+[a.scuttler_source if p=='Features/LifebloodScuttlers.cs' and a.scuttler_source else source/'src'/p for p in production])
 compile('Transplant',[root/'Transplant.cs'],['-r:'+str(managed/'Mono.Cecil.dll')]);shutil.copy2(managed/'Mono.Cecil.dll',out/'Mono.Cecil.dll')
 subprocess.run([dotnet,str(out/'Transplant.dll'),str(managed/'Assembly-CSharp.dll'),str(out/'NativeProbe.dll'),str(out/'PatchedProbe.dll')],check=True)
 shutil.copy2(out/'PatchedProbe.dll',out/'NativeProbe.dll');subprocess.run([dotnet,str(out/'NativeProbe.dll')],check=True)

#!/usr/bin/env python3
"""Run production alpha226 hooks with exact native method IL; Unity is simulated."""
from pathlib import Path
import subprocess,json,shutil,argparse,tempfile,os
root=Path(__file__).resolve().parent;source=root.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--managed',type=Path,required=True);p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));a=p.parse_args()
dotnet=shutil.which(a.dotnet) or str(Path(a.dotnet).resolve());managed=a.managed.resolve()
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1);sdk=Path(location.rstrip(']'));framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((framework/'ref').iterdir())
hooks={'TinkEffect':['OnTriggerEnter2D'],'BreakableInfectedVine':['OnTriggerEnter2D'],'InfectedBurstLarge':['OnTriggerEnter2D'],'InfectedBurstSmall':['OnTriggerEnter2D'],'GrassCut':['ShouldCut'],'SteepSlope':['OnCollisionEnter2D','OnCollisionStay2D','OnCollisionExit2D'],'ShadowGateColliderControl':['FixedUpdate','OnDestroy'],'AlertRange':['OnTriggerEnter2D','OnTriggerExit2D'],'EnemyWakeRegion':['OnTriggerEnter2D','OnTriggerExit2D'],'Walker':['UpdateWaitingForConditions','UpdateWalking'],'CheckAlertRange':['Apply'],'CheckAlertRangeByName':['Apply']}
def generate_hooks():
 text='using System;namespace On {'
 for cls,methods in hooks.items():
  text+='public static class '+cls+' {'
  for name in methods:
   static=cls=='GrassCut';ret='bool' if static else 'void';contact='Collision2D' if name.startswith('OnCollision') else 'Collider2D' if name.startswith('OnTrigger') or static else None
   args=[] if static else ['global::'+cls+' self'];names=[] if static else ['self']
   if contact:args+=['UnityEngine.'+contact+' other'];names+=['other']
   args=','.join(args);names=','.join(names);call=('global::GrassCut.Native_ShouldCut(other)' if static else 'self.Native_'+name+'('+('other' if contact else '')+')')
   text+=f'public delegate {ret} orig_{name}({args});public delegate {ret} hook_{name}(orig_{name} orig,{args});public static event hook_{name} {name};public static int Count_{name}=>{name}?.GetInvocationList().Length??0;'
   text+=f'public static {ret} Run_{name}({args})'+'{'+f'orig_{name} next=({names})=>{call};if({name}!=null)foreach(hook_{name} hook in {name}.GetInvocationList())'+'{var previous=next;next=('+names+')=>hook(previous,'+names+');}'+('return ' if static else '')+'next('+names+');}'
  text+='}'
 text+='public static class LineOfSightDetector {public delegate void orig_Update(global::LineOfSightDetector self);}}'
 return text
with tempfile.TemporaryDirectory(prefix='local8-world-native-') as folder:
 out=Path(folder);(out/'Hooks.cs').write_text(generate_hooks())
 def compile(name,files,extra=[]):
  args=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/(name+'.dll'))]+['-r:'+str(p) for p in refs.glob('*.dll')]+extra+list(map(str,files))
  (out/(name+'.rsp')).write_text('\n'.join('"'+s+'"' for s in args));(out/(name+'.runtimeconfig.json')).write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
  subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/(name+'.rsp'))],check=True)
 production=['Features/ColliderGeometry.cs','Features/WorldContactRouting.cs','Features/SteepSlopeRouting.cs','Features/ShadowGateRouting.cs','Features/PartySensorOccupancy.cs','Features/WalkerTargeting.cs','Features/EnemySightRouting.cs','Features/PhysicsGuard.cs','Core/Reflect.cs','Core/ReferenceField.cs']
 compile('NativeProbe',[root/'Doubles.cs',root/'Test.cs',out/'Hooks.cs']+[source/'src'/p for p in production])
 compile('Transplant',[root/'Transplant.cs'],['-r:'+str(managed/'Mono.Cecil.dll')]);shutil.copy2(managed/'Mono.Cecil.dll',out/'Mono.Cecil.dll')
 subprocess.run([dotnet,str(out/'Transplant.dll'),str(managed/'Assembly-CSharp.dll'),str(out/'NativeProbe.dll'),str(out/'PatchedProbe.dll')],check=True)
 shutil.copy2(out/'PatchedProbe.dll',out/'NativeProbe.dll');subprocess.run([dotnet,str(out/'NativeProbe.dll')],check=True)

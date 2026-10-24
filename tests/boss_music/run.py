#!/usr/bin/env python3
"""Run the complete boss-music guard and exact native TransitionToAudioSnapshot.OnEnter IL."""
from pathlib import Path
import argparse,json,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--dotnet',required=True);p.add_argument('--managed',type=Path,required=True);p.add_argument('--source',type=Path);a=p.parse_args()
dotnet=str(Path(a.dotnet).resolve());version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1);sdk=Path(location.rstrip(']'));fx=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((fx/'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='local8-boss-music-') as folder:
 out=Path(folder)
 def compile(name,files,extra=[]):
  options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/(name+'.dll'))]+['-r:'+str(f) for f in refs.glob('*.dll')]+extra+list(map(str,files))
  (out/(name+'.rsp')).write_text('\n'.join('"'+s+'"' for s in options));(out/(name+'.runtimeconfig.json')).write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':fx.name}}}))
  subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/(name+'.rsp'))],check=True)
 compile('Test',[here/'Doubles.cs',here/'Test.cs',a.source or root/'src/Features/BossMusicGuard.cs'])
 common=(root/'tests/party_rewards/Transplant.cs').read_text();start=common.index(' static void Main(')
 main=''' static void Main(string[] args){using var native=AssemblyDefinition.ReadAssembly(args[0]);using var fixture=AssemblyDefinition.ReadAssembly(args[1]);target=fixture.MainModule;var source=native.MainModule.Types.Single(t=>t.FullName=="HutongGames.PlayMaker.Actions.TransitionToAudioSnapshot").Methods.Single(m=>m.Name=="OnEnter");var dest=Find(source.DeclaringType.FullName).Methods.Single(m=>m.Name=="Native_OnEnter");Copy(source,dest);fixture.Write(args[2]);}}'''
 (out/'Transplant.cs').write_text(common[:start]+main);compile('Transplant',[out/'Transplant.cs'],['-r:'+str(a.managed/'Mono.Cecil.dll')]);shutil.copy2(a.managed/'Mono.Cecil.dll',out/'Mono.Cecil.dll')
 subprocess.run([dotnet,str(out/'Transplant.dll'),str(a.managed/'Assembly-CSharp.dll'),str(out/'Test.dll'),str(out/'Patched.dll')],check=True);shutil.copy2(out/'Patched.dll',out/'Test.dll');subprocess.run([dotnet,str(out/'Test.dll')],check=True)

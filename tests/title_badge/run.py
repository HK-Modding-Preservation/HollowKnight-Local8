#!/usr/bin/env python3
"""Run the real title badge lifecycle against a controlled Unity object tree."""
from pathlib import Path
import argparse,json,os,re,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'));ap.add_argument('--sfcore-source',type=Path);args=ap.parse_args()
dotnet=shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1);sdk=Path(location.rstrip(']'))
framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((framework/'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='local8-title-badge-') as folder:
    out=Path(folder)
    if args.sfcore_source:
        text=args.sfcore_source.read_text(encoding='utf-8-sig');start=text.index('private static Vector3 GetPositionOffsetForIndex(');opening=text.index('{',start);end=opening+1;depth=1
        while depth:depth+=(text[end]=='{')-(text[end]=='}');end+=1
        reference=text[start:end].replace('private static Vector3 GetPositionOffsetForIndex','internal static Vector3 Position',1)
    else:
        reference='internal static Vector3 Position(int i){float[] x={-1.05f,-.22f,.61f,1.44f,2.27f,3.1f,3.93f,4.76f,5.59f,6.42f};return new Vector3(x[i%10],-.16f+.83f*(i/10));}'
    (out/'Reference.cs').write_text('using UnityEngine;internal static class SFCoreReference {'+reference+'}')
    options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
    options+=['-r:'+str(p) for p in refs.glob('*.dll')]
    options+=[str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'Reference.cs')]+[str(root/'src'/p) for p in ['Features/TitleCoopBadge.cs','Features/DlcBadgePlacement.cs','Core/ReleaseInfo.cs']]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
    subprocess.run([dotnet,str(out/'Test.dll')],check=True)

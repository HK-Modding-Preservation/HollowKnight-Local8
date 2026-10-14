#!/usr/bin/env python3
"""Exercise production mushroom contacts against controlled hero/collision doubles."""
from pathlib import Path
import argparse, json, os, shutil, subprocess, tempfile
here = Path(__file__).resolve().parent
source = here.parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
args = parser.parse_args()
dotnet = shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version, location = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()[-1].rsplit(' [', 1)
sdk = Path(location.rstrip(']'))
framework = sorted((sdk.parent/'packs'/'Microsoft.NETCore.App.Ref').iterdir(), key=lambda p: tuple(map(int, p.name.split('.'))))[-1]
refs = next((framework/'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='local8-mushrooms-') as folder:
    out = Path(folder)
    options = ['-nologo', '-target:exe', '-langversion:latest', '-optimize+', '-out:' + str(out/'Test.dll')]
    options += ['-r:' + str(p) for p in sorted(refs.glob('*.dll'))]
    options += [str(here/'Doubles.cs'), str(here/'Test.cs')]
    options += [str(source/'src/Features/MushroomBounceRouting.cs')]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': refs.name, 'framework': {'name': 'Microsoft.NETCore.App', 'version': framework.name}}}))
    subprocess.run([dotnet, str(sdk/version/'Roslyn/bincore/csc.dll'), '@'+str(out/'test.rsp')], check=True)
    subprocess.run([dotnet, str(out/'Test.dll')], check=True)

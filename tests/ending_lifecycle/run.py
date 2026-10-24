#!/usr/bin/env python3
"""Run the complete production final-absorption lifecycle with controlled Unity IO."""
from pathlib import Path
import argparse, json, subprocess, tempfile

here = Path(__file__).resolve().parent
root = here.parents[1]
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--dotnet', required=True)
p.add_argument('--source', type=Path, help='Optional previous EndingAbsorption source for a negative regression run')
a = p.parse_args()
dotnet = str(Path(a.dotnet).resolve())
version, location = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()[-1].rsplit(' [', 1)
sdk = Path(location.rstrip(']'))
fx = sorted((sdk.parent / 'packs/Microsoft.NETCore.App.Ref').iterdir())[-1]
refs = next((fx / 'ref').iterdir())
with tempfile.TemporaryDirectory(prefix='local8-ending-lifecycle-') as folder:
    out = Path(folder)
    files = [here / 'Doubles.cs', here / 'Test.cs', a.source or root / 'src/Features/EndingAbsorption.cs']
    options = ['-nologo', '-target:exe', '-langversion:latest', '-optimize+', '-out:' + str(out / 'Test.dll')]
    options += ['-r:' + str(f) for f in refs.glob('*.dll')] + list(map(str, files))
    (out / 'test.rsp').write_text('\n'.join('"' + s + '"' for s in options))
    (out / 'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': refs.name, 'framework': {'name': 'Microsoft.NETCore.App', 'version': fx.name}}}))
    subprocess.run([dotnet, str(sdk / version / 'Roslyn/bincore/csc.dll'), '@' + str(out / 'test.rsp')], check=True)
    subprocess.run([dotnet, str(out / 'Test.dll')], check=True)

#!/usr/bin/env python3
"""Execute the production language resolver against all ten resource columns."""
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
with tempfile.TemporaryDirectory(prefix='local8-localization-') as folder:
    out = Path(folder)
    options = ['-nologo', '-target:exe', '-langversion:latest', '-optimize+', '-out:' + str(out/'Test.dll')]
    options += ['-r:' + str(p) for p in sorted(refs.glob('*.dll'))]
    options += [str(here/'Doubles.cs'), str(here/'Test.cs')]
    text = (source/'src/Core/Charms.cs').read_text()
    start = text.index('internal static string SanitizeLocalized(')
    opening = text.index('{', start); depth = 1; end = opening + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}'); end += 1
    (out/'Resolver.cs').write_text('using System; namespace KO.HollowKnight8 { internal static class Charms {' + text[start:end] + '}}')
    options += [str(out/'Resolver.cs'), str(source/'src/Features/RoleDefinition.cs'), str(source/'src/Features/UiLocalization.cs')]
    options += ['-resource:'+str(source/'assets'/name)+','+name for name in ['Local8.UI.Text','Local8.Roles.Text']]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {'tfm': refs.name, 'framework': {'name': 'Microsoft.NETCore.App', 'version': framework.name}}}))
    subprocess.run([dotnet, str(sdk/version/'Roslyn/bincore/csc.dll'), '@'+str(out/'test.rsp')], check=True)
    subprocess.run([dotnet, str(out/'Test.dll')], check=True)

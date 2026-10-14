#!/usr/bin/env python3
"""Execute production royal charm keys, native sprites and icon-cache methods."""
from pathlib import Path
import argparse, json, re, subprocess, tempfile

here = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--dotnet', required=True)
parser.add_argument('--source', type=Path, default=here.parents[1])
parser.add_argument('--preview-directory', type=Path)
args = parser.parse_args()
root = args.source.resolve()
dotnet = str(Path(args.dotnet).resolve())
version, location = subprocess.check_output([dotnet, '--list-sdks'], text=True).splitlines()[-1].rsplit(' [', 1)
sdk = Path(location.rstrip(']'))
framework = sorted((sdk.parent / 'packs/Microsoft.NETCore.App.Ref').iterdir())[-1]
refs = next((framework / 'ref').iterdir())

def method(text, marker):
    start = text.index(marker)
    opening = text.index('{', start)
    depth, end = 1, opening + 1
    while depth:
        depth += (text[end] == '{') - (text[end] == '}')
        end += 1
    return text[start:end]

source = (root / 'src/Core/Charms.cs').read_text()
host = "using System;using System.Collections.Generic;using UnityEngine;namespace KO.HollowKnight8 { internal static class Charms {"
host += "internal static bool Editing;internal static InvCharmBackboard[] NativeBoards=Array.Empty<InvCharmBackboard>();private static void ReleaseNative(){}"
host += "private static readonly Dictionary<int,Texture2D> icons=new();private static readonly Dictionary<int,Sprite> iconSources=new();"
for marker in ['internal static string Name(', 'internal static string Description(', 'internal static Texture2D Icon(', 'internal static void Reset(']:
    host += method(source, marker)
host += "}}"
production = ['Features/CharmPresentation.cs']
with tempfile.TemporaryDirectory(prefix='local8-charm-presentation-') as folder:
    out = Path(folder)
    (out / 'Host.cs').write_text(host)
    options = ['-nologo', '-target:exe', '-langversion:latest', '-optimize+', '-out:' + str(out / 'Test.dll')]
    if 'Features/CharmMenuBackdrop.cs' in production:
        options.append('-define:BACKDROP')
    options += ['-r:' + str(p) for p in refs.glob('*.dll')]
    options += [str(here / 'Doubles.cs'), str(here / 'Test.cs'), str(out / 'Host.cs')]
    options += [str(root / 'src' / p) for p in production]
    (out / 'test.rsp').write_text('\n'.join('"' + item + '"' for item in options))
    (out / 'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {
        'tfm': refs.name, 'framework': {'name': 'Microsoft.NETCore.App', 'version': framework.name}}}))
    subprocess.run([dotnet, str(sdk / version / 'Roslyn/bincore/csc.dll'), '@' + str(out / 'test.rsp')], check=True)
    subprocess.run([dotnet, str(out / 'Test.dll')] + ([str(args.preview_directory.resolve())] if args.preview_directory else []), check=True)

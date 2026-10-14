#!/usr/bin/env python3
"""Run the real native charm-card/role rendering and ownership guards on a recorded host."""
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

cards = method((root / 'src/Core/Hud.cs').read_text(), 'private static void NativeCharmCards(')
guard = method((root / 'src/Features/CharmMenuExit.cs').read_text(), 'internal static bool NativeActive(')
grid_cursor = method((root / 'src/Core/Hud.cs').read_text(), 'private static void DrawNativeCharmCursors(')
host = '''using System;using System.Collections.Generic;using UnityEngine;using UnityEngine.EventSystems;
namespace KO.HollowKnight8 {
internal static class Hud {
 internal static List<Rect> Cards=new();
 internal static void Render(CoopSession s,float w,float h){Cards.Clear();NativeCharmCards(s,w,h,1);}
 private static void Box(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 private static void DrawCharmCard(PlayerSlot p,PlayerData data,Rect r){Cards.Add(r);CharmCardUI.Draw(p,data,r);RoleUI.CardOverlay(p,data,r);}
 private static GUIStyle tag=new GUIStyle();internal static void GridCursors(CoopSession s,float scale)=>DrawNativeCharmCursors(s,scale);
''' + cards + grid_cursor + ' } internal static class CharmMenuExit {' + guard + '}}'
probe = method((root / 'src/Core/Charms.cs').read_text(), 'private static void ProbeNative(')
host += '''namespace KO.HollowKnight8 { internal static partial class Charms {
 private static int nativeFrame=-1;private static bool nativeVisible;private static GameObject nativePane;private static InvCharmBackboard[] nativeBoards=Array.Empty<InvCharmBackboard>();private static float nextBoardProbe;
 internal static bool ProbeState(){ProbeNative();NativePane=nativePane;NativeBoards=nativeBoards;return nativeVisible;}
''' + probe + method((root / 'src/Core/Charms.cs').read_text(), 'internal static int FirstOwned(') + method((root / 'src/Core/Charms.cs').read_text(), 'internal static int Neighbor(') + '}}'
roles = (root / 'src/Features/RoleSystem.cs').read_text()
host += 'namespace KO.HollowKnight8 {internal static partial class RoleSystem {'
for marker in ['internal static void NativeInput(', 'internal static bool MoveDue(', 'internal static void Prime(', 'static int ReturnToCatalog(', 'internal static void ClearNativeFocus(']:
    body = roles if marker in roles else (here.parents[1] / 'src/Features/RoleSystem.cs').read_text()
    host += method(body,marker)
host += '}}'
production = ['Features/CharmCardUI.cs', 'Features/CharmDescriptionLayout.cs', 'Features/RoleUI.cs']
for name in ['CharmMenuLayout', 'CharmMenuBackdrop', 'CharmMenuVisibility', 'CharmMenuDiagnostics', 'EquippedCharmCursor', 'CharmCursorUI', 'CharmCatalogTint']:
    if (root / 'src/Features' / (name + '.cs')).exists() or name in ['CharmMenuVisibility','CharmMenuDiagnostics','EquippedCharmCursor','CharmCursorUI','CharmCatalogTint']:
        production.append('Features/' + name + '.cs')
production += ['Core/CharmGridRules.cs','Core/CharmCell.cs']
with tempfile.TemporaryDirectory(prefix='local8-native-charms-') as folder:
    out = Path(folder)
    (out / 'Host.cs').write_text(host)
    options = ['-nologo', '-target:exe', '-langversion:latest', '-optimize+', '-out:' + str(out / 'Test.dll')]
    if 'Features/CharmMenuBackdrop.cs' in production:
        options.append('-define:BACKDROP')
    options += ['-r:' + str(p) for p in refs.glob('*.dll')]
    options += [str(here / 'Doubles.cs'), str(here / 'Test.cs'), str(out / 'Host.cs')]
    options += [str(root / 'src' / p if (root / 'src' / p).exists() else here.parents[1] / 'src' / p) for p in production]
    options += ['-resource:' + str(root / 'assets' / p.name) + ',' + p.name for p in (root / 'assets').iterdir()]
    (out / 'test.rsp').write_text('\n'.join('"' + item + '"' for item in options))
    (out / 'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions': {
        'tfm': refs.name, 'framework': {'name': 'Microsoft.NETCore.App', 'version': framework.name}}}))
    subprocess.run([dotnet, str(sdk / version / 'Roslyn/bincore/csc.dll'), '@' + str(out / 'test.rsp')], check=True)
    subprocess.run([dotnet, str(out / 'Test.dll')] + ([str(args.preview_directory.resolve())] if args.preview_directory else []), check=True)

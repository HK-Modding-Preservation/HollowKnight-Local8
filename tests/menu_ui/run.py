#!/usr/bin/env python3
"""Execute the production menu/theme on a controlled IMGUI host (no Unity player)."""
from pathlib import Path
import argparse,json,os,re,shutil,subprocess,tempfile
here=Path(__file__).resolve().parent;root=here.parents[1]
ap=argparse.ArgumentParser(description=__doc__)
ap.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
ap.add_argument('--preview-directory')
args=ap.parse_args();dotnet=shutil.which(args.dotnet) or str(Path(args.dotnet).resolve())
version,location=subprocess.check_output([dotnet,'--list-sdks'],text=True).splitlines()[-1].rsplit(' [',1)
sdk=Path(location.rstrip(']'));framework=sorted((sdk.parent/'packs/Microsoft.NETCore.App.Ref').iterdir())[-1];refs=next((framework/'ref').iterdir())
hud=(root/'src/Core/Hud.cs').read_text()
names=['DrawMenu','Box','ChangeTab','DeviceRow','Panel','PlayerOptions','KeyboardOptions','GameOptions','AdvancedOptions','PvpOptions','PvpNumber','Help','DrawTooltip','DrawLogButtons','CharmPanel']
def method(source,name):
    start=re.search(r'\tprivate static [^\n]+\b'+name+r'\(',source).start()
    opening=source.index('{',start);depth=1;end=opening+1
    while depth:
        depth+=(source[end]=='{')-(source[end]=='}');end+=1
    return source[start:end]
fields='''private static GUIStyle text=new GUIStyle(),title=new GUIStyle(),button=new GUIStyle(),tag=new GUIStyle();
internal static GUIStyle small=new GUIStyle();private static Texture2D pixel=Texture2D.whiteTexture;
internal static int selected;private static int tab,pvpPage;private static bool addingKeyboard,adventurePage;
private static Vector2 scroll;private static float[] menuHeight=new float[5];private static string lastTip="";private static float tipSince;
private static Color[] palette={Color.white,new Color(.4f,.85f,1),new Color(1,.55f,.45f),new Color(.55f,1,.6f),new Color(.95f,.65f,1),new Color(1,.88f,.4f),new Color(.55f,.62f,1),new Color(1,.64f,.84f),Color.white,new Color(.55f,1,.9f),new Color(1,.35f,.35f),new Color(.75f,.55f,1)};
internal static void ResetTest(int page,int pvp=0){tab=page;pvpPage=pvp;adventurePage=false;selected=0;addingKeyboard=false;scroll=Vector2.zero;menuHeight=new float[5];}
internal static void Render(Local8Runtime r,float w,float h){DrawMenu(r,w,h);}
internal static void EditKeyboard(int who,bool adding){selected=who;addingKeyboard=adding;tab=4;scroll=Vector2.zero;}
internal static int CurrentTab=>tab;
internal static Vector2 Scroll=>scroll;
internal static GUIStyle CurrentText=>text;
internal static void ScrollBy(float y){scroll.y=Math.Max(0,scroll.y+y);}
internal static void Icon(Texture tex,Rect rect,Color tint){Color old=GUI.color;try{GUI.color=tint;GUI.DrawTexture(rect,tex,ScaleMode.ScaleToFit,true);}finally{GUI.color=old;}}
'''
clock=method((root/'src/Features/PvpRoundClock.cs').read_text().replace(' internal static void Number','\tprivate static void Number'),'Number').replace('private static void Number','internal static void Number').replace('Migrate();','')
with tempfile.TemporaryDirectory(prefix='hk8-menu-ui-') as folder:
    out=Path(folder)
    (out/'MenuHost.cs').write_text('using System;using System.Linq;using InControl;using UnityEngine;namespace KO.HollowKnight8 {internal static class Hud{'+fields+'\n'.join(method(hud,n) for n in names)+'} internal static class PvpRoundClock{'+clock+'}}')
    production=['Features/MenuLayout.cs','Features/MenuTheme.cs','Features/MenuArt.cs','Features/PvpArenaUI.cs','Features/RespawnSlider.cs','Features/KeyboardWarning.cs','Features/RoleDefinition.cs','Features/UiLocalization.cs','Core/ReleaseInfo.cs','Features/PlayerProfileData.cs','Features/PlayerProfilesUI.cs','Features/AdventureStatsUI.cs','Features/PerformanceMenu.cs','Core/PerformanceConfiguration.cs','Core/PerformancePresets.cs','Core/NativeGraphicsOptions.cs']
    options=['-nologo','-target:exe','-langversion:latest','-optimize+','-out:'+str(out/'Test.dll')]
    options+=['-r:'+str(p) for p in refs.glob('*.dll')]
    options+=[str(here/'Doubles.cs'),str(here/'Test.cs'),str(out/'MenuHost.cs')]+[str(root/'src'/p) for p in production]
    options+=['-resource:'+str(root/'assets'/n)+','+n for n in ['Local8.UI.Text','Local8.Roles.Text','Local8.UI.Atlas']]
    (out/'test.rsp').write_text('\n'.join('"'+s+'"' for s in options))
    (out/'Test.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':refs.name,'framework':{'name':'Microsoft.NETCore.App','version':framework.name}}}))
    subprocess.run([dotnet,str(sdk/version/'Roslyn/bincore/csc.dll'),'@'+str(out/'test.rsp')],check=True)
    subprocess.run([dotnet,str(out/'Test.dll')]+([str(Path(args.preview_directory).resolve())] if args.preview_directory else []),check=True)

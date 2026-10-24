using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using KO.HollowKnight8;
using UnityEngine;
using UnityEngine.EventSystems;
using InControl;

static partial class Test {
 static void DlcPlacement(){
  var random=new Random(1123);
  foreach(float scale in new[]{.5f,1f,2f}){
   var anchor=new Rect(2*scale,scale,.6f,.6f);
   var view=new Rect(0,0,16f*scale,9f*scale);
   var icons=Enumerable.Range(0,5).Select(i=>{var p=DlcBadgePlacement.Offset(i);return new Rect(anchor.x+p.x,anchor.y+p.y,anchor.width,anchor.height);}).ToList();
   Check(DlcBadgePlacement.Find(anchor,view,icons,out var spot)&&Math.Abs(spot.x-anchor.x-3.1f)<.00001f&&Math.Abs(spot.y-anchor.y+.16f)<.00001f,"DLC badge does not use SFCore's first custom slot");
   Check(DlcBadgePlacement.Find(anchor,view,icons,out var reserved,1)&&Math.Abs(reserved.x-anchor.x-3.93f)<.00001f,"registered mod reservation is ignored before its renderer appears");
   var first=spot;icons.Add(first);
   Check(DlcBadgePlacement.Find(anchor,view,icons,out spot)&&!DlcBadgePlacement.Overlaps(first,spot),"late mod icon is overwritten");
   for(int count=0;count<120;count++){
    icons.Add(new Rect((float)random.NextDouble()*view.width,(float)random.NextDouble()*view.height,.65f*scale,.65f*scale));
    var snapshot=icons.ToArray();bool found=DlcBadgePlacement.Find(anchor,view,icons,out spot);
    Check(snapshot.SequenceEqual(icons),"placement modifies native/foreign rectangles");
    if(found){
     Check(spot.xMin>=view.xMin&&spot.xMax<=view.xMax&&spot.yMin>=view.yMin&&spot.yMax<=view.yMax,"badge leaves visible viewport");
     Check(icons.All(icon=>!DlcBadgePlacement.Overlaps(spot,icon)),"badge overlaps a native/foreign obstacle");
     Check(spot.width==anchor.width&&spot.height==anchor.height,"placement enlarges icon cell");
    }
   }
   Check(!DlcBadgePlacement.Find(anchor,view,new[]{view},out spot),"full menu forces an overlapping badge");
   Check(!DlcBadgePlacement.Find(anchor,new Rect(0,0,.2f*scale,.2f*scale),Array.Empty<Rect>(),out spot),"oversized badge escapes tiny viewport");
  }
  var rowAnchor=new Rect(3,1,.6f,.6f);var rows=Enumerable.Range(5,5).Select(i=>{var p=DlcBadgePlacement.Offset(i);return new Rect(rowAnchor.x+p.x,rowAnchor.y+p.y,.6f,.6f);}).ToList();
  Check(DlcBadgePlacement.Find(rowAnchor,new Rect(0,0,9,6.75f),rows,out var next)&&Math.Abs(next.y-rowAnchor.y-.67f)<.00001f&&Math.Abs(next.x-rowAnchor.x+1.05f)<.00001f,"full DLC row does not wrap on SFCore's grid in 4:3");
  Check(!DlcBadgePlacement.Find(new Rect(),new Rect(0,0,16,9),Array.Empty<Rect>(),out next),"zero-size native badge creates an invalid placement");
 }
 static void LobbyPage(string[] languages){
  var r=Runtime(0);GameManager.instance=new(){Menu=true};r.Panel=false;
  var a=new InputDevice{Name="DualSense A"};var b=new InputDevice{Name="DualSense B"};Controls.Available=new[]{a,b};
  TitleLobby.Tick(r);Check(r.Lobby.Count==0,"idle connected controllers claim menu slots");
  KeypadInput.PressedKeys.Add(KeyCode.Return);TitleLobby.Tick(r);KeypadInput.PressedKeys.Clear();
  b.CommandWasPressed=true;TitleLobby.Tick(r);b.CommandWasPressed=false;
  Check(r.Lobby.Count==2&&r.Lobby.Primary.Device==InputDevice.Null&&r.Lobby.Members[1].Device==b&&r.Session.Players.Count==0,"pregame Start creates actors or replaces keyboard P1");
  r.Lobby.Clear();GameManager.instance.inputHandler.inputActions.menuSubmit.WasPressed=true;
  GameManager.instance.inputHandler.inputActions.menuSubmit.LastInputType=BindingSourceType.DeviceBindingSource;
  GameManager.instance.inputHandler.inputActions.ActiveDevice=b;TitleLobby.Tick(r);
  Check(r.Lobby.Primary.Device==b,"native controller confirmation lost its exact device");
  GameManager.instance.inputHandler.inputActions.menuSubmit.WasPressed=false;
  r.Panel=true;a.CommandWasPressed=true;MenuInput.Waiting=true;TitleLobby.Tick(r);MenuInput.Waiting=false;
  Check(r.Lobby.Count==1,"binding capture accidentally joins a controller");
  TitleLobby.Tick(r);a.CommandWasPressed=false;Check(r.Lobby.Count==2&&r.Lobby.Members[1].Device==a,"controller cannot join with panel open");
  r.Lobby.Clear();r.Enabled.Value=false;a.CommandWasPressed=true;TitleLobby.Tick(r);r.Enabled.Value=true;
  Check(r.Lobby.Count==0,"disabled mod captures menu joins");
  GameManager.instance.IsLoadingSceneTransition=true;TitleLobby.Tick(r);GameManager.instance.IsLoadingSceneTransition=false;
  Check(r.Lobby.Count==0,"controller joins during a menu scene transition");
  Application.isFocused=false;TitleLobby.Tick(r);Application.isFocused=true;a.CommandWasPressed=false;
  Check(r.Lobby.Count==0,"background game captures joins");
  foreach(string language in languages){
   Language.Language.Code=language;
   foreach(var size in new[]{(1600f,900f),(984f,554f),(1024f,768f)}){
    r.Panel=false;GUI.Reset(size.Item1,size.Item2);TitleLobby.Entry(r,size.Item1,size.Item2);
    var button=GUI.Controls.Single();Check(button.Rect.xMax<=size.Item1-17&&button.Rect.y>=size.Item2-90&&button.Rect.yMax<size.Item2-40,"settings entry is not in the lower right");
    Check(button.Text==UiLocalization.Get("lobby.settings")&&GUI.Paints.Any(p=>p.text==UiLocalization.Get("lobby.shortcut").Replace("{key}","F8")),"entry lacks localized settings/F8 help");
    GUI.ClickPoint=button.Rect.center;TitleLobby.Entry(r,size.Item1,size.Item2);GUI.ClickPoint=null;
    Check(r.Panel,"title settings button does not open the panel");
    for(int page=0;page<4;page++){
     Hud.ResetTest(page);Render(r,size.Item1,size.Item2);Render(r,size.Item1,size.Item2);
     Check(GUI.enabled&&GUI.Paints.All(p=>p.rect.All(float.IsFinite)&&p.rect[2]>=0&&p.rect[3]>=0),"pregame settings produce invalid geometry/state");
     Check(!GUI.Paints.Any(p=>p.text==UiLocalization.Get("panel.enter_game")),"pregame panel still requires loading a save");
     Check(!GUI.Controls.Where(c=>c.Text==UiLocalization.Get("action.gather")||c.Text==UiLocalization.Get("action.repair")).Any(c=>c.Enabled),"pregame panel offers actor-only actions");
    }
   }
  }
  Language.Language.Code="EN";Hud.ResetTest(0);Click(r,c=>c.Text==UiLocalization.Get("lobby.keyboard"));
  Check(r.Lobby.Count==1&&r.Lobby.Primary.Device==InputDevice.Null,"lobby keyboard button fails");
  Click(r,c=>c.Text==UiLocalization.Get("action.join")+"  ·  "+b.Name);
  Check(r.Lobby.Count==2&&r.Lobby.Members[1].Device==b,"lobby device button fails");
  Click(r,c=>c.Text==UiLocalization.Get("common.assign_p1")+"  ·  "+b.Name);
  Check(r.Lobby.Count==2&&r.Lobby.Primary.Device==b&&r.Lobby.Members[1].Device==InputDevice.Null,"menu P1 controller selection duplicates slots");
  Click(r,c=>c.Text==UiLocalization.Get("input.keyboard")&&c.Tip==UiLocalization.Get("input.keyboard_tip"));
  Check(r.Lobby.Count==2&&r.Lobby.Primary.Device==InputDevice.Null&&r.Lobby.Members[1].Device==b,"menu P1 keyboard choice loses guest controller");
  Click(r,c=>c.Text==UiLocalization.Get("common.assign_p1")+"  ·  "+a.Name);
  Check(r.Lobby.Count==2&&r.Lobby.Primary.Device==a&&r.Lobby.Members[1].Device==b,"unjoined P1 pad adds a phantom keyboard player");
  Click(r,c=>c.Text==UiLocalization.Get("action.remove_last"));Check(r.Lobby.Count==1,"lobby remove callback changes the game roster");
  Hud.ResetTest(1);Click(r,c=>c.Tip==UiLocalization.Get("performance.hazard_effects_tip"));
  Click(r,c=>c.Tip==UiLocalization.Get("performance.health_effects_tip"));
  Check(!r.Performance.HazardEffects&&!r.Performance.HudHealthEffects,"pregame feedback toggles fail");
  PerformancePresets.Apply(r,2,true);Check(!r.Performance.HazardEffects&&!r.Performance.HudHealthEffects,"automatic preset re-enables feedback");
  // Disable only the native event modules and restore their exact previous states.
  var live=new BaseInputModule();var alreadyOff=new BaseInputModule{enabled=false};
  var system=new EventSystem{Modules=new[]{live,alreadyOff}};EventSystem.current=system;
  LobbyMenuInput.Sync(true);LobbyMenuInput.Sync(true);Check(!live.enabled&&!alreadyOff.enabled,"native menu remains active under the lobby panel");
  var late=new BaseInputModule();system.Modules=new[]{live,alreadyOff,late};LobbyMenuInput.Sync(true);
  Check(!late.enabled,"late native input module bypasses the lobby panel");
  var nextModule=new BaseInputModule();EventSystem.current=new(){Modules=new[]{nextModule}};LobbyMenuInput.Sync(true);
  Check(live.enabled&&late.enabled&&!alreadyOff.enabled&&!nextModule.enabled,"event system change loses module states");
  LobbyMenuInput.Close();LobbyMenuInput.Sync(false);Check(!nextModule.enabled,"close input can activate the menu underneath");
  Time.unscaledTime+=.11f;LobbyMenuInput.Sync(false);Check(nextModule.enabled,"closing panel leaves native menu disabled");EventSystem.current=null;Time.unscaledTime=0;
  GameManager.instance=null;Controls.Available=Array.Empty<InputDevice>();
 }
 static void LobbyPreviews(string path){
  Language.Language.Code="ES";var r=Runtime(0);GameManager.instance=new(){Menu=true};
  r.Panel=false;GUI.Reset(1600,900);TitleLobby.Entry(r,1600,900);SavePaints(Path.Combine(path,"title-entry.json"));
  r.Panel=true;r.Lobby.Join(InputDevice.Null);r.Lobby.Join(new InputDevice{Name="DualSense"});
  SavePreview(Path.Combine(path,"lobby.json"),r,0);
  Hud.ResetTest(1);Click(r,c=>c.Tip==UiLocalization.Get("performance.hazard_effects_tip"));Render(r);SavePaints(Path.Combine(path,"feedback-settings.json"));
  GameManager.instance=null;
 }
}

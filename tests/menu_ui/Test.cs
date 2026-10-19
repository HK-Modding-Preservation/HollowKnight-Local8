using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using KO.HollowKnight8;
using UnityEngine;
using InControl;
static class Test {
 static int checks;
 static void Check(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static Local8Runtime Runtime(int count){var r=new Local8Runtime();Plugin.Self=r;for(int i=0;i<count;i++)r.Session.Players.Add(new PlayerSlot{Index=i,Device=i<2?InputDevice.Null:new InputDevice{Name="Controller "+i},Color=i%2==0?MenuTheme.Accent:new Color(.92f,.57f,.9f)});return r;}
 static void Render(Local8Runtime r,float w=1600,float h=900){GUI.Reset(w,h);Hud.Render(r,w,h);}
 static void Click(Local8Runtime r,Func<HostControl,bool> find){
  Render(r);Render(r);HostControl c=GUI.Controls.FirstOrDefault(find);Check(c!=null,"Expected control missing");
  if(!c.Clip.Contains(c.Rect.center)){Hud.ScrollBy(c.Rect.y-c.Clip.y-c.Clip.height/2);Render(r);c=GUI.Controls.First(find);}
  GUI.ClickPoint=c.Rect.center;Render(r);GUI.ClickPoint=null;
 }
 static void SavePreview(string path,Local8Runtime r,int tab,int sub=0){Hud.ResetTest(tab,sub);Render(r);Render(r);
  SavePaints(path);
 }
 static void SavePaints(string path){
  var textures=GUI.Textures.ToDictionary(x=>x.Key,x=>new {width=x.Value.width,height=x.Value.height,rgba=Convert.ToBase64String(x.Value.Pixels.SelectMany(p=>new[]{(byte)(p.r*255),(byte)(p.g*255),(byte)(p.b*255),(byte)(p.a*255)}).ToArray())});
  File.WriteAllText(path,JsonSerializer.Serialize(new{width=GUI.ViewWidth,height=GUI.ViewHeight,paints=GUI.Paints,textures},new JsonSerializerOptions{IncludeFields=true}));
 }
 static float ProfileRender(Local8Runtime r,PlayerSlot p,float width){GUI.Reset(width+10,1500);float row=0;PlayerProfilesUI.Draw(r.Session,p,width,ref row);return row;}
 static void ProfileClick(Local8Runtime r,PlayerSlot p,float width,Func<HostControl,bool> find){ProfileRender(r,p,width);var c=GUI.Controls.FirstOrDefault(find);Check(c!=null,"Profile action missing");GUI.ClickPoint=c.Rect.center;ProfileRender(r,p,width);GUI.ClickPoint=null;ProfileRender(r,p,width);}
 static void Profiles(string[] langs){
  foreach(string lang in langs)foreach(float width in new[]{690f,320f}){
   Language.Language.Code=lang;PlayerProfilesUI.Reset();PlayerProfiles.All=Array.Empty<PlayerProfileData>();PlayerProfiles.LoadAllowed=true;
   var r=Runtime(3);var p=r.Session.Players[1];Plugin.Self=r;GUI.FieldInput=null;ProfileRender(r,p,width);
   Check(GUI.Paints.Any(x=>x.text==UiLocalization.Get("profile.title")+" · P2"),"Profile target is ambiguous");
   Check(GUI.Paints.Any(x=>x.text==UiLocalization.Get("profile.name")),"Profile field has no name label");
   Check(GUI.Paints.Any(x=>x.text==UiLocalization.Get("profile.name_hint")),"Empty profile field has no hint");
   Check(!GUI.Controls.Single(x=>x.Text==UiLocalization.Get("profile.create")).Enabled,"Blank name creates a profile");
   Check(!GUI.Controls.Any(x=>x.Text==UiLocalization.Get("profile.new")),"Guest sees an unexplained identity-reset action");
   int saves=PlayerProfiles.Saves;GUI.FieldInput="Raul";ProfileClick(r,p,width,c=>c.Text==UiLocalization.Get("profile.create"));GUI.FieldInput=null;
   string id=p.ProfileId;Check(PlayerProfiles.Saves==saves+1&&PlayerProfiles.Find(id).Name=="Raul"&&r.Session.Players[0].ProfileId==null,"Create profile has the wrong player owner");
   Check(GUI.Paints.Any(x=>x.text==UiLocalization.Get("profile.autosave_help")),"Active profile does not explain automatic saving");
   Check(GUI.Controls.Any(x=>x.Text==UiLocalization.Get("profile.save")),"Active name cannot be saved");
   var free=new PlayerProfileData{Id="free",Name="Koopa"};var busy=new PlayerProfileData{Id="busy",Name="dd22"};
   PlayerProfiles.All=PlayerProfiles.All.Concat(new[]{free,busy}).ToArray();r.Session.Players[2].ProfileId=busy.Id;PlayerProfiles.LoadAllowed=false;
   ProfileClick(r,p,width,c=>c.Text==UiLocalization.Get("profile.load"));
   Check(!GUI.Paints.Any(x=>x.text==UiLocalization.Get("profile.load_help")),"Unnecessary profile-example paragraph remains visible");
   Check(GUI.Paints.Any(x=>x.text==UiLocalization.Get("profile.load_at_bench")),"Disabled loading has no visible explanation");
   var blocked=GUI.Controls.Single(x=>x.Text==free.Name);Check(!blocked.Enabled&&blocked.Tip==UiLocalization.Get("profile.load_at_bench"),"Unavailable profile can be loaded or has wrong hint");
   var occupied=GUI.Controls.Single(x=>x.Text==busy.Name+" · "+UiLocalization.Get("profile.busy_label"));Check(!occupied.Enabled&&occupied.Tip==UiLocalization.Get("profile.in_use"),"Occupied profile is not clearly identified");
   int loads=PlayerProfiles.Loads;ProfileClick(r,p,width,c=>c.Text==free.Name);Check(PlayerProfiles.Loads==loads&&p.ProfileId==id,"Disabled profile steals the identity");
   PlayerProfiles.LoadAllowed=true;ProfileClick(r,p,width,c=>c.Text==free.Name);Check(p.ProfileId==free.Id&&r.Session.Players[2].ProfileId==busy.Id,"Loading affects another player");
   Check(GUI.Paints.Any(x=>x.kind=="textfield"&&x.text==free.Name),"Loaded name is not shown in the field");
   int detaches=PlayerProfiles.Detaches;ProfileClick(r,p,width,c=>c.Text==UiLocalization.Get("profile.new"));
   Check(PlayerProfiles.Detaches==detaches&&p.ProfileId==free.Id,"Leaving a profile resets statistics without the explicit choice");
   Check(GUI.Paints.Any(x=>x.text==UiLocalization.Get("profile.leave_help")),"Profile reset is not explained");
   ProfileClick(r,p,width,c=>c.Text==UiLocalization.Get("common.cancel"));Check(PlayerProfiles.Detaches==detaches&&p.ProfileId==free.Id,"Cancel detached the profile");
   ProfileClick(r,p,width,c=>c.Text==UiLocalization.Get("profile.new"));ProfileClick(r,p,width,c=>c.Text==UiLocalization.Get("profile.confirm_leave"));
   Check(PlayerProfiles.Detaches==detaches+1&&p.ProfileId==null&&PlayerProfiles.All.Contains(free),"Guest action deletes the saved profile");
   float height=ProfileRender(r,p,width);Check(height<1500&&GUI.Paints.All(x=>x.rect[2]>0),"Profile page clips narrow translated content");
   foreach(var button in GUI.Controls)Check(button.Rect.x>=0&&button.Rect.xMax<=width,"Profile button exceeds content width");
  }
  GUI.FieldInput=null;PlayerProfiles.LoadAllowed=true;PlayerProfiles.All=Array.Empty<PlayerProfileData>();PlayerProfilesUI.Reset();Language.Language.Code="EN";
 }
 static void ContinuousBars(){
  int allocations=Texture2D.Allocations;
  foreach(float scale in new[]{.65f,.8f,1f,1.6f})foreach(float fraction in new[]{.02f,.23f,.6f,1f}){
   GUI.Reset();Rect rail=new Rect(12.37f,18.23f,517.7f*scale,6f*scale);MenuArt.FillBar(rail,new Color(.2f,.85f,.3f),fraction);
   Check(GUI.Paints.Count==1,"Progress rail still joins separately rounded slices at fractional pixels");var draw=GUI.Paints.Single();
   Check(Math.Abs(draw.rect[2]-rail.width*fraction)<.001f&&draw.uv.SequenceEqual(new[]{0f,0f,fraction,1f}),"Progress changes its full-width coordinate field while dragging");
  }
  Check(Texture2D.Allocations==allocations,"Dragging bars allocates new textures");
  foreach(Color tint in new[]{new Color(1,.1f,.1f),new Color(.1f,1,.1f),new Color(.1f,.1f,1),MenuTheme.Accent}){
   GUI.Reset();MenuTheme.Slider(new Rect(10.17f,15.25f,670.4f,30),.47f,0,1,tint);
   Check(!GUI.Paints.Any(x=>x.texture=="Local8.Menu.bar_track"),"Slider outline still has sliced atlas joins");
   Check(GUI.Paints.Count(x=>x.texture=="Local8.Menu.bar.fill")==4,"Slider rail or progress was dropped");
   Check(GUI.Paints.Any(x=>x.texture=="Local8.Menu.bar.fill"&&Math.Abs(x.color[1]-tint.g)<.001f&&Math.Abs(x.uv[2]-.47f)<.001f),"Slider loses its RGB/progress tint");
  }
  Color old=new Color(.2f,.3f,.4f,.7f);GUI.color=old;GUI.ThrowTexture=true;
  try{MenuArt.FillBar(new Rect(1,1,90,6),Color.white,.5f);}catch(InvalidOperationException){}
  finally{GUI.ThrowTexture=false;}
  Check(GUI.color.r==old.r&&GUI.color.a==old.a,"Continuous progress draw leaks its tint after failure");GUI.color=Color.white;
 }
 static void ArenaFolds(Local8Runtime r,string[] langs){
  foreach(string lang in langs)foreach(float width in new[]{690f,320f}){
   Language.Language.Code=lang;
   foreach(string name in new[]{"health","abilities","players"})typeof(PvpArenaUI).GetField(name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,false);
   PvpArena.Options.Arena=1;Local8Mod.Settings.PvpSpellsOnly=false;Local8Mod.Settings.PvpSoulRegeneration=11;
   Action render=()=>{GUI.Reset(width+10,4000);float row=0;PvpArenaUI.Draw(r,r.Session,width,ref row);Check(row<4000,"Expanded arena content lost scroll extent");};
   Action<Func<HostControl,bool>> click=find=>{render();var c=GUI.Controls.Single(find);GUI.ClickPoint=c.Rect.center;render();GUI.ClickPoint=null;render();};
   render();Check(!GUI.Controls.Any(x=>x.Tip==UiLocalization.Get("pvp.spells_only_tip")),"Spell rule remains outside its closed fold");
   click(c=>c.Text==UiLocalization.Get("menu.abilities"));
   var fold=GUI.Controls.Single(c=>c.Text==UiLocalization.Get("menu.abilities"));
   var rule=GUI.Controls.Single(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip"));
   Check(rule.Rect.x>=fold.Rect.x+12,"Expanded abilities have no visible indent");
   Check(GUI.Paints.Any(p=>p.text==UiLocalization.Get("common.no")&&p.rect[1]>=rule.Rect.y&&p.rect[1]<rule.Rect.yMax),"Spells-only setting has no localized No state");
   Check(!GUI.Paints.Any(p=>p.text==UiLocalization.Get("pvp.soul_regeneration")+": 11"),"Disabled spell rule shows a regeneration slider");
   var masks=PvpArena.Options.Masks;var wings=PvpArena.Options.InfiniteWings;
   click(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip"));
   Check(Local8Mod.Settings.PvpSpellsOnly&&PvpArena.Options.Masks==masks&&PvpArena.Options.InfiniteWings==wings,"Changing spell rule reapplies an arena preset");
   rule=GUI.Controls.Single(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip"));
   Check(GUI.Paints.Any(p=>p.text==UiLocalization.Get("common.yes")&&p.rect[1]>=rule.Rect.y&&p.rect[1]<rule.Rect.yMax),"Spells-only setting has no localized Yes state");
   var regen=GUI.Paints.Single(p=>p.text==UiLocalization.Get("pvp.soul_regeneration")+": 11");
   Check(regen.rect[0]>rule.Rect.x+8&&regen.rect[1]>rule.Rect.y,"Conditional SOUL row has no deeper indent");
   GUI.SliderValue=0;render();GUI.SliderValue=null;Check(Local8Mod.Settings.PvpSoulRegeneration==1,"Regeneration slider accepts zero");
   click(c=>c.Text=="-"&&Math.Abs(c.Rect.y-regen.rect[1]-21)<1);Check(Local8Mod.Settings.PvpSoulRegeneration==1,"Regeneration minus button accepts zero");
   Local8Mod.Settings.PvpSoulRegeneration=11;PvpMatch.Running=true;GUI.SliderValue=99;render();GUI.SliderValue=null;
   Check(Local8Mod.Settings.PvpSoulRegeneration==11&&!GUI.Controls.Single(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip")).Enabled,"Running match edits spell regeneration");
   click(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip"));Check(Local8Mod.Settings.PvpSpellsOnly,"Running match edits its weapon rule");PvpMatch.Running=false;
   click(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip"));Check(!Local8Mod.Settings.PvpSpellsOnly&&!GUI.Paints.Any(p=>p.text==UiLocalization.Get("pvp.soul_regeneration")+": 11"),"No does not hide the regeneration row");
   click(c=>c.Text==UiLocalization.Get("menu.abilities"));Check(!GUI.Controls.Any(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip")),"Collapsed abilities remain visible");
   click(c=>c.Text==UiLocalization.Get("menu.health_soul"));
   var healthFold=GUI.Controls.Single(c=>c.Text==UiLocalization.Get("menu.health_soul"));
   var healthLabel=GUI.Paints.Single(p=>p.text==PvpArena.Text("masks")+": "+PvpArena.Text("native"));
   Check(healthLabel.rect[0]>=healthFold.Rect.x+12,"Health/SOUL children have no indent");
   click(c=>c.Text==PvpArena.Text("perplayer"));
   var playersFold=GUI.Controls.Single(c=>c.Text==PvpArena.Text("perplayer"));
   Check(GUI.Paints.Single(p=>p.text=="P2  "+PvpArena.Text("masks")+": "+PvpArena.Text("native")).rect[0]>=playersFold.Rect.x+12,"Per-player children have no indent");
   foreach(var c in GUI.Controls)Check(c.Rect.x>=0&&c.Rect.xMax<=width,"Indented arena control exceeds the content width");
   // The shared weapon rule remains configurable for duels in the current room.
   PvpArena.Options.Arena=0;click(c=>c.Text==UiLocalization.Get("menu.abilities"));
   Check(GUI.Controls.Any(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip")),"Current-room duel lost its spell rule");
   Check(!GUI.Paints.Any(p=>p.text==PvpArena.Text("wings")),"Current-room rule presents unsupported arena overrides");
  }
  foreach(string name in new[]{"health","abilities","players"})typeof(PvpArenaUI).GetField(name,System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,false);
  PvpArena.Options.Arena=0;Local8Mod.Settings.PvpSpellsOnly=false;Language.Language.Code="EN";
 }
 static void PerformancePage(string[] langs){
  foreach(string lang in langs)foreach(float width in new[]{200f,240f,320f,440f,450f,600f,620f,690f}){
   Language.Language.Code=lang;var r=Runtime(8);GameManager.instance=new GameManager();PerformancePresets.Apply(r,2,true);
   GUI.Reset(width+10,4000);float row=0;PerformanceMenu.Draw(r,width,ref row);
   Check(row<4000&&row>GUI.Controls.Max(c=>c.Rect.yMax),"Performance page loses its scroll extent");
   foreach(var c in GUI.Controls)Check(c.Rect.x>=0&&c.Rect.xMax<=width&&c.Rect.width>0,"Translated performance button exceeds width");
   for(int i=0;i<GUI.Controls.Count;i++)for(int j=i+1;j<GUI.Controls.Count;j++){
    var a=GUI.Controls[i].Rect;var b=GUI.Controls[j].Rect;Check(a.x>=b.xMax||b.x>=a.xMax||a.y>=b.yMax||b.y>=a.yMax,"Performance choices overlap");
   }
   foreach(var paint in GUI.Paints.Where(p=>p.kind=="button"&&p.text.Length>0||p.kind=="label"&&p.tooltip==UiLocalization.Get("performance.automatic_tip"))){
    float textWidth=paint.rect[2]-paint.padding[0]-paint.padding[1],length=paint.text.Sum(c=>(c>1000?1f:.5f)*paint.size);
    float height=Math.Max(1,(float)Math.Ceiling(length/textWidth))*paint.size*1.25f+paint.padding[2]+paint.padding[3];
    Check(height<=paint.rect[3]+.6f,"Translated performance label is clipped: "+lang+" "+width+" "+paint.text);
   }
   Check(GUI.Paints.All(p=>p.text==null||!p.text.StartsWith("ui.performance.")),"Performance translation missing");
   foreach(string key in new[]{"particles_tip","blur_tip","decoration_tip","bloom_tip"})
    Check(GUI.Controls.Count(c=>c.Tip==UiLocalization.Get("performance."+key))>=3,"Performance choices lack translated contextual help: "+lang+" "+key);
  }
  Language.Language.Code="EN";var owner=Runtime(5);GameManager.instance=new GameManager();Hud.ResetTest(1);
  Click(owner,c=>c.Text==UiLocalization.Get("performance.maximum"));Check(owner.Performance.Preset==2&&!owner.Performance.Automatic,"F8 maximum preset callback fails");
  Click(owner,c=>c.Tip==UiLocalization.Get("performance.automatic_tip"));Check(owner.Performance.Automatic&&owner.Performance.Preset==2,"F8 automatic toggle fails");
  Click(owner,c=>c.Tip==UiLocalization.Get("effects.tip"));Check(owner.Performance.Preset==3&&!owner.Performance.Automatic&&!owner.ReduceHitEffects.Value,"F8 manual override fails");
  Click(owner,c=>c.Text==UiLocalization.Get("performance.restore"));Check(owner.Performance.Preset==0&&!owner.Performance.Automatic&&owner.Exports==1,"F8 restore callback fails");
  GameManager.instance=null;
 }
 static void PrimaryInputMenu(string[] languages){
  foreach(string language in languages){Language.Language.Code=language;
   var r=Runtime(8);Controls.Available=Enumerable.Range(0,8).Select(i=>new InputDevice{Name="Controller "+i}).ToArray();
   Controls.Available[2]=r.Session.Players[2].Device;
   for(int width=200;width<=690;width+=35){
    GUI.Reset(width+10,5000);float end=Hud.RenderPlayers(r,width);
    var inputs=GUI.Controls.Where(c=>c.Tip==UiLocalization.Get("input.auto_tip")||c.Tip==UiLocalization.Get("input.keyboard_tip")||c.Tip==UiLocalization.Get("input.assign_tip")).ToArray();
    Check(inputs.Length==10,"P1 mode or assigned-controller buttons missing");
    Check(inputs.All(c=>c.Enabled),"occupied controller cannot be assigned to P1");
    foreach(var c in inputs)Check(c.Rect.x>=0&&c.Rect.xMax<=width&&c.Rect.width>0&&c.Rect.yMax<end,"P1 controls exceed layout/scroll extent");
    for(int i=0;i<inputs.Length;i++)for(int j=i+1;j<inputs.Length;j++){var a=inputs[i].Rect;var b=inputs[j].Rect;Check(a.x>=b.xMax||b.x>=a.xMax||a.y>=b.yMax||b.y>=a.yMax,"P1 controls overlap");}
    Check(GUI.Paints.Any(p=>p.text==UiLocalization.Get("input.p1_title")&&p.rect[1]<80),"P1 control selection hidden below player profiles");
   }
   Hud.ResetTest(0);Click(r,c=>c.Tip==UiLocalization.Get("input.auto_tip"));Check(r.Session.AssignCalls==1,"Automatic P1 callback missing");
   Click(r,c=>c.Tip==UiLocalization.Get("input.keyboard_tip"));Check(r.Session.AssignCalls==2&&r.Session.Primary.Device==InputDevice.Null,"P1 keyboard callback missing");
   Click(r,c=>c.Tip==UiLocalization.Get("input.assign_tip"));Check(r.Session.AssignCalls==3,"P1 controller assignment callback missing");
  }
  Controls.Available=Array.Empty<InputDevice>();
 }
 static void Main(string[] args){
  var r=Runtime(8);string[] langs={"ES","EN","FR","DE","IT","PT","RU","ZH","JA","KO"};
  foreach(string lang in langs){Language.Language.Code=lang;
   foreach(var screen in new[]{(1920,1080),(1280,720),(1024,768),(640,360),(2560,1440)}){
    float scale=Mathf.Clamp(screen.Item2/900f,.65f,1.6f),w=screen.Item1/scale,h=screen.Item2/scale;
    for(int tab=0;tab<5;tab++){
     Hud.ResetTest(tab);GUISkin original=GUI.skin;GUIStyle originalText=Hud.CurrentText;
     Render(r,w,h);Render(r,w,h);
     Check(ReferenceEquals(GUI.skin,original),"Menu leaked skin into native UI");Check(ReferenceEquals(Hud.CurrentText,originalText),"Menu leaked HUD styles");
     Check(MenuLayout.Width(w)<=w&&MenuLayout.Height(h)<=h,"Panel exceeds viewport");
     foreach(var paint in GUI.Paints){Check(paint.rect.All(float.IsFinite),"Non-finite UI geometry");Check(paint.rect[2]>=0&&paint.rect[3]>=0,"Negative UI rectangle");Check(paint.text==null||!paint.text.StartsWith("ui.menu."),"Missing menu translation");}
     Check(GUI.enabled,"Menu leaked disabled input");
    }
   }
   for(int page=0;page<4;page++){Hud.ResetTest(2,page);Render(r);Render(r);Check(GUI.Controls.Any(c=>c.Text==UiLocalization.Get("pvp.start_rounds"))== (page<3),"Wrong PvP actions for page");}
   string sample=UiLocalization.Get("menu.font_sample")+UiLocalization.Get("tab.players")+UiLocalization.Get("respawn.label");
   foreach(char c in sample)Check(char.IsWhiteSpace(c)||MenuTheme.Label.font.HasCharacter(c),"Font drops language glyphs "+lang);
  }
  PrimaryInputMenu(langs);
  Language.Language.Code="EN";Hud.ResetTest(1);Render(r);int allocation=Texture2D.Allocations,queries=Resources.Queries;
  int decodes=Texture2D.Decodes;
  for(int i=0;i<200;i++)Render(r);
  Check(Texture2D.Decodes==decodes,"Menu decodes its atlas every frame");
  Check(decodes==1&&MenuArt.Ready,"Approved atlas not loaded exactly once");
  Check(Texture2D.Created.Where(t=>t.name=="Local8.Menu.atlas").All(t=>t.Destroyed),"Temporary atlas not released");
  Check(MenuArt.Texture(MenuArt.Players).width==64&&MenuArt.Texture(MenuArt.Players).height==64,"Wrong players crop");
  Check(MenuArt.Texture(MenuArt.Header).width==512&&MenuArt.Texture(MenuArt.Header).height==112,"Wrong header crop");
  Check(MenuArt.Texture(MenuArt.Button).Pixels[20*256+128].a==0,"Button frame alpha hole lost");
  Check(Texture2D.Created.First(t=>t.name=="Local8.Menu.button.normal").Pixels[20*256+128].a>.95f,"Button interior is transparent");
  Check(Texture2D.Allocations==allocation,"Menu recreates decorative textures per frame");Check(Resources.Queries==queries,"Menu scans loaded fonts per frame");
  foreach(float y in new[]{-5f,0f,123f,10000f}){float pos=MenuLayout.ClampScroll(y,940,500);Check(pos>=0&&pos<=440,"Scroll can strand content");}
  Check(MenuLayout.ContentHeight(210,500)==500,"Short pages have artificial scroll space");Check(MenuLayout.ContentHeight(720,500)==732,"Measured page truncates options");
  // The UI mutations exercise production callbacks. They do not simulate game physics.
  r=Runtime(2);Hud.ResetTest(1);bool initial=r.ZoomByPlayerCount.Value;
  Click(r,c=>c.Tip==UiLocalization.Get("camera.tip"));Check(r.ZoomByPlayerCount.Value!=initial,"Shared camera toggle did not apply");
  Click(r,c=>c.Text==UiLocalization.Get("difficulty.extreme"));Check(r.Difficulty.Value==3,"Difficulty selection lost its callback");
  bool lights=r.PlayerLights.Value;float gather=r.GatherDistance.Value;
  Click(r,c=>c.Tip==UiLocalization.Get("camera.player_lights.tip"));Check(r.PlayerLights.Value!=lights&&r.GatherDistance.Value==gather,"Player light control still edits unrelated gathering distance");
  bool smooth=r.SmoothPlayerLights.Value;Click(r,c=>c.Tip==UiLocalization.Get("camera.smooth_lights.tip"));Check(r.SmoothPlayerLights.Value!=smooth,"Light smoothing toggle did not apply");
  r.SideMargin.Value=7;Click(r,c=>c.Text=="+");Check(r.SideMargin.Value==7.5f,"Image plus lost the camera-margin callback");
  Click(r,c=>c.Text=="-");Check(r.SideMargin.Value==7,"Image minus lost the camera-margin callback");
  GUI.SliderValue=42;Render(r);Check(r.RespawnSeconds.Value==15,"Disabled respawn slider changed saved time");r.TimedRespawn.Value=true;Render(r);Check(r.RespawnSeconds.Value==42,"Enabled respawn slider failed");GUI.SliderValue=null;
  Hud.ResetTest(0);Click(r,c=>c.Tip==UiLocalization.Get("menu.customize_tip")&&c.Rect.x>800);Check(Hud.selected==1,"Player card selected wrong actor");
  Click(r,c=>c.Text==UiLocalization.Get("menu.controls"));Check(Hud.CurrentTab==4,"Keyboard edit route unavailable");
  string[] expected={"up","down","left","right","jump","attack","dash","cast","focus","dreamNail","superDash","quickMap","rescue"};
  for(int i=0;i<expected.Length;i++){Hud.EditKeyboard(1,false);ActionKeys.Capturing=0;Click(r,c=>c.Text==expected[i]+"Key");Check(ActionKeys.Capturing==i+10,"Wrong independent key binding index "+expected[i]);}
  Click(r,c=>c.Text==UiLocalization.Get("tab.game"));Check(ActionKeys.Capturing==0&&Hud.CurrentTab==1,"Leaving key capture stranded controls");
  Hud.ResetTest(0);Click(r,c=>c.Text==UiLocalization.Get("keyboard.add"));Check(Hud.CurrentTab==4,"Add keyboard route missing");
  Click(r,c=>c.Text==UiLocalization.Get("keyboard.add_now"));Check(r.Session.Players.Count==3,"Add keyboard edits the existing selected player");Check(Hud.selected==2,"New keyboard player has wrong owner");
  Hud.ResetTest(0);Click(r,c=>c.Tip==UiLocalization.Get("menu.repair_tip"));Check(r.Session.RepairCalls==1,"Recovery callback missing");
  Click(r,c=>c.Tip==UiLocalization.Get("menu.gather_tip"));Check(r.Session.GatherCalls==1,"Gather callback missing");
  Click(r,c=>c.Tip==UiLocalization.Get("menu.remove_tip"));Check(r.Session.RemoveCalls==1&&r.Session.Players.Count==2,"Remove callback missing");
  var solo=Runtime(1);Hud.ResetTest(0);Click(solo,c=>c.Tip==UiLocalization.Get("menu.remove_tip"));Check(solo.Session.RemoveCalls==0,"Remove enabled without extra players");
  Hud.ResetTest(2,2);Local8Mod.Settings.DuelSeconds=0;Render(r);Check(GUI.Paints.Any(p=>p.text!=null&&p.text.Contains(UiLocalization.Get("pvp.unlimited"))),"Infinite default clock absent");
  Hud.ResetTest(2,0);Local8Mod.Settings.PvpNailDamage=1;Click(r,c=>c.Text=="+");Check(Local8Mod.Settings.PvpNailDamage==2&&Local8Mod.Settings.PvpSpellDamage==1,"Image step altered the wrong damage rule");
  Click(r,c=>c.Text=="-");Check(Local8Mod.Settings.PvpNailDamage==1,"Image minus lost the damage callback");
  Render(r);
  var number=GUI.Paints.Single(x=>x.text==UiLocalization.Get("pvp.nail")+": 1");
  Check(number.size==MenuTheme.Label.fontSize&&number.color.SequenceEqual(new[]{MenuTheme.Paper.r,MenuTheme.Paper.g,MenuTheme.Paper.b,MenuTheme.Paper.a}),"PvP numbers still use the small muted style");
  Check(!GUI.Controls.Any(c=>c.Tip==UiLocalization.Get("pvp.spells_only_tip")),"Spell rule is duplicated on the combat page");
  Hud.ResetTest(2,2);
  Render(r);Check(GUI.Paints.Single(x=>x.text!=null&&x.text.StartsWith(UiLocalization.Get("pvp.bestof"))).size==MenuTheme.Label.fontSize,"Best-of label does not match round time");
  Click(r,c=>c.Text==UiLocalization.Get("pvp.start_rounds"));Check(PvpMatch.Starts==1,"PvP start callback missing");
  ArenaFolds(r,langs);
  // Mouse chooses the target slot, and the click modifies that slot's equipment.
  r=Runtime(3);Hud.ResetTest(0);Charms.Open(r.Session.Players[0]);
  Click(r,c=>c.Text=="P2");Check(Charms.Selected==1,"F8 player selection label still has no click action");
  Render(r);HostControl charm=GUI.Controls.First(c=>c.Text==""&&c.Rect.y>220&&c.Rect.height>40);
  GUI.ClickPoint=charm.Rect.center;Render(r);GUI.ClickPoint=null;
  Check(r.Session.Players[1].Charms.Equipped.Count==1&&r.Session.Players[0].Charms.Equipped.Count==0,"Mouse equipped P1 instead of selected P2");
  Charms.Bench=false;Render(r);Check(GUI.Paints.Any(p=>p.text==UiLocalization.Get("charms.bench")),"F8 does not show selected player's bench restriction");Charms.Bench=true;Charms.Close();
  Hud.ResetTest(2,1);Click(r,c=>c.Text==UiLocalization.Get("stats.adventure"));
  Check(GUI.Paints.Any(p=>p.text==UiLocalization.Get("stats.title")),"Adventure score page is missing");
  Check(!GUI.Controls.Any(p=>p.Text==UiLocalization.Get("pvp.start_rounds")),"Adventure statistics page starts PvP");
  Hud.ResetTest(0);GUI.FieldInput="Raul";Click(r,c=>c.Text==UiLocalization.Get("profile.create"));GUI.FieldInput=null;Check(PlayerProfiles.Saves==1,"Profile name save callback missing");
  Check(MenuTheme.Skin.verticalScrollbarThumb.normal.background.name=="Local8.Menu.scroll.thumb","Scrollbar still stretches the horizontal diamond");
  Check(MenuTheme.Skin.verticalScrollbar.fixedWidth>=14,"Scrollbar too narrow to grab");
  var track=MenuTheme.Skin.verticalScrollbar;var thumb=MenuTheme.Skin.verticalScrollbarThumb;
  Check(track.fixedWidth==thumb.fixedWidth&&thumb.margin.left==0&&thumb.margin.right==0,"Scrollbar thumb is horizontally displaced from its track");
  ContinuousBars();Profiles(langs);PerformancePage(langs);
  foreach(var texture in Texture2D.Created.Where(t=>t.name=="Local8.Menu.bar.fill"))foreach(var color in texture.Pixels)
   Check(color.a==0||color.r==1&&color.g==1&&color.b==1,"Black source pixels contaminate tinted sliders");
  // Failure during custom charm rendering restores the menu's skin and styles exactly.
  Hud.ResetTest(0);Charms.Editing=true;Charms.Selected=0;Charms.ThrowIcon=true;GUISkin before=GUI.skin;GUIStyle text=Hud.CurrentText;
  bool threw=false;try{Render(r);}catch(Exception){threw=true;}Charms.Editing=false;Charms.ThrowIcon=false;
  Check(threw&&ReferenceEquals(GUI.skin,before)&&ReferenceEquals(Hud.CurrentText,text),"Exception stranded native UI skin");
  Color old=new Color(.2f,.3f,.4f,.7f);GUI.color=old;GUI.ThrowTexture=true;try{MenuTheme.Fill(new Rect(1,1,4,4),Color.white);}catch(InvalidOperationException){}GUI.ThrowTexture=false;Check(GUI.color.r==old.r&&GUI.color.a==old.a,"Theme leaked tint after draw failure");GUI.color=Color.white;
  if(args.Length>0){Directory.CreateDirectory(args[0]);var demo=Runtime(4);Language.Language.Code="ES";SavePreview(Path.Combine(args[0],"game.json"),demo,1);SavePreview(Path.Combine(args[0],"players.json"),demo,0);SavePreview(Path.Combine(args[0],"pvp.json"),demo,2);SavePreview(Path.Combine(args[0],"settings.json"),demo,3);Charms.Open(demo.Session.Players[1]);SavePreview(Path.Combine(args[0],"charms.json"),demo,0);Charms.Close();
   Hud.ResetTest(2,1);Click(demo,c=>c.Text==UiLocalization.Get("stats.adventure"));Render(demo);
   var statsTextures=GUI.Textures.ToDictionary(x=>x.Key,x=>new {width=x.Value.width,height=x.Value.height,rgba=Convert.ToBase64String(x.Value.Pixels.SelectMany(p=>new[]{(byte)(p.r*255),(byte)(p.g*255),(byte)(p.b*255),(byte)(p.a*255)}).ToArray())});
   File.WriteAllText(Path.Combine(args[0],"adventure.json"),JsonSerializer.Serialize(new{width=GUI.ViewWidth,height=GUI.ViewHeight,paints=GUI.Paints,textures=statsTextures},new JsonSerializerOptions{IncludeFields=true}));
   PlayerProfiles.All=new[]{new PlayerProfileData{Id="koopa",Name="Koopa"},new PlayerProfileData{Id="dd22",Name="dd22"}};demo.Session.Players[2].ProfileId="dd22";PlayerProfiles.LoadAllowed=false;
   var target=demo.Session.Players[1];PlayerProfilesUI.Reset();ProfileClick(demo,target,690,c=>c.Text==UiLocalization.Get("profile.load"));SavePaints(Path.Combine(args[0],"profiles-guest.json"));
   PlayerProfiles.LoadAllowed=true;ProfileClick(demo,target,690,c=>c.Text=="Koopa");ProfileRender(demo,target,690);SavePaints(Path.Combine(args[0],"profiles-active.json"));
   ProfileClick(demo,target,320,c=>c.Text==UiLocalization.Get("profile.new"));SavePaints(Path.Combine(args[0],"profiles-narrow.json"));
   Local8Mod.Settings.PvpSpellsOnly=true;Local8Mod.Settings.PvpSoulRegeneration=11;PvpArena.Options.Arena=1;Hud.ResetTest(2,3);Click(demo,c=>c.Text==UiLocalization.Get("menu.abilities"));SavePreview(Path.Combine(args[0],"spells-only.json"),demo,2,3);SavePreview(Path.Combine(args[0],"rounds.json"),demo,2,2);PvpArena.Options.Arena=0;
   PlayerProfiles.LoadAllowed=true;Language.Language.Code="JA";SavePreview(Path.Combine(args[0],"japanese.json"),demo,1);}
  var owned=Texture2D.Created.Where(t=>t.name.StartsWith("Local8.Menu.")).ToArray();MenuTheme.Release();Check(owned.All(t=>t.Destroyed),"Theme leaked owned textures");int released=Texture2D.Allocations;Hud.ResetTest(1);Render(r);Check(Texture2D.Allocations==released+29,"Theme cannot initialize after cleanup");
  Console.WriteLine("PASS "+checks+" production menu checks: ten languages, five resolutions, independent bindings, callbacks, measured scrolling, font/texture caches and scope restoration. Unity player visuals remain untested.");
 }
}

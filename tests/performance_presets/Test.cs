using System;
using System.Linq;
using System.Text.Json;
using KO.HollowKnight8;
using UnityEngine;

static class Test {
 static int checks;
 static readonly JsonSerializerOptions Json=new(){IncludeFields=true};
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static Local8Runtime Runtime(int count,int particles=1,ShaderQualities blur=ShaderQualities.Medium){
  if(Plugin.Self!=null)PerformancePresets.Shutdown(Plugin.Self);
  Time.unscaledTime=0;GameManager.instance=new(){gameSettings=new(){particleEffectsLevel=particles,shaderQuality=blur}};
  var r=new Local8Runtime();r.Performance.Ensure(false,true);Plugin.Self=r;Roster(r,count);PerformancePresets.Install(r);return r;
 }
 static void Roster(Local8Runtime r,int count){while(r.Session.Players.Count>count)r.Session.Players.RemoveAt(r.Session.Players.Count-1);while(r.Session.Players.Count<count)r.Session.Players.Add(new());}
 static void Tick(Local8Runtime r)=>PerformancePresets.Tick(r);
 static void Automatic(){
  var r=Runtime(1);var gs=GameManager.instance.gameSettings;
  PerformancePresets.Install(r);PerformancePresets.Install(r);Check(On.GameSettings.Count==1,"repeated install duplicates the native save hook");
  foreach(int count in new[]{0,1,2,3,4,5,6,7,8,4,2,8,1}){
   Roster(r,count);Tick(r);int expected=count>=5?2:count>=3?1:0;
   Check(r.Performance.Automatic&&r.Performance.Preset==expected,"wrong automatic threshold "+count);
   Check(gs.particleEffectsLevel==(expected==0?1:0)&&gs.shaderQuality==(expected==0?ShaderQualities.Medium:ShaderQualities.Low),"native quality did not follow profile "+count);
   Check(r.Performance.Detail==expected&&r.Performance.Bloom==expected&&r.PerformanceMode.Value==(expected>0),"profile graphics tier differs "+count);
   Check(r.Session.Players.All(p=>p.Health==5&&p.Role==4&&p.Skin=="CustomKnight"&&p.Charms.SequenceEqual(new[]{1,6,40,81})),"profile mutates gameplay or custom charms");
  }
  Roster(r,8);Tick(r);
  int before=NativeRenderBudget.Resets,refreshes=GameManager.instance.Refreshes;
  Roster(r,6);Tick(r);Roster(r,8);Tick(r);
  Check(NativeRenderBudget.Resets==before&&GameManager.instance.Refreshes==refreshes,"joins within the same automatic tier needlessly recreate renderer resources");
  foreach(var p in r.Session.Players)p.Alive=false;Tick(r);
  Check(r.Performance.Preset==2,"deaths lowered the roster profile");
  PerformancePresets.Apply(r,0);Tick(r);Check(!r.Performance.Automatic&&r.Performance.Preset==0&&gs.particleEffectsLevel==1,"manual normal ignored");
  Roster(r,5);Tick(r);Check(r.Performance.Preset==0,"automatic overrides a manual choice");
  PerformancePresets.Automatic(r,true);Tick(r);Check(r.Performance.Preset==2,"automatic cannot resume");
  PerformancePresets.Customize(r);r.Performance.Detail=0;r.Performance.Bloom=1;r.ReduceHitEffects.Value=false;Tick(r);
  Check(r.Performance.Preset==3&&!r.Performance.Automatic&&r.PerformanceMode.Value&&!PerformanceTuning.ReduceDamage,"custom damage switch ignored");
  Check(PerformanceTuning.CosmeticRate(10)==10,"bloom-only optimization lowers decoration");
  r.Performance.Detail=2;Tick(r);Check(PerformanceTuning.CosmeticRate(10)==2,"maximum decoration rate not reduced");
  PerformancePresets.Restore(r);Tick(r);Check(!r.Performance.Automatic&&gs.particleEffectsLevel==1&&gs.shaderQuality==ShaderQualities.Medium&&r.Exports==1,"restore does not restore baseline/save profile");
 }
 static void Originals(){
  foreach(int particles in new[]{0,1})foreach(ShaderQualities blur in Enum.GetValues<ShaderQualities>()){
   var r=Runtime(8,particles,blur);var gm=GameManager.instance;var gs=gm.gameSettings;Tick(r);
   Check(gs.particleEffectsLevel==0&&gs.shaderQuality==ShaderQualities.Low,"maximum did not apply native low");
   gs.resolution=17;gs.vsync=false;gs.SaveVideoSettings();var saved=gs.Saved.Single();
   Check(saved.particles==particles&&saved.blur==blur,"native preferences permanently overwritten");
   Check(saved.resolution==17&&!saved.vsync&&saved.cap==144&&saved.fullscreen&&saved.controls=="keyboard-p1-and-p2","native save loses unrelated user preferences");
   Check(gs.particleEffectsLevel==0&&gs.shaderQuality==ShaderQualities.Low,"saving loses live preset");
   int refreshes=gm.Refreshes;gs.ThrowSave=true;try{gs.SaveVideoSettings();}catch(Exception){}gs.ThrowSave=false;
   Check(gs.particleEffectsLevel==0&&gs.shaderQuality==ShaderQualities.Low&&gm.Refreshes==refreshes,"save exception loses override or refreshes particles");
   r.Enabled.Value=false;Tick(r);Check(gs.particleEffectsLevel==particles&&gs.shaderQuality==blur,"disable loses originals");
   Check(!r.Performance.Particles.Held&&!r.Performance.Blur.Held,"disable retains stale ownership");
   r.Enabled.Value=true;Tick(r);Check(gs.particleEffectsLevel==0,"reenabling loses preset");
   r.Session.Active=false;Tick(r);Check(gs.particleEffectsLevel==particles&&gs.shaderQuality==blur,"leaving co-op fails to restore");
   r.Session.Active=true;Tick(r);PerformancePresets.Shutdown(r);
   Check(gs.particleEffectsLevel==particles&&gs.shaderQuality==blur&&On.GameSettings.Count==0,"shutdown fails to restore/unhook");
   Plugin.Self=null;
  }
  {
   var r=Runtime(8);var gs=GameManager.instance.gameSettings;Tick(r);
   gs.DuringSave=()=>PerformancePresets.Shutdown(r);gs.SaveVideoSettings();
   Check(gs.particleEffectsLevel==1&&gs.shaderQuality==ShaderQualities.Medium&&On.GameSettings.Count==0,"unload during save reapplies live low");
   gs.DuringSave=null;
  }
  {
   var r=Runtime(8);var gs=GameManager.instance.gameSettings;Tick(r);gs.DuringSave=()=>r.Enabled.Value=false;gs.SaveVideoSettings();
   Check(gs.particleEffectsLevel==1&&gs.shaderQuality==ShaderQualities.Medium,"disable during save reapplies low");gs.DuringSave=null;Tick(r);
  }
  {
   var r=Runtime(8);var gs=GameManager.instance.gameSettings;Tick(r);gs.DuringSave=()=>gs.shaderQuality=ShaderQualities.High;gs.SaveVideoSettings();gs.DuringSave=null;
   Check(gs.shaderQuality==ShaderQualities.High&&gs.particleEffectsLevel==0&&!r.Performance.Automatic,"native save callback overwrites another mod's quality edit");
   PerformancePresets.Restore(r);Check(gs.shaderQuality==ShaderQualities.High&&gs.particleEffectsLevel==1,"restoration overwrites a quality edit made during save");
  }
 }
 static void ExternalEdits(){
  foreach(bool saveFirst in new[]{false,true})foreach(bool editParticles in new[]{false,true}){
   var r=Runtime(8);var gs=GameManager.instance.gameSettings;Tick(r);
   if(editParticles)gs.particleEffectsLevel=1;else gs.shaderQuality=ShaderQualities.High;
   if(saveFirst)gs.SaveVideoSettings();else Tick(r);
   Check(!r.Performance.Automatic&&r.Performance.Preset==3,"external graphics edits lost");
   Check(gs.particleEffectsLevel==(editParticles?1:0)&&gs.shaderQuality==(editParticles?ShaderQualities.Low:ShaderQualities.High),"external setting overridden");
   Roster(r,5);Tick(r);Check(!r.Performance.Automatic,"join overwrote external setting");
   PerformancePresets.Restore(r);
   Check(gs.particleEffectsLevel==1&&gs.shaderQuality==(editParticles?ShaderQualities.Medium:ShaderQualities.High),"restore overwrites an external video preference");
  }
  {
   var r=Runtime(8);Tick(r);var replacement=new GameSettings{shaderQuality=ShaderQualities.High};GameManager.instance.gameSettings=replacement;Tick(r);
   Check(!r.Performance.Automatic&&replacement.particleEffectsLevel==1&&replacement.shaderQuality==ShaderQualities.High,"new settings object loses externally chosen quality");
  }
  {
   var r=Runtime(8);var gm=GameManager.instance;GameManager.instance=null;Tick(r);Check(!r.Performance.Particles.Held,"missing GameManager invents native baseline");
   GameManager.instance=gm;Tick(r);Check(gm.gameSettings.particleEffectsLevel==0,"deferred profile never applies");
  }
 }
 static void Migration(){
  var legacy=JsonSerializer.Deserialize<Local8Settings>("{\"PerformanceMode\":true,\"ReduceHitEffects\":false,\"SkinIds\":[\"custom-p1\",\"custom-p2\",null,null,null,null,null,null],\"Performance\":null}",Json);
  legacy.Ensure();Check(legacy.Performance.Automatic&&legacy.Performance.Preset==1&&!legacy.Performance.NormalHitEffects,"legacy settings migration loses auto/baseline");
  Check(legacy.SkinIds[1]=="custom-p2","migration changes old skins");
  legacy.Performance.Automatic=false;legacy.Performance.Preset=3;legacy.Performance.Detail=2;legacy.Performance.Bloom=0;
  var roundtrip=JsonSerializer.Deserialize<Local8Settings>(JsonSerializer.Serialize(legacy,Json),Json);roundtrip.Ensure();
  Check(!roundtrip.Performance.Automatic&&roundtrip.Performance.Preset==3&&roundtrip.Performance.Detail==2&&roundtrip.Performance.Bloom==0,"manual preset lost across restart");
  var malformed=JsonSerializer.Deserialize<Local8Settings>("{\"Performance\":{\"Initialized\":true,\"Automatic\":false,\"Preset\":90,\"Detail\":-30,\"Bloom\":90,\"NativeParticles\":90,\"NativeBlur\":-30,\"Particles\":{\"Held\":true,\"Original\":30,\"Applied\":0},\"Blur\":null}}",Json);malformed.Ensure();
  Check(malformed.Performance.Preset==3&&malformed.Performance.Detail==0&&malformed.Performance.Bloom==2&&malformed.Performance.NativeParticles==1&&malformed.Performance.NativeBlur==-1&&!malformed.Performance.Particles.Held&&malformed.Performance.Blur!=null,"invalid settings not bounded");
  var r=Runtime(8);Tick(r);var serialized=JsonSerializer.Serialize(r.Performance,Json);PerformancePresets.Shutdown(r);
  r=Runtime(8,0,ShaderQualities.Low);r.Performance=JsonSerializer.Deserialize<PerformanceConfiguration>(serialized,Json);r.Performance.Ensure(true,true);PerformancePresets.Install(r);Tick(r);PerformancePresets.Restore(r);
  Check(GameManager.instance.gameSettings.particleEffectsLevel==1&&GameManager.instance.gameSettings.shaderQuality==ShaderQualities.Medium,"restart cannot recover matching saved baseline");
  r=Runtime(8,1,ShaderQualities.Medium);r.Performance=JsonSerializer.Deserialize<PerformanceConfiguration>(serialized,Json);r.Performance.Ensure(true,true);PerformancePresets.Install(r);Tick(r);
  Check(r.Performance.Automatic&&r.Performance.Preset==2&&GameManager.instance.gameSettings.particleEffectsLevel==0&&GameManager.instance.gameSettings.shaderQuality==ShaderQualities.Low,"clean restart mistakes saved original preferences for a manual edit");
  PerformancePresets.Restore(r);Check(GameManager.instance.gameSettings.particleEffectsLevel==1&&GameManager.instance.gameSettings.shaderQuality==ShaderQualities.Medium,"clean restart loses restored original preferences");
  r=Runtime(8,1,ShaderQualities.High);r.Performance=JsonSerializer.Deserialize<PerformanceConfiguration>(serialized,Json);r.Performance.Ensure(true,true);PerformancePresets.Install(r);Tick(r);
  Check(!r.Performance.Automatic&&GameManager.instance.gameSettings.shaderQuality==ShaderQualities.High,"stale backup overrides a new startup preference");
 }
 static void RefreshAndSteady(){
  var r=Runtime(8);var gm=GameManager.instance;gm.FailRefreshes=1;Tick(r);
  Check(gm.Refreshes==1&&gm.gameSettings.particleEffectsLevel==0,"native override failure not contained");
  for(int i=0;i<1000;i++)Tick(r);Check(gm.Refreshes==1,"failed refresh retried every frame");
  Time.unscaledTime=1;Tick(r);Check(gm.Refreshes==2,"failed native particle listener never retries");
  for(int i=0;i<5000;i++)Tick(r);
  int resets=NativeRenderBudget.Resets,logs=Diagnostics.Messages.Count,refreshes=gm.Refreshes;
  long memory=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)Tick(r);long allocated=GC.GetAllocatedBytesForCurrentThread()-memory;
  Check(allocated==0&&gm.Refreshes==refreshes&&NativeRenderBudget.Resets==resets&&Diagnostics.Messages.Count==logs,"steady profiles allocate/refresh/log on every frame");
  Console.WriteLine("Steady profiles: 10,000 updates allocate "+allocated+" bytes, 0 extra native refreshes, 0 renderer resets, 0 logs.");
  r=Runtime(8);gm=GameManager.instance;gm.FailRefreshes=10;Tick(r);for(int i=1;i<=10;i++){Time.unscaledTime=i;Tick(r);}
  Check(gm.Refreshes==3,"broken listener retry budget unbounded");
 }
 static float Menu(Local8Runtime r,float width){MenuTheme.Clear();float row=0;PerformanceMenu.Draw(r,width,ref row);return row;}
 static void MenuActions(){
  var r=Runtime(5);Tick(r);
  for(int width=200;width<=720;width+=10){float end=Menu(r,width);var controls=MenuTheme.Controls;
   Check(end>controls.Max(c=>c.Rect.yMax),"scroll extent excludes final buttons");
   foreach(var c in controls)Check(c.Rect.width>0&&c.Rect.x>=0&&c.Rect.xMax<=width,"profile button exceeds viewport");
   for(int i=0;i<controls.Count;i++)for(int j=i+1;j<controls.Count;j++){var a=controls[i].Rect;var b=controls[j].Rect;Check(a.x>=b.xMax||b.x>=a.xMax||a.y>=b.yMax||b.y>=a.yMax,"profile controls overlap");}
  }
  Check(MenuTheme.Paragraphs.Any(p=>p.Contains("Players 5")&&p.Contains("maximum")),"recommended/current profile absent");
  MenuTheme.Click=c=>c.Text=="performance.normal"&&c.Tip=="performance.presets_tip";Menu(r,690);MenuTheme.Click=null;Tick(r);
  Check(r.Performance.Preset==0&&!r.Performance.Automatic&&GameManager.instance.gameSettings.particleEffectsLevel==1,"preset button ineffective");
  MenuTheme.Click=c=>c.Text=="performance.automatic";Menu(r,690);MenuTheme.Click=null;Tick(r);Check(r.Performance.Preset==2&&r.Performance.Automatic,"automatic UI ineffective");
  var selected=MenuTheme.Controls.First(c=>c.Text=="performance.low"&&c.Selected);float y=selected.Rect.y;
  MenuTheme.Click=c=>c.Text=="performance.low"&&c.Rect.y==y;Menu(r,690);MenuTheme.Click=null;Check(r.Performance.Automatic,"clicking selected choice unexpectedly disables auto");
  MenuTheme.Click=c=>c.Text=="performance.normal"&&c.Rect.y==y;Menu(r,690);MenuTheme.Click=null;Tick(r);
  Check(r.Performance.Preset==3&&!r.Performance.Automatic&&GameManager.instance.gameSettings.particleEffectsLevel==1,"manual native particle option ineffective");
  MenuTheme.Click=c=>c.Text=="effects.label";Menu(r,690);MenuTheme.Click=null;Tick(r);Check(!r.ReduceHitEffects.Value&&!PerformanceTuning.ReduceDamage,"manual hurt-effects checkbox ineffective");
  MenuTheme.Click=c=>c.Text=="performance.restore";Menu(r,690);MenuTheme.Click=null;Tick(r);
  Check(r.Performance.Preset==0&&r.Exports==1&&GameManager.instance.gameSettings.shaderQuality==ShaderQualities.Medium,"restore button ineffective");
 }
 static GameObject Particle(float rate,int max,bool start=true){var obj=new GameObject();var p=obj.Add<ParticleSystem>();p.Rate=rate;p.Max=max;var reducer=obj.Add<ReduceParticleEffects>();if(start)reducer.Start();return obj;}
 static void ParticleCopies(){
  var r=Runtime(8,0);Tick(r);var source=Particle(90,501);var p=source.GetComponent<ParticleSystem>();Check(p.Rate.constant==45&&p.Max==250,"native particle fixture wrong");
  for(int i=0;i<7;i++){
   GameObject clone;
   using(NativeParticleCopy.Begin(source)){Check(p.Rate.constant==90&&p.Max==501,"clone seed remains reduced");clone=Particle(p.Rate.constant,p.Max,false);}
   Check(p.Rate.constant==45&&p.Max==250,"copy changes primary emitter");clone.GetComponent<ReduceParticleEffects>().Start();var cp=clone.GetComponent<ParticleSystem>();
   Check(cp.Rate.constant==45&&cp.Max==250,"low applied twice to new player");
   GameManager.instance.gameSettings.particleEffectsLevel=1;clone.GetComponent<ReduceParticleEffects>().SetEmission();Check(cp.Rate.constant==90&&cp.Max==501,"clone cannot restore normal density");GameManager.instance.gameSettings.particleEffectsLevel=0;
  }
  try{using(NativeParticleCopy.Begin(source))throw new Exception("instantiate failed");}catch(Exception){}
  Check(p.Rate.constant==45&&p.Max==250,"instantiate failure loses original primary values");
  p.Rate=new(){mode=ParticleSystemCurveMode.Curve,constant=12,CurveId=88};p.Max=37;
  using(NativeParticleCopy.Begin(source))Check(p.Rate.CurveId==88&&p.Max==37,"copy overrides external mod emitter/curve");
  Check(p.Rate.CurveId==88&&p.Rate.mode==ParticleSystemCurveMode.Curve&&p.Max==37,"custom particle curve lost");
  var uninitialized=Particle(66,400,false);using(NativeParticleCopy.Begin(uninitialized))Check(uninitialized.GetComponent<ParticleSystem>().Rate.constant==66,"uninitialized native cache used");
  p.Rate=45;p.Max=250;p.ThrowRateOnce=true;using(NativeParticleCopy.Begin(source)){}
  Check(p.Rate.constant==45&&p.Max==250,"partial particle seed failure loses primary settings");
  GameManager.instance.gameSettings.particleEffectsLevel=1;Check(NativeParticleCopy.Begin(source)==null,"normal profile performs particle copy work");
 }
 static int Main(){try{Automatic();Originals();ExternalEdits();Migration();RefreshAndSteady();MenuActions();ParticleCopies();PerformancePresets.Shutdown(Plugin.Self);Check(On.GameSettings.Count==0,"native save hook leaked");Console.WriteLine("PASS performance presets: "+checks+" checks (controlled host; no Unity FPS measurement)");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}

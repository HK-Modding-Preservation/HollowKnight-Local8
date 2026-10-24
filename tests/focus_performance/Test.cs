using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;
using KO.HollowKnight8;

static class Test {
 static int checks;
 static void Check(bool ok, string why) { checks++; if (!ok) throw new Exception(why); }
 static GameObject Child(string name, Transform parent) { var g = new GameObject(name); g.transform.parent = parent; return g; }
 static Camera Camera(string name, int layers = -1) { var c = new GameObject(name).Add<Camera>(); c.cullingMask = layers; return c; }
 static SpriteRenderer Sprite(string name, Transform parent, string asset = "black_solid") {
  var r = Child(name, parent).Add<SpriteRenderer>(); r.sprite = new Sprite { name = asset }; return r;
 }
 sealed class Targets : FsmStateAction {
  public GameObject Direct; public Component Component; public FsmGameObject Wrapped;
  public FsmObject Object; public FsmOwnerDefault Owner; public FsmOwnerDefault Own;
 }
 sealed class Fixture {
  internal CoopSession Session = new(); internal Camera Main, Other;
  internal readonly List<Renderer> Masks = new(), Whites = new(), Solids = new();
  internal Fixture(int count) {
   FocusBackdropGuard.Reset(); PlayerLighting.Reset(); VanillaHud.Restore(); BlurPlane.All.Clear(); GameObject.All.Clear(); Time.frameCount++; Time.unscaledTime = 5;
   CoopEnding.Active = EndingAbsorption.Active = false; Diagnostics.Messages.Clear();
   GroupVignette.Throw = false; GroupVignette.Renders = 0;
   var gm = new GameObject("GM").Add<GameManager>(); GameManager.instance = gm;
   Main = Camera("Main"); Other = Camera("Auxiliary");
   GameCameras.instance = new GameObject("Cameras").Add<GameCameras>(); GameCameras.instance.mainCamera = Main;
   Plugin.Self = new Local8Runtime { Session = Session };
   for (int i = 0; i < count; i++) {
    var h = new GameObject("Hero" + i).Add<HeroController>(); var p = new PlayerSlot { Index = i, Hero = h }; Session.Players.Add(p);
    h.heroLight = Sprite("HeroLight", h.transform, "light_effect_v02"); Whites.Add(h.heroLight);
    h.vignette = Sprite("Vignette", h.transform); Masks.Add(h.vignette);
    Masks.Add(Child("DarknessBG", h.vignette.transform).Add<MeshRenderer>());
    // A renamed native root must still be found through its field.
    h.vignetteFSM = Child("Native renamed root", h.transform).Add<PlayMakerFSM>();
    Masks.Add(Child("Plate", h.vignetteFSM.transform).Add<MeshRenderer>());
    Whites.Add(Child("Dust", h.vignetteFSM.transform).Add<ParticleSystemRenderer>());
    h.spellControl = Child("SpellControl", h.transform).Add<PlayMakerFSM>();
    var solid = Sprite("Detached solid " + i, null); Solids.Add(solid);
    var mesh = new GameObject("Detached tk2d " + i).Add<MeshRenderer>();
    mesh.gameObject.Add<tk2dBaseSprite>().Definition = new tk2dSpriteDefinition { name = "black_solid" }; Solids.Add(mesh);
    var white = Sprite("Focus Effect", h.transform, "Focus lines"); Whites.Add(white);
    var pet = Sprite("Pet", h.transform, "Grimmchild"); Whites.Add(pet);
    h.spellControl.FsmStates = new[] {
     new FsmState { Name = "Focus Start", Actions = new FsmStateAction[] { new Targets { Direct = solid.gameObject, Component = mesh,
      Wrapped = new FsmGameObject { Value = white.gameObject }, Object = new FsmObject { Value = pet },
      Owner = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.SpecifyGameObject, GameObject = new FsmGameObject { Value = solid.gameObject } },
      Own = new FsmOwnerDefault { OwnerOption = OwnerDefaultOption.UseOwner, GameObject = new FsmGameObject { Value = white.gameObject } } } } },
     new FsmState { Name = "Focus Start D", Actions = new FsmStateAction[] { new Targets { Direct = Sprite("Story Fade", h.transform).gameObject } } }
    };
    h.spellControl.ActiveStateName = "Inactive";
   }
   gm.heroLight = Session.Primary.Hero.heroLight;
  }
  internal void Focus(int i, bool on = true) { var h = Session.Players[i].Hero; h.cState.focusing = on; h.spellControl.ActiveStateName = on ? "Focus" : "Inactive"; }
  internal void VerifyWhite() { foreach (var r in Whites) Check(r.enabled, "native white FX, glow or pet disabled"); }
 }
 static void Masks() {
  for (int n = 1; n <= 8; n++) for (int who = 0; who < n; who++) {
   var f = new Fixture(n); f.Focus(who);
   foreach (var camera in new[] { f.Main, f.Other }) {
    FocusBackdropGuard.BeforeCamera(camera);
    foreach (var r in f.Masks) Check(!r.enabled, "vignette mesh or sprite escaped camera guard");
    for (int i = 0; i < n; i++) for (int j = 0; j < 2; j++) Check(f.Solids[2*i+j].enabled == (i != who), "Focus applied to wrong player");
    f.VerifyWhite(); FocusBackdropGuard.AfterCamera(camera);
    foreach (var r in f.Masks.Concat(f.Solids)) Check(r.enabled, "original renderer state not restored");
   }
  }
  {
   var f = new Fixture(8); for (int i = 0; i < 8; i++) f.Focus(i);
   FocusBackdropGuard.BeforeCamera(f.Main); foreach (var r in f.Solids) Check(!r.enabled, "simultaneous Focus mask escaped");
   f.VerifyWhite(); FocusBackdropGuard.AfterCamera(f.Main);
   f.Masks[0].enabled = false; f.Solids[0].enabled = false;
   FocusBackdropGuard.BeforeCamera(f.Main); FocusBackdropGuard.AfterCamera(f.Main);
   Check(!f.Masks[0].enabled && !f.Solids[0].enabled, "guard enables initially disabled plate");
   f.Masks[1].gameObject.activeInHierarchy = false; f.Masks[2].gameObject.layer = 5;
   var camera = Camera("Excluded", 1);
   FocusBackdropGuard.BeforeCamera(camera);
   Check(f.Masks[1].enabled && f.Masks[2].enabled, "inactive or camera-excluded renderer changed");
   FocusBackdropGuard.AfterCamera(camera);
  }
 }
 static void Lifetimes() {
  var f = new Fixture(2); f.Focus(1);
  FocusBackdropGuard.BeforeCamera(f.Main); FocusBackdropGuard.BeforeCamera(f.Other);
  FocusBackdropGuard.AfterCamera(f.Main); Check(!f.Masks[0].enabled, "out-of-order post exposes outer pass");
  FocusBackdropGuard.AfterCamera(f.Other); Check(f.Masks[0].enabled, "nested pass native restoration failed");
  FocusBackdropGuard.BeforeCamera(f.Main); FocusBackdropGuard.BeforeCamera(f.Other);
  FocusBackdropGuard.AfterCamera(f.Other); Check(!f.Masks[0].enabled, "inner pass restored original too soon");
  FocusBackdropGuard.AfterCamera(f.Main); Check(f.Masks[0].enabled, "outer pass did not restore original");
  FocusBackdropGuard.BeforeCamera(f.Main); FocusBackdropGuard.BeforeCamera(f.Main);
  FocusBackdropGuard.AfterCamera(f.Main); Check(!f.Masks[0].enabled, "same-camera nesting restored too early");
  FocusBackdropGuard.AfterCamera(f.Main); Check(f.Masks[0].enabled, "same-camera nesting leaked");
  FocusBackdropGuard.BeforeCamera(f.Main); Plugin.Self.Enabled.Value = false;
  FocusBackdropGuard.BeforeCamera(f.Other); Check(f.Masks.All(r => r.enabled), "same-frame disable leaks an interrupted render pass");
  Plugin.Self.Enabled.Value = true;
  FocusBackdropGuard.BeforeCamera(f.Main); Time.frameCount++;
  f.Session.Active = false; FocusBackdropGuard.BeforeCamera(f.Other);
  Check(f.Masks.All(r => r.enabled), "missing callback or ended session leaves hidden plates");
  f.Session.Active = true; FocusBackdropGuard.BeforeCamera(f.Main); FocusBackdropGuard.Reset();
  Check(f.Masks.All(r => r.enabled), "scene reset leaks renderer suppression");
  f.Solids[0].destroyed = true; f.Focus(0); FocusBackdropGuard.BeforeCamera(f.Main); FocusBackdropGuard.AfterCamera(f.Main);
  Check(!Diagnostics.Messages.Any(m => m.Contains("Exception")), "destroyed renderer throws");
  f.Focus(0,false); Time.unscaledTime+=1;
  var targets=(Targets)f.Session.Primary.Hero.spellControl.FsmStates[0].Actions[0];
  targets.Component.destroyed=true; targets.Object.Value.destroyed=true;
  f.Focus(0); FocusBackdropGuard.BeforeCamera(f.Main); FocusBackdropGuard.AfterCamera(f.Main);
  Check(!Diagnostics.Messages.Any(m => m.Contains("Exception")), "destroyed direct or FsmObject Component.gameObject throws as in submitted log");
  f.Focus(0, false); f.Focus(1, false); Time.unscaledTime += 1;
  FocusBackdropGuard.BeforeCamera(f.Main); Check(f.Solids[1].enabled && f.Solids[2].enabled, "Focus plate persists after healing ends"); FocusBackdropGuard.AfterCamera(f.Main);
 }
 static void StoryAndOwnership() {
  var f = new Fixture(2); f.Focus(1);
  foreach (var exclude in new[] { "ending", "absorption", "Focus Start D", "down", "loading", "wipe", "disabled", "menu" }) {
   CoopEnding.Active = exclude == "ending"; EndingAbsorption.Active = exclude == "absorption";
   f.Session.Players[1].Hero.spellControl.ActiveStateName = exclude == "Focus Start D" ? exclude : "Focus";
   f.Session.Players[1].Alive = exclude != "down";
   GameManager.instance.IsLoadingSceneTransition = exclude == "loading"; f.Session.TeamWipe = exclude == "wipe";
   Plugin.Self.Enabled.Value = exclude != "disabled"; GameManager.instance.Gameplay = exclude != "menu";
   FocusBackdropGuard.Reset(); FocusBackdropGuard.BeforeCamera(f.Main);
   Check(f.Solids.All(r => r.enabled), "story/death/loading plate suppressed during " + exclude);
   FocusBackdropGuard.AfterCamera(f.Main);
  }
  var replacement = new Fixture(1); replacement.Focus(0);
  FocusBackdropGuard.BeforeCamera(replacement.Main); FocusBackdropGuard.AfterCamera(replacement.Main);
  replacement.Session.Primary.Hero.spellControl.ActiveStateName = "Focus Start D";
  FocusBackdropGuard.BeforeCamera(replacement.Main);
  Check(replacement.Solids.All(r => r.enabled), "ordinary-heal grace period interferes with story Focus");
  FocusBackdropGuard.AfterCamera(replacement.Main);
  replacement.Session.Primary.Hero.spellControl.ActiveStateName = "Focus"; replacement.Session.Primary.Alive = false;
  FocusBackdropGuard.BeforeCamera(replacement.Main);
  Check(replacement.Solids.All(r => r.enabled), "ordinary-heal grace period interferes with dead player");
  FocusBackdropGuard.AfterCamera(replacement.Main); replacement.Session.Primary.Alive = true;
  var fresh = Sprite("New mask", null); replacement.Session.Primary.Hero.spellControl = new GameObject("Replaced spell").Add<PlayMakerFSM>();
  replacement.Session.Primary.Hero.spellControl.ActiveStateName = "Focus";
  replacement.Session.Primary.Hero.spellControl.FsmStates = new[] { new FsmState { Name = "Focus", Actions = new FsmStateAction[] { new Targets { Wrapped = new FsmGameObject { Value = fresh.gameObject } } } } };
  FocusBackdropGuard.BeforeCamera(replacement.Main); Check(!fresh.enabled, "replacement spell not rediscovered"); FocusBackdropGuard.AfterCamera(replacement.Main);
 }
 static void RealIntegration() {
  PlayerLighting.Install();
  try {
   var f = new Fixture(2); f.Focus(1);
   for (int i = 0; i < 3; i++) foreach (var c in new[] { f.Other, f.Main, f.Other }) {
    Time.frameCount++; UnityEngine.Camera.onPreCull(c);
    Check(f.Masks.All(r => !r.enabled), "actual PlayerLighting callback missed camera");
    f.VerifyWhite(); UnityEngine.Camera.onPostRender(c);
    Check(f.Masks.All(r => r.enabled), "actual PlayerLighting restore order leaked mask");
   }
   int scans = GameObject.HierarchyReads, reflections = Hooks.ReflectionReads;
   for (int i = 0; i < 1000; i++) { Time.frameCount++; UnityEngine.Camera.onPreCull(f.Main); UnityEngine.Camera.onPostRender(f.Main); }
   Check(GameObject.HierarchyReads == scans && Hooks.ReflectionReads == reflections, "steady healing rescans actor hierarchy or reflection");
   GroupVignette.Throw = true; UnityEngine.Camera.onPreCull(f.Main); UnityEngine.Camera.onPostRender(f.Main);
   Check(f.Masks.All(r => r.enabled), "render exception leaks mask");
   GroupVignette.Throw = false;
  } finally { PlayerLighting.Uninstall(); }
 }
 static bool Near(float a,float b)=>Math.Abs(a-b)<.00001f;
 static bool Same(Matrix4x4 a,Matrix4x4 b)=>a.Equals(b);
 static Matrix4x4 Lens(bool ortho,float far=50)=>NativeBackground.Depth(new Matrix4x4{
  m00=1.3f,m01=.05f,m02=.12f,m03=.25f,m10=.07f,m11=1.8f,m12=.04f,m13=.2f,m32=ortho?0:-1,m33=ortho?1:0
 },ortho,.3f,far);
 static LightBlurredBackground BackgroundFixture(Fixture f) {
  var blur=GameCameras.instance.gameObject.Add<LightBlurredBackground>();
  blur.Initialize(f.Main,f.Other,500);f.Main.farClipPlane=50;f.Other.farClipPlane=500;
  var plane=new GameObject("Native blur plane").Add<BlurPlane>();plane.gameObject.Add<MeshRenderer>();BlurPlane.All.Add(plane);
  return blur;
 }
 static bool Feedback(Camera camera)=>BlurPlane.All.Any(p=>p&&p.GetComponent<MeshRenderer>() is MeshRenderer r&&r.enabled&&
  (camera.cullingMask&(1<<r.gameObject.layer))!=0&&r.sharedMaterial!=null&&ReferenceEquals(r.sharedMaterial.mainTexture,camera.targetTexture));
 static void CaptureIsolation() {
  // A native background is not just an enabled blur mesh. Its source must be
  // clean: foreground Focus sprites/particles and RT-consuming planes cannot
  // be submitted to that capture. The main pass still needs all native FX.
  for(int count=1;count<=8;count++)for(int who=0;who<count;who++)foreach(bool reduced in new[]{false,true}) {
   var f=new Fixture(count);var blur=BackgroundFixture(f);Plugin.Self.PerformanceMode.Value=reduced;
   NativeRenderBudget.BeforeCamera(f.Other);
   var primary=BlurPlane.All[0].GetComponent<MeshRenderer>();
   primary.sharedMaterial=new Material{mainTexture=f.Other.targetTexture};
   var farPlane=new GameObject("More distant native compositor").Add<BlurPlane>();
   farPlane.gameObject.Add<MeshRenderer>().sharedMaterial=primary.sharedMaterial;farPlane.transform.position=new Vector3{z=50};BlurPlane.All.Add(farPlane);
   // Discover the hero at idle, then instantiate an opaque Focus sprite whose
   // atlas name is neither black_solid nor vignette. It is legitimate in main.
   FocusBackdropGuard.BeforeCamera(f.Other);FocusBackdropGuard.AfterCamera(f.Other);
   var focus=Sprite("Native deferred Focus effect",f.Session.Players[who].Hero.transform,"Focus Background FX");
   var dust=Child("Native Focus particles",f.Session.Players[who].Hero.transform).Add<ParticleSystemRenderer>();
   f.Focus(who);var texture=blur.Texture;int resized=blur.ResizeCount;
   FocusBackdropGuard.BeforeCamera(f.Other);
   Check(!focus.enabled&&!dust.enabled,"Focus foreground draw contaminates the native blurred capture");
   Check(!Feedback(f.Other),"native background samples its own render texture and can turn black");
   // Model deferred tk2d/native callbacks restoring renderers after pre-cull.
   focus.enabled=dust.enabled=primary.enabled=true;f.Masks[0].enabled=true;
   var latePlane=new GameObject("Compositor activated during pre-cull").Add<BlurPlane>();
   var lateRenderer=latePlane.gameObject.Add<MeshRenderer>();lateRenderer.sharedMaterial=primary.sharedMaterial;BlurPlane.All.Add(latePlane);
   FocusBackdropGuard.BeforeRender(f.Other);
   Check(!focus.enabled&&!dust.enabled&&!f.Masks[0].enabled,"pre-render flush reintroduces Focus black overlay");
   Check(!Feedback(f.Other)&&!lateRenderer.enabled,"late compositor enters its own background capture");
   Check(ReferenceEquals(texture,blur.Texture)&&resized==blur.ResizeCount,"healing capture allocates or replaces its native texture");
   FocusBackdropGuard.AfterCamera(f.Other);
   Check(focus.enabled&&dust.enabled&&primary.enabled&&lateRenderer.enabled,"background suppression leaks into scene rendering");
   f.VerifyWhite();
   FocusBackdropGuard.BeforeCamera(f.Main);
   Check(focus.enabled&&dust.enabled&&primary.enabled,"restoring native blur removes real main-camera healing FX");
   f.Masks[0].enabled=true;f.Solids[2*who].enabled=true;
   FocusBackdropGuard.BeforeRender(f.Main);
   Check(!f.Masks[0].enabled&&!f.Solids[2*who].enabled,"deferred sprite flush escapes main-camera Focus protection");
   f.VerifyWhite();FocusBackdropGuard.AfterCamera(f.Main);
   Check(f.Masks.All(r=>r.enabled)&&f.Solids.All(r=>r.enabled),"render transaction does not restore native mask state");
   int reads=GameObject.HierarchyReads;
   for(int i=0;i<120;i++){
    Time.frameCount++;FocusBackdropGuard.BeforeCamera(f.Other);FocusBackdropGuard.BeforeRender(f.Other);FocusBackdropGuard.AfterCamera(f.Other);
    FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeRender(f.Main);FocusBackdropGuard.AfterCamera(f.Main);
   }
   Check(reads==GameObject.HierarchyReads,"background isolation rescans all hero renderers every frame");
  }
  {
   var f=new Fixture(2);BackgroundFixture(f);f.Focus(0);
   var disabled=f.Whites[0];disabled.enabled=false;
   var excluded=f.Whites[1];excluded.gameObject.layer=9;f.Other.cullingMask&=~(1<<9);
   FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeCamera(f.Other);
   Check(!disabled.enabled&&excluded.enabled,"capture alters originally disabled or excluded-layer foreground");
   FocusBackdropGuard.AfterCamera(f.Other);
   Check(!disabled.enabled&&excluded.enabled&&!f.Masks[0].enabled,"nested capture exposes outer Focus mask or loses native FX state");
   FocusBackdropGuard.AfterCamera(f.Main);Check(!disabled.enabled&&f.Masks[0].enabled,"outer capture restore enables an originally disabled effect");
   FocusBackdropGuard.BeforeCamera(f.Other);Time.frameCount++;Plugin.Self.Enabled.Value=false;
   FocusBackdropGuard.BeforeRender(f.Main);
   Check(!disabled.enabled&&f.Whites.Skip(1).All(r=>r.enabled),"interrupted capture leaves native scene effects hidden");
  }
  foreach(float brokenFar in new[]{500f,1000f,float.NaN,float.PositiveInfinity,-1f}){
   var f=new Fixture(2);BackgroundFixture(f);f.Focus(0);var plane=BlurPlane.All[0];plane.transform.position=new Vector3{z=12};
   f.Main.worldToCameraMatrix=new Matrix4x4{m00=1,m11=1,m22=-1,m23=-38,m33=1};
   f.Main.farClipPlane=brokenFar;var old=f.Other.projectionMatrix;
   FocusBackdropGuard.BeforeCamera(f.Other);FocusBackdropGuard.BeforeRender(f.Other);
   Check(Near(f.Other.nearClipPlane,50.001f)&&f.Other.farClipPlane==500,"temporary scene far value empties native blurred capture");
   Check(f.Main.farClipPlane.Equals(brokenFar),"background range recovery changes the scene camera");
   FocusBackdropGuard.AfterCamera(f.Other);Check(f.Other.nearClipPlane==.3f&&Same(f.Other.projectionMatrix,old),"recovered background range leaks after rendering");
  }
  {
   // Exercise the actual subscriber ordering, including PlayerLighting.Probe
   // and its initial reset, rather than only calling the guard directly.
   var f=new Fixture(4);BackgroundFixture(f);f.Focus(2);PlayerLighting.Install();
   try {
    for(int frame=0;frame<4;frame++) {
     Time.frameCount++;UnityEngine.Camera.onPreCull(f.Other);
     Check(f.Whites.All(r=>!r.enabled),"actual background callback submits player/Focus foreground");
     f.Masks[0].enabled=true;UnityEngine.Camera.onPreRender(f.Other);
     Check(!f.Masks[0].enabled,"actual pre-render callback does not restore background protection");
     UnityEngine.Camera.onPostRender(f.Other);f.VerifyWhite();
     UnityEngine.Camera.onPreCull(f.Main);f.VerifyWhite();
     f.Solids[4].enabled=true;UnityEngine.Camera.onPreRender(f.Main);
     Check(!f.Solids[4].enabled&&BlurPlane.All[0].GetComponent<MeshRenderer>().enabled,"actual main pass loses native blur or exposes Focus plate");
     UnityEngine.Camera.onPostRender(f.Main);f.VerifyWhite();
    }
   } finally {PlayerLighting.Uninstall();}
  }
 }
 static void Background() {
  for(int count=1;count<=8;count++)for(int who=0;who<count;who++)foreach(bool ortho in new[]{false,true}){
   var f=new Fixture(count);var blur=BackgroundFixture(f);f.Main.orthographic=ortho;f.Main.projectionMatrix=Lens(ortho);
   f.Main.transform.position=new Vector3{x=90,y=45,z=-38};f.Main.fieldOfView=42;f.Main.orthographicSize=12;
   f.Main.worldToCameraMatrix=new Matrix4x4{m00=1,m11=1,m22=-1,m03=-90,m13=-45,m23=-38,m33=1};
   var bgPosition=f.Other.transform.position;var bgLens=f.Other.projectionMatrix;
   var plane=BlurPlane.All[0].GetComponent<MeshRenderer>();var old=f.Main.projectionMatrix;f.Focus(who);
   FocusBackdropGuard.BeforeCamera(f.Other);
   Check(Same(f.Other.worldToCameraMatrix,f.Main.worldToCameraMatrix)&&f.Other.transform.position.Equals(bgPosition),"background view misses main or mutates the native camera pose");
   Check(f.Other.fieldOfView==42&&f.Other.orthographicSize==12&&f.Other.orthographic==ortho,"background lens not synchronized");
   Check(f.Other.nearClipPlane==50&&f.Other.farClipPlane==500,"background depth range copies main clipping");
   Check(Near(f.Other.projectionMatrix.m00,old.m00)&&Near(f.Other.projectionMatrix.m13,old.m13),"background loses tk2d framing/offsets");
   Check(Near(f.Other.projectionMatrix.m22,NativeBackground.Depth(old,ortho,50,500).m22),"background depth row clips distant artwork");
   FocusBackdropGuard.AfterCamera(f.Other);
   Check(f.Other.fieldOfView==60&&f.Other.orthographicSize==7&&Same(f.Other.projectionMatrix,bgLens)&&f.Other.ViewResets>0,"background sync leaks native lens/view changes after the pass");
   FocusBackdropGuard.BeforeCamera(f.Main);
   Check(f.Main.farClipPlane==50&&plane.enabled,"Focus replaces native blurred artwork with direct sharp artwork");
   Check(Same(f.Main.projectionMatrix,old),"Focus widens the main projection instead of keeping native background split");
   f.Main.projectionMatrix=old;FocusBackdropGuard.BeforeRender(f.Main);
   Check(Same(f.Main.projectionMatrix,old)&&plane.enabled,"pre-render tk2d handling suppresses native blur");
   f.VerifyWhite();FocusBackdropGuard.AfterCamera(f.Main);
   Check(f.Main.farClipPlane==50&&Same(f.Main.projectionMatrix,old)&&plane.enabled,"Focus fails to restore native rendering");
   var stranger=Camera("BlurCamera");stranger.projectionMatrix=Lens(false,70);var before=stranger.projectionMatrix;
   FocusBackdropGuard.BeforeCamera(stranger);FocusBackdropGuard.AfterCamera(stranger);
   Check(Same(stranger.projectionMatrix,before),"a foreign camera is changed because of its name");
  }
  // Solid-looking compositor meshes and direct/wrapped native Focus references
  // must remain visible. Only the player's genuine detached masks are hidden.
  for(int count=1;count<=8;count++)for(int who=0;who<count;who++){
   var f=new Fixture(count);var blur=BackgroundFixture(f);var plane=BlurPlane.All[0];
   var renderer=plane.GetComponent<MeshRenderer>();
   plane.gameObject.Add<tk2dBaseSprite>().Definition=new tk2dSpriteDefinition{name="black_solid"};
   var ordinary=f.Session.Players[who].Hero.spellControl.FsmStates[0];
   var own=(Targets)ordinary.Actions[0];
   ordinary.Actions=new FsmStateAction[]{own,new Targets{Direct=plane.gameObject,Component=renderer,
     Wrapped=new FsmGameObject{Value=plane.gameObject},Object=new FsmObject{Value=renderer},
     Owner=new FsmOwnerDefault{OwnerOption=OwnerDefaultOption.SpecifyGameObject,GameObject=new FsmGameObject{Value=plane.gameObject}}}};
   f.Main.farClipPlane=50;f.Main.projectionMatrix=Lens(false);
   var texture=blur.Texture;int allocations=blur.ResizeCount;var projection=f.Main.projectionMatrix;
   foreach(bool focus in new[]{false,true,true,false}){
    f.Focus(who,focus);if(!focus)Time.unscaledTime+=.3f;
    foreach(var camera in new[]{f.Other,f.Main}){
     FocusBackdropGuard.BeforeCamera(camera);FocusBackdropGuard.BeforeRender(camera);
     Check(renderer.enabled==(camera==f.Main),"native compositor must be visible only in the scene pass: "+camera.name);
     Check(f.Main.farClipPlane==50&&Same(projection,f.Main.projectionMatrix),"healing changes foreground/background clip split");
     Check(ReferenceEquals(texture,blur.Texture)&&allocations==blur.ResizeCount,"ordinary healing replaces or reallocates native background texture");
     if(focus)Check(!f.Solids[2*who].enabled&&!f.Solids[2*who+1].enabled,"restoring blur reintroduces real solid Focus masks");
     if(camera==f.Main)f.VerifyWhite();else Check(f.Whites.All(r=>!r.enabled),"foreground effect contaminates the distant background capture");FocusBackdropGuard.AfterCamera(camera);
     Check(renderer.enabled,"blur visibility leaks after healing render");
    }
   }
   renderer.enabled=false;f.Focus(who);FocusBackdropGuard.BeforeCamera(f.Main);
   Check(!renderer.enabled,"guard forces an originally disabled compositor visible");
   FocusBackdropGuard.AfterCamera(f.Main);Check(!renderer.enabled,"disabled native compositor changes after post-render");
  }
  {
   // A cached ordinary renderer can become a native compositor later. Guard
   // the live component too, rather than relying only on initial discovery.
   var f=new Fixture(2);BackgroundFixture(f);f.Focus(1);
   FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.AfterCamera(f.Main);
   var cached=f.Solids[2];cached.gameObject.Add<BlurPlane>();
   FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeRender(f.Main);
   Check(cached.enabled&&!f.Solids[3].enabled,"cached renderer cannot distinguish added BlurPlane from remaining Focus mask");
   FocusBackdropGuard.AfterCamera(f.Main);
  }
  {
   var f=new Fixture(2);BackgroundFixture(f);f.Focus(1);var old=Lens(false);f.Main.projectionMatrix=old;
   var customView=new Matrix4x4{m00=1,m11=1,m22=-1,m03=28,m13=17,m33=1};
   f.Other.worldToCameraMatrix=customView;f.Main.worldToCameraMatrix=new Matrix4x4{m00=1,m11=1,m22=-1,m03=-32,m33=1};
   FocusBackdropGuard.BeforeCamera(f.Other);FocusBackdropGuard.BeforeCamera(f.Other);
   FocusBackdropGuard.BeforeRender(f.Other);FocusBackdropGuard.AfterCamera(f.Other);
   Check(Same(f.Other.worldToCameraMatrix,f.Main.worldToCameraMatrix),"nested background restoration erases outer view");
   FocusBackdropGuard.AfterCamera(f.Other);Check(Same(f.Other.worldToCameraMatrix,customView),"background restoration erases another mod's custom view");
   FocusBackdropGuard.BeforeCamera(f.Main);Time.frameCount++;f.Focus(1,false);Time.unscaledTime+=1;FocusBackdropGuard.BeforeCamera(f.Main);
   Check(f.Main.farClipPlane==50&&Same(f.Main.projectionMatrix,old),"missing post-render alters native depth split next frame");FocusBackdropGuard.AfterCamera(f.Main);
   foreach(var exclude in new[]{"ending","absorption","Focus Start D","down","loading","wipe","disabled","menu"}){
    FocusBackdropGuard.Reset();f.Focus(1);CoopEnding.Active=exclude=="ending";EndingAbsorption.Active=exclude=="absorption";
    if(exclude=="Focus Start D")f.Session.Players[1].Hero.spellControl.ActiveStateName=exclude;
    f.Session.Players[1].Alive=exclude!="down";GameManager.instance.IsLoadingSceneTransition=exclude=="loading";
    f.Session.TeamWipe=exclude=="wipe";Plugin.Self.Enabled.Value=exclude!="disabled";GameManager.instance.Gameplay=exclude!="menu";
    FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeRender(f.Main);
    Check(f.Main.farClipPlane==50&&BlurPlane.All[0].GetComponent<MeshRenderer>().enabled,"Focus background bypass interferes with "+exclude);
    FocusBackdropGuard.AfterCamera(f.Main);
   }
  }
 }
 static void BlurredComposition() {
  // Use a completed native blur texture with intermediate pixel values: a
  // direct sharp scene redraw or a black native compositor cannot satisfy it.
  float[] expected={.25f,.5f,.5f,.5f,.5f,.25f};
  for(int count=1;count<=8;count++)for(int who=0;who<count;who++)foreach(bool ortho in new[]{false,true})foreach(bool budget in new[]{false,true}) {
   var f=new Fixture(count);var blur=BackgroundFixture(f);f.Other.depth=f.Main.depth-5;
   f.Main.orthographic=ortho;f.Main.projectionMatrix=Lens(ortho);Plugin.Self.PerformanceMode.Value=budget;
   NativeRenderBudget.BeforeCamera(f.Other);blur.Texture.Pixels=(float[])expected.Clone();
   var plane=BlurPlane.All[0].GetComponent<MeshRenderer>();var texture=blur.Texture;var projection=f.Main.projectionMatrix;
   var external=new UnityEngine.Rendering.CommandBuffer{name="other mod"};
   f.Main.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque,external);
   // Ordinary gameplay retains the native compositor and external buffers.
   FocusBackdropGuard.BeforeCamera(f.Other);FocusBackdropGuard.BeforeRender(f.Other);FocusBackdropGuard.AfterCamera(f.Other);
   FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeRender(f.Main);
   Check(plane.enabled&&f.Main.CommandCount==1,"non-healing gameplay changes the native backdrop");FocusBackdropGuard.AfterCamera(f.Main);
   f.Focus(who);FocusBackdropGuard.BeforeCamera(f.Other);FocusBackdropGuard.BeforeRender(f.Other);FocusBackdropGuard.AfterCamera(f.Other);
   FocusBackdropGuard.BeforeCamera(f.Main);
   Check(!plane.enabled,"black native compositor is still submitted during healing");
   Check(f.Main.CommandCount==2,"blurred backdrop was not scheduled before the foreground");
   plane.enabled=true;FocusBackdropGuard.BeforeRender(f.Main);
   Check(!plane.enabled,"late reactivation of the native compositor covers the blurred copy");
   Check(f.Main.ExecuteBackdrop()?.SequenceEqual(expected)==true,"background pixels are black or sharp instead of the completed native blur");
   var copy=f.Main.Commands.Values.SelectMany(v=>v).Single(c=>c!=external);
   Check(ReferenceEquals(copy.Source,texture)&&copy.Viewport.Equals(f.Main.pixelRect),"composition uses the wrong capture or changes the foreground viewport");
   Check(f.Main.farClipPlane==50&&Same(projection,f.Main.projectionMatrix),"blurred repair widens the scene depth or changes its lens");
   f.VerifyWhite();FocusBackdropGuard.AfterCamera(f.Main);
   Check(plane.enabled&&f.Main.CommandCount==1&&f.Main.Commands.Values.SelectMany(v=>v).Contains(external),"healing cleanup damages other mods' buffers or the native compositor");
   int allocations=UnityEngine.Rendering.CommandBuffer.Allocations,reads=GameObject.HierarchyReads,resizes=blur.ResizeCount;
   for(int frame=0;frame<20;frame++){
    Time.frameCount++;FocusBackdropGuard.BeforeCamera(f.Other);FocusBackdropGuard.BeforeRender(f.Other);FocusBackdropGuard.AfterCamera(f.Other);
    FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeRender(f.Main);Check(f.Main.ExecuteBackdrop()?.SequenceEqual(expected)==true,"steady healing loses native blur");FocusBackdropGuard.AfterCamera(f.Main);
   }
   Check(allocations==UnityEngine.Rendering.CommandBuffer.Allocations&&reads==GameObject.HierarchyReads&&resizes==blur.ResizeCount,
    "healing recreates GPU buffers/textures or scans the hierarchy every frame");
   f.Focus(who,false);Time.unscaledTime+=.3f;FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeRender(f.Main);
   Check(plane.enabled&&f.Main.CommandCount==1,"healing command persists after Focus ends");FocusBackdropGuard.AfterCamera(f.Main);
   FocusBackdropGuard.Reset();Check(copy.Released&&f.Main.CommandCount==1,"scene/reset leaks owned GPU commands or removes another mod's commands");
  }
  foreach(string invalid in new[]{"uncreated","missing","same target","disabled camera","inactive camera","wrong order","disabled controller","no planes"}) {
   var f=new Fixture(2);var blur=BackgroundFixture(f);f.Other.depth=-5;f.Focus(1);var plane=BlurPlane.All[0].GetComponent<MeshRenderer>();
   if(invalid=="uncreated")blur.Texture.Created=false;
   if(invalid=="missing")f.Other.targetTexture=null;
   if(invalid=="same target")f.Main.targetTexture=blur.Texture;
   if(invalid=="disabled camera")f.Other.enabled=false;
   if(invalid=="inactive camera")f.Other.gameObject.activeInHierarchy=false;
   if(invalid=="wrong order")f.Other.depth=2;
   if(invalid=="disabled controller")blur.enabled=false;
   if(invalid=="no planes")BlurPlane.All.Clear();
   FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeRender(f.Main);
   Check(f.Main.CommandCount==0&&plane.enabled,"invalid native texture/order still replaces the backdrop: "+invalid);FocusBackdropGuard.AfterCamera(f.Main);
  }
  {
   var f=new Fixture(3);var blur=BackgroundFixture(f);f.Other.depth=-5;f.Focus(1);blur.Texture.Pixels=(float[])expected.Clone();
   FocusBackdropGuard.BeforeCamera(f.Main);var old=blur.Texture;
   // Graphics-quality/aspect changes may replace the texture between callbacks.
   blur.RenderTextureHeight=720;typeof(LightBlurredBackground).GetMethod("OnCameraAspectChanged",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(blur,new object[]{1.77778f});
   blur.Texture.Pixels=expected.Select(x=>x*.8f).ToArray();FocusBackdropGuard.BeforeRender(f.Main);
   Check(f.Main.ExecuteBackdrop()?.SequenceEqual(blur.Texture.Pixels)==true,"late texture replacement samples an obsolete/released native capture");
   Check(f.Main.CommandCount==1&&!ReferenceEquals(old,blur.Texture),"texture replacement duplicates the composition buffer");
   FocusBackdropGuard.AfterCamera(f.Main);
   // Initially disabled compositors keep their original visibility.
   var plane=BlurPlane.All[0].GetComponent<MeshRenderer>();plane.enabled=false;
   FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.AfterCamera(f.Main);Check(!plane.enabled,"repair forces an originally disabled native plane on");
   plane.enabled=true;FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeCamera(f.Main);
   Check(f.Main.CommandCount==2,"nested main render lost its outer composition");FocusBackdropGuard.AfterCamera(f.Main);
   Check(f.Main.CommandCount==1&&!plane.enabled,"nested cleanup removes the outer blur or exposes a black plane");FocusBackdropGuard.AfterCamera(f.Main);
   Check(f.Main.CommandCount==0&&plane.enabled,"nested composition fails to restore exact native state");
   foreach(var mode in new[]{"missing post","disabled","loading","wipe","menu"}) {
    Plugin.Self.Enabled.Value=true;GameManager.instance.IsLoadingSceneTransition=false;f.Session.TeamWipe=false;GameManager.instance.Gameplay=true;
    FocusBackdropGuard.BeforeCamera(f.Main);Check(f.Main.CommandCount==1,"test setup did not attach the healing copy");
    if(mode=="missing post")Time.frameCount++;
    if(mode=="disabled")Plugin.Self.Enabled.Value=false;
    if(mode=="loading")GameManager.instance.IsLoadingSceneTransition=true;
    if(mode=="wipe")f.Session.TeamWipe=true;
    if(mode=="menu")GameManager.instance.Gameplay=false;
    FocusBackdropGuard.BeforeCamera(f.Other);FocusBackdropGuard.AfterCamera(f.Other);
    Check(f.Main.CommandCount==0&&plane.enabled,"interrupted rendering leaves the healing override attached: "+mode);
   }
  }
  {
   // Exercise actual subscribers and callback ordering, including Probe/reset.
   var f=new Fixture(4);var blur=BackgroundFixture(f);f.Other.depth=-5;f.Focus(2);PlayerLighting.Install();
   try{
    for(int frame=0;frame<5;frame++) {
     Time.frameCount++;UnityEngine.Camera.onPreCull(f.Other);UnityEngine.Camera.onPreRender(f.Other);UnityEngine.Camera.onPostRender(f.Other);
     UnityEngine.Camera.onPreCull(f.Main);UnityEngine.Camera.onPreRender(f.Main);
     Check(f.Main.CommandCount==1&&!BlurPlane.All[0].GetComponent<MeshRenderer>().enabled,"real callback order misses the blurred bypass");f.VerifyWhite();
     UnityEngine.Camera.onPostRender(f.Main);Check(f.Main.CommandCount==0&&BlurPlane.All[0].GetComponent<MeshRenderer>().enabled,"real callback order leaks the bypass");
    }
   }finally{PlayerLighting.Uninstall();}
  }
  foreach(string exclude in new[]{"ending","absorption","Focus Start D","down","loading","wipe","disabled","menu"}) {
   var f=new Fixture(2);BackgroundFixture(f);f.Other.depth=-5;f.Focus(1);
   CoopEnding.Active=exclude=="ending";EndingAbsorption.Active=exclude=="absorption";
   if(exclude=="Focus Start D")f.Session.Players[1].Hero.spellControl.ActiveStateName=exclude;
   f.Session.Players[1].Alive=exclude!="down";GameManager.instance.IsLoadingSceneTransition=exclude=="loading";
   f.Session.TeamWipe=exclude=="wipe";Plugin.Self.Enabled.Value=exclude!="disabled";GameManager.instance.Gameplay=exclude!="menu";
   FocusBackdropGuard.BeforeCamera(f.Main);FocusBackdropGuard.BeforeRender(f.Main);
   Check(f.Main.CommandCount==0&&BlurPlane.All[0].GetComponent<MeshRenderer>().enabled,"healing copy interferes with story/death/loading: "+exclude);FocusBackdropGuard.AfterCamera(f.Main);
  }
  {
   var f=new Fixture(2);var blur=BackgroundFixture(f);f.Other.depth=-5;f.Focus(1);var plane=BlurPlane.All[0].GetComponent<MeshRenderer>();
   FocusBackdropGuard.BeforeCamera(f.Main);Check(!plane.enabled&&f.Main.CommandCount==1,"late invalidation setup failed");
   blur.Texture.Created=false;FocusBackdropGuard.BeforeRender(f.Main);
   Check(plane.enabled&&f.Main.CommandCount==0,"withdrawn copy leaves native background hidden");FocusBackdropGuard.AfterCamera(f.Main);
   blur.Texture.Created=true;f.Main.actualRenderingPath=RenderingPath.DeferredShading;
   FocusBackdropGuard.BeforeCamera(f.Main);Check(f.Main.Commands[UnityEngine.Rendering.CameraEvent.BeforeGBuffer].Count==1,"deferred camera runs the background copy at a forward-only event");
   f.Main.actualRenderingPath=RenderingPath.Forward;FocusBackdropGuard.BeforeRender(f.Main);
   Check(f.Main.CommandCount==1&&f.Main.Commands[UnityEngine.Rendering.CameraEvent.BeforeForwardOpaque].Count==1&&!plane.enabled,"render-path change duplicates buffers or exposes the compositor");FocusBackdropGuard.AfterCamera(f.Main);
  }
  Console.WriteLine("Healing composition: native blurred capture pixels preserved before sharp foreground; black compositor bypassed. Controlled command-stream checks, not Unity GPU screenshots.");
 }
 static void GraphicsBudget() {
  var f=new Fixture(4);var blur=BackgroundFixture(f);
  var bloom=f.Main.gameObject.Add<UnityStandardAssets.ImageEffects.BloomOptimized>();var noise=f.Main.gameObject.Add<UnityStandardAssets.ImageEffects.FastNoise>();
  NativeRenderBudget.BeforeCamera(f.Main);Check(bloom.blurIterations==3&&blur.RenderTextureHeight==540&&noise.enabled,"default mode changes native graphics");
  int initial=blur.ResizeCount;Plugin.Self.PerformanceMode.Value=true;NativeRenderBudget.BeforeCamera(f.Other);
  Check(bloom.blurIterations==1&&bloom.CurrentResolution==UnityStandardAssets.ImageEffects.BloomOptimized.Resolution.Low,"mode does not reduce bloom work");
  Check(!noise.enabled&&blur.PassGroupCount==2&&blur.RenderTextureHeight==540&&blur.Texture.height==540,"balanced mode changes native background sampling");
  Check(blur.ResizeCount==initial,"performance mode reallocates the native background texture");
  for(int i=0;i<1000;i++){Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);NativeRenderBudget.BeforeCamera(f.Other);}
  Check(blur.ResizeCount==initial,"steady mode recreates GPU textures every frame");
  Plugin.Self.PerformanceMode.Value=false;NativeRenderBudget.BeforeCamera(f.Main);
  Check(noise.enabled&&bloom.blurIterations==3&&bloom.CurrentResolution==UnityStandardAssets.ImageEffects.BloomOptimized.Resolution.High,"off does not restore native noise/bloom");
  Check(blur.PassGroupCount==2&&blur.RenderTextureHeight==540&&blur.Texture.height==540,"off does not restore native background texture");
  noise.enabled=false;bloom.blurIterations=0;bloom.SetResolution(UnityStandardAssets.ImageEffects.BloomOptimized.Resolution.Low);blur.PassGroupCount=1;
  Plugin.Self.PerformanceMode.Value=true;Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);NativeRenderBudget.Reset();
  Check(!noise.enabled&&bloom.blurIterations==0&&blur.PassGroupCount==1&&bloom.CurrentResolution==UnityStandardAssets.ImageEffects.BloomOptimized.Resolution.Low,"mode enables originally disabled effects");
  Plugin.Self.PerformanceMode.Value=true;NativeRenderBudget.BeforeCamera(f.Main);
  GameManager.instance.gameSettings.shaderQuality=ShaderQualities.Low;Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);NativeRenderBudget.Reset();
  Check(blur.PassGroupCount==1,"restoring budget overwrites a new native graphics quality");
  Plugin.Self.PerformanceMode.Value=true;NativeRenderBudget.BeforeCamera(f.Main);blur.RenderTextureHeight=720;bloom.blurIterations=4;
  Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);Check(blur.RenderTextureHeight==720&&bloom.blurIterations==1,"budget ignores updated external graphics values");
  GameManager.instance.IsLoadingSceneTransition=true;NativeRenderBudget.BeforeCamera(f.Main);
  Check(blur.RenderTextureHeight==720&&bloom.blurIterations==4,"scene transition retains temporary budget values");
  GameManager.instance.IsLoadingSceneTransition=false;NativeRenderBudget.BeforeCamera(f.Main);
  NativeRenderBudget.Reset();
  Plugin.Self.Performance=new PerformanceConfiguration {Initialized=true, Detail=2, Bloom=2};
  bloom.blurIterations=4;noise.enabled=true;blur.RenderTextureHeight=720;blur.PassGroupCount=2;
  Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);
  Check(!bloom.enabled&&!noise.enabled&&blur.RenderTextureHeight==720&&blur.PassGroupCount==2,"maximum profile changes native background sampling or leaves bloom active");
  int maximumResizes=blur.ResizeCount;
  for(int i=0;i<1000;i++){Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);NativeRenderBudget.BeforeCamera(f.Other);}
  Check(blur.ResizeCount==maximumResizes,"maximum reallocates background textures each frame");
  NativeRenderBudget.Reset();
  Check(bloom.enabled&&noise.enabled&&bloom.blurIterations==4&&blur.RenderTextureHeight==720,"maximum cannot restore original rendering");
  Plugin.Self.Performance.Detail=0;Plugin.Self.Performance.Bloom=1;
  Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);
  Check(bloom.enabled&&bloom.blurIterations==1&&noise.enabled&&blur.RenderTextureHeight==720&&blur.PassGroupCount==2,"custom bloom-only setting changes decoration/background");
  NativeRenderBudget.Reset();Plugin.Self.Performance.Detail=2;Plugin.Self.Performance.Bloom=0;
  Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);
  Check(bloom.enabled&&bloom.blurIterations==4&&!noise.enabled&&blur.RenderTextureHeight==720,"custom detail-only setting changes original bloom");
  NativeRenderBudget.Reset();bloom.enabled=false;Plugin.Self.Performance.Bloom=2;
  Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);NativeRenderBudget.Reset();
  Check(!bloom.enabled,"restoring maximum enables a previously disabled bloom");
  bloom.enabled=true;Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);bloom.enabled=true;NativeRenderBudget.Reset();
  Check(bloom.enabled,"restoring maximum overwrites an external bloom edit");
  Plugin.Self.Performance.Detail=2;blur.RenderTextureHeight=96;
  Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);Check(blur.RenderTextureHeight==96,"maximum enlarges an already smaller native texture");NativeRenderBudget.Reset();
  Plugin.Self.Performance=null;Time.frameCount++;NativeRenderBudget.BeforeCamera(f.Main);
  bloom.destroyed=true;noise.destroyed=true;blur.destroyed=true;NativeRenderBudget.Reset();
  Check(!Diagnostics.Messages.Any(m=>m.Contains("Exception")),"destroyed graphics component throws on restoration");
  Console.WriteLine("Native budget: background resolution and blur passes retained in every profile; bloom iterations 3 -> 1; no background render-texture reallocations. Not a Unity GPU/flicker test.");
 }
 static void HudBudget() {
  var f=new Fixture(4);var canvas=new GameObject("HUD canvas");GameCameras.instance.hudCanvas=canvas;
  var hp=Child("Health",canvas.transform);var soul=Child("Soul Orb",canvas.transform);
  var mask=Child("Mask",hp.transform).Add<MeshRenderer>();var orb=Child("Orb sprite",soul.transform).Add<MeshRenderer>();orb.forceRenderingOff=true;
  var book=Child("Inventory",canvas.transform);for(int i=0;i<1000;i++)Child("UI "+i,book.transform).Add<MeshRenderer>();
  var ui=Child("Journal",book.transform).Add<MeshRenderer>();VanillaHud.Install();
  try{
   int before=GameObject.RendererVisits;
   for(int tick=0;tick<120;tick++){Time.frameCount++;Time.unscaledTime+=1/60f;UnityEngine.Camera.onPreCull(f.Main);UnityEngine.Camera.onPreRender(f.Main);}
   int visits=GameObject.RendererVisits-before;
   Check(mask.forceRenderingOff&&orb.forceRenderingOff&&!ui.forceRenderingOff,"HUD budget hides journal or exposes vanilla vitals");
   Check(visits<12000,"HUD still scans every inventory renderer each frame");
   var damaged=Child("New damage mask",hp.transform).Add<MeshRenderer>();Time.frameCount++;UnityEngine.Camera.onPreCull(f.Main);
   Check(damaged.forceRenderingOff,"new damage renderer flashes before HUD cache refresh");
   var reserved=Child("Extra Soul",canvas.transform).Add<MeshRenderer>();Time.frameCount++;UnityEngine.Camera.onPreCull(f.Main);
   Check(reserved.forceRenderingOff,"new root child is not immediately discovered");
   Plugin.Self.Enabled.Value=false;UnityEngine.Camera.onPreCull(f.Main);
   Check(!mask.forceRenderingOff&&orb.forceRenderingOff&&!damaged.forceRenderingOff&&!reserved.forceRenderingOff,"HUD restore loses original visibility");
   Console.WriteLine("HUD fixture: 120-frame scans visit "+visits+" renderers versus over 120,000 for full-canvas scans; newly created damage masks hidden on the same frame.");
  }finally{VanillaHud.Uninstall();}
 }
 static void HudFeedback() {
  var f=new Fixture(2);var canvas=new GameObject("HUD canvas");GameCameras.instance.hudCanvas=canvas;
  var health=Child("Health",canvas.transform);var mask=Child("Mask",health.transform);
  var journal=Child("Journal",canvas.transform);var hero=Child("Knight",null);
  var burst=new GameObject("Detached heal burst");var particle=burst.Add<ParticleSystemRenderer>();
  var alreadyHidden=Child("Initially hidden",burst.transform).Add<MeshRenderer>();alreadyHidden.forceRenderingOff=true;
  VanillaHud.Install();
  try{
   VanillaHud.Spawned(burst,mask);
   Check(particle.forceRenderingOff&&alreadyHidden.forceRenderingOff,"detached native healing burst remains at original HUD");
   Time.frameCount++;UnityEngine.Camera.onPreCull(f.Main);
   Check(particle.forceRenderingOff,"first camera restores a newly hidden burst when discovering the canvas");
   VanillaHud.Spawned(burst,hero);
   Check(!particle.forceRenderingOff&&alreadyHidden.forceRenderingOff,"pooled effect reuse loses original renderer flags");
   Time.frameCount++;UnityEngine.Camera.onPreRender(f.Main);
   Check(!particle.forceRenderingOff,"old HUD registration hides a recycled world effect again");
   foreach(var source in new[]{hero,journal,canvas}){
    VanillaHud.Spawned(burst,source);Check(!particle.forceRenderingOff,"non-vital/world source is mistaken for HUD feedback");
   }
   VanillaHud.Spawned(burst,null);VanillaHud.Spawned(null,mask);
   Check(!particle.forceRenderingOff,"missing source suppresses a world effect");
   var cameraRoot=new GameObject("HudCamera");GameCameras.instance.hudCamera=cameraRoot.Add<Camera>();
   var cameraHealth=Child("Health",cameraRoot.transform);VanillaHud.Spawned(burst,cameraHealth);
   Check(particle.forceRenderingOff,"HUD-camera health outside canvas is ignored");
   VanillaHud.Spawned(burst,hero);
   for(int gate=0;gate<5;gate++){
    if(gate==0)Plugin.Self.Enabled.Value=false;
    if(gate==1)Plugin.Self.HideOriginalHud.Value=false;
    if(gate==2)Plugin.Self.Session.Active=false;
    if(gate==3)GameManager.instance.Gameplay=false;
    if(gate==4)GameManager.instance.IsLoadingSceneTransition=true;
    VanillaHud.Spawned(burst,mask);Check(!particle.forceRenderingOff,"native feedback is suppressed outside active replacement HUD");
    Plugin.Self.Enabled.Value=Plugin.Self.HideOriginalHud.Value=Plugin.Self.Session.Active=GameManager.instance.Gameplay=true;
    GameManager.instance.IsLoadingSceneTransition=false;
   }
   VanillaHud.Spawned(burst,mask);Plugin.Self.HideOriginalHud.Value=false;UnityEngine.Camera.onPreCull(f.Main);
   Check(!particle.forceRenderingOff&&alreadyHidden.forceRenderingOff,"show-original-HUD cannot restore detached feedback");
   Plugin.Self.HideOriginalHud.Value=true;VanillaHud.Spawned(burst,mask);particle.destroyed=true;
   VanillaHud.Spawned(burst,hero);VanillaHud.Restore();
   Check(alreadyHidden.forceRenderingOff,"destroyed pooled renderers corrupt visibility restoration");
   Console.WriteLine("HUD feedback: detached native bursts hidden before render, world focus/journal untouched, pool reuse and disable restore original flags.");
  }finally{VanillaHud.Uninstall();}
 }
 static void HudSpawnHooks(){
  var f=new Fixture(2);var canvas=new GameObject("HUD canvas");GameCameras.instance.hudCanvas=canvas;
  var mask=Child("Health",canvas.transform);var hero=new GameObject("Hero");var prior=new GameObject("Prior caller");
  var burst=new GameObject("Detached effect");var particle=burst.Add<ParticleSystemRenderer>();
  var hudAction=new HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool{Fsm=new(){GameObject=mask}};
  var worldAction=new HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool{Fsm=new(){GameObject=hero}};
  VanillaHud.SpawnSource=prior;
  try{
   SpawnHookHost.SpellPoolAction(a=>{
    Check(VanillaHud.SpawnSource==mask,"pool hook does not set native HUD source before activation");
    SpawnHookHost.SpellPoolAction(b=>Check(VanillaHud.SpawnSource==hero,"nested action inherits incorrect HUD ownership"),worldAction);
    Check(VanillaHud.SpawnSource==mask,"nested pool action does not restore outer HUD source");
    VanillaHud.Spawned(burst,VanillaHud.SpawnSource);
   },hudAction);
   Check(particle.forceRenderingOff&&VanillaHud.SpawnSource==prior,"pool action loses caller scope or fails to hide effect");
   bool threw=false;try{SpawnHookHost.SpellPoolAction(a=>throw new InvalidOperationException("fixture"),hudAction);}catch(InvalidOperationException){threw=true;}
   Check(threw&&VanillaHud.SpawnSource==prior,"pool exception leaks HUD ownership to unrelated spawns");
   VanillaHud.Spawned(burst,hero);
   var stored=new FsmGameObject();var create=new HutongGames.PlayMaker.Actions.CreateObject{Fsm=new(){GameObject=mask},storeObject=stored};
   SpawnHookHost.SpellCreateAction(a=>a.storeObject.Value=burst,create);
   Check(particle.forceRenderingOff&&ReferenceEquals(stored,create.storeObject)&&stored.Value==burst,"CreateObject observer changes native stored result or misses healing effect");
   VanillaHud.Spawned(burst,hero);create.Fsm.GameObject=hero;
   SpawnHookHost.SpellCreateAction(a=>a.storeObject.Value=burst,create);
   Check(!particle.forceRenderingOff,"world CreateObject is hidden by a previous HUD context");
  }finally{VanillaHud.SpawnSource=null;VanillaHud.Restore();}
 }
 sealed class Entry { internal int Order; internal Transform Root; internal int Owner; internal bool Busy = true; }
 static Entry[] Legacy(List<Entry> entries, Transform source) => entries.Where(e => e.Root && source.IsChildOf(e.Root)).ToArray();
 static void Hierarchy() {
  GameObject.All.Clear(); var rng = new Random(198);
  var index = new InteractionAncestors<Entry>(); var entries = new List<Entry>(); var nodes = new List<Transform>();
  for (int i = 0; i < 240; i++) nodes.Add(Child("Node" + i, i == 0 ? null : nodes[rng.Next(i)]).transform);
  for (int i = 0; i < 90; i++) { var e = new Entry { Order = i, Root = nodes[rng.Next(nodes.Count)] }; entries.Add(e); index.Register(e.Root, e); }
  for (int i = 0; i < 5000; i++) {
   int at = rng.Next(nodes.Count); var node = nodes[at];
   if (i % 11 == 0 && at > 0) node.parent = nodes[rng.Next(at)];
   if (i % 17 == 0) { var e = entries[rng.Next(entries.Count)]; e.Root = nodes[rng.Next(nodes.Count)]; index.Register(e.Root, e); }
   var expected = Legacy(entries, node); var actual = index.Matches(node);
   Check(expected.SequenceEqual(actual), "indexed routing differs from full native hierarchy scan");
   Check(ReferenceEquals(actual, index.Matches(node)), "unchanged hierarchy allocates results");
  }
  index.Clear(); var top = Child("Top", null).transform; var a = Child("A", top).transform; var b = Child("B", top).transform;
  var carrier = Child("Carrier", a).transform; var child = Child("Query", carrier).transform;
  var first = new Entry { Order = 0, Root = a, Owner = 1 }; var second = new Entry { Order = 1, Root = b, Owner = 2 };
  index.Register(a, first); index.Register(b, second); var old = index.Matches(child);
  carrier.parent = b; var current = index.Matches(child);
  Check(current.Single() == second && old.Single() == first, "grandparent reparent or in-flight snapshot corrupt");
  second.Owner = 7; second.Busy = false; Check(index.Matches(child)[0].Owner == 7 && !index.Matches(child)[0].Busy, "index freezes live player/input state");
  child.destroyed = true; Check(index.Matches(child).Length == 0, "destroyed source yields interaction");
  index.Clear(); Check(index.Matches(top).Length == 0 && index.Rebuilds == 1, "room reset retains old bindings");
 }
 static void Benchmark() {
  var index = new InteractionAncestors<Entry>(); var entries = new List<Entry>(); var queries = new List<Transform>();
  var room = new GameObject("Workshop fixture").transform;
  for (int i = 0; i < 80; i++) {
   var root = Child("Statue" + i, room).transform; var e = new Entry { Root = root, Order = i }; entries.Add(e); index.Register(root, e);
   queries.Add(Child("Statue FSM" + i, Child("Child", root).transform).transform);
  }
  for (int i = 0; i < 920; i++) queries.Add(Child("World FSM" + i, Child("Decoration", room).transform).transform);
  foreach (var q in queries) index.Matches(q);
  var watch = Stopwatch.StartNew(); int legacyHits = 0; long legacyTests = 0;
  for (int tick = 0; tick < 80; tick++) foreach (var q in queries) foreach (var e in entries) { legacyTests++; if (q.IsChildOf(e.Root)) legacyHits++; }
  watch.Stop(); double oldMs = watch.Elapsed.TotalMilliseconds;
  long rebuilds = index.Rebuilds, lookups = index.RootLookups, bytes = GC.GetAllocatedBytesForCurrentThread(); int hits = 0;
  watch.Restart(); for (int tick = 0; tick < 80; tick++) foreach (var q in queries) hits += index.Matches(q).Length; watch.Stop();
  long allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
  Check(hits == legacyHits, "benchmark lost positive routes");
  Check(index.Rebuilds == rebuilds && index.RootLookups == lookups && allocated == 0, "warm routing rebuilds or allocates");
  Console.WriteLine("Controlled hierarchy benchmark: legacy=" + oldMs.ToString("F1") + "ms, indexed=" + watch.Elapsed.TotalMilliseconds.ToString("F1") + "ms; legacy hierarchy tests=" + legacyTests + "; steady allocations=" + allocated + " bytes. Not an in-game FPS measurement.");
 }
 static void Main() {
  Masks(); Lifetimes(); StoryAndOwnership(); RealIntegration(); CaptureIsolation(); Background(); BlurredComposition(); GraphicsBudget(); HudBudget(); HudFeedback(); HudSpawnHooks(); Hierarchy(); Benchmark();
  Console.WriteLine("PASS " + checks + " Focus/camera/interaction regression checks");
 }
}

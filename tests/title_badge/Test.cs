using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using KO.HollowKnight8;
class Test {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static GameObject Child(string name,Transform parent){var g=new GameObject(name);g.transform.parent=parent;return g;}
 static Rect Area(Bounds b)=>Rect.MinMaxRect(b.min.x,b.min.y,b.max.x,b.max.y);
 static GameObject Badge()=>GameObject.All.Single(g=>g&&g.name.StartsWith("Local8 Co-op DLC Icon"));
 static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
 static void At(SpriteRenderer image,SpriteRenderer anchor,int index){var expected=anchor.transform.position+SFCoreReference.Position(index);Check(Near(image.transform.position.x,expected.x)&&Near(image.transform.position.y,expected.y),"badge is not at shared SFCore slot "+index);}
 static void Main(){
  for(int i=0;i<200;i++){var actual=DlcBadgePlacement.Offset(i);var expected=SFCoreReference.Position(i);Check(Near(actual.x,expected.x)&&Near(actual.y,expected.y),"grid differs from SFCore at index "+i);}
  var runtime=new Local8Runtime();var ui=new GameObject("UIManager").Add<UIManager>();UIManager.instance=ui;
  var canvas=Child("UICanvas",ui.transform).Add<Canvas>();ui.UICanvas=canvas;
  ui.mainMenuScreen=Child("MainMenuScreen",canvas.transform).Add<CanvasGroup>();
  var group=Child("TeamCherryLogo",ui.mainMenuScreen.transform);
  group.Add<Renderer>().Box=new(new(8,1),new(16,2));
  var titleCamera=new GameObject("Menu camera").Add<Camera>();canvas.worldCamera=titleCamera;
  var wrongCamera=new GameObject("Game HUD camera").Add<Camera>();wrongCamera.View=new(100,100,1,1);
  GameCameras.instance=new(){mainCamera=titleCamera,hudCamera=wrongCamera};
  var nativeSprite=new Sprite{bounds=new(new(),new(.6f,.6f))};
  float[] offsets={0,-.9f,.83f,1.66f,2.49f};
  var originals=offsets.Select((x,i)=>{var g=Child(i==0?"Hidden_Dreams_Logo":"DLC "+i,group.transform);g.transform.position=new(2+x,1);var image=g.Add<SpriteRenderer>();image.sprite=nativeSprite;return image;}).ToArray();
  var positions=originals.Select(x=>x.transform.position).ToArray();
  TitleCoopBadge.Tick(runtime);var badge=Badge();var image=badge.GetComponent<SpriteRenderer>();
  At(image,originals[0],5);Check(image.bounds.size.x<=.7001f&&image.bounds.size.y<=.7001f,"badge exceeds the native grid cell");
  Check(badge.activeInHierarchy&&titleCamera.Reads>0&&wrongCamera.Reads==0,"title badge used gameplay HUD projection or remains hidden");
  Check(image.sprite.texture==MenuArt.Glyph&&image.sprite!=nativeSprite,"badge replaces native artwork or fails to reuse F8 glyph");
  Check(originals.All(x=>!DlcBadgePlacement.Overlaps(Area(x.bounds),Area(image.bounds))),"badge overlaps native icons");
  for(int i=0;i<originals.Length;i++)Check(originals[i].transform.position.x==positions[i].x&&originals[i].transform.position.y==positions[i].y&&originals[i].sprite==nativeSprite,"native icon was moved/replaced");
  SFCore.DlcIconHelper.AddDlcIcon(nativeSprite);TitleCoopBadge.Tick(runtime);At(image,originals[0],6);
  var mod=Child("CUSTOM DLC ICON PaleCourt",group.transform).Add<SpriteRenderer>();mod.sprite=nativeSprite;mod.transform.position=originals[0].transform.position+SFCoreReference.Position(5);var occupied=Area(mod.bounds);
  TitleCoopBadge.Tick(runtime);At(image,originals[0],6);Check(!DlcBadgePlacement.Overlaps(occupied,Area(image.bounds))&&mod.enabled,"Pale Court badge is overlapped or disabled");
  var late=Child("Other mod DLC",group.transform).Add<SpriteRenderer>();late.sprite=nativeSprite;late.transform.position=badge.transform.position;
  TitleCoopBadge.Tick(runtime);At(image,originals[0],7);Check(!DlcBadgePlacement.Overlaps(Area(late.bounds),Area(image.bounds))&&late.enabled,"late unregistered mod badge is overlapped or disabled");
  SFCore.DlcIconHelper.AddDlcIcon(nativeSprite);TitleCoopBadge.Tick(runtime);At(image,originals[0],7);
  canvas.worldCamera=null;TitleCoopBadge.Tick(runtime);Check(badge.activeInHierarchy&&wrongCamera.Reads==0,"missing canvas camera does not fall back to native main camera");
  int allocations=Sprite.Created;for(int i=0;i<100;i++){TitleCoopBadge.Tick(runtime);Check(Badge()==badge,"steady tick replaces badge object");}
  Check(Sprite.Created==allocations,"steady title tick creates new sprites");
  TitleLobby.Visible=false;TitleCoopBadge.Tick(runtime);Check(!badge.activeSelf,"badge remains active outside title");
  TitleLobby.Visible=true;runtime.Enabled.Value=false;TitleCoopBadge.Tick(runtime);Check(!badge.activeSelf,"disabled mod still displays badge");
  runtime.Enabled.Value=true;TitleCoopBadge.Tick(runtime);Check(badge.activeSelf,"badge cannot return after enabling mod");
  ui.mainMenuScreen.gameObject.SetActive(false);TitleCoopBadge.Tick(runtime);Check(!badge.activeInHierarchy,"badge escapes inactive main menu");ui.mainMenuScreen.gameObject.SetActive(true);
  for(int i=SFCore.DlcIconHelper.Count;i<195;i++)SFCore.DlcIconHelper.AddDlcIcon(nativeSprite);
  TitleCoopBadge.Tick(runtime);Check(!badge.activeSelf&&originals.All(x=>x.enabled)&&mod.enabled,"full menu overlaps badge or hides a foreign icon");
  int reservations=SFCore.DlcIconHelper.Count;var ownSprite=image.sprite;TitleCoopBadge.Release();Check(badge.Destroyed&&ownSprite.Destroyed&&!nativeSprite.Destroyed&&!MenuArt.Glyph.Destroyed&&mod&&late&&SFCore.DlcIconHelper.Count==reservations,"teardown edits foreign reservations or shared/native resources");
  Console.WriteLine("PASS "+checks+" title badge checks: SFCore reference grid, Pale Court reservations, exact F8 texture, parent-footer exclusion, late icons, camera/visibility and owned teardown. Native Unity layout remains untested.");
 }
}

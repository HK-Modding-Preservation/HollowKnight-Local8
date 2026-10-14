using System;
using System.Linq;
using UnityEngine;
using KO.HollowKnight8;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
static class Test {
 static int checks,failures;
 static void Check(bool value,string text){checks++;if(!value){failures++;Console.WriteLine("FAIL "+text);}}
 static Sprite Art(string name)=>new(){name=name};
 static PlayMakerFSM RoyalFsm(GameObject obj){var fsm=obj.Add(new PlayMakerFSM());foreach(string state in new[]{"R Queen","R King","R Final","R Shade"})fsm.Fsm.States[state]=new(){Actions=new FsmStateAction[]{new SetSpriteRendererSprite{sprite=new(){Value=Art(state)}}}};return fsm;}
 static void Initial(){Charms.Reset();Charms.NativeBoards=Array.Empty<InvCharmBackboard>();Resources.Displays=Array.Empty<CharmDisplay>();Time.unscaledTime=0;GameManager.instance=new();CharmIconList.Instance=new();CharmIconList.Instance.Sprites[36]=Art("fake-unlocker");}
 static void Royal(){
  Initial();var boardObj=new GameObject();var iconObj=new GameObject();var fsm=RoyalFsm(iconObj);var display=iconObj.Add(new CharmDisplay{id=36,whiteCharm=Art("white"),blackCharm=Art("void")});
  Charms.NativeBoards=new[]{boardObj.Add(new InvCharmBackboard{charmNum=36,charmObject=iconObj})};
  Texture2D previous=null;int before=HudAssets.Copies;
  for(int state=0;state<=4;state++){
   GameManager.instance.playerData.Royal=state;string suffix=state>3?"C":state==3?"B":"A";
   Check(Charms.Name(36)=="CHARM_NAME_36_"+suffix+"<br>text","Royal name follows progression "+state);
   Check(Charms.Description(36)=="CHARM_DESC_36_"+suffix+"\ntext","Royal description follows progression "+state);
   string expected=state>3?"R Shade":state==3?"R Final":state==2?"R King":"R Queen";
   var texture=Charms.Icon(36);Check(texture&&texture.name==expected,"Native royal FSM sprite "+state);
   Check(Charms.Icon(36)==texture,"Unchanged sprite reuses its texture");
   if(previous!=null&&previous!=texture)Check(previous.Destroyed,"Royal progress releases previous copied icon");
   previous=texture;
  }
  Check(HudAssets.Copies-before==4,"Both fragment sides, white and void each copied once");
  Check(!Language.Language.Keys.Contains("CHARM_NAME_36")&&!Language.Language.Keys.Contains("CHARM_DESC_36"),"Custom Knight placeholder keys never requested");
  Check(CharmIconList.Instance.RoyalRequests==0,"Generic spriteList[36] is never used");
  var replacement=Art("skin-void");((SetSpriteRendererSprite)fsm.Fsm.GetState("R Shade").Actions[0]).sprite.Value=replacement;
  var changed=Charms.Icon(36);Check(changed.name=="skin-void"&&previous.Destroyed,"Skin changes update royal icon without scene reset");
  Charms.Reset();Check(changed.Destroyed&&!replacement.Destroyed&&!display.Destroyed,"Reset destroys copies, preserves native sprites/components");
  Charms.NativeBoards=Array.Empty<InvCharmBackboard>();Resources.Displays=new[]{display};GameManager.instance.playerData.Royal=3;
  Check(Charms.Icon(36).name=="R Final","F8 resolves native royal sprites before pane probe");
  UnityEngine.Object.Destroy(fsm);Charms.Reset();Time.unscaledTime=1;GameManager.instance.playerData.Royal=4;
  var fallback=Charms.Icon(36);Check(fallback.name=="void","Native CharmDisplay fallback uses blackCharm");
  UnityEngine.Object.Destroy(display);Time.unscaledTime=2;Check(Charms.Icon(36)==null&&fallback.Destroyed,"Destroyed scene source cannot leave a stale royal texture");
  var later=new GameObject();var laterDisplay=later.Add(new CharmDisplay{id=36,whiteCharm=Art("late-white"),blackCharm=Art("late-void")});Resources.Displays=new[]{laterDisplay};Time.unscaledTime=3;
  Check(Charms.Icon(36).name=="late-void","Missing native source is retried when available");
  GameManager.instance.playerData.Royal=3;var white=Charms.Icon(36);Check(white.name=="late-white","Display fallback follows new royal state");
  Charms.Reset();Check(white.Destroyed,"Final royal texture released");
 }
 static void Others(){
  Initial();foreach(int id in new[]{1,6,23,24,25,40,41,87}){
   var a=Art("original-"+id);CharmIconList.Instance.Sprites[id]=a;
   Check(Charms.Name(id)=="CHARM_NAME_"+id+"<br>text","Vanilla and mod name keys preserved");
   Check(Charms.Description(id)=="CHARM_DESC_"+id+"\ntext","Vanilla and mod descriptions preserved");
   var copied=Charms.Icon(id);Check(copied.name==a.name&&Charms.Icon(id)==copied,"Cache preserves stable vanilla/mod sprites");
   CharmIconList.Instance.Sprites[id]=Art("replacement-"+id);var next=Charms.Icon(id);Check(next.name=="replacement-"+id&&copied.Destroyed,"Fragile/Grimmchild/skin sprite changes invalidate only its cache");
   CharmIconList.Instance.Sprites[id]=null;Check(Charms.Icon(id)==null&&next.Destroyed,"Disappearing provider sprite releases copied icon");
   CharmIconList.Instance.Sprites[id]=a;Check(Charms.Icon(id).name==a.name,"Provider can recover without reset");
  }
  var current=Charms.Icon(6);HudAssets.Throw=true;CharmIconList.Instance.Sprites[6]=Art("retry");Check(Charms.Icon(6)==null&&current.Destroyed,"Failed extraction cannot keep the prior sprite");HudAssets.Throw=false;Check(Charms.Icon(6).name=="retry","Failed extraction retries");
  Language.Language.Throw=true;Check(Charms.Name(6)=="Amuleto 6"&&Charms.Description(6)=="","Localization exception fallback preserved");Language.Language.Throw=false;
  var copies=new[]{1,6,23,24,25,40,41,87}.Select(Charms.Icon).ToArray();Charms.Reset();Check(copies.All(t=>t.Destroyed),"Reset releases all copied icons");
  Check(Diagnostics.Errors==1,"Extraction exception remains diagnosed");
 }
 static int Main(){Royal();Others();Console.WriteLine((failures==0?"PASS ":"FAILED ")+checks+" charm-presentation checks; failures="+failures+". Native scene FSM assets remain unexecuted.");return failures==0?0:1;}
}

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using KO.HollowKnight8;
using UnityEngine;
using UnityEngine.EventSystems;

static class Test
{
    static int checks, failures;
    static void Check(bool ok, string message)
    {
        checks++;
        if (!ok) { failures++; if (failures <= 20) Console.WriteLine("FAIL " + message); }
    }
    static bool Near(float a, float b) => Math.Abs(a - b) < .02f;
    static CoopSession Party(int count)
    {
        var s = new CoopSession();
        for (int i = 0; i < count; i++)
        {
            var p = new PlayerSlot { Index = i, Color = new Color(.3f + .06f * i, .8f, .9f) };
            p.Charms.Order.AddRange(Enumerable.Range(1, i % 7 + 1));
            p.Charms.Used = p.Charms.Order.Count;
            s.Players.Add(p); Charms.NativeCursor[i] = 1;
        }
        Plugin.Self = new Local8Runtime { Session = s }; Charms.NativeMenuOpen = true;
        return s;
    }
    static void Layout(string preview)
    {
        foreach (var size in new[] { (960,540), (1280,720), (1920,1080), (2560,1440), (3840,2160), (1280,1024), (3440,1440) })
        {
            Screen.width = size.Item1; Screen.height = size.Item2;
            float scale = Mathf.Clamp(Screen.height / 900f, .65f, 1.6f), w = Screen.width / scale, h = Screen.height / scale;
            for (int count = 2; count <= 8; count++)
            {
                var s = Party(count); GUI.Reset(w,h); Hud.Render(s,w,h);
                Check(Hud.Cards.Count == count, "Every actor has a card");
                var first = Hud.Cards[0];
                foreach (Rect r in Hud.Cards)
                {
                    Check(Near(r.y, first.y) && Near(r.height, first.height), "P1-P8 must share one full-height row at " + count);
                    Check(r.width > 0 && r.x >= 0 && r.xMax <= w && r.yMax <= h, "Card remains inside the viewport");
                    Check(r.y >= h*.197f+3.9f, "Cards lowered clear of the native panel ornament");
                }
                for (int i=1;i<count;i++) Check(Hud.Cards[i-1].xMax < Hud.Cards[i].x, "Adjacent cards do not overlap");
                if (count <= 4) Check(Near(first.height, h*.294f-8), "Existing four-player card height retained");
                if (count > 4) Check(Hud.Cards.Last().xMax > w*.90f, "Large party uses the available top-row width");
                var roleTitle=GUI.Paints.First(p=>p.kind=="label"&&p.text=="roles");
                Check(count<=4 || roleTitle.rect[1] >= first.yMax, "Role panel cannot cover P5-P8");
                for(int i=0;i<count;i++)
                {
                    GUI.Reset(w,h); var r=Hud.Cards[i]; var p=s.Players[i];
                    foreach(int amount in new[]{0,1,6,11,16})
                    {
                        p.Charms.Order.Clear();p.Charms.Order.AddRange(Enumerable.Range(1,amount));GUI.Reset(w,h);
                        CharmCardUI.Draw(p,s.Data,r);
                        var label=GUI.Paints.First(paint=>paint.kind=="label"&&paint.text=="P"+(p.Index+1));
                        Check(label.rect[3]>=label.size*1.25f,"P label has room for the native font line height");
                        var icons=GUI.Paints.Where(paint=>paint.texture!=null&&paint.texture.StartsWith("charm-")).ToArray();
                        Check(icons.Length==amount,"Every equipped charm is drawn");
                        foreach(var icon in icons)
                            Check(icon.rect[0]>=r.x+7.9f&&icon.rect[0]+icon.rect[2]<=r.xMax-7.9f&&icon.rect[2]>0&&icon.rect[1]+icon.rect[3]<r.yMax-48,
                                "Adaptive equipped icons fit card and leave notches clear");
                    }
                }
                // Eight people may choose the same role. Labels and role
                // descriptions must still fit without covering equipped icons.
                RoleSystem.SameRole=true;GUI.Reset(w,h);Hud.Render(s,w,h);
                var owners=GUI.Paints.Where(p=>p.kind=="label"&&p.rect[0]>=w*.61f&&p.size==(count>4?11:14)&&p.text.Length==2&&p.text[0]=='P').ToArray();
                Check(owners.Length==count,"Every same-role owner stays visible");
                RoleSystem.SameRole=false;
                foreach(var player in s.Players)Charms.NativeCursor[player.Index]=-1;
                GUI.Reset(w,h);Hud.Render(s,w,h);
                Check(GUI.Paints.Count(p=>p.texture!=null&&p.texture.StartsWith("charm-"))==s.Players.Sum(p=>p.Charms.Ids().Length),"Browsing roles retains equipped charm icons");
                foreach(var player in s.Players)Charms.NativeCursor[player.Index]=1;
                if (preview!=null && size==(1920,1080) && (count==4 || count==5 || count==8))
                {
                    s=Party(count);GUI.Reset(w,h);Hud.Render(s,w,h);
                    Directory.CreateDirectory(preview);
                    var backdrops=Texture2D.Created.Where(t=>t.name=="Local8.CharmMenuBackdrop"&&!t.Destroyed).Select(t=>new{t.width,t.height,alpha=t.Pixels.Select(pixel=>(byte)Mathf.RoundToInt(pixel.a*255)).ToArray()}).ToArray();
                    File.WriteAllText(Path.Combine(preview,"cards-"+count+".json"),JsonSerializer.Serialize(new{width=w,height=h,paints=GUI.Paints,backdrops},new JsonSerializerOptions{IncludeFields=true}));
                }
            }
        }
    }
    static void Ownership()
    {
        Party(8); var root=new GameObject(); var pane=new GameObject();pane.transform.parent=root.transform;
        GameManager.instance=new GameManager{inventoryFSM=root.Add(new PlayMakerFSM())};Charms.NativePane=pane;
        var sibling=new GameObject();sibling.transform.parent=root.transform;
        foreach(var selected in new[]{pane,sibling,root,new GameObject()})
        {
            EventSystem.current=new EventSystem{currentSelectedGameObject=selected};
            Check(CharmMenuExit.NativeActive(true),"Real inventory ownership survives sibling/stale EventSystem focus");
        }
        var gm=GameManager.instance;
        gm.inventoryFSM.ActiveStateName="Closed";Check(!CharmMenuExit.NativeActive(true),"Closing releases native inventory");gm.inventoryFSM.ActiveStateName="Open";
        gm.inventoryFSM.isActiveAndEnabled=false;Check(!CharmMenuExit.NativeActive(true),"Disabled inventory releases ownership");gm.inventoryFSM.isActiveAndEnabled=true;
        gm.inventoryFSM.gameObject.activeInHierarchy=false;Check(!CharmMenuExit.NativeActive(true),"Hidden inventory releases ownership");gm.inventoryFSM.gameObject.activeInHierarchy=true;
        gm.isPaused=true;Check(!CharmMenuExit.NativeActive(true),"Pause still wins");gm.isPaused=false;
        gm.IsLoadingSceneTransition=true;Check(!CharmMenuExit.NativeActive(true),"Loading still wins");gm.IsLoadingSceneTransition=false;
        Check(!CharmMenuExit.NativeActive(false),"Leaving charms cannot keep ownership");
        ShopMenuRouting.Buyer=Plugin.Self.Session.Primary;ShopMenuRouting.MenuVisible=true;Check(!CharmMenuExit.NativeActive(true),"Real shop still wins");ShopMenuRouting.Buyer=null;
        StagMenuRouting.HasOwner=true;Check(!CharmMenuExit.NativeActive(true),"Stag still wins");StagMenuRouting.HasOwner=false;
        PickupCard.Owner=Plugin.Self.Session.Primary;Check(!CharmMenuExit.NativeActive(true),"Pickup still wins");PickupCard.Owner=null;
        InteractionRouter.ActivePlayer=Plugin.Self.Session.Primary;InteractionRouter.ActivePlayer.Vitals.AtBench=false;
        InteractionRouter.ActiveIsBench=true;
        Check(CharmMenuExit.NativeActive(true),"Bench interaction remains a bench after the mutable atBench flag changes");
        InteractionRouter.ActiveIsBench=false;
        Check(!CharmMenuExit.NativeActive(true),"Non-bench dialogue still wins");InteractionRouter.ActivePlayer=null;
        Charms.NativePane=new GameObject();EventSystem.current=new EventSystem{currentSelectedGameObject=new GameObject()};
        Check(!CharmMenuExit.NativeActive(true),"Unowned unrelated UI stays protected");
        EventSystem.current.currentSelectedGameObject.activeInHierarchy=false;
        Check(CharmMenuExit.NativeActive(true),"Inactive stale focus cannot disable visible charms");EventSystem.current=null;
    }
    static InvCharmBackboard[] Catalog(GameObject pane)
    {
        var boards=new InvCharmBackboard[42];
        for(int i=0;i<40;i++){var obj=new GameObject();obj.transform.parent=pane.transform;obj.transform.position=new(.15f+(i%10)*.045f,.45f-(i/10)*.09f,1);boards[i]=obj.Add(new InvCharmBackboard{charmNum=i+1,gotCharmString="gotCharm_"+(i+1)});}
        var extra=new GameObject();extra.transform.parent=pane.transform;extra.transform.position=new(.15f,.09f,1);boards[40]=extra.Add(new InvCharmBackboard{charmNum=701,gotCharmString="gotCharm_701"});CharmExtensions.Register(701);
        var padding=new GameObject();padding.transform.parent=pane.transform;padding.transform.position=new(.20f,.09f,1);boards[41]=padding.Add(new InvCharmBackboard{charmNum=750});
        return boards;
    }
    static void Step(CoopSession session,PlayerSlot player,int direction=0,bool submit=false)
    {
        foreach(var p in session.Players){p.Actions.Dir=0;p.Actions.Submit=false;}
        player.Actions.Dir=direction;player.Actions.Submit=submit;Time.unscaledTime+=.4f;RoleSystem.NativeInput(session);
    }
    static void EquippedNavigation()
    {
        Screen.width=1920;Screen.height=1080;var s=Party(8);Charms.NativePane=new GameObject();Charms.NativeBoards=Catalog(Charms.NativePane);Charms.wasNative=false;Charms.Toggles.Clear();
        foreach(var p in s.Players){p.Charms.Order.Clear();p.Charms.Order.AddRange(new[]{1,6,36,701+p.Index});CharmExtensions.Register(701+p.Index);}
        Step(s,s.Primary);
        foreach(var player in s.Players)
        {
            Step(s,player,2);var state=RoleSystem.State(player);
            Check(state.Equipped.Active&&state.Equipped.Selected==1,"P1-P8 independently enter their equipped panel from the catalog top boundary");
            for(int i=0;i<3;i++)Step(s,player,1);
            Check(state.Equipped.Selected==701+player.Index&&!CharmExtensions.IsRole(Charms.NativeCursor[player.Index]),"Sparse mod ID stays a real equipped charm, never a role sentinel");
            var before=s.Players.Select(p=>p.Charms.Ids()).ToArray();int calls=Charms.Toggles.Count;Step(s,player,submit:true);
            Check(Charms.Toggles.Count==calls+1&&Charms.Toggles.Last()==(player.Index,701+player.Index),"Production NativeInput routes removal to the correct player and mod ID");
            foreach(var other in s.Players)if(other!=player)Check(other.Charms.Ids().SequenceEqual(before[other.Index]),"Removing one panel's charm does not alter another player's equipment");
            Check(state.Equipped.Active&&state.Equipped.Selected==36,"Removed final slot clamps to the remaining preceding charm");
            calls=Charms.Toggles.Count;Step(s,player,submit:true);Check(Charms.Toggles.Count==calls,"Held submit cannot remove the next charm automatically");
            GUI.Reset();Hud.GridCursors(s,1.2f);Check(!GUI.Paints.Any(p=>p.kind=="label"&&p.text=="P"+(player.Index+1)),"A player in their equipped panel has no duplicate cursor in the native catalog");
            Rect card=CharmMenuLayout.Card(CharmMenuLayout.Cards(1600,900,8),player.Index,8);GUI.Reset();CharmCardUI.Draw(player,s.Data,card);
            var icon=GUI.Paints.First(p=>p.texture=="charm-36");Rect selected=new Rect(icon.rect[0],icon.rect[1],icon.rect[2],icon.rect[3]);
            var corners=GUI.Paints.Where(p=>p.texture=="white"&&p.rect[0]>=selected.x-.01f&&p.rect[1]>=selected.y-.01f&&p.rect[0]+p.rect[2]<=selected.xMax+.01f&&p.rect[1]+p.rect[3]<=selected.yMax+.01f).ToArray();
            Check(corners.Length==8&&corners.All(p=>Near(p.color[0],player.Color.r)&&Near(p.color[1],player.Color.g)),"Actual card draws the same eight square-corner segments on the selected icon with its owner's color");
            Step(s,player,3);Check(!state.Equipped.Active&&Charms.NativeCursor[player.Index]==1,"Down from the last equipped row returns to that player's original catalog charm");
        }
        var primary=s.Primary;primary.Charms.Order.Clear();Step(s,primary,2);Check(!RoleSystem.State(primary).Equipped.Active,"Empty loadout cannot trap a player in an empty panel");
        primary.Charms.Order.AddRange(new[]{1,6,36,701,901,1701,5001,8001,19001,29001,39001});foreach(int id in primary.Charms.Order)CharmExtensions.Register(id);
        Step(s,primary,2);var x=RoleSystem.State(primary);int selectedId=x.Equipped.Selected;
        primary.Charms.Order.Reverse();Step(s,primary);Check(x.Equipped.Selected==selectedId,"Provider reordering retains selection by charm ID");primary.Charms.Order.Reverse();Step(s,primary);
        primary.Charms.Order.Remove(1);Step(s,primary);Check(x.Equipped.Active&&x.Equipped.Selected==6,"Provider removal selects a valid remaining slot");
        primary.Charms.Order.Clear();Step(s,primary);Check(!x.Equipped.Active&&Charms.NativeCursor[primary.Index]==1,"Last charm disappearing returns to a valid catalog cursor");
        Charms.NativeCursor[0]=701;Step(s,primary,2);Check(Charms.NativeCursor[0]==31&&!x.Equipped.Active,"Up from a mod-added fifth catalog row preserves native/mod grid navigation");
        for(int i=0;i<3;i++)Step(s,primary,2);Check(Charms.NativeCursor[0]==1,"Up traverses native rows before trying to enter the panel");
        primary.Charms.Order.AddRange(new[]{1,6});Step(s,primary,2);Check(x.Equipped.Active,"Panel is reachable after navigating from a mod-added catalog row");
        RoleSystem.ClearNativeFocus();Check(s.Players.All(p=>!RoleSystem.State(p).Equipped.Active),"Closing/reset clears all transient panel focus");
        Charms.NativeCursor[0]=10;Step(s,primary,1);Check(CharmExtensions.IsRole(Charms.NativeCursor[0])&&!x.Equipped.Active,"Right from the native row boundary still enters roles");Step(s,primary,-1);Check(Charms.NativeCursor[0]==10,"Roles still return to the native catalog");
        // Narrow card: real column count controls row movement, including partial rows.
        s=Party(5);Charms.NativeBoards=Catalog(Charms.NativePane);Charms.wasNative=false;primary=s.Primary;primary.Charms.Order.Clear();primary.Charms.Order.AddRange(new[]{1,6,36,701,901,1701,5001,8001,19001,29001,39001});Step(s,primary);Step(s,primary,2);x=RoleSystem.State(primary);
        Rect r=CharmMenuLayout.Card(CharmMenuLayout.Cards(1600,900,5),0,5);Check(CharmMenuLayout.IconColumns(r)==6,"Fixture's five-player card has six icon columns");
        Step(s,primary,3);Check(x.Equipped.Selected==5001,"Down uses the rendered icon columns to reach the next row");Step(s,primary,2);Check(x.Equipped.Selected==1,"Up returns to the same rendered column");
        for(int i=0;i<5;i++)Step(s,primary,1);Step(s,primary,3);Check(x.Equipped.Selected==39001,"Down into a partial final row chooses its last valid icon");Step(s,primary,3);Check(!x.Equipped.Active&&Charms.NativeCursor[0]==1,"Down after the final row returns to catalog");
        var cursor=new EquippedCharmCursor();cursor.Enter(new[]{701,901,5001},901);Check(cursor.Selected==901,"Entering highlights the catalog charm when already equipped");
        cursor.Move(3,1,new[]{701,901,5001});Check(cursor.Selected==5001,"Single-column panel moves down without a fixed four-column assumption");
        Check(cursor.Index(Array.Empty<int>())==-1&&cursor.Active,"Drawing an empty snapshot does not mutate live input focus");Check(!cursor.Sync(Array.Empty<int>())&&!cursor.Active,"Input sync safely releases an empty snapshot");
    }
    static void CatalogTint()
    {
        var s=Party(4);s.Primary.Charms.Order.AddRange(new[]{6,701,702});CharmExtensions.Register(701);CharmExtensions.Register(702);s.Data.Unowned.Add(702);
        var pane=new GameObject();Charms.NativePane=pane;var camera=new Camera();GameCameras.instance=new(){hudCamera=camera};
        var icon=new GameObject();icon.transform.parent=pane.transform;var native=icon.Add(new SpriteRenderer{color=new(.25f,.25f,.25f,.8f)});
        var flashObj=new GameObject();flashObj.transform.parent=icon.transform;var flash=flashObj.Add(new SpriteRenderer{color=new(.2f,.2f,.2f,0f)});var effectObj=new GameObject();effectObj.transform.parent=icon.transform;var effect=effectObj.Add(new SpriteRenderer{color=new(.3f,.4f,.5f,.3f)});
        icon.Add(new CharmDisplay{spriteRenderer=native});
        var modIcon=new GameObject();modIcon.transform.parent=pane.transform;var mod=modIcon.Add(new SpriteRenderer{color=new(.4f,.3f,.2f,.6f)});
        var lockedIcon=new GameObject();lockedIcon.transform.parent=pane.transform;var locked=lockedIcon.Add(new SpriteRenderer{color=new(.1f,.1f,.1f,.5f)});
        var normalIcon=new GameObject();normalIcon.transform.parent=pane.transform;var normal=normalIcon.Add(new SpriteRenderer{color=new(.7f,.8f,.9f,1)});
        var remoteObj=new GameObject();remoteObj.transform.parent=pane.transform;var remote=remoteObj.Add(new SpriteRenderer{color=new(.2f,.5f,.3f,.7f)});var remoteLink=new GameObject();remoteLink.Add(new CharmDisplay{spriteRenderer=remote});s.Primary.Charms.Order.Add(703);CharmExtensions.Register(703);
        var boards=new[]{new GameObject().Add(new InvCharmBackboard{charmNum=6,charmObject=icon}),new GameObject().Add(new InvCharmBackboard{charmNum=701,charmObject=modIcon}),new GameObject().Add(new InvCharmBackboard{charmNum=702,charmObject=lockedIcon}),new GameObject().Add(new InvCharmBackboard{charmNum=7,charmObject=normalIcon}),new GameObject().Add(new InvCharmBackboard{charmNum=703,charmObject=remoteLink})};Charms.NativeBoards=boards;
        CharmCatalogTint.Install();Camera.onPreCull(camera);
        Check(native.color.r==1&&native.color.g==1&&native.color.b==1&&native.color.a==1,"P1 equipped native catalog icon has neither color nor opacity dimming");
        Check(mod.color.r==1&&mod.color.g==1&&mod.color.b==1&&mod.color.a==1,"P1 equipped mod ID>40 receives the same visual treatment");
        Check(remote.color.r==1&&remote.color.a==1,"Explicit CharmDisplay link also supports a separately parented mod icon");
        Check(flash.color.a==0&&Near(flash.color.r,.2f)&&Near(effect.color.a,.3f)&&Near(effect.color.g,.4f),"Separate invisible flashes and translucent effect layers retain their full authored color/alpha");
        Check(Near(locked.color.r,.1f)&&Near(normal.color.r,.7f),"Unowned charms and charms not equipped by P1 retain their original scene colors");
        Camera.onPostRender(camera);Check(Near(native.color.r,.25f)&&Near(mod.color.g,.3f),"Post-render restores exact native/provider tint rather than changing gameplay data");
        native.color=new(.5f,.6f,.7f,.4f);Camera.onPreCull(camera);Camera.onPostRender(camera);Check(Near(native.color.g,.6f)&&Near(native.color.a,.4f),"Changing native tint/animation is captured and restored each render");
        Camera.onPreCull(camera);CharmCatalogTint.BeforeCamera(new Camera());Check(Near(native.color.g,.6f),"Another camera cannot inherit a tint override after a missed post-render callback");
        s.Data.ThrowOwned=701;int errors=Diagnostics.Errors;Camera.onPreCull(camera);Check(Near(native.color.g,.6f)&&Diagnostics.Errors==errors+1,"Provider failure restores any partial tint changes and is diagnosed");s.Data.ThrowOwned=0;
        Camera.onPreCull(camera);Charms.NativeMenuOpen=false;CharmCatalogTint.BeforeCamera(camera);Check(Near(native.color.g,.6f),"Menu exit restores scene colors");Charms.NativeMenuOpen=true;
        Camera.onPreCull(camera);CharmCatalogTint.Uninstall();Check(Near(native.color.g,.6f)&&Camera.onPreCull==null&&Camera.onPostRender==null,"Shutdown restores colors and removes callbacks");
    }
    static void Visibility()
    {
        Party(5);var pane=new GameObject();var camera=new Camera();var boards=new InvCharmBackboard[40];
        GameObject.Objects.Clear();GameObject.Objects.Add(pane);GameObject.TaggedPane=pane;GameManager.instance=new();GameCameras.instance=new(){hudCamera=camera};
        for(int i=0;i<40;i++){var obj=new GameObject();obj.transform.parent=pane.transform;obj.transform.position=new(.15f+(i%10)*.045f,.45f-(i/10)*.09f,1);boards[i]=obj.Add(new InvCharmBackboard{charmNum=i+1});}
        foreach(var pivot in new[]{new Vector3(-.02f,.6f,1),new Vector3(1.02f,.6f,1),new Vector3(.5f,.98f,1),new Vector3(.5f,.02f,1)})
        {pane.transform.position=pivot;Check(CharmMenuVisibility.Visible(pane,camera,boards,out var anchor)&&anchor.x>.15f&&anchor.x<.6f,"Visible catalog remains active despite its parent pivot being outside the old 5%-95% range");Time.frameCount++;Check(Charms.ProbeState(),"Actual production ProbeNative preserves the visible catalog with an offscreen pivot");}
        for(int i=0;i<40;i++)boards[i].transform.position=new(1.2f+(i%10)*.045f,.45f-(i/10)*.09f,1);
        pane.transform.position=new(.5f,.5f,1);Check(!CharmMenuVisibility.Visible(pane,camera,boards,out _),"Other inventory pages cannot keep an offscreen catalog active even if its pivot remains inside");
        Time.frameCount++;Check(!Charms.ProbeState(),"Actual production ProbeNative releases an offscreen catalog despite a visible pivot");
        pane.activeInHierarchy=false;Check(!CharmMenuVisibility.Visible(pane,camera,Array.Empty<InvCharmBackboard>(),out _),"Inactive pane releases immediately");pane.activeInHierarchy=true;
        Check(CharmMenuVisibility.Visible(pane,camera,Array.Empty<InvCharmBackboard>(),out _),"Unprobed/custom sparse pane can use its visible pivot");
        pane.transform.position=new(.5f,.5f,-1);Check(!CharmMenuVisibility.Visible(pane,camera,Array.Empty<InvCharmBackboard>(),out _),"Pane behind the HUD camera releases");
        CharmMenuDiagnostics.Reset();Diagnostics.Logs.Clear();Charms.NativePane=pane;Time.unscaledTime=5;
        CharmMenuDiagnostics.Report(true,"native-inventory");
        for(int i=0;i<100;i++)CharmMenuDiagnostics.Report(true,"native-inventory");
        Check(Diagnostics.Logs.Count==1,"Stable menu logs no repeated frame messages");Time.unscaledTime+=.3f;
        CharmMenuDiagnostics.Report(false,"catalog-offscreen");Check(Diagnostics.Logs.Count==2&&Diagnostics.Logs.Last().Contains("reason=catalog-offscreen"),"Loss log includes the exact guard reason");
        CharmMenuDiagnostics.Reset();Diagnostics.Logs.Clear();
    }
    static Renderer Render(GameObject root,float x,float y,float width,float height,bool off=false)
    {
        var obj=new GameObject();obj.transform.parent=root.transform;
        var r=obj.Add(new Renderer{forceRenderingOff=off,bounds=new Bounds{
            center=new Vector3(x+width/2,1-y-height/2),min=new Vector3(x,1-y-height),max=new Vector3(x+width,1-y)}});
        GameObject.All.Add(r);return r;
    }
    static void Hidden()
    {
        foreach(var count in new[]{4,5,8})
        {
            var s=Party(count);Screen.width=1920;Screen.height=1080;
            Charms.NativePane=new GameObject();var camera=new Camera();GameCameras.instance=new(){hudCamera=camera};GameObject.All.Clear();
            var summary=Render(Charms.NativePane,.20f,.23f,.08f,.06f);
            var feather=Render(Charms.NativePane,.10f,.21f,.004f,.015f);
            var description=Render(Charms.NativePane,.66f,.27f,.12f,.10f,true);
            var board=new GameObject();board.transform.parent=Charms.NativePane.transform;var link=board.Add(new InvCharmBackboard{charmNum=6});
            // Real InvCharmBackboard.OnEnable positions its explicitly linked
            // charmObject beside the board, not necessarily as its child.
            var fury=Render(Charms.NativePane,.577f,.64f,.035f,.053f);
            var royal=Render(Charms.NativePane,.577f,.81f,.035f,.053f);
            var royalBoard=new GameObject();royalBoard.transform.parent=Charms.NativePane.transform;
            var royalLink=royalBoard.Add(new InvCharmBackboard{charmNum=36,charmObject=royal.gameObject});
            var flash=Render(fury.gameObject,.578f,.64f,.033f,.051f,true);
            var orb=Render(Charms.NativePane,.605f,.66f,.01f,.02f);
            link.charmObject=fury.gameObject;link.newOrb=orb.gameObject;Charms.NativeBoards=new[]{link,royalLink};
            var selectable=Render(board,.2f,.55f,.04f,.06f);
            var wide=Render(Charms.NativePane,.05f,.16f,.90f,.025f);
            var cursor=Render(Charms.NativePane,.65f,.23f,.02f,.02f);cursor.transform.name="Cursor";
            Time.frameCount++;RoleUI.BeforeCamera(camera);
            Check(summary.forceRenderingOff&&feather.forceRenderingOff,"Native summary and feather-edge sprites hidden before rendering");
            Check(selectable.forceRenderingOff==false&&wide.forceRenderingOff==false&&cursor.forceRenderingOff==false,"Catalog, ornaments and separately-owned cursor preserved");
            Check(!fury.forceRenderingOff&&!royal.forceRenderingOff&&!orb.forceRenderingOff&&flash.forceRenderingOff,"Sibling-linked Fury, royal charm, flash and new orb retain native render state");
            var spawned=Render(Charms.NativePane,.21f,.24f,.02f,.02f);Time.frameCount++;RoleUI.BeforeCamera(camera);
            Check(spawned.forceRenderingOff,"New native sprite cannot leak during a half-second probe delay");
            summary.transform.parent=board.transform;Time.frameCount++;RoleUI.BeforeCamera(camera);
            Check(!summary.forceRenderingOff,"Reparenting into the selectable grid restores the sprite");
            summary.transform.parent=Charms.NativePane.transform;Time.frameCount++;RoleUI.BeforeCamera(camera);
            Check(summary.forceRenderingOff,"Returning to replaced summary reacquires only its own render state");
            var outside=new GameObject();spawned.transform.parent=outside.transform;Time.frameCount++;RoleUI.BeforeCamera(camera);
            Check(!spawned.forceRenderingOff,"A pooled renderer leaving the pane is immediately restored");
            Charms.NativeMenuOpen=false;RoleUI.BeforeCamera(camera);
            Check(!summary.forceRenderingOff&&!feather.forceRenderingOff&&!spawned.forceRenderingOff&&description.forceRenderingOff,"Exit restores each original rendering state");
            Charms.NativeMenuOpen=true;Time.frameCount++;RoleUI.BeforeCamera(camera);RoleUI.Release();
            Check(!summary.forceRenderingOff&&!spawned.forceRenderingOff,"Shutdown restores the inventory");
        }
    }
#if BACKDROP
    static void Backdrop()
    {
        Color old=new Color(.2f,.3f,.4f,.5f);GUI.color=old;GUI.Reset();
        CharmMenuBackdrop.Draw(new Rect(100,100,300,200),900,new Color(.02f,.02f,.02f,.6f));
        Check(Near(GUI.color.a,old.a)&&Near(GUI.color.r,old.r),"Backdrop restores GUI.color");
        var opaque=GUI.Paints.Single(p=>p.texture=="white");Check(opaque.color[3]==1&&opaque.rect.SequenceEqual(new float[]{100,100,300,200}),"Whole protected interior remains fully opaque");
        var tex=Texture2D.Created.Last(t=>t.name=="Local8.CharmMenuBackdrop");
        Check(GUI.Paints.Count(p=>p.texture=="Local8.CharmMenuBackdrop")==1&&GUI.Paints.All(p=>p.uv==null),"Feather is one full texture without slice joins");
        Check(tex.Pixels[0].a==0&&tex.Pixels[(tex.height/2)*tex.width].a==0,"Feather reaches transparency at outside edge");
        Check(tex.Pixels[(tex.height/2)*tex.width+6].a>0&&tex.Pixels[(tex.height/2)*tex.width+6].a<1,"Feather has intermediate opacity");
        for(int x=12;x<tex.width-12;x++)
            Check(tex.Pixels[(tex.height/2)*tex.width+x].a==1,"No internal seam along the panel center");
        for(int x=20;x<tex.width-20;x++)
            Check(Near(tex.Pixels[6*tex.width+x].a,tex.Pixels[6*tex.width+20].a),"Top feather is continuous along its entire straight edge");
        int allocations=Texture2D.Allocations;for(int i=0;i<100;i++)CharmMenuBackdrop.Draw(new Rect(100,100,300,200),900,Color.black);
        Check(allocations==Texture2D.Allocations,"Backdrop surface is reused");
        for(int i=0;i<12;i++)CharmMenuBackdrop.Draw(new Rect(100,100,300+i*11,200+i*7),900,Color.black);
        Check(Texture2D.Created.Count(t=>t.name=="Local8.CharmMenuBackdrop"&&!t.Destroyed)<=4,"Resolution/layout changes retain at most four feather surfaces");
        GUI.ThrowTexture=true;try{CharmMenuBackdrop.Draw(new Rect(100,100,300,200),900,Color.black);}catch(InvalidOperationException){}finally{GUI.ThrowTexture=false;}
        Check(Near(GUI.color.a,old.a)&&Near(GUI.color.r,old.r),"Drawing failure restores GUI.color");
        CharmMenuBackdrop.Release();Check(tex.Destroyed,"Shutdown releases feather surface");
    }
#endif
    static void Descriptions()
    {
        foreach(int players in new[]{2,4,5,8})foreach(bool bench in new[]{false,true})foreach(int count in new[]{1,7,11,16})
        {
            var session=Party(players);var p=session.Players[0];p.Editable=bench;p.Charms.Order.Clear();p.Charms.Order.AddRange(Enumerable.Range(1,count));
            Rect area=CharmMenuLayout.Card(CharmMenuLayout.Cards(1600,900,players),0,players);
            string effect="Collects loose currency and brings it to the player.";
            string lore=string.Join(" ",Enumerable.Repeat("An old legend remembered by travellers.",7));
            Charms.DescriptionOverride=effect+"\n\n"+lore;GUI.Reset();CharmCardUI.Draw(p,session.Data,area);
            var desc=GUI.Paints.Single(x=>x.kind=="label"&&x.tooltip==Charms.DescriptionOverride);
            Check(desc.text.StartsWith(effect),"Gameplay description loses priority to lore");
            var measured=new GUIStyle{fontSize=desc.size,wordWrap=true};
            Check(measured.CalcHeight(new GUIContent(desc.text),desc.rect[2])<=desc.rect[3]+.01f,"Description remains clipped");
            var notches=GUI.Paints.Single(x=>x.kind=="label"&&x.text.StartsWith("charms.notches"));
            Check(notches.rect[1]>=area.yMax-38.1f&&desc.rect[1]+desc.rect[3]<=notches.rect[1],"Lowered notches cover description");
            Charms.DescriptionOverride=null;
        }
        foreach(int id in new[]{3,6,7,13,19,20,27,28,36,40,81})
        {
            string lore=string.Join(" ",Enumerable.Repeat("Ancient lore.",35)),effect="Increases the strength of the equipped player.";
            string full=id==81?effect+"\n\n"+lore:lore+"\n\n"+effect;
            var style=new GUIStyle{wordWrap=true};var rect=new Rect(0,0,180,45);
            string shown=CharmDescriptionLayout.FitText(id,full,rect,style,11);
            Check(shown.StartsWith(effect),"Native lore-first/custom effect-first description loses gameplay paragraph");
            Check(style.CalcHeight(new GUIContent(shown),rect.width)<=rect.height,"Prioritized native effect is clipped");
        }
        Check(CharmDescriptionLayout.Text(0,"<b>Effect</b><br><br>Lore")=="Effect\n\nLore","Native/mod markup normalization loses text");
        Charms.DescriptionOverride=null;
    }
    static int Main(string[] args)
    {
        Layout(args.Length>0?args[0]:null);Descriptions();Ownership();Visibility();Hidden();EquippedNavigation();CatalogTint();
#if BACKDROP
        Backdrop();
#endif
        Console.WriteLine((failures==0?"PASS ":"FAILED ")+checks+" native charm-menu checks; failures="+failures+". Native Unity gameplay/GPU remain untested.");
        return failures==0?0:1;
    }
}

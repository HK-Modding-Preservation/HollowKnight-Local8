using System;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class PlayerProfilesUI
{
    private static PlayerSlot editing;
    private static string name="";
    private static bool showSaved,confirmLeave;
    internal static void Draw(CoopSession session,PlayerSlot player,float width,ref float row)
    {
        var active=PlayerProfiles.Find(player.ProfileId);
        if(editing!=player){editing=player;name=active?.Name??"";showSaved=false;confirmLeave=false;}
        MenuTheme.Section(UiLocalization.Get("profile.title")+" · P"+(player.Index+1),width,ref row);
        string current=active==null?UiLocalization.Get("profile.guest"):UiLocalization.Get("profile.current")+active.Name;
        row+=MenuTheme.Paragraph(5,row,width-10,current,player.Color,15)+7;
        string help=UiLocalization.Get(active==null?"profile.create_help":"profile.autosave_help");
        row+=MenuTheme.Paragraph(5,row,width-10,help,MenuTheme.Muted)+12;
        MenuTheme.Text(new Rect(5,row,width-10,22),UiLocalization.Get("profile.name"),MenuTheme.Muted,13);row+=24;
        bool narrow=width<470;float fieldWidth=narrow?width-10:width-204;
        Rect field=new Rect(5,row,fieldWidth,36);
        MenuTheme.Fill(field,new Color(.05f,.06f,.08f));MenuTheme.Border(field,MenuTheme.Muted);
        GUI.SetNextControlName("Local8.Profile.Name");
        name=GUI.TextField(new Rect(field.x+9,field.y+4,field.width-18,28),name,24,MenuTheme.Label);
        if(name.Length==0)MenuTheme.Text(new Rect(field.x+10,field.y+5,field.width-20,25),UiLocalization.Get("profile.name_hint"),MenuTheme.Muted,13);
        Rect save=narrow?new Rect(5,row+43,width-10,38):new Rect(width-190,row,185,36);
        bool enabled=GUI.enabled;
        try
        {
            GUI.enabled=enabled&&AdventureStatisticsRules.Name(name).Length>0;
            string label=UiLocalization.Get(active==null?"profile.create":"profile.save");
            if(MenuTheme.Button(save,new GUIContent(label,UiLocalization.Get("profile.save_tip")),MenuTheme.ButtonStyle))
            {PlayerProfiles.Save(player,name);GUI.FocusControl(null);confirmLeave=false;}
        }
        finally{GUI.enabled=enabled;}
        row+=narrow?89:46;
        var profiles=PlayerProfiles.All;
        if(profiles.Length>0)
        {
            if(MenuTheme.Fold(new Rect(5,row,width-10,38),UiLocalization.Get("profile.load"),showSaved))showSaved=!showSaved;
            row+=46;
            if(showSaved)
            {
                float inset=MenuTheme.FoldInset(width),start=row;
                bool canLoad=PlayerProfiles.CanLoad(player,session);
                if(!canLoad)row+=MenuTheme.Paragraph(5+inset,row,width-inset-10,UiLocalization.Get("profile.load_at_bench"),MenuTheme.Accent)+10;
                int columns=width-inset>=540?2:1;float cell=(width-inset-10)/columns;
                for(int i=0;i<profiles.Length;i++)
                {
                    var profile=profiles[i];bool busy=PlayerProfiles.Busy(profile,session,player),loaded=profile.Id==player.ProfileId;
                    string label=profile.Name;
                    string tip=UiLocalization.Get("profile.load_tip");
                    if(busy){label+=" · "+UiLocalization.Get("profile.busy_label");tip=UiLocalization.Get("profile.in_use");}
                    else if(loaded)label+=" · "+UiLocalization.Get("profile.active_label");
                    else if(!canLoad)tip=UiLocalization.Get("profile.load_at_bench");
                    Rect r=new Rect(5+inset+i%columns*cell,row+i/columns*43,cell-6,36);
                    try
                    {
                        GUI.enabled=enabled&&!busy&&(loaded||canLoad);
                        if(MenuTheme.Choice(r,new GUIContent(label,tip),loaded)&&PlayerProfiles.Load(player,profile))
                        {name=profile.Name;confirmLeave=false;GUI.FocusControl(null);}
                    }
                    finally{GUI.enabled=enabled;}
                }
                row+=Mathf.CeilToInt(profiles.Length/(float)columns)*43+4;
                MenuTheme.FoldGuide(start,row,inset);row+=5;
            }
        }
        if(active!=null)
        {
            row+=7;
            if(!confirmLeave)
            {
                if(MenuTheme.Button(new Rect(5,row,width-10,36),new GUIContent(UiLocalization.Get("profile.new"),UiLocalization.Get("profile.new_tip")),MenuTheme.ButtonStyle))confirmLeave=true;
                row+=43;
            }
            else
            {
                row+=MenuTheme.Paragraph(5,row,width-10,UiLocalization.Get("profile.leave_help"),MenuTheme.Muted)+8;
                float half=(width-16)*.5f;
                if(MenuTheme.Button(new Rect(5,row,half,38),UiLocalization.Get("profile.confirm_leave"),MenuTheme.ButtonStyle))
                {PlayerProfiles.Detach(player);name="";confirmLeave=false;GUI.FocusControl(null);}
                if(MenuTheme.Button(new Rect(11+half,row,half,38),UiLocalization.Get("common.cancel"),MenuTheme.ButtonStyle))confirmLeave=false;
                row+=46;
            }
        }
    }
    internal static void Reset(){editing=null;name="";showSaved=false;confirmLeave=false;}
}

using System;
using System.Globalization;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class AdventureStatsUI
{
    private static readonly string[] keys={"kills","assists","damage","health","healing","revives","deaths","time"};
    internal static void Draw(CoopSession session,float width,ref float row)
    {
        MenuTheme.Section(UiLocalization.Get("stats.title"),width,ref row);
        MenuTheme.Text(new Rect(5,row,width-10,42),UiLocalization.Get("stats.tip"),MenuTheme.Muted,13);row+=48;
        int columns=width>=620?4:2;float cell=(width-30)/columns;
        foreach(var p in session.Players)
        {
            var a=PlayerProfiles.Stats(p);float height=43+Mathf.CeilToInt(keys.Length/(float)columns)*70;
            Rect r=new Rect(5,row,width-10,height);
            MenuTheme.Fill(r,new Color(.025f,.032f,.048f));MenuTheme.Border(r,new Color(p.Color.r,p.Color.g,p.Color.b,.45f));
            string label="P"+(p.Index+1)+(string.IsNullOrEmpty(p.ProfileId)?"":"  ·  "+PlayerProfiles.Display(p));
            MenuTheme.Text(new Rect(r.x+12,r.y+7,r.width-24,27),label,p.Color,19);
            long[] values={a.EnemiesKilled,a.Assists,a.DamageDealt,a.HealthRecovered,a.HealingGiven,a.Revives,a.Deaths,0};
            for(int i=0;i<keys.Length;i++)
            {
                float x=r.x+12+i%columns*cell,y=r.y+38+i/columns*70;
                MenuTheme.Text(new Rect(x,y,cell-10,34),UiLocalization.Get("stats."+keys[i]),MenuTheme.Muted,13);
                string value=i==7?Duration(a.SecondsPlayed):values[i].ToString("N0",CultureInfo.CurrentCulture);
                MenuTheme.Text(new Rect(x,y+37,cell-10,24),value,MenuTheme.Paper,18);
            }
            row+=height+12;
        }
    }
    internal static string Duration(double seconds)
    {
        long total=(long)Math.Max(0,Math.Min(1e12,seconds));
        return (total/3600).ToString("00")+":"+(total/60%60).ToString("00")+":"+(total%60).ToString("00");
    }
}

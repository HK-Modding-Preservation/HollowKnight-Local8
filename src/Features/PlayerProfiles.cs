using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class PlayerProfiles
{
    private static readonly MethodInfo saveSettings=typeof(Modding.Mod).GetMethod("SaveGlobalSettings",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,Type.EmptyTypes,null);
    private static bool dirty,writing;
    private static float nextSave;
    internal static bool Applying { get; private set; }
    internal static PlayerProfileData[] All => Local8Mod.Settings.PlayerProfiles??Array.Empty<PlayerProfileData>();
    internal static PlayerProfileData Find(string id) => string.IsNullOrEmpty(id)?null:Array.Find(All,p=>p!=null&&p.Id==id);
    internal static string Display(PlayerSlot p) => Find(p?.ProfileId)?.Name??("P"+(p.Index+1));

    internal static void Normalize()
    {
        var valid=new List<PlayerProfileData>();var ids=new HashSet<string>(StringComparer.Ordinal);
        foreach(var profile in All)
        {
            if(profile==null||string.IsNullOrEmpty(profile.Id)||profile.Id.Length>64||!ids.Add(profile.Id))continue;
            profile.Name=AdventureStatisticsRules.Name(profile.Name);
            if(profile.Name.Length==0)continue;
            if(!RoleCatalog.Known(profile.RoleId))profile.RoleId="none";
            profile.Charms=(profile.Charms??Array.Empty<int>()).Where(id=>id>0).Distinct().Take(256).ToArray();
            if(profile.Adventure==null)profile.Adventure=new PlayerAdventureStatistics();profile.Adventure.Normalize();
            if(profile.KeyboardBindings!=null&&profile.KeyboardBindings.Length>131072)profile.KeyboardBindings=null;
            valid.Add(profile);if(valid.Count==128)break;
        }
        Local8Mod.Settings.PlayerProfiles=valid.ToArray();
    }
    internal static PlayerAdventureStatistics Stats(PlayerSlot p)
    {
        if(p.Adventure==null)
        {
            var saved=Local8Mod.Save.AdventureStats;
            p.Adventure=saved!=null&&p.Index>=0&&p.Index<saved.Length&&saved[p.Index]!=null?saved[p.Index].Copy():new PlayerAdventureStatistics();
            p.Adventure.Normalize();
        }
        return p.Adventure;
    }
    internal static void Dirty() => dirty=true;
    internal static bool Busy(PlayerProfileData profile,CoopSession session,PlayerSlot except)
    {
        if(profile==null||session==null)return false;
        return session.Players.Any(p=>p!=except&&p.ProfileId==profile.Id);
    }
    internal static void Capture(CoopSession session)
    {
        if(session==null)return;
        if(Local8Mod.Save.AdventureStats==null||Local8Mod.Save.AdventureStats.Length!=8)Local8Mod.Save.AdventureStats=new PlayerAdventureStatistics[8];
        foreach(var p in session.Players)
        {
            Local8Mod.Save.AdventureStats[p.Index]=Stats(p).Copy();
            var profile=Find(p.ProfileId);if(profile==null)continue;
            profile.Adventure=Stats(p).Copy();
            // Arena overrides must never overwrite an adventurer's equipment.
            if(PvpCharms.Active||PvpArena.Active||PvpMatch.Running)continue;
            // Keep preferences for locked or temporarily absent mod charms. Loading
            // a different adventure must not erase a person's future equipment.
            var unavailable=(profile.Charms??Array.Empty<int>()).Where(id=>!CharmExtensions.Owned(session.Data,id));
            profile.Charms=p.Charms.Order.Where(p.Charms.IsEquipped).Concat(p.Charms.Ids()).Concat(unavailable).Distinct().Take(256).ToArray();
            string role=Local8Mod.Save.RoleIds!=null&&p.Index<Local8Mod.Save.RoleIds.Length?Local8Mod.Save.RoleIds[p.Index]:null;
            profile.RoleId=RoleCatalog.Known(role)?role:"none";
            profile.Color=ColorUtility.ToHtmlStringRGB(p.Color);profile.SkinId=p.SkinId;
            if(p.Index>0&&Controls.IsKeyboard(p.Device)&&p.Actions!=null)profile.KeyboardBindings=p.Actions.Save();
        }
    }
    internal static bool Save(PlayerSlot p,string name)
    {
        var session=Plugin.Self?.Session;
        name=AdventureStatisticsRules.Name(name);
        if(p==null||session==null||name.Length==0)return false;
        var existing=Find(p.ProfileId);
        // A name is user-facing, but avoid ambiguous duplicate entries in the picker.
        if(All.Any(item=>item!=existing&&string.Equals(item.Name,name,StringComparison.OrdinalIgnoreCase)))
        {Plugin.Self.Notice(UiLocalization.Get("profile.name_used"));return false;}
        if(existing==null)
        {
            if(All.Length>=128){Plugin.Self.Notice(UiLocalization.Get("profile.limit"));return false;}
            existing=new PlayerProfileData{Id=Guid.NewGuid().ToString("N"),Name=name};
            Local8Mod.Settings.PlayerProfiles=All.Concat(new[]{existing}).ToArray();p.ProfileId=existing.Id;
        }
        existing.Name=name;Capture(session);Dirty();bool saved=Flush(session);
        Plugin.Self.Notice(UiLocalization.Get(saved?"profile.saved":"profile.save_failed"));return saved;
    }
    internal static void Detach(PlayerSlot p)
    {
        if(p==null)return;Capture(Plugin.Self?.Session);
        p.ProfileId=null;p.Adventure=new PlayerAdventureStatistics();p.ProfileLoadedThisJoin=true;
        // Leave equipment and controls in place. This creates a new identity only.
        Dirty();Flush(Plugin.Self?.Session);
    }
    internal static bool CanLoad(PlayerSlot p,CoopSession s)
    {
        if(p==null||s==null||!p.Alive||!p.Ready||p.Hazard||p.SpawnPending||p.ArenaTransfer||(p.InputBlocked&&!Plugin.Self.Panel)||!p.Hero||p.Hero.cState.transitioning||(GameManager.instance&&GameManager.instance.IsLoadingSceneTransition)||PlayerContext.Current!=null||PvpMatch.Running||PvpArena.Active||PvpCharms.Active||s.TeamWipe||ScriptedParty.Active||CoopEnding.Active||PickupCard.Owner!=null||InteractionRouter.ActivePlayer!=null||ShopMenuRouting.MenuVisible||StagMenuRouting.HasOwner)return false;
        return Charms.CanEdit(p)||(!p.ProfileLoadedThisJoin&&Time.unscaledTime-p.LifeStartedAt<=30f);
    }
    internal static bool Load(PlayerSlot p,PlayerProfileData profile)
    {
        var s=Plugin.Self?.Session;
        if(profile==null||p==null||s==null||Find(profile.Id)!=profile)return false;
        if(Busy(profile,s,p)){Plugin.Self.Notice(UiLocalization.Get("profile.in_use"));return false;}
        if(!CanLoad(p,s)){Plugin.Self.Notice(UiLocalization.Get("profile.load_at_bench"));return false;}
        if(p.ProfileId==profile.Id)return true;
        Capture(s);
        // Detached copies prevent one slot's snapshots/settings serialization from
        // silently sharing mutable statistics with a different player.
        var statistics=(profile.Adventure??new PlayerAdventureStatistics()).Copy();statistics.Normalize();
        int[] charms=(int[])(profile.Charms??Array.Empty<int>()).Clone();
        string role=RoleCatalog.Known(profile.RoleId)?profile.RoleId:"none";
        string keys=profile.KeyboardBindings;Color color;
        string previousProfile=p.ProfileId,previousRole=Local8Mod.Save.RoleIds?[p.Index],previousSkin=p.SkinId;
        var previousStats=p.Adventure;var previousCharms=p.Charms;var previousVitals=p.Vitals.Copy();Color previousColor=p.Color;
        bool previouslyLoaded=p.ProfileLoadedThisJoin;
        Applying=true;
        try
        {
            p.ProfileId=profile.Id;p.Adventure=statistics;p.ProfileLoadedThisJoin=true;
            if(ColorUtility.TryParseHtmlString("#"+profile.Color,out color))
            {p.Color=color;Local8Mod.Settings.Colors[p.Index]=ColorUtility.ToHtmlStringRGB(color);}
            SkinBridge.Release(p);p.SkinId=profile.SkinId;Local8Mod.Settings.SkinIds[p.Index]=p.SkinId;
            using(PlayerContext.Enter(p))
            {
                // Initialize only equips items actually owned in the current game
                // and respects notch/overcharm limits. No story/geo/vital grants.
                p.Charms=new CharmLoadout();p.Charms.Initialize(s.Data,charms);p.Charms.Write(s.Data);
                RoleSystem.ApplyProfile(p,role);
                Charms.UpdateMaximum(p,s.Data,false);p.Vitals.Write(s.Data);
                p.Hero.CharmUpdate();p.Capture(s.Data);
                p.Vitals.Health=Math.Min(previousVitals.Health,p.CurrentMaxHealth);
                p.Vitals.Blue=Math.Min(previousVitals.Blue,p.Vitals.Blue);
                p.Vitals.Soul=previousVitals.Soul;p.Vitals.Reserve=previousVitals.Reserve;p.Vitals.Write(s.Data);
            }
        }
        catch(Exception e)
        {
            p.ProfileId=previousProfile;p.Adventure=previousStats;p.Charms=previousCharms;p.Vitals=previousVitals;
            p.Color=previousColor;p.SkinId=previousSkin;p.ProfileLoadedThisJoin=previouslyLoaded;
            Local8Mod.Settings.Colors[p.Index]=ColorUtility.ToHtmlStringRGB(previousColor);Local8Mod.Settings.SkinIds[p.Index]=previousSkin;
            try{using(PlayerContext.Enter(p)){RoleSystem.ApplyProfile(p,previousRole??"none");p.Apply(s.Data);p.Hero.CharmUpdate();p.Vitals=previousVitals;p.Vitals.Write(s.Data);}}
            catch(Exception rollback){Diagnostics.Throttled("PROFILE restore",rollback);}
            Diagnostics.Throttled("PROFILE load",e);Plugin.Self.Notice(UiLocalization.Get("profile.save_failed"));return false;
        }
        finally{Applying=false;}
        SummonRouting.Refresh(p);Charms.Save(s);
        bool keysConflict=false;
        if(p.Index>0&&Controls.IsKeyboard(p.Device)&&!string.IsNullOrEmpty(keys))
        {
            // A saved profile can move to another keyboard slot. Detect conflicts
            // before applying it, rather than giving one keyboard two heroes.
            if(ProfileKeyboard.CanApply(p,keys,s))ProfileKeyboard.Apply(p,keys);
            else keysConflict=true;
        }
        Dirty();bool persisted=Flush(s);
        Plugin.Self.Notice(UiLocalization.Get(!persisted?"profile.save_failed":keysConflict?"profile.keys_conflict":"profile.loaded"));return true;
    }
    internal static bool Flush(CoopSession s)
    {
        if(writing)return false;
        Capture(s);
        if(!dirty)return true;
        try
        {
            writing=true;
            if(Local8Mod.Instance==null||saveSettings==null)return false;
            saveSettings.Invoke(Local8Mod.Instance,null);dirty=false;nextSave=Time.unscaledTime+30f;return true;
        }
        catch(Exception e){Diagnostics.Throttled("PROFILES save",e);nextSave=Time.unscaledTime+5f;return false;}
        finally{writing=false;}
    }
    internal static void Tick(Local8Runtime runtime)
    {
        var s=runtime.Session;
        if(s==null)return;
        var gm=GameManager.instance;
        if(runtime.Enabled.Value&&gm&&s.Gameplay&&!gm.isPaused&&!gm.IsLoadingSceneTransition&&!runtime.Panel&&!PvpMatch.Running&&!PvpArena.Active)
        {
            foreach(var p in s.Players)if(p.Ready&&p.Alive&&p.Connected)
            {var stats=Stats(p);stats.SecondsPlayed=Math.Min(1e12,stats.SecondsPlayed+Mathf.Clamp(Time.unscaledDeltaTime,0,.25f));dirty=true;}
        }
        if(dirty&&Time.unscaledTime>=nextSave&&PlayerContext.Current==null)Flush(s);
    }
}

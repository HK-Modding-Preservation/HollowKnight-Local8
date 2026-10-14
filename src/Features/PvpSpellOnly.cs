using System;
using UnityEngine;

namespace KO.HollowKnight8;

// A duel owns a fixed rule snapshot. Never redirect the recipients through a
// world interaction or change their saved bindings/ordinary adventure resources.
internal static class PvpSpellOnly
{
    internal const int DefaultRegeneration=11;
    private static readonly PlayerSlot[] players=new PlayerSlot[8];
    private static readonly double[] fractions=new double[8];
    private static readonly string[] charmSources={"thorn","spore","dung","hatchling","grimm","shield","sharp shadow"};
    private static CoopSession session;
    private static bool enabled;
    private static int regeneration;

    internal static int Rate(int value)=>Math.Max(1,Math.Min(99,value));
    internal static bool Active=>enabled&&Local8Mod.Settings.PvpMode==2&&PvpMatch.Running;
    private static bool Member(PlayerSlot p)=>p!=null&&p.Index>=0&&p.Index<players.Length&&players[p.Index]==p;

    internal static void Begin(CoopSession s)
    {
        End();if(!Local8Mod.Settings.PvpSpellsOnly||s==null)return;
        session=s;enabled=true;regeneration=Rate(Local8Mod.Settings.PvpSoulRegeneration);
        foreach(var p in s.Players)if(p.Index>=0&&p.Index<players.Length)players[p.Index]=p;
    }
    internal static void Round()=>Array.Clear(fractions,0,fractions.Length);
    internal static void End()
    {
        enabled=false;session=null;regeneration=0;
        Array.Clear(players,0,players.Length);Round();
    }
    internal static string StartIssue(CoopSession s)
    {
        if(!Local8Mod.Settings.PvpSpellsOnly)return "";
        // In an adventure-room duel, retain the game's unlocked spells. Arena
        // duels can supply temporary spell levels through their existing rules.
        if(s.Data.fireballLevel<=0&&s.Data.quakeLevel<=0&&s.Data.screamLevel<=0)return UiLocalization.Get("pvp.spells_required");
        foreach(var p in s.Players)if(p.Vitals.MaxSoul<33)return UiLocalization.Get("pvp.spell_capacity");
        return "";
    }
    internal static void ArenaRules(PvpArenaOptions rules,CoopSession s)
    {
        if(!Local8Mod.Settings.PvpSpellsOnly)return;
        // Prevent an arena's Off/1-point capacity settings from making a spell
        // duel unwinnable. Modify the live copy, never the saved arena options.
        rules.SoulCapacity=Math.Max(33,rules.SoulCapacity);
        if(rules.SpellLevel==0||(rules.SpellLevel<0&&s.Data.fireballLevel<=0&&s.Data.quakeLevel<=0&&s.Data.screamLevel<=0))rules.SpellLevel=1;
    }
    internal static void Input(PlayerSlot p)
    {
        if(!Active||!PvpMatch.CanFight||!Member(p)||p.Actions==null||!p.Alive||!p.Ready||!p.Hero||p.Hero.controlReqlinquished||p.Hero.cState.transitioning||!session.Gameplay||Charms.NativeMenuOpen||RoleSystem.JoiningMenu||ShopMenuRouting.MenuVisible||StagMenuRouting.HasOwner||Plugin.Self==null||Plugin.Self.Panel||!GameManager.instance||GameManager.instance.isPaused)return;
        p.Actions.attack.ClearInputState();
        p.Actions.dreamNail.ClearInputState();
        p.Actions.superDash.ClearInputState();
    }
    internal static bool Allows(PvpCombat.Attack attack)
    {
        if(!Active)return true;
        if(attack==null||attack.Kind!=PvpKind.Spell||!attack.Collider)return false;
        // Some offensive charms use the native SPELL attack type. They are
        // still charm attacks, even when no familiar tag is present.
        var hero=attack.Owner!=null&&attack.Owner.Hero?attack.Owner.Hero.transform:null;
        for(var t=attack.Collider.transform;t&&t!=hero;t=t.parent)
            foreach(string charm in charmSources)
                if(t.name.IndexOf(charm,StringComparison.OrdinalIgnoreCase)>=0)return false;
        return true;
    }
    internal static void Tick(Local8Runtime runtime)
    {
        if(!Active||runtime==null||runtime.Session!=session||!runtime.Enabled.Value||!PvpMatch.CanFight||runtime.Panel)return;
        var gm=GameManager.instance;
        if(!session.Active||!session.Gameplay||session.TeamWipe||!gm||gm.isPaused||gm.IsLoadingSceneTransition||PlayerContext.Current!=null)return;
        float delta=Time.deltaTime;
        if(delta<=0||float.IsNaN(delta)||float.IsInfinity(delta))return;
        for(int i=0;i<players.Length;i++)
        {
            var p=players[i];
            if(p==null||!session.Players.Contains(p)||!p.Alive||!p.Ready||!p.Connected||!p.Hero||p.Hazard||p.ArenaTransfer||p.SpawnPending||p.Vitals.AtBench||p.Hero.cState.transitioning)
            {fractions[i]=0;continue;}
            int maximum=Math.Max(0,p.Vitals.MaxSoul);
            if(p.Vitals.Soul>=maximum){fractions[i]=0;continue;}
            double gain=fractions[i]+regeneration*Math.Min(delta,1f);
            int amount=(int)Math.Min(Math.Floor(gain),maximum-(long)p.Vitals.Soul);
            fractions[i]=gain-Math.Floor(gain);
            if(amount>0){p.Vitals.Soul+=amount;session.Commit(p);}
            if(p.Vitals.Soul>=maximum)fractions[i]=0;
        }
    }
}

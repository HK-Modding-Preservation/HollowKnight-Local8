using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class AdventureStats
{
    private sealed class Encounter
    {
        internal readonly Dictionary<PlayerSlot,float> Contributors=new Dictionary<PlayerSlot,float>();
        internal PlayerSlot Last;
        internal bool Counted;
        internal int Depth;
        internal long AccountedDamage;
    }
    private static ConditionalWeakTable<HealthManager,Encounter> encounters=new ConditionalWeakTable<HealthManager,Encounter>();
    private static bool installed;
    private static CoopSession Session => Plugin.Self ? Plugin.Self.Session : null;
    internal static bool Recording => Session!=null && !PlayerProfiles.Applying && Plugin.Self.Enabled.Value && !PvpMatch.Running && !PvpArena.Active;

    internal static void Install()
    {
        if(installed)return;installed=true;
        On.HealthManager.Hit+=Hit;On.HealthManager.ApplyExtraDamage+=ExtraDamage;
        On.HealthManager.Die+=Die;
        On.HeroController.AddHealth+=HeroHeal;On.PlayerData.AddHealth+=DataHeal;
    }
    internal static void Uninstall()
    {
        if(!installed)return;installed=false;
        On.HealthManager.Hit-=Hit;On.HealthManager.ApplyExtraDamage-=ExtraDamage;
        On.HealthManager.Die-=Die;
        On.HeroController.AddHealth-=HeroHeal;On.PlayerData.AddHealth-=DataHeal;
        ClearRoom();
    }
    internal static void ClearRoom() => encounters=new ConditionalWeakTable<HealthManager,Encounter>();
    private static bool Person(PlayerSlot p) => Recording && p!=null && Session.Players.Contains(p);
    private static Encounter Begin(HealthManager enemy,PlayerSlot p)
    {
        if(!Person(p)||!enemy||enemy.hp<=0||enemy.GetIsDead()||enemy.GetComponent<LocalShade>())return null;
        Encounter e=encounters.GetValue(enemy,key=>new Encounter());
        if(e.Counted){encounters.Remove(enemy);e=encounters.GetValue(enemy,key=>new Encounter());}
        e.Depth++;return e;
    }
    private static void Finish(HealthManager enemy,Encounter e,PlayerSlot p,int hp,long accounted,bool completed)
    {
        if(e==null)return;
        e.Depth--;
        if(!completed)return;
        // HP and GetIsDead are managed fields even if Die destroyed the Unity object.
        int removed=AdventureStatisticsRules.Damage(hp,enemy.hp);
        int damage=(int)Math.Max(0,removed-(e.AccountedDamage-accounted));
        if(damage>0)
        {
            e.AccountedDamage=AdventureStatisticsRules.Add(e.AccountedDamage,damage);
            var stats=PlayerProfiles.Stats(p);stats.DamageDealt=AdventureStatisticsRules.Add(stats.DamageDealt,damage);
            e.Contributors[p]=Time.unscaledTime;e.Last=p;PlayerProfiles.Dirty();
        }
        if(e.Depth==0 && enemy.GetIsDead())CountKill(e);
    }
    private static void CountKill(Encounter e)
    {
        if(e.Counted||e.Last==null||!Person(e.Last))return;
        if(!e.Contributors.TryGetValue(e.Last,out float when)||Time.unscaledTime-when>30)return;
        e.Counted=true;
        var winner=PlayerProfiles.Stats(e.Last);winner.EnemiesKilled=AdventureStatisticsRules.Add(winner.EnemiesKilled,1);
        foreach(var pair in e.Contributors)
            if(pair.Key!=e.Last&&Person(pair.Key)&&Time.unscaledTime-pair.Value<=30)
            {var stats=PlayerProfiles.Stats(pair.Key);stats.Assists=AdventureStatisticsRules.Add(stats.Assists,1);}
        PlayerProfiles.Dirty();
    }
    private static void Hit(On.HealthManager.orig_Hit orig,HealthManager enemy,HitInstance hit)
    {
        var p=Session?.Resolve(hit.Source)??PlayerContext.Current;
        Encounter e=Begin(enemy,p);int hp=e==null?0:enemy.hp;long accounted=e?.AccountedDamage??0;bool completed=false;
        try{orig(enemy,hit);completed=true;}finally{Finish(enemy,e,p,hp,accounted,completed);}
    }
    private static void ExtraDamage(On.HealthManager.orig_ApplyExtraDamage orig,HealthManager enemy,int amount)
    {
        var p=PlayerContext.Current;Encounter e=Begin(enemy,p);int hp=e==null?0:enemy.hp;long accounted=e?.AccountedDamage??0;bool completed=false;
        try{orig(enemy,amount);completed=true;}finally{Finish(enemy,e,p,hp,accounted,completed);}
    }
    private static void Die(On.HealthManager.orig_Die orig,HealthManager enemy,float? direction,AttackTypes type,bool evade)
    {
        orig(enemy,direction,type,evade);
        if(Recording && !ReferenceEquals(enemy,null) && enemy.GetIsDead() && encounters.TryGetValue(enemy,out var e) && e.Depth==0)CountKill(e);
    }
    private static void HeroHeal(On.HeroController.orig_AddHealth orig,HeroController hero,int amount)
    {
        PlayerSlot p=Session?.Resolve(hero);
        if(!Person(p)){orig(hero,amount);return;}
        using(PlayerContext.Enter(p))
        {
            int hp=Session.Data.health;long already=StatsRecovered(p);
            orig(hero,amount);
            long nested=StatsRecovered(p)-already;
            Recovered(p,Math.Max(0,AdventureStatisticsRules.Healing(hp,Session.Data.health,p.CurrentMaxHealth)-(int)Math.Min(int.MaxValue,nested)));
        }
    }
    private static long StatsRecovered(PlayerSlot p) => PlayerProfiles.Stats(p).HealthRecovered;
    private static void DataHeal(On.PlayerData.orig_AddHealth orig,PlayerData data,int amount)
    {
        PlayerSlot p=PlayerContext.Current??Session?.Primary;
        int hp=data.health;orig(data,amount);
        if(Person(p)&&Session.Data==data)Recovered(p,AdventureStatisticsRules.Healing(hp,data.health,p.CurrentMaxHealth));
    }
    private static void Recovered(PlayerSlot p,int amount)
    {
        if(amount<=0||!Person(p)||p.Down)return;
        var stats=PlayerProfiles.Stats(p);stats.HealthRecovered=AdventureStatisticsRules.Add(stats.HealthRecovered,amount);PlayerProfiles.Dirty();
    }
    internal static void Given(PlayerSlot p,int amount)
    {
        if(amount<=0||!Person(p))return;
        var stats=PlayerProfiles.Stats(p);stats.HealingGiven=AdventureStatisticsRules.Add(stats.HealingGiven,amount);PlayerProfiles.Dirty();
    }
    internal static void Down(PlayerSlot p)
    {
        if(!Person(p))return;var stats=PlayerProfiles.Stats(p);stats.Deaths=AdventureStatisticsRules.Add(stats.Deaths,1);PlayerProfiles.Dirty();
    }
    internal static void Revive(PlayerSlot p)
    {
        if(!Person(p))return;var stats=PlayerProfiles.Stats(p);stats.Revives=AdventureStatisticsRules.Add(stats.Revives,1);PlayerProfiles.Dirty();
    }
}

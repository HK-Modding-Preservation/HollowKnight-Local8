using System;

namespace KO.HollowKnight8;

// Modding API settings use public fields. IDs, never names or controller/slot
// numbers, identify a person. These records contain no world/story/vital data.
public sealed class PlayerProfileData
{
    public string Id;
    public string Name;
    public string RoleId = "none";
    public int[] Charms = new int[0];
    public string Color;
    public string SkinId;
    public string KeyboardBindings;
    public PlayerAdventureStatistics Adventure = new PlayerAdventureStatistics();
}

public sealed class PlayerAdventureStatistics
{
    public long EnemiesKilled;
    public long Assists;
    public long DamageDealt;
    public long HealthRecovered;
    public long HealingGiven;
    public long Deaths;
    public long Revives;
    public double SecondsPlayed;

    internal PlayerAdventureStatistics Copy() => (PlayerAdventureStatistics)MemberwiseClone();
    internal void Normalize()
    {
        EnemiesKilled=Math.Max(0,EnemiesKilled);Assists=Math.Max(0,Assists);
        DamageDealt=Math.Max(0,DamageDealt);HealthRecovered=Math.Max(0,HealthRecovered);
        HealingGiven=Math.Max(0,HealingGiven);Deaths=Math.Max(0,Deaths);Revives=Math.Max(0,Revives);
        if(double.IsNaN(SecondsPlayed)||double.IsInfinity(SecondsPlayed)||SecondsPlayed<0)SecondsPlayed=0;
    }
}

internal static class AdventureStatisticsRules
{
    internal static long Add(long total,long amount) => amount<=0 ? Math.Max(0,total) : Math.Max(0,total)>long.MaxValue-amount ? long.MaxValue : Math.Max(0,total)+amount;
    // Count real HP removed, not the requested attack, blocked hits or overkill.
    internal static int Damage(int before,int after) => before<=0 ? 0 : (int)Math.Min(before,Math.Max(0L,(long)before-after));
    internal static int Healing(int before,int after,int maximum) => Math.Max(0,Math.Min(after,maximum)-Math.Max(0,before));
    internal static string Name(string value)
    {
        if(string.IsNullOrWhiteSpace(value))return "";
        var result=new System.Text.StringBuilder();
        foreach(char c in value.Trim())if(!char.IsControl(c)&&c!='<'&&c!='>')
        {if(result.Length>=24)break;result.Append(c);}
        // Do not leave a dangling UTF-16 surrogate at the limit.
        if(result.Length>0&&char.IsHighSurrogate(result[result.Length-1]))result.Length--;
        return result.ToString().Trim();
    }
}

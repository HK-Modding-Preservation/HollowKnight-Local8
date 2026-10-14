using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

// Synchronous frame snapshots. The roster remains independent of mutations
// during actor recovery; living/rescuable lists have separate exact-length
// storage. Arrays are never retained by a coroutine or a gameplay owner.
internal sealed class FramePlayers
{
    private PlayerSlot[] roster = Array.Empty<PlayerSlot>();
    private PlayerSlot[] living = Array.Empty<PlayerSlot>();
    private PlayerSlot[] revivable = Array.Empty<PlayerSlot>();
    private readonly PlayerSlot[] scratch = new PlayerSlot[8];
    private bool actorStarted;

    internal PlayerSlot[] Snapshot(List<PlayerSlot> players)
    {
        actorStarted = false;
        Resize(ref roster, players.Count);
        players.CopyTo(roster);
        return roster;
    }

    internal bool TryStartActor()
    {
        if (actorStarted) return false;
        actorStarted = true;
        return true;
    }

    internal static PlayerSlot FindByIndex(List<PlayerSlot> players, int index)
    {
        if (players != null)
            foreach (PlayerSlot player in players)
                if (player.Index == index) return player;
        return null;
    }

    internal PlayerSlot[] Living(List<PlayerSlot> players)
    {
        int count = 0;
        foreach (PlayerSlot player in players)
            if (player.Alive) scratch[count++] = player;
        return Copy(ref living, count);
    }

    internal PlayerSlot[] Revivable(PlayerSlot[] players)
    {
        int count = 0;
        foreach (PlayerSlot player in players)
            if (!EmergencyWarp.Active(player)) scratch[count++] = player;
        return Copy(ref revivable, count);
    }

    private PlayerSlot[] Copy(ref PlayerSlot[] target, int count)
    {
        Resize(ref target, count);
        Array.Copy(scratch, target, count);
        Array.Clear(scratch, 0, scratch.Length);
        return target;
    }

    private static void Resize(ref PlayerSlot[] target, int count)
    {
        if (target.Length != count)
            target = count == 0 ? Array.Empty<PlayerSlot>() : new PlayerSlot[count];
    }
}

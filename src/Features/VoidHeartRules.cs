using System.Collections.Generic;

namespace KO.HollowKnight8;

internal static class VoidHeartRules
{
    internal static bool Acquired(PlayerData data) => data != null && data.GetInt("royalCharmState") > 3;

    internal static bool Mandatory(PlayerData data) => Acquired(data) && !BossSequenceController.BoundCharms
        && (!PvpCharms.Active || PvpCharms.Allowed);

    internal static int[] Include(PlayerData data, int[] ids)
    {
        if (!Mandatory(data)) return ids;
        if (ids != null) foreach (int id in ids) if (id == 36) return ids;
        var result = new int[(ids == null ? 0 : ids.Length) + 1];
        if (ids != null) System.Array.Copy(ids, result, ids.Length);
        result[result.Length - 1] = 36;
        return result;
    }

    internal static void Ensure(CoopSession session)
    {
        if (session == null || !session.Active || PlayerContext.Current != null || !Mandatory(session.Data)) return;
        bool changed = false;
        foreach (var player in session.Players)
        {
            if (!player.Ready || !player.Alive || !player.Hero || player.Charms.IsEquipped(36)) continue;
            using (PlayerContext.Enter(player))
            {
                var data = session.Data;
                data.SetBool("equippedCharm_36", true);
                var order = new List<int>(data.equippedCharms ?? new List<int>());
                if (!order.Contains(36)) order.Add(36);
                data.equippedCharms = order;
                data.CalculateNotchesUsed();
                data.overcharmed = data.charmSlotsFilled > data.charmSlots;
                player.Hero.CharmUpdate();
                player.Capture(data);
            }
            Diagnostics.Write("VOID HEART restored P" + (player.Index + 1));
            changed = true;
        }
        if (changed) Charms.Save(session);
    }
}

using UnityEngine;

namespace KO.HollowKnight8;

// Room reconstruction is not another death. Keep its original clock and
// retry safe landings independently of the nonfatal hazard-recovery timer.
internal static class TimedRespawnRecovery
{
    internal static float Remaining(PlayerSlot p, float seconds)
    {
        return Mathf.Max(0f, seconds - (Time.time - p.DownAt));
    }

    internal static void RestoreDown(CoopSession session, PlayerSlot p)
    {
        float originalDeath = p.DownAt;
        try
        {
            session.Down(p, spawnShade: false);
        }
        finally
        {
            p.DownAt = originalDeath;
            p.NextTimedRespawnAttempt = 0f;
        }
    }

    private static bool LivingAnchor(PlayerSlot p, PlayerSlot down)
    {
        return p != null && p != down && p.Hero && p.Ready && p.Alive && p.Connected
            && !p.SpawnPending && p.Vitals.Health + p.Vitals.Blue > 0
            && !p.Hero.cState.dead && !p.Hero.cState.transitioning
            && !TransitionVote.Holding(p) && !EmergencyWarp.Active(p);
    }

    internal static void Tick(CoopSession session, PlayerSlot p, bool enabled, float seconds)
    {
        if (!enabled || session == null || !session.Active || !session.Gameplay || session.TeamWipe
            || p == null || !p.Down || !p.Hero || !p.Ready || p.SpawnPending || p.Retiring
            || PvpMatch.Running || PvpArena.Active || Remaining(p, seconds) > 0f
            || Time.time < p.NextTimedRespawnAttempt)
            return;

        // Do not use a loading proxy, hidden room-vote entrant or another corpse
        // as the living survivor. Party-wide death continues through PartyDeath.
        bool hasLiving = false;
        for (int i = 0; i < session.Players.Count; i++)
            if (LivingAnchor(session.Players[i], p)) { hasLiving = true; break; }
        if (!hasLiving) return;

        p.NextTimedRespawnAttempt = Time.time + .1f;

        // Preserve the verified shared platform during Radiance's final climb.
        // Recover rechecks this checkpoint and all terrain/body checks itself.
        Vector3? checkpoint = null;
        if (!RadianceAscentCheckpoint.BeforeRecovery(session, p, ref checkpoint)) return;
        if (checkpoint.HasValue)
        {
            session.Recover(p, respawn: true, reviveAt: checkpoint);
            return;
        }

        // At most eight participants: no allocations and no unbounded floor
        // search. Prefer the oldest survivor, then the next safe living one.
        uint tried = 0;
        for (int attempt = 0; attempt < session.Players.Count; attempt++)
        {
            int best = -1;
            for (int i = 0; i < session.Players.Count; i++)
            {
                PlayerSlot candidate = session.Players[i];
                if ((tried & (1u << i)) != 0 || !LivingAnchor(candidate, p)) continue;
                if (best < 0 || candidate.LifeStartedAt < session.Players[best].LifeStartedAt
                    || candidate.LifeStartedAt == session.Players[best].LifeStartedAt
                    && candidate.Index < session.Players[best].Index)
                    best = i;
            }
            if (best < 0) return;
            tried |= 1u << best;
            PlayerSlot ally = session.Players[best];
            Vector3 destination;
            if (!session.NearAlly(ally, p, out destination)) continue;

            session.Recover(p, respawn: true, reviveAt: destination);
            if (!p.Down)
            {
                p.NextTimedRespawnAttempt = 0f;
                Diagnostics.Write("TIMED respawn P" + (p.Index + 1) + " near P" + (ally.Index + 1));
                return;
            }
        }
        // A closed gate or hazard can legitimately block every landing. Leave
        // the original deadline expired and retry quickly when it becomes safe.
    }
}

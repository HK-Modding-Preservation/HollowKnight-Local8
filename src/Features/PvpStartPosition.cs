using UnityEngine;

namespace KO.HollowKnight8;

// Resolve all destinations before changing any actor. Airborne players keep a
// validated floor destination, not the unsafe mid-air position of the request.
internal static class PvpStartPosition
{
    internal static bool OnBench(PlayerSlot p) => p != null && p.Hero &&
        (BenchSeats.Seated(p) || p.Vitals.AtBench && p.Hero.cState.nearBench ||
         InteractionRouter.ActivePlayer == p && InteractionRouter.ActiveIsBench);

    internal static bool Modal => ShopMenuRouting.MenuVisible || StagMenuRouting.HasOwner ||
        PickupCard.Owner != null || Charms.NativeMenuOpen || RoleSystem.JoiningMenu ||
        InteractionRouter.ActivePlayer != null && !InteractionRouter.ActiveIsBench;

    internal static bool Try(CoopSession session, PlayerSlot p, out Vector3 at, out string reason)
    {
        at = Vector3.zero;
        if (!p.Connected) { reason = UiLocalization.Get("pvp.disconnected"); return false; }
        if (!p.Ready || !p.Hero || p.SpawnPending || p.ArenaTransfer || p.Retiring)
        { reason = UiLocalization.Get("pvp.player_loading"); return false; }
        if (!p.Alive) { reason = UiLocalization.Get("pvp.must_alive"); return false; }
        if (p.Hero.cState.transitioning || p.Hero.IsDreamReturning ||
            EmergencyWarp.Active(p) || EmergencyWarp.Holding(p) ||
            p.Hero.controlReqlinquished && !OnBench(p))
        { reason = UiLocalization.Get("pvp.finish_action"); return false; }
        Vector3 current = p.Hero.transform.position;
        if (session.IsSafe(current, p)) at = current;
        else if (p.HasSafePoint && session.IsSafe(p.SafePoint, p)) at = p.SafePoint;
        else if (p.HasPreviousSafePoint && session.IsSafe(p.PreviousSafePoint, p)) at = p.PreviousSafePoint;
        else if (!session.FindSafePosition(current, p, out at))
        { reason = UiLocalization.Get("pvp.unsafe_start"); return false; }
        reason = "";
        return true;
    }

    internal static void Prepare(CoopSession session, PlayerSlot p)
    {
        if (!OnBench(p) && !p.Vitals.AtBench) return;
        using (PlayerContext.Enter(p))
        {
            BenchSeats.Leave(p);
            if (InteractionRouter.ActivePlayer == p && InteractionRouter.ActiveIsBench)
                InteractionRouter.CloseForTransition();
            p.Vitals.AtBench = false;
            p.Vitals.DisablePause = false;
            session.Data.atBench = false;
            ActorRecovery.Reset(p);
            session.Commit(p);
        }
    }
}

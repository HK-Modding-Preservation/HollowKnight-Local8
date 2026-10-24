using System.Runtime.CompilerServices;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class FuryRecovery
{
    private sealed class Effect
    {
        internal PlayMakerFSM Fsm;
        internal SpriteFlash[] Flashes;
        internal float NextCheck;
    }

    private static readonly ConditionalWeakTable<HeroController, Effect> effects = new ConditionalWeakTable<HeroController, Effect>();

    internal static bool ShouldEnd(bool equipped, int health, int blue, bool down)
    {
        return down || !equipped || health != 1 || blue > 0;
    }

    internal static void Tick(CoopSession session)
    {
        if (session == null || !session.Active || session.TeamWipe) return;
        foreach (var player in session.Players)
            try { Refresh(player); }
            catch (System.Exception error) { Diagnostics.Throttled("FURY cleanup P" + (player.Index + 1), error); }
    }

    internal static void Refresh(PlayerSlot player)
    {
        if (player == null || !player.Hero) return;
        var effect = effects.GetOrCreateValue(player.Hero);
        if (Time.unscaledTime < effect.NextCheck) return;
        effect.NextCheck = Time.unscaledTime + .25f;
        if (!ShouldEnd(player.Charms.Equipped[6], player.Vitals.Health, player.Vitals.Blue, player.Down)) return;

        if (!effect.Fsm)
        {
            foreach (var fsm in player.Hero.GetComponentsInChildren<PlayMakerFSM>(true))
                if (fsm && fsm.FsmName == "Fury" && fsm.gameObject.name == "Charm Effects") { effect.Fsm = fsm; break; }
            if (!effect.Fsm) return;
            effect.Flashes = player.Hero.GetComponentsInChildren<SpriteFlash>(true);
        }

        string state = effect.Fsm.ActiveStateName;
        bool active = state == "Activate" || state == "Stay Furied" || state == "Recheck";
        bool changed = false;
        using (PlayerContext.Enter(player))
        {
            // Co-op respawn writes health directly, so vanilla never receives
            // its healing event. Run this actor's native cleanup state once.
            if (active && effect.Fsm.Fsm.GetState("Deactivate") != null)
            {
                effect.Fsm.SetState("Deactivate");
                changed = true;
            }
            foreach (var flash in effect.Flashes)
            {
                if (!flash || !Reflect.Get(flash, "repeatFlash", false)) continue;
                Color color = Reflect.Get(flash, "flashColour", Color.clear);
                if (Mathf.Abs(color.r - .71f) < .01f && Mathf.Abs(color.g - .18f) < .01f && Mathf.Abs(color.b - .18f) < .01f)
                {
                    // Native CancelFlash only sets a cancellation request;
                    // it leaves repeatFlash true after the next Update.
                    Reflect.Set(flash, "repeatFlash", false);
                    flash.CancelFlash();
                    changed = true;
                }
            }
        }
        if (changed) Diagnostics.Write("FURY cleared stale effect P" + (player.Index + 1));
    }
}

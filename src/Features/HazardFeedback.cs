using System;
using GlobalEnums;
using UnityEngine;

namespace KO.HollowKnight8;

internal static class HazardFeedback
{
    internal static HazardType Kind(int hazard)
    {
        return hazard == 3 ? HazardType.ACID : hazard == 5 ? HazardType.PIT : HazardType.SPIKES;
    }

    internal static void Play(PlayerSlot player, HazardType kind, float angle = 0f)
    {
        if (player == null || !player.Hero || player.Down || player.Hazard) return;
        if (Plugin.Self?.Performance?.HazardEffects == false) return;
        try
        {
            using (PlayerContext.Enter(player))
            {
                var hero = player.Hero;
                // The death prefabs contain a second animated Knight. Only use
                // the brief vanilla hit streaks; recovery owns the real actor.
                var prefab = hero.takeHitPrefab;
                if (prefab)
                {
                    prefab.Spawn(hero.transform.position);
                }
                else
                {
                    var flash = hero.GetComponent<SpriteFlash>();
                    if (flash) flash.flashWhiteQuick();
                    var audio = hero.GetComponent<HeroAudioController>();
                    if (audio) audio.PlaySound(HeroSounds.TAKE_HIT);
                }
            }
        }
        catch (Exception error) { Diagnostics.Throttled("HAZARD feedback P" + (player.Index + 1), error); }
    }
}

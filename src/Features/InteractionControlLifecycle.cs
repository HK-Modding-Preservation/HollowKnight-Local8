using System;
using HutongGames.PlayMaker;

namespace KO.HollowKnight8;

// A held hero is not evidence that the nearby NPC owns the hold. Inventory,
// benches and boss sequences all use the same HeroController control flag.
// Observe the native handoff instead, with the exact action/state source.
internal static class InteractionControlLifecycle
{
    [ThreadStatic] internal static Fsm Source;
    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.HeroController.RelinquishControl += Take;
        On.HeroController.RelinquishControlNotVelocity += TakeWithoutVelocity;
        On.HeroController.RegainControl += Return;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.HeroController.RelinquishControl -= Take;
        On.HeroController.RelinquishControlNotVelocity -= TakeWithoutVelocity;
        On.HeroController.RegainControl -= Return;
        Source = null;
    }

    private static void Take(On.HeroController.orig_RelinquishControl orig, HeroController hero)
    {
        bool held = hero.controlReqlinquished;
        Fsm source = Source ?? WorldRouting.Current;
        orig(hero);
        if (!held && hero && hero.controlReqlinquished)
            InteractionRouter.ControlClaimed(source, hero);
    }

    private static void TakeWithoutVelocity(On.HeroController.orig_RelinquishControlNotVelocity orig, HeroController hero)
    {
        bool held = hero.controlReqlinquished;
        Fsm source = Source ?? WorldRouting.Current;
        orig(hero);
        if (!held && hero && hero.controlReqlinquished)
            InteractionRouter.ControlClaimed(source, hero);
    }

    private static void Return(On.HeroController.orig_RegainControl orig, HeroController hero)
    {
        orig(hero);
        if (hero && !hero.controlReqlinquished) InteractionRouter.ControlReturned(hero);
    }
}

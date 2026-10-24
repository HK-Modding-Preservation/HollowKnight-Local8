using UnityEngine;

namespace KO.HollowKnight8;

// World contacts occur outside HeroController.Update. The collider which
// actually touched the object supplies the owner, including owned nail attacks.
internal static class WorldContactRouting
{
    private static bool installed;
    internal static CoopSession Session => Plugin.Self == null ? null : Plugin.Self.Session;

    internal static bool Eligible(PlayerSlot p) => p != null && p.Hero && p.Ready && p.Alive
        && !p.Down && !p.Hazard && !p.Retiring && !p.SpawnPending && !p.ArenaTransfer
        && (p.Index == 0 || p.Connected) && !EmergencyWarp.Active(p)
        && !p.Hero.cState.dead && !p.Hero.cState.transitioning;

    internal static PlayerSlot Owner(Component contact)
    {
        var s = Session;
        return s != null && s.Active && contact ? s.Resolve(contact) : null;
    }

    // A spell/summon OwnerTag does not make its collider a hero body.
    internal static PlayerSlot Body(Component contact)
    {
        if (!contact) return null;
        var hero = contact.GetComponentInParent<HeroController>();
        var p = hero ? Owner(hero) : null;
        return p != null && p.Hero == hero && ColliderGeometry.HeroBody(contact, hero) ? p : null;
    }

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.TinkEffect.OnTriggerEnter2D += Tink;
        On.BreakableInfectedVine.OnTriggerEnter2D += Vine;
        On.InfectedBurstLarge.OnTriggerEnter2D += Large;
        On.InfectedBurstSmall.OnTriggerEnter2D += Small;
        On.GrassCut.ShouldCut += Grass;
        SteepSlopeRouting.Install();
        ShadowGateRouting.Install();
        PartySensorOccupancy.Install();
        WalkerTargeting.Install();
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.TinkEffect.OnTriggerEnter2D -= Tink;
        On.BreakableInfectedVine.OnTriggerEnter2D -= Vine;
        On.InfectedBurstLarge.OnTriggerEnter2D -= Large;
        On.InfectedBurstSmall.OnTriggerEnter2D -= Small;
        On.GrassCut.ShouldCut -= Grass;
        SteepSlopeRouting.Uninstall();
        ShadowGateRouting.Uninstall();
        PartySensorOccupancy.Uninstall();
        WalkerTargeting.Uninstall();
        EnemySightRouting.Reset();
        ColliderGeometry.Reset();
    }

    internal static void Reset()
    {
        SteepSlopeRouting.Reset();
        ShadowGateRouting.Reset();
        PartySensorOccupancy.Reset();
        WalkerTargeting.Reset();
        EnemySightRouting.Reset();
        ColliderGeometry.Reset();
    }

    internal static void Tick(CoopSession s)
    {
        if (s == null || !s.Active) { Reset(); return; }
        SteepSlopeRouting.Tick();
        ShadowGateRouting.Tick();
        PartySensorOccupancy.Tick();
        WalkerTargeting.Tick();
    }

    private static void Tink(On.TinkEffect.orig_OnTriggerEnter2D orig, TinkEffect self, Collider2D other)
    {
        var p = other && other.CompareTag("Nail Attack") ? Owner(other) : null;
        if (p == null) { orig(self, other); return; }
        var sourceHero = other.GetComponentInParent<HeroController>();
        if (!Eligible(p) || (sourceHero && sourceHero != p.Hero)) return;
        using (PlayerContext.Enter(p)) orig(self, other);
    }

    private static PlayerSlot CrystalBody(Collider2D other) => other && other.CompareTag("HeroBox")
        && other.GetComponentInParent<HeroController>() ? Owner(other) : null;

    private static bool CrystalEligible(PlayerSlot p, Collider2D other) => Eligible(p)
        && other.GetComponentInParent<HeroController>() == p.Hero;

    private static void Vine(On.BreakableInfectedVine.orig_OnTriggerEnter2D orig, BreakableInfectedVine self, Collider2D other)
    {
        var p = CrystalBody(other);
        if (p == null) { orig(self, other); return; }
        if (!CrystalEligible(p, other)) return;
        using (PlayerContext.Enter(p)) orig(self, other);
    }

    private static void Large(On.InfectedBurstLarge.orig_OnTriggerEnter2D orig, InfectedBurstLarge self, Collider2D other)
    {
        var p = CrystalBody(other);
        if (p == null) { orig(self, other); return; }
        if (!CrystalEligible(p, other)) return;
        using (PlayerContext.Enter(p)) orig(self, other);
    }

    private static void Small(On.InfectedBurstSmall.orig_OnTriggerEnter2D orig, InfectedBurstSmall self, Collider2D other)
    {
        var p = CrystalBody(other);
        if (p == null) { orig(self, other); return; }
        if (!CrystalEligible(p, other)) return;
        using (PlayerContext.Enter(p)) orig(self, other);
    }

    private static bool Grass(On.GrassCut.orig_ShouldCut orig, Collider2D other)
    {
        var p = CrystalBody(other);
        if (p == null) return orig(other);
        if (!CrystalEligible(p, other)) return false;
        using (PlayerContext.Enter(p)) return orig(other);
    }
}

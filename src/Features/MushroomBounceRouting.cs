using System;
using System.Collections.Generic;
using System.Reflection;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

// Physics callbacks do not run inside a hero Update/PlayMaker owner context.
// NailSlash caches a HeroController; BounceShroom's body callback uses the
// singleton. Resolve the contacting actor, never a nearest/ambient player.
internal static class MushroomBounceRouting
{
    private static readonly FieldInfo SlashHero = Reflect.Field(typeof(NailSlash), "heroCtrl");
    private static readonly HashSet<HeroController> logged = new HashSet<HeroController>();
    private static bool installed;
    [ThreadStatic] private static Fsm source;
    [ThreadStatic] private static PlayerSlot owner;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.NailSlash.OnTriggerEnter2D += SlashContact;
        On.CollisionEnterEvent.HandleCollision += BodyContact;
    }

    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.NailSlash.OnTriggerEnter2D -= SlashContact;
        On.CollisionEnterEvent.HandleCollision -= BodyContact;
        Reset();
    }

    internal static void Reset() { logged.Clear(); source = null; owner = null; }

    // Only the exact legacy mushroom FSM currently receiving this contact.
    // Other world FSMs and subsequent Update calls keep their existing routing.
    internal static PlayerSlot Resolve(Fsm f) => f != null && ReferenceEquals(f, source) ? owner : null;

    private static PlayerSlot BodyOwner(CoopSession s, Component component)
    {
        if (!component) return null;
        HeroController hero = component.GetComponentInParent<HeroController>();
        return hero ? s.Resolve(hero) : null;
    }

    private static bool Ready(PlayerSlot p) => p != null && p.Hero && p.Alive && p.Ready
        && !p.Down && !p.Hazard && !p.Retiring && !p.SpawnPending && !p.ArenaTransfer
        && !p.InputBlocked && !p.Hero.controlReqlinquished && !p.Hero.cState.transitioning;

    private static Fsm Legacy(GameObject obj)
    {
        PlayMakerFSM fsm = FSMUtility.LocateFSM(obj, "Bounce Shroom");
        return fsm ? fsm.Fsm : null;
    }

    private static void SlashContact(On.NailSlash.orig_OnTriggerEnter2D orig, NailSlash self, Collider2D other)
    {
        CoopSession s = Plugin.Self?.Session;
        if (s == null || !s.Active || !self || !other || !other.GetComponent<BounceShroom>() || SlashHero == null)
        { orig(self, other); return; }
        PlayerSlot p = BodyOwner(s, self);
        if (p == null) { orig(self, other); return; }
        if (!Ready(p)) return;

        HeroController previousHero = (HeroController)SlashHero.GetValue(self);
        Fsm previousSource = source;
        PlayerSlot previousOwner = owner;
        source = Legacy(other.gameObject); owner = p;
        try
        {
            SlashHero.SetValue(self, p.Hero);
            using (PlayerContext.Enter(p)) orig(self, other);
            if (logged.Add(p.Hero))
                Diagnostics.Write("MUSHROOM nail owner=P" + (p.Index + 1) + " repaired_cache=" + (previousHero != p.Hero) + " target=" + other.name);
        }
        finally
        {
            // Keep the original cache outside this call, including nested
            // contacts/exceptions. Do not overwrite another mod's new value.
            try
            {
                if (self && ReferenceEquals(SlashHero.GetValue(self), p.Hero))
                    SlashHero.SetValue(self, previousHero);
            }
            finally { source = previousSource; owner = previousOwner; }
        }
    }

    private static void BodyContact(On.CollisionEnterEvent.orig_HandleCollision orig, CollisionEnterEvent self, Collision2D collision)
    {
        CoopSession s = Plugin.Self?.Session;
        BounceShroom shroom = self ? self.GetComponentInParent<BounceShroom>() : null;
        if (s == null || !s.Active || !shroom || !shroom.active || collision == null)
        { orig(self, collision); return; }
        PlayerSlot p = BodyOwner(s, collision.collider);
        if (p == null && collision.gameObject)
            p = BodyOwner(s, collision.gameObject.transform);
        if (p == null) { orig(self, collision); return; }
        if (!Ready(p)) return;
        Fsm previousSource = source;
        PlayerSlot previousOwner = owner;
        source = Legacy(shroom.gameObject); owner = p;
        try { using (PlayerContext.Enter(p)) orig(self, collision); }
        finally { source = previousSource; owner = previousOwner; }
    }
}

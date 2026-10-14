using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

// Native SteepSlope caches a singleton, although the rigidbody comes from the
// collision. Scope that cache to the same actor and retain overlapping slopes.
internal static class SteepSlopeRouting
{
    private sealed class Contact
    {
        internal SteepSlope Slope;
        internal Collider2D Body, Surface;
    }
    private sealed class Sliding
    {
        internal PlayerSlot Player;
        internal HeroController Hero;
        internal readonly List<Contact> Contacts = new List<Contact>();
        internal bool Left, Right, BeforeLeft, BeforeRight;
    }
    private static readonly ReferenceField Hero = ReferenceField.For(Reflect.Field(typeof(SteepSlope), "hc"));
    private static readonly Dictionary<HeroController, Sliding> actors = new Dictionary<HeroController, Sliding>();
    private static readonly List<HeroController> remove = new List<HeroController>();
    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.SteepSlope.OnCollisionEnter2D += Enter;
        On.SteepSlope.OnCollisionStay2D += Stay;
        On.SteepSlope.OnCollisionExit2D += Exit;
    }
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.SteepSlope.OnCollisionEnter2D -= Enter;
        On.SteepSlope.OnCollisionStay2D -= Stay;
        On.SteepSlope.OnCollisionExit2D -= Exit;
        Reset();
    }
    internal static void Reset()
    {
        foreach (var state in actors.Values) { state.Contacts.Clear(); Apply(state); }
        actors.Clear(); remove.Clear();
    }

    private static PlayerSlot Owner(Collision2D c) => c != null && c.gameObject && c.gameObject.CompareTag("Player")
        && c.gameObject.GetComponentInParent<HeroController>() ? WorldContactRouting.Owner(c.gameObject.transform) : null;

    private static HeroController ContactHero(Collision2D c) => c.gameObject.GetComponentInParent<HeroController>();

    private static Sliding Track(PlayerSlot p, SteepSlope self, Collision2D c)
    {
        Sliding state;
        if (!actors.TryGetValue(p.Hero, out state))
            actors[p.Hero] = state = new Sliding { Player = p, Hero = p.Hero };
        foreach (var contact in state.Contacts)
            if (contact.Slope == self && contact.Body == c.collider && contact.Surface == c.otherCollider) return state;
        // Capture each flag before native Enter writes it, not after the write.
        bool left = Reflect.Get(self, "slideLeft", false), right = Reflect.Get(self, "slideRight", false);
        if (left && !state.Left) { state.BeforeLeft = p.Hero.cState.slidingLeft; state.Left = true; }
        if (right && !state.Right) { state.BeforeRight = p.Hero.cState.slidingRight; state.Right = true; }
        state.Contacts.Add(new Contact { Slope = self, Body = c.collider, Surface = c.otherCollider });
        return state;
    }

    private static void Apply(Sliding state)
    {
        if (!state.Hero) return;
        bool left = false, right = false;
        foreach (var contact in state.Contacts)
        {
            if (!contact.Slope) continue;
            left |= Reflect.Get(contact.Slope, "slideLeft", false);
            right |= Reflect.Get(contact.Slope, "slideRight", false);
        }
        if (left) state.Hero.cState.slidingLeft = true;
        else if (state.Left)
        {
            if (state.Hero.cState.slidingLeft) state.Hero.cState.slidingLeft = state.BeforeLeft;
            state.Left = false;
        }
        if (right) state.Hero.cState.slidingRight = true;
        else if (state.Right)
        {
            if (state.Hero.cState.slidingRight) state.Hero.cState.slidingRight = state.BeforeRight;
            state.Right = false;
        }
    }

    internal static void Tick()
    {
        remove.Clear();
        foreach (var entry in actors)
        {
            var state = entry.Value;
            for (int i = state.Contacts.Count - 1; i >= 0; i--)
            {
                var c = state.Contacts[i];
                if (!WorldContactRouting.Eligible(state.Player) || state.Player.Hero != state.Hero
                    || !c.Slope || !c.Slope.enabled || !c.Slope.gameObject.activeInHierarchy
                    || !c.Body || !c.Body.enabled || !c.Body.gameObject.activeInHierarchy
                    || (c.Surface && (!c.Surface.enabled || !c.Surface.gameObject.activeInHierarchy
                        || c.Surface.Distance(c.Body).distance > .1f))) state.Contacts.RemoveAt(i);
            }
            Apply(state);
            if (state.Contacts.Count == 0) remove.Add(entry.Key);
        }
        foreach (var key in remove) actors.Remove(key);
    }

    private static void Enter(On.SteepSlope.orig_OnCollisionEnter2D orig, SteepSlope self, Collision2D c)
    {
        var p = Owner(c);
        if (p == null || Hero == null) { orig(self, c); return; }
        if (!WorldContactRouting.Eligible(p) || p.Hero != ContactHero(c)) return;
        var state = Track(p, self, c);
        var previous = Hero.GetValue(self);
        try { Hero.SetValue(self, p.Hero); using (PlayerContext.Enter(p)) orig(self, c); }
        finally
        {
            if (self && ReferenceEquals(Hero.GetValue(self), p.Hero)) Hero.SetValue(self, previous);
            Apply(state);
        }
    }

    private static void Stay(On.SteepSlope.orig_OnCollisionStay2D orig, SteepSlope self, Collision2D c)
    {
        var p = Owner(c);
        if (p == null || Hero == null) { orig(self, c); return; }
        if (!WorldContactRouting.Eligible(p) || p.Hero != ContactHero(c)) return;
        var state = Track(p, self, c);
        var previous = Hero.GetValue(self);
        try { Hero.SetValue(self, p.Hero); using (PlayerContext.Enter(p)) orig(self, c); }
        finally
        {
            if (self && ReferenceEquals(Hero.GetValue(self), p.Hero)) Hero.SetValue(self, previous);
            Apply(state);
        }
    }

    private static void Exit(On.SteepSlope.orig_OnCollisionExit2D orig, SteepSlope self, Collision2D c)
    {
        var p = Owner(c);
        if (p == null || Hero == null) { orig(self, c); return; }
        var contactHero = ContactHero(c);
        if (p.Hero != contactHero)
        {
            // A cached OwnerTag may now identify the replacement actor. Release
            // the old body's slope without clearing the new hero's state.
            Sliding old;
            if (actors.TryGetValue(contactHero, out old))
            { old.Contacts.Clear(); Apply(old); actors.Remove(contactHero); }
            return;
        }
        Sliding state;
        actors.TryGetValue(p.Hero, out state);
        bool left = p.Hero.cState.slidingLeft, right = p.Hero.cState.slidingRight;
        var previous = Hero.GetValue(self);
        try { Hero.SetValue(self, p.Hero); using (PlayerContext.Enter(p)) orig(self, c); }
        finally
        {
            if (self && ReferenceEquals(Hero.GetValue(self), p.Hero)) Hero.SetValue(self, previous);
            if (state != null)
            {
                // Undo the native blanket clear before releasing just this pair.
                if (state.Left && !p.Hero.cState.slidingLeft) p.Hero.cState.slidingLeft = left;
                if (state.Right && !p.Hero.cState.slidingRight) p.Hero.cState.slidingRight = right;
                for (int i = state.Contacts.Count - 1; i >= 0; i--)
                    if (state.Contacts[i].Slope == self && state.Contacts[i].Body == c.collider
                        && state.Contacts[i].Surface == c.otherCollider) state.Contacts.RemoveAt(i);
                Apply(state);
                if (state.Contacts.Count == 0) actors.Remove(p.Hero);
            }
        }
    }
}

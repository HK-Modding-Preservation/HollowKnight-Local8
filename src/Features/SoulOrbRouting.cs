using System;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

// A totem's delayed pooled orbs belong to its hitter. Native Zoom still controls
// flight, collection, reward amount and flash, under that actor's context.
internal static class SoulOrbRouting
{
    private sealed class Owner
    {
        internal PlayerSlot Player;
        internal HeroController Hero;
        internal bool Valid => Player != null && Hero && Player.Hero == Hero && !Player.Retiring;
    }

    private sealed class Contact : IDisposable
    {
        internal readonly GameObject Source;
        internal readonly Owner Owner;
        private readonly Contact before;
        internal Contact(Fsm f, PlayerSlot p)
        {
            Source = f.GameObject;
            Owner = new Owner { Player = p, Hero = p.Hero };
            before = contact;
            contact = this;
        }
        public void Dispose() { if (ReferenceEquals(contact, this)) contact = before; }
    }

    private sealed class Flight : IEnumerator, IDisposable
    {
        private readonly SoulOrb orb;
        private readonly Owner owner;
        private readonly IEnumerator native;
        private bool disposed;
        internal Flight(SoulOrb orb, Owner owner, IEnumerator native)
        { this.orb = orb; this.owner = owner; this.native = native; }
        public object Current => native.Current;
        public bool MoveNext()
        {
            if (disposed || !orb) return false;
            if (!orbs.TryGetValue(orb, out var active)) { Dispose(); Cancel(); return false; }
            if (active != owner) return false;
            if (!owner.Valid)
            {
                // Never give an orphaned flight to another slot or P1. Finish
                // its lifetime without running the native award/flash branch.
                Dispose();
                Cancel();
                return false;
            }
            using (PlayerContext.Enter(owner.Player)) return native.MoveNext();
        }
        private void Cancel()
        {
            if (!orb || !orb.gameObject.activeInHierarchy) return;
            if (orb.dontRecycle) orb.gameObject.SetActive(false);
            else orb.gameObject.Recycle();
        }
        public void Reset() { throw new NotSupportedException(); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            using (PlayerContext.Enter(owner.Valid ? owner.Player : null)) (native as IDisposable)?.Dispose();
        }
    }

    private static readonly Dictionary<GameObject, Owner> totems = new Dictionary<GameObject, Owner>();
    private static readonly Dictionary<SoulOrb, Owner> orbs = new Dictionary<SoulOrb, Owner>();
    [ThreadStatic] private static Contact contact;
    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        SoulTotemSharing.Install();
        On.SoulOrb.OnEnable += Enable;
        On.SoulOrb.Start += Start;
        On.SoulOrb.OnDisable += Disable;
        On.SoulOrb.Zoom += Zoom;
    }
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.SoulOrb.OnEnable -= Enable;
        On.SoulOrb.Start -= Start;
        On.SoulOrb.OnDisable -= Disable;
        On.SoulOrb.Zoom -= Zoom;
        SoulTotemSharing.Uninstall();
        Reset();
    }
    internal static void Reset() { totems.Clear(); orbs.Clear(); contact = null; SoulTotemSharing.Reset(); }

    private static bool Totem(Fsm f) => f != null && f.GameObject
        && ((f.GameObject.name ?? "").IndexOf("soul totem", StringComparison.OrdinalIgnoreCase) >= 0
            || (f.Name ?? "").IndexOf("soul totem", StringComparison.OrdinalIgnoreCase) >= 0);

    internal static PlayerSlot Track(Fsm f, Collider2D attack)
    {
        var s = Plugin.Self == null ? null : Plugin.Self.Session;
        if (s == null || !s.Active || !Totem(f) || !attack || !attack.CompareTag("Nail Attack")) return null;
        var hero = attack.GetComponentInParent<HeroController>();
        var p = hero ? s.Resolve(hero) : null;
        if (p == null || !p.Alive || !p.Ready || p.Retiring || p.Hero != hero) return null;
        return p;
    }
    internal static IDisposable EnterContact(Fsm f, PlayerSlot p) => p == null ? null : new Contact(f, p);
    internal static void StateEntered(FsmState state)
    {
        // Native OnTriggerEnter/Stay runs UpdateStateChanges before returning.
        // An ignored nail contact must not steal a previous accepted hit's
        // delayed orbs. Commit only when this source actually enters a state.
        var f = state == null ? null : state.Fsm;
        if (contact != null && contact.Owner.Valid && Totem(f) && f.GameObject == contact.Source)
            totems[f.GameObject] = contact.Owner;
    }
    internal static PlayerSlot Resolve(Fsm f)
    {
        if (!Totem(f)) return null;
        if (contact != null && contact.Source == f.GameObject && contact.Owner.Valid) return contact.Owner.Player;
        if (!totems.TryGetValue(f.GameObject, out var owner) || !owner.Valid) return null;
        return owner.Player;
    }

    private static void Enable(On.SoulOrb.orig_OnEnable orig, SoulOrb self)
    {
        orbs.Remove(self);
        if (SoulTotemSharing.Enabled(self))
        {
            orig(self);
            Reflect.Set(self, "target", (Transform)null);
            return;
        }
        var s = Plugin.Self == null ? null : Plugin.Self.Session;
        if (s != null && s.Active)
        {
            var p = !PlayerContext.TargetingEnemy ? PlayerContext.Current : null;
            p = p ?? s.Primary;
            if (p != null && p.Hero && !p.Retiring)
            {
                var owner = new Owner { Player = p, Hero = p.Hero };
                orbs[self] = owner;
                Reflect.Set(self, "target", owner.Hero.transform);
            }
        }
        orig(self);
    }
    private static void Start(On.SoulOrb.orig_Start orig, SoulOrb self)
    {
        orbs.TryGetValue(self, out var owner);
        using (PlayerContext.Enter(owner != null && owner.Valid ? owner.Player : null)) orig(self);
        if (SoulTotemSharing.Contains(self)) Reflect.Set(self, "target", (Transform)null);
        if (owner != null && owner.Valid) Reflect.Set(self, "target", owner.Hero.transform);
    }
    private static void Disable(On.SoulOrb.orig_OnDisable orig, SoulOrb self)
    {
        orbs.Remove(self);
        SoulTotemSharing.Disabled(self);
        orig(self);
    }
    private static IEnumerator Zoom(On.SoulOrb.orig_Zoom orig, SoulOrb self, bool doZoom)
    {
        if (SoulTotemSharing.Zoom(self, doZoom, out var radial)) return radial;
        var routine = orig(self, doZoom);
        return orbs.TryGetValue(self, out var owner) ? new Flight(self, owner, routine) : routine;
    }
}

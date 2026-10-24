using System;
using System.Collections;
using System.Collections.Generic;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace KO.HollowKnight8;

// Keep native totem depletion, cooldown and random orb count. Its orbs become
// a short outward flourish; each spawned unit grants its normal 2 SOUL nearby.
internal static class SoulTotemSharing
{
    internal const float Radius = 12f;
    private sealed class Recipient { internal PlayerSlot Player; internal HeroController Hero; }
    private sealed class Batch
    {
        internal GameObject Source;
        internal readonly List<Recipient> Players = new List<Recipient>(8);
        internal readonly List<Burst> Orbs = new List<Burst>(9);
        internal int Epoch;
    }
    private sealed class Burst
    {
        internal SoulOrb Orb;
        internal Rigidbody2D Body;
        internal SpriteRenderer Sprite;
        internal Color Color;
        internal Vector2 Direction;
    }
    private static readonly Dictionary<SoulOrb, Burst> bursts = new Dictionary<SoulOrb, Burst>();
    [ThreadStatic] private static Batch current;
    private static int epoch;
    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.OnEnter += Fling;
    }
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.OnEnter -= Fling;
        Reset();
    }
    private static bool Totem(Fsm f)
    {
        if (f == null || !f.GameObject) return false;
        return string.Equals(f.Name, "soul_totem", StringComparison.OrdinalIgnoreCase)
            || (f.Name ?? "").IndexOf("soul totem", StringComparison.OrdinalIgnoreCase) >= 0
            || (f.GameObject.name ?? "").IndexOf("soul totem", StringComparison.OrdinalIgnoreCase) >= 0;
    }
    private static bool Eligible(PlayerSlot p) => WorldContactRouting.Eligible(p)
        && p.Vitals.Health > 0 && !TransitionVote.Holding(p);

    private static void Fling(On.HutongGames.PlayMaker.Actions.FlingObjectsFromGlobalPool.orig_OnEnter orig, FlingObjectsFromGlobalPool action)
    {
        var s = Plugin.Self == null ? null : Plugin.Self.Session;
        var prefab = action.gameObject == null ? null : action.gameObject.Value;
        if (s == null || !s.Active || !Totem(action.Fsm) || !prefab || !prefab.GetComponent<SoulOrb>())
        { orig(action); return; }
        var batch = new Batch { Source = action.Fsm.GameObject, Epoch = epoch };
        foreach (var p in s.Players)
            if (Eligible(p) && Vector2.Distance(p.Hero.transform.position, batch.Source.transform.position) <= Radius)
                batch.Players.Add(new Recipient { Player = p, Hero = p.Hero });
        var before = current;
        current = batch;
        try { orig(action); }
        finally
        {
            if (current == batch) current = before;
            // Native Fling assigns its high launch velocity after OnEnable.
            // Bound the custom flourish before physics runs, without changing
            // native action fields, RNG, spawn count or pooled prefab settings.
            foreach (var burst in batch.Orbs)
                if (Live(burst) && burst.Body) burst.Body.velocity = burst.Direction * 2f;
            if (batch.Epoch == epoch)
                Diagnostics.Write("TOTEM shared source=" + batch.Source.name + " orbs=" + batch.Orbs.Count + " nearby=" + batch.Players.Count + " radius=" + Radius);
        }
    }
    internal static bool Enabled(SoulOrb orb)
    {
        Disabled(orb);
        var batch = current;
        if (batch == null || batch.Epoch != epoch) return false;
        float angle = batch.Orbs.Count * 2.399963f;
        var sprite = orb.GetComponent<SpriteRenderer>();
        var burst = new Burst { Orb = orb, Body = orb.GetComponent<Rigidbody2D>(), Sprite = sprite,
            Color = sprite ? sprite.color : Color.white, Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) };
        bursts[orb] = burst; batch.Orbs.Add(burst);
        if (orb.awardSoul)
            foreach (var recipient in batch.Players)
            {
                if (batch.Epoch != epoch) break;
                var p = recipient.Player;
                if (p.Hero != recipient.Hero || !Eligible(p)) continue;
                using (PlayerContext.Enter(p)) p.Hero.AddMPCharge(2);
            }
        return true;
    }
    internal static bool Contains(SoulOrb orb) => bursts.ContainsKey(orb);
    internal static void Disabled(SoulOrb orb)
    {
        if (!bursts.TryGetValue(orb, out var burst)) return;
        bursts.Remove(orb);
        if (burst.Sprite) burst.Sprite.color = burst.Color;
    }
    private static bool Live(Burst burst) => burst.Orb && burst.Orb.gameObject.activeInHierarchy
        && bursts.TryGetValue(burst.Orb, out var active) && active == burst;
    internal static bool Zoom(SoulOrb orb, bool doZoom, out IEnumerator routine)
    {
        if (!bursts.TryGetValue(orb, out var burst)) { routine = null; return false; }
        routine = Fly(burst, doZoom);
        return true;
    }
    private static IEnumerator Fly(Burst burst, bool animate)
    {
        try
        {
            for (float elapsed = 0; animate && elapsed < .7f && Live(burst); elapsed += Time.deltaTime)
            {
                float remaining = 1f - elapsed / .7f;
                if (burst.Body) burst.Body.velocity = burst.Direction * (2f * remaining);
                if (burst.Sprite)
                {
                    var color = burst.Color; color.a *= remaining;
                    burst.Sprite.color = color;
                }
                yield return null;
            }
        }
        finally
        {
            if (Live(burst))
            {
                Disabled(burst.Orb);
                if (burst.Orb.dontRecycle) burst.Orb.gameObject.SetActive(false);
                else burst.Orb.gameObject.Recycle();
            }
        }
    }
    internal static void Reset()
    {
        epoch++; current = null;
        var active = new List<Burst>(bursts.Values); bursts.Clear();
        foreach (var burst in active)
        {
            if (burst.Sprite) burst.Sprite.color = burst.Color;
            if (!burst.Orb || !burst.Orb.gameObject.activeInHierarchy) continue;
            if (burst.Orb.dontRecycle) burst.Orb.gameObject.SetActive(false);
            else burst.Orb.gameObject.Recycle();
        }
    }
}

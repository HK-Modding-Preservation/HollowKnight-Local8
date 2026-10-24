using UnityEngine;
using System.Collections.Generic;

namespace KO.HollowKnight8;

internal static class ColliderGeometry
{
    private static readonly Dictionary<HeroController, Collider2D[]> bodies = new Dictionary<HeroController, Collider2D[]>();
    internal static void Reset() { bodies.Clear(); }

    internal static bool HeroBody(Component collider, HeroController hero)
    {
        return collider && hero && (collider.gameObject == hero.gameObject || collider.GetComponent<HeroBox>());
    }

    internal static Collider2D[] HeroBodies(HeroController hero)
    {
        if (!bodies.TryGetValue(hero, out var result))
        {
            var found = new List<Collider2D>();
            foreach (var collider in hero.GetComponentsInChildren<Collider2D>(true))
                if (HeroBody(collider, hero)) found.Add(collider);
            bodies[hero] = result = found.ToArray();
        }
        return result;
    }

    internal static bool Overlaps(Collider2D left, Collider2D right)
    {
        // Bounds.Intersects also tests Z. Physics2D does not: sensor artwork
        // and individual heroes can occupy different rendering depths.
        var a = left.bounds;
        var b = right.bounds;
        return Mathf.Abs(a.center.x - b.center.x) <= (a.size.x + b.size.x) * .5f
            && Mathf.Abs(a.center.y - b.center.y) <= (a.size.y + b.size.y) * .5f
            && left.Distance(right).isOverlapped;
    }
}

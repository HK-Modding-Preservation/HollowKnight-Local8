using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KO.HollowKnight8;

// Only ownership is cached. Health, charms, input and FSM state stay live.
// A frame boundary or an explicit hierarchy registration ends the cache.
internal sealed class OwnerQueryCache
{
    private sealed class Identity : IEqualityComparer<Object>
    {
        internal static readonly Identity Instance = new Identity();
        public bool Equals(Object a, Object b) => ReferenceEquals(a, b);
        public int GetHashCode(Object value) => RuntimeHelpers.GetHashCode(value);
    }
    private struct Entry { internal PlayerSlot Owner; internal HeroController Hero; }
    private readonly Dictionary<Object, Entry> entries = new Dictionary<Object, Entry>(Identity.Instance);
    private int frame = -1;
    internal bool TryGet(Object value, int now, out PlayerSlot owner)
    {
        if (frame != now) { entries.Clear(); frame = now; }
        Entry entry;
        if (entries.TryGetValue(value, out entry) &&
            (entry.Owner == null || (entry.Owner.Hero && ReferenceEquals(entry.Owner.Hero, entry.Hero))))
        { owner = entry.Owner; return true; }
        owner = null; return false;
    }
    internal void Store(Object value, PlayerSlot owner)
    {
        if (entries.Count >= 8192) entries.Clear();
        entries[value] = new Entry { Owner = owner, Hero = owner == null ? null : owner.Hero };
    }
    internal void Clear() { entries.Clear(); frame = -1; }
}

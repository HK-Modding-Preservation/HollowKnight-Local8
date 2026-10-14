using System;
using System.Collections.Generic;
using UnityEngine;

namespace KO.HollowKnight8;

// Store structure, never a player or an input decision. Every positive match
// still goes through the original live owner/Inside/Busy checks in Resolve.
internal sealed class InteractionAncestors<T> where T : class
{
    private sealed class Binding { internal Transform Root; internal T Value; internal int Order; }
    private sealed class Query
    {
        internal int Revision = -1;
        internal readonly List<Transform> Chain = new List<Transform>(12);
        internal readonly List<Binding> Matches = new List<Binding>();
        internal T[] Values = Empty;
    }
    private sealed class Ordering : IComparer<Binding>
    {
        internal static readonly Ordering Instance = new Ordering();
        public int Compare(Binding a, Binding b) => a.Order.CompareTo(b.Order);
    }
    private static readonly T[] Empty = new T[0];
    private readonly Dictionary<Transform, List<Binding>> roots = new Dictionary<Transform, List<Binding>>();
    private readonly Dictionary<T, Binding> registered = new Dictionary<T, Binding>();
    private readonly Dictionary<Transform, Query> queries = new Dictionary<Transform, Query>();
    private int revision, order;
    internal long Calls, Rebuilds, RootLookups;

    internal void Register(Transform root, T value)
    {
        if (!root || value == null) return;
        Binding binding;
        if (registered.TryGetValue(value, out binding))
        {
            if (binding.Root == root) return;
            List<Binding> previous;
            if (roots.TryGetValue(binding.Root, out previous)) previous.Remove(binding);
            binding.Root = root;
        }
        else
        {
            binding = new Binding { Root = root, Value = value, Order = order++ };
            registered[value] = binding;
        }
        List<Binding> list;
        if (!roots.TryGetValue(root, out list)) roots[root] = list = new List<Binding>();
        list.Add(binding); revision++;
    }

    private static bool SameChain(Query query, Transform source)
    {
        int index = 0;
        for (Transform at = source; at; at = at.parent)
            if (index >= query.Chain.Count || query.Chain[index++] != at) return false;
        return index == query.Chain.Count;
    }

    internal T[] Matches(Transform source)
    {
        Calls++;
        if (!source) return Empty;
        Query query;
        if (!queries.TryGetValue(source, out query)) queries[source] = query = new Query();
        if (query.Revision == revision && SameChain(query, source)) return query.Values;
        Rebuilds++; query.Chain.Clear(); query.Matches.Clear();
        for (Transform at = source; at; at = at.parent)
        {
            query.Chain.Add(at); RootLookups++;
            List<Binding> list;
            if (roots.TryGetValue(at, out list)) query.Matches.AddRange(list);
        }
        // The previous full dictionary scan visited entries in registration
        // order. Preserve that priority for nested NPC and menu controllers.
        query.Matches.Sort(Ordering.Instance);
        // Publish a fresh snapshot only when structure changes. A nested FSM
        // can reparent its object while an outer query is still enumerating.
        query.Values = query.Matches.Count == 0 ? Empty : new T[query.Matches.Count];
        for (int i = 0; i < query.Values.Length; i++) query.Values[i] = query.Matches[i].Value;
        query.Revision = revision;
        return query.Values;
    }

    internal void Clear()
    {
        roots.Clear(); registered.Clear(); queries.Clear(); revision = order = 0; Calls = Rebuilds = RootLookups = 0;
    }
}

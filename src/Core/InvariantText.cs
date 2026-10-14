using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

// Cache the exact invariant conversion, never a live Unity name or a routing
// decision. Callers still read object.name on every call, including renames.
internal static class InvariantText
{
    private static readonly Dictionary<string, string> lower = new Dictionary<string, string>(StringComparer.Ordinal);
    internal static string Lower(string value)
    {
        // Preserve the original null-name exception rather than changing it
        // to an empty-name match. Unity's normal object names are non-null.
        if (value == null) return value.ToLowerInvariant();
        string result;
        if (lower.TryGetValue(value, out result)) return result;
        if (lower.Count >= 2048) lower.Clear();
        lower[value] = result = value.ToLowerInvariant();
        return result;
    }
    internal static void Clear() => lower.Clear();
}

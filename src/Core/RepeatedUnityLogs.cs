using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

// Keep the first full trace of each exact error, then report repeat counts.
// This changes our copy of Unity's log only, not Unity/DebugMod behaviour.
internal sealed class RepeatedUnityLogs
{
    private sealed class Entry { internal double Next; internal int Count; }
    private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
    internal void Record(string message, double now, Action<string> write)
    {
        if (!entries.TryGetValue(message, out var entry))
        {
            if (entries.Count >= 128) { Flush(write); entries.Clear(); }
            entries[message] = new Entry { Next = now + 5 };
            write(message); return;
        }
        entry.Count++;
        if (now < entry.Next) return;
        Summary(message, entry, write); entry.Next = now + 5;
    }
    private static void Summary(string message, Entry entry, Action<string> write)
    {
        if (entry.Count == 0) return;
        int end = message.IndexOf('\n'); string heading = end < 0 ? message : message.Substring(0, end);
        write("UNITY repeated " + entry.Count + " times (identical trace above): " + heading);
        entry.Count = 0;
    }
    internal void Flush(Action<string> write)
    {
        foreach (var pair in entries) Summary(pair.Key, pair.Value, write);
    }
    internal void Clear() { entries.Clear(); }
}

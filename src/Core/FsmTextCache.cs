using System.Collections.Generic;
using HutongGames.PlayMaker;
using UnityEngine;

namespace KO.HollowKnight8;

// Keep the exact old invariant-lowercase classification. Check both live names
// each time so renamed/modded/pool-reused controllers change immediately.
internal sealed class FsmTextCache
{
    private sealed class Text
    {
        internal GameObject Root;
        internal string Name, ObjectName, Kind;
    }
    private readonly Dictionary<Fsm, Text> cache = new Dictionary<Fsm, Text>();

    internal string Kind(Fsm fsm)
    {
        GameObject root = fsm.GameObject;
        string name = fsm.Name, objectName = root ? root.name : "";
        Text text;
        if (cache.TryGetValue(fsm, out text) && ReferenceEquals(text.Root, root) && text.Name == name && text.ObjectName == objectName)
            return text.Kind;
        if (text == null)
        {
            if (cache.Count >= 8192) cache.Clear();
            cache[fsm] = text = new Text();
        }
        text.Root = root; text.Name = name; text.ObjectName = objectName;
        return text.Kind = (name + " " + objectName).ToLowerInvariant();
    }

    internal void Clear() => cache.Clear();
}

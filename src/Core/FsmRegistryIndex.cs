using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace KO.HollowKnight8;

// Index registration membership/order only. Native code still reads each live
// component/Fsm and runs every action/event. List's mutation token catches even
// same-count Remove/Insert/reorder and changes made by nested event callbacks.
internal sealed class FsmRegistryIndex
{
    private static readonly Func<List<PlayMakerFSM>, int> readVersion = VersionReader();
    private static readonly List<PlayMakerFSM> empty = new List<PlayMakerFSM>();
    private readonly Dictionary<GameObject, List<PlayMakerFSM>> byObject = new Dictionary<GameObject, List<PlayMakerFSM>>();
    private List<PlayMakerFSM> registry;
    private int version;
    private bool indexed;
    internal long Queries, Rebuilds, EntriesRead;
    internal static bool Supported => readVersion != null;

    private static Func<List<PlayMakerFSM>, int> VersionReader()
    {
        try
        {
            Type type = typeof(List<PlayMakerFSM>);
            FieldInfo field = type.GetField("_version", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(int)) return null;
            var read = new DynamicMethod("Local8_FsmRegistryVersion", typeof(int), new[] { type }, type.Module, true);
            ILGenerator il = read.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, field); il.Emit(OpCodes.Ret);
            var getter = (Func<List<PlayMakerFSM>, int>)read.CreateDelegate(typeof(Func<List<PlayMakerFSM>, int>));
            getter(new List<PlayMakerFSM>()); // Check runtime permission before enabling the path.
            return getter;
        }
        catch (Exception) { return null; }
    }

    internal List<PlayMakerFSM> ForObject(List<PlayMakerFSM> source, GameObject target)
    {
        Queries++;
        if (source == null || !target || readVersion == null) return source;
        int current = readVersion(source);
        if (!indexed || !ReferenceEquals(registry, source) || version != current)
        {
            // Publish new per-object lists on rebuild. An outer native loop can
            // still be enumerating an earlier snapshot during a nested callback.
            byObject.Clear();
            foreach (PlayMakerFSM component in source)
            {
                EntriesRead++;
                if (!component) continue;
                GameObject root = component.gameObject;
                if (!root) continue;
                List<PlayMakerFSM> list;
                if (!byObject.TryGetValue(root, out list)) byObject[root] = list = new List<PlayMakerFSM>();
                list.Add(component); // Keep duplicates and native registration order.
            }
            registry = source; version = current; indexed = true; Rebuilds++;
        }
        List<PlayMakerFSM> result;
        return byObject.TryGetValue(target, out result) ? result : empty;
    }

    internal void Clear()
    {
        byObject.Clear(); registry = null; indexed = false;
        Queries = Rebuilds = EntriesRead = 0;
    }
}

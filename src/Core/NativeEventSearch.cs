using System;
using System.Collections.Generic;
using System.Reflection;
using HutongGames.PlayMaker;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace KO.HollowKnight8;

// Change only the collection scanned by native BroadcastEventToGameObject.
// Native recursion, live hierarchy, receiver snapshots, ProcessEvent and all
// existing On hooks remain in their original methods and original order.
internal static class NativeEventSearch
{
    private static readonly FsmRegistryIndex index = new FsmRegistryIndex();
    private static ILHook hook;
    private static bool installed, patched;
    internal static string Performance => "event_index_active=" + (hook != null && patched && FsmRegistryIndex.Supported) +
        " event_index_queries=" + index.Queries + " event_index_rebuilds=" + index.Rebuilds + " event_index_entries_read=" + index.EntriesRead;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.HutongGames.PlayMaker.Fsm.BroadcastEventToGameObject_GameObject_FsmEvent_FsmEventData_bool_bool += ObjectBroadcast;
        On.HutongGames.PlayMaker.Fsm.BroadcastEvent_FsmEvent_bool += GlobalBroadcast;
        try
        {
            MethodInfo method = typeof(Fsm).GetMethod("BroadcastEventToGameObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(GameObject), typeof(FsmEvent), typeof(FsmEventData), typeof(bool), typeof(bool) }, null);
            if (method == null || !FsmRegistryIndex.Supported) throw new NotSupportedException("Native event search signature/version reader unavailable");
            hook = new ILHook(method, Patch);
            Diagnostics.Write("EVENT SEARCH native object index installed=" + patched);
        }
        catch (Exception error)
        {
            if (hook != null) { hook.Dispose(); hook = null; }
            patched = false;
            Diagnostics.Throttled("EVENT SEARCH native fallback", error);
        }
    }

    internal static void Patch(ILContext il)
    {
        patched = false;
        var cursor = new ILCursor(il);
        // Require the exact native list/enumerator pair. An incompatible IL
        // modification leaves the original body intact, rather than guessing.
        if (!cursor.TryGotoNext(MoveType.After,
            instruction => instruction.MatchCall(typeof(PlayMakerFSM), "get_FsmList"))) return;
        if (cursor.Next == null || !(cursor.Next.Operand is Mono.Cecil.MethodReference next) ||
            next.Name != "GetEnumerator" || !next.DeclaringType.FullName.StartsWith("System.Collections.Generic.List`1<PlayMakerFSM>", StringComparison.Ordinal)) return;
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.Emit(OpCodes.Call, typeof(NativeEventSearch).GetMethod(nameof(Select), BindingFlags.Static | BindingFlags.NonPublic));
        patched = true;
    }

    private static List<PlayMakerFSM> Select(List<PlayMakerFSM> source, GameObject target)
    {
        var runtime = Plugin.Self;
        if (!runtime || runtime.Session == null || !runtime.Session.Active) return source;
        return index.ForObject(source, target);
    }

    private static void ObjectBroadcast(On.HutongGames.PlayMaker.Fsm.orig_BroadcastEventToGameObject_GameObject_FsmEvent_FsmEventData_bool_bool orig,
        Fsm self, GameObject target, FsmEvent evt, FsmEventData data, bool children, bool exclude)
    {
        using (ImpactMetrics.Broadcast()) orig(self, target, evt, data, children, exclude);
    }
    private static void GlobalBroadcast(On.HutongGames.PlayMaker.Fsm.orig_BroadcastEvent_FsmEvent_bool orig, Fsm self, FsmEvent evt, bool exclude)
    {
        using (ImpactMetrics.Broadcast()) orig(self, evt, exclude);
    }

    internal static void Reset() => index.Clear();
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.HutongGames.PlayMaker.Fsm.BroadcastEventToGameObject_GameObject_FsmEvent_FsmEventData_bool_bool -= ObjectBroadcast;
        On.HutongGames.PlayMaker.Fsm.BroadcastEvent_FsmEvent_bool -= GlobalBroadcast;
        if (hook != null) { hook.Dispose(); hook = null; }
        patched = false; Reset();
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using HutongGames.PlayMaker;

namespace KO.HollowKnight8;

// A small, bounded sample of synchronous events inside TakeDamage only.
// No frame tracing, action hooks, input interception or gameplay decisions.
// Sample times are inclusive elapsed times; they are never extrapolated to
// the whole broadcast or added to the parent damage/broadcast timer.
internal static class DamageEventMetrics
{
    private const int SampleLimit = 256;
    internal enum Phase { Guard, Route, Context, Bind, Native, World, Restore }
    private sealed class Receiver { internal Fsm Fsm; internal int Samples; internal long Ticks; }
    private static readonly Dictionary<Fsm, Receiver> receivers = new Dictionary<Fsm, Receiver>();
    private static readonly List<Receiver> ranked = new List<Receiver>(SampleLimit);
    private static readonly long[] ticks = new long[7];
    private static readonly string[] names = { "guard", "route", "context", "bind", "native", "world", "restore" };
    [ThreadStatic] private static int tracing;
    private static uint random = 0x71c843d1;
    private static int epoch, reserved, samples;
    private static long calls;
    private static Fsm slowest;
    private static long slowestTicks, slowestNative, slowestWorld;
    private static string Ms(long value) => (value * (1000.0 / Stopwatch.Frequency)).ToString("0.###", CultureInfo.InvariantCulture);

    internal struct Trace : IDisposable
    {
        private Fsm receiver;
        private int generation;
        private bool active;
        private long last, guard, route, context, bind, native, world, restore;
        private Phase current;
        internal Trace(Fsm f)
        {
            receiver = f; generation = epoch; active = true;
            guard = route = context = bind = native = world = restore = 0;
            current = Phase.Guard; last = Stopwatch.GetTimestamp(); tracing++;
        }
        internal void Mark(Phase phase)
        {
            if (!active) return;
            long now = Stopwatch.GetTimestamp();
            Add(phase, now - last); last = now;
            current = phase == Phase.Restore ? phase : phase + 1;
        }
        private void Add(Phase phase, long elapsed)
        {
            switch (phase)
            {
                case Phase.Guard: guard += elapsed; break;
                case Phase.Route: route += elapsed; break;
                case Phase.Context: context += elapsed; break;
                case Phase.Bind: bind += elapsed; break;
                case Phase.Native: native += elapsed; break;
                case Phase.World: world += elapsed; break;
                case Phase.Restore: restore += elapsed; break;
            }
        }
        public void Dispose()
        {
            if (!active) return;
            Add(current, Stopwatch.GetTimestamp() - last);
            active = false; tracing--;
            if (generation != epoch) return;
            samples++;
            ticks[0] += guard; ticks[1] += route; ticks[2] += context; ticks[3] += bind;
            ticks[4] += native; ticks[5] += world; ticks[6] += restore;
            if (receiver == null) return;
            long elapsed = guard + route + context + bind + native + world + restore;
            if (elapsed > slowestTicks)
            {
                slowest = receiver; slowestTicks = elapsed;
                slowestNative = native; slowestWorld = world;
            }
            Receiver value;
            if (!receivers.TryGetValue(receiver, out value))
            {
                // A 128-label limit used to discard costly receivers despite
                // their time being included in the 256-sample total.
                if (receivers.Count >= SampleLimit) return;
                receivers[receiver] = value = new Receiver { Fsm = receiver };
            }
            value.Samples++; value.Ticks += elapsed;
        }
    }

    internal static Trace Begin(Fsm receiver)
    {
        if (!ImpactMetrics.InDamage) return default(Trace);
        calls++;
        if (tracing != 0 || reserved >= SampleLimit) return default(Trace);
        // Avoid choosing the same registry positions on every broadcast.
        random ^= random << 13; random ^= random >> 17; random ^= random << 5;
        if ((random & 31) != 0) return default(Trace);
        reserved++;
        return new Trace(receiver);
    }
    internal static void ResetWindow()
    {
        epoch++; calls = 0; reserved = samples = 0;
        slowest = null; slowestTicks = slowestNative = slowestWorld = 0;
        Array.Clear(ticks, 0, ticks.Length); receivers.Clear(); ranked.Clear();
        // A still-running trace belongs to the old window but must unwind its
        // own thread depth normally. Never reset an in-flight scope's depth.
    }
    internal static string Report()
    {
        long total = 0; foreach (long value in ticks) total += value;
        string result = "event_calls=" + calls + " event_samples=" + samples + " event_sample_ms=" + Ms(total);
        for (int i = 0; i < ticks.Length; i++) result += " event_" + names[i] + "_ms=" + Ms(ticks[i]);
        ranked.Clear(); ranked.AddRange(receivers.Values);
        ranked.Sort((a, b) => b.Ticks.CompareTo(a.Ticks));
        for (int i = 0; i < Math.Min(3, ranked.Count); i++)
        {
            Receiver value = ranked[i];
            string label = value.Fsm.Name ?? "unnamed";
            // Bounded labels keep one summary compact even for modded names.
            label = label.Replace(' ', '_').Replace('\n', '_').Replace('\r', '_');
            if (label.Length > 48) label = label.Substring(0, 48);
            result += " event_top" + (i + 1) + "=" + label + ":" + value.Samples + ":" + Ms(value.Ticks);
        }
        if (slowest != null)
        {
            string label = slowest.Name ?? "unnamed";
            if (slowest.GameObject) label = slowest.GameObject.name + "/" + label;
            label = label.Replace(' ', '_').Replace('\n', '_').Replace('\r', '_');
            if (label.Length > 96) label = label.Substring(0, 96);
            result += " event_slowest=" + label + ":" + Ms(slowestTicks) +
                " event_slowest_native_ms=" + Ms(slowestNative) + " event_slowest_world_ms=" + Ms(slowestWorld);
        }
        ResetWindow(); return result;
    }
}

using System;
using System.Diagnostics;
using System.Globalization;

namespace KO.HollowKnight8;

// A few checkpoints per cold spawn/reuse, not per-frame instrumentation.
// Instantiate time includes all synchronous native/mod Awake/OnEnable work.
internal struct SpawnMetrics : IDisposable
{
    private long started, last;
    private string parts;
    private int player;
    private bool active, reused;
    internal SpawnMetrics(PlayerSlot actor, bool reused)
    {
        player = actor.Index; this.reused = reused; parts = ""; active = true;
        started = last = Stopwatch.GetTimestamp();
    }
    private static string Ms(long ticks) => (ticks * (1000.0 / Stopwatch.Frequency)).ToString("0.###", CultureInfo.InvariantCulture);
    internal void Mark(string stage)
    {
        if (!active) return;
        long now = Stopwatch.GetTimestamp(); parts += " " + stage + "_ms=" + Ms(now - last); last = now;
    }
    public void Dispose()
    {
        if (!active) return;
        long total = Stopwatch.GetTimestamp() - started; active = false;
        Diagnostics.Write("SPAWN PERF P" + (player + 1) + " reuse=" + reused + " total_ms=" + Ms(total) + parts);
    }
}

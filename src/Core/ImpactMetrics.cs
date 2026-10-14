using System;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace KO.HollowKnight8;

// Only two timestamps per damage call/native damage call and per pool spawn
// occurring inside it, plus outermost event broadcasts during damage.
// No Update/FSM/action tracing and no per-frame census.
// Timers are inclusive: pool work is part of native damage, not an extra total.
internal static class ImpactMetrics
{
    [ThreadStatic] private static int depth;
	[ThreadStatic] private static int broadcastDepth;
    private static long damageTicks, nativeTicks, poolTicks, worstTicks;
    private static long nativeSequence, poolSequence;
	private static long broadcastTicks, broadcastSequence;
	private static int broadcastCalls;
    private static int calls, nativeCalls, poolCalls;
    private static float nextLog;
    internal static bool InDamage => depth > 0;
    private static double Ms(long ticks) => ticks * (1000.0 / Stopwatch.Frequency);
    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    internal struct Timer : IDisposable
    {
        private int kind, player;
        private long started, nativeBefore, poolBefore, broadcastBefore;
        internal Timer(int kind, int player = -1)
        {
            this.kind = kind; this.player = player;
            started = kind == 0 || kind == 5 ? 0 : Stopwatch.GetTimestamp();
            nativeBefore = nativeSequence; poolBefore = poolSequence;
			broadcastBefore = broadcastSequence;
            if (kind == 1) depth++;
        }
        public void Dispose()
        {
            if (kind == 0) return;
			if (kind == 5) { kind = 0; broadcastDepth--; return; }
            long ticks = Stopwatch.GetTimestamp() - started;
            int type = kind; kind = 0;
			if (type == 4) { broadcastDepth--; broadcastTicks += ticks; broadcastSequence += ticks; broadcastCalls++; return; }
            if (type == 2) { nativeTicks += ticks; nativeSequence += ticks; nativeCalls++; return; }
            if (type == 3) { poolTicks += ticks; poolSequence += ticks; poolCalls++; return; }
            depth--; calls++; damageTicks += ticks; worstTicks = Math.Max(worstTicks, ticks);
            if (Ms(ticks) < 20 || Time.unscaledTime < nextLog) return;
            nextLog = Time.unscaledTime + 5f;
            Diagnostics.Write("IMPACT PERF P" + (player + 1) + " frame=" + Time.frameCount +
                " total_ms=" + Number(Ms(ticks)) + " native_ms=" + Number(Ms(nativeSequence - nativeBefore)) +
                " pool_ms=" + Number(Ms(poolSequence - poolBefore)) + " broadcast_ms=" + Number(Ms(broadcastSequence - broadcastBefore)) + " time_scale=" + Number(Time.timeScale));
        }
    }

    internal static Timer Damage(PlayerSlot player) => new Timer(1, player.Index);
    internal static Timer NativeDamage() => depth > 0 ? new Timer(2) : default(Timer);
    internal static Timer PooledEffect() => depth > 0 ? new Timer(3) : default(Timer);
	internal static Timer Broadcast()
	{
		if (depth <= 0) return default(Timer);
		return new Timer(broadcastDepth++ == 0 ? 4 : 5);
	}

    internal static void ResetWindow()
    {
        DamageEventMetrics.ResetWindow();
        DamageParticleBudget.ResetWindow();
        damageTicks = nativeTicks = poolTicks = worstTicks = 0;
        calls = nativeCalls = poolCalls = 0;
		broadcastTicks = 0; broadcastCalls = 0;
    }
    internal static string Report()
    {
        string result = "damage_calls=" + calls + " damage_total_ms=" + Number(Ms(damageTicks)) +
            " damage_max_ms=" + Number(Ms(worstTicks)) + " damage_native_calls=" + nativeCalls +
            " damage_native_ms=" + Number(Ms(nativeTicks)) + " damage_pool_calls=" + poolCalls +
            " damage_pool_ms=" + Number(Ms(poolTicks)) + " damage_broadcast_calls=" + broadcastCalls + " damage_broadcast_ms=" + Number(Ms(broadcastTicks)) + " ref_fields_fast=" + ReferenceField.Compiled +
			" ref_fields_fallback=" + ReferenceField.Fallback + " " + DamageEventMetrics.Report() + " " + DamageParticleBudget.Report();
        ResetWindow();
        return result;
    }
}

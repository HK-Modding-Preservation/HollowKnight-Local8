using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KO.HollowKnight8;

// Instantiate copies the live ParticleSystem but not the native reducer's
// private cached high/low values. Seed only vanilla-managed Low values with
// their cached high values during the copy, then restore P1 in finally. The
// clone's own Start applies Low once; restoring Normal can recover its high.
internal sealed class NativeParticleCopy : IDisposable
{
    private static readonly FieldInfo Init = Reflect.Field(typeof(ReduceParticleEffects), "init");
    private static readonly FieldInfo HighRate = Reflect.Field(typeof(ReduceParticleEffects), "emissionRateHigh");
    private static readonly FieldInfo LowRate = Reflect.Field(typeof(ReduceParticleEffects), "emissionRateLow");
    private static readonly FieldInfo HighMax = Reflect.Field(typeof(ReduceParticleEffects), "maxParticlesHigh");
    private static readonly FieldInfo LowMax = Reflect.Field(typeof(ReduceParticleEffects), "maxParticlesLow");
    private readonly List<Snapshot> values = new List<Snapshot>();
    private struct Snapshot
    {
        internal ParticleSystem Emitter;
        internal ParticleSystem.MinMaxCurve Rate;
        internal int Max;
        internal bool ChangeRate, ChangeMax;
    }
    internal static NativeParticleCopy Begin(GameObject source)
    {
        var gm = GameManager.instance;
        if (!source || !gm || gm.gameSettings == null || gm.gameSettings.particleEffectsLevel != 0 ||
            Init == null || HighRate == null || LowRate == null || HighMax == null || LowMax == null) return null;
        var copy = new NativeParticleCopy();
        try
        {
            foreach (var reducer in source.GetComponentsInChildren<ReduceParticleEffects>(true))
            {
                if (!reducer || !(bool)Init.GetValue(reducer)) continue;
                var ps = reducer.GetComponent<ParticleSystem>();
                if (!ps) continue;
                var emission = ps.emission; var main = ps.main;
                var rate = emission.rateOverTime;
                float high = (float)HighRate.GetValue(reducer), low = (float)LowRate.GetValue(reducer);
                int max = (int)HighMax.GetValue(reducer), min = (int)LowMax.GetValue(reducer);
                // Preserve custom curves or emitter values supplied by a mod.
                bool changeRate = rate.mode == ParticleSystemCurveMode.Constant && rate.constant == low && high != low;
                bool changeMax = main.maxParticles == min && max != min;
                if (!changeRate && !changeMax) continue;
                copy.values.Add(new Snapshot { Emitter = ps, Rate = rate, Max = main.maxParticles, ChangeRate = changeRate, ChangeMax = changeMax });
                if (changeRate) emission.rateOverTime = high;
                if (changeMax) main.maxParticles = max;
            }
            return copy;
        }
        catch (Exception e)
        {
            copy.Dispose(); Diagnostics.Throttled("PERF native particle copy", e); return null;
        }
    }
    public void Dispose()
    {
        foreach (var saved in values)
        {
            if (!saved.Emitter) continue;
            try
            {
                if (saved.ChangeRate) { var emission = saved.Emitter.emission; emission.rateOverTime = saved.Rate; }
                if (saved.ChangeMax) { var main = saved.Emitter.main; main.maxParticles = saved.Max; }
            }
            catch (Exception e) { Diagnostics.Throttled("PERF restore particle copy", e); }
        }
        values.Clear();
    }
}

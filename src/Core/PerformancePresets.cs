using System;

namespace KO.HollowKnight8;

internal static class PerformancePresets
{
    private static Local8Runtime owner;
    private static int roster = -1, logged = -1;
    private static bool previousAuto;

    internal static int PlayerCount(Local8Runtime runtime)
    {
        int count = runtime.Session == null ? 0 : runtime.Session.Players.Count;
        return count > 0 ? count : runtime.Lobby.Count;
    }

    internal static void Install(Local8Runtime runtime)
    {
        owner = runtime; roster = logged = -1; previousAuto = !runtime.Performance.Automatic;
        NativeGraphicsOptions.Install();
    }
    internal static void Tick(Local8Runtime runtime)
    {
        if (runtime == null || runtime.Performance == null) return;
        try
        {
            if (owner != runtime) Install(runtime);
            int count = PlayerCount(runtime);
            var options = runtime.Performance;
            if (options.Automatic && (count != roster || !previousAuto))
            {
                int recommended = PerformanceConfiguration.Recommend(count);
                if (recommended != options.Preset || !previousAuto) Apply(runtime, recommended, true);
            }
            roster = count; previousAuto = options.Automatic;
            runtime.PerformanceMode.Value = options.Detail > 0 || options.Bloom > 0;
            NativeGraphicsOptions.Tick(runtime);
            int signature = Signature(runtime);
            if (signature != logged)
            {
                logged = signature;
                Diagnostics.Write("PERF PROFILE players=" + count + " " + Report(runtime));
            }
        }
        catch (Exception e) { Diagnostics.Throttled("PERF settings", e); }
    }
    internal static void Apply(Local8Runtime runtime, int preset, bool automatic = false)
    {
        NativeRenderBudget.Reset();
        runtime.Performance.Select(preset);
        runtime.Performance.Automatic = automatic;
        runtime.PerformanceMode.Value = runtime.Performance.Detail > 0 || runtime.Performance.Bloom > 0;
        runtime.ReduceHitEffects.Value = runtime.Performance.Preset == 0 ? runtime.Performance.NormalHitEffects : true;
        logged = -1;
    }
    internal static void Automatic(Local8Runtime runtime, bool enabled)
    {
        runtime.Performance.Automatic = enabled;
        if (enabled) Apply(runtime, PerformanceConfiguration.Recommend(PlayerCount(runtime)), true);
        previousAuto = enabled;
    }
    internal static void Customize(Local8Runtime runtime)
    {
        NativeRenderBudget.Reset(); runtime.Performance.Customize(); logged = -1;
    }
    internal static void Restore(Local8Runtime runtime)
    {
        Apply(runtime, 0);
        NativeGraphicsOptions.Restore(runtime);
        runtime.ExportSettings();
    }
    internal static void Shutdown(Local8Runtime runtime)
    {
        NativeRenderBudget.Reset(); NativeGraphicsOptions.Restore(runtime); NativeGraphicsOptions.Uninstall();
        owner = null; roster = logged = -1;
    }
    internal static int Signature(Local8Runtime runtime)
    {
        var o = runtime.Performance;
        return o.Preset | (o.Automatic ? 4 : 0) | ((o.NativeParticles + 1) << 3) |
            ((o.NativeBlur + 1) << 5) | (o.Detail << 7) | (o.Bloom << 9) |
            (runtime.ReduceHitEffects.Value ? 2048 : 0) | (o.HazardEffects ? 4096 : 0) | (o.HudHealthEffects ? 8192 : 0);
    }
    internal static string Report(Local8Runtime runtime)
    {
        var o = runtime.Performance; var gm = GameManager.instance;
        return "profile=" + (o.Preset == 0 ? "normal" : o.Preset == 1 ? "balanced" : o.Preset == 2 ? "maximum" : "custom") +
            " automatic=" + o.Automatic + " detail=" + o.Detail + " bloom=" + o.Bloom +
            " hazard_effects=" + o.HazardEffects + " hud_health_effects=" + o.HudHealthEffects +
            " native_particles=" + (gm && gm.gameSettings != null ? gm.gameSettings.particleEffectsLevel : -1) +
            " native_blur=" + (gm && gm.gameSettings != null ? (int)gm.gameSettings.shaderQuality : -1);
    }
}

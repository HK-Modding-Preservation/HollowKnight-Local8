using System;
using UnityEngine;

namespace KO.HollowKnight8;

// These are the same fields/callbacks as MenuSetting's vanilla video choices.
// BlurManager applies shaderQuality in its own Update; particle listeners are
// refreshed only after a real change, never per frame or per camera.
internal static class NativeGraphicsOptions
{
    private static bool installed, saving;
    private static GameSettings current;
    private static Local8Runtime owner, attached;
    private static GameSettings pendingParticles;
    private static int refreshAttempts;
    private static float nextRefresh;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        On.GameSettings.SaveVideoSettings += Save;
    }
    internal static void Uninstall()
    {
        if (!installed) return;
        installed = false;
        On.GameSettings.SaveVideoSettings -= Save;
        owner = attached = null; current = null; saving = false;
        pendingParticles = null;
    }
    private static bool Observe(Local8Runtime runtime, GameSettings settings)
    {
        var o = runtime.Performance;
        bool particles = o.Particles.Observe(settings.particleEffectsLevel);
        bool blur = o.Blur.Observe((int)settings.shaderQuality);
        if (!particles && !blur) return false;
        // Keep external/native menu edits. They become a manual profile and
        // a new baseline for the property the user changed.
        o.Customize();
        if (particles) o.NativeParticles = settings.particleEffectsLevel;
        if (blur) o.NativeBlur = (int)settings.shaderQuality;
        NativeRenderBudget.Reset();
        Diagnostics.Write("PERF native video edit preserved; profile=custom particles=" + settings.particleEffectsLevel + " blur=" + (int)settings.shaderQuality);
        return true;
    }
    internal static void Tick(Local8Runtime runtime)
    {
        var gm = GameManager.instance;
        if (!gm || gm.gameSettings == null || saving) return;
        if (!runtime.Enabled.Value || runtime.Session == null || !runtime.Session.Active)
        { Restore(runtime); return; }
        var settings = gm.gameSettings;
        if (attached != runtime)
        {
            attached = runtime;
            runtime.Performance.Particles.Resume(settings.particleEffectsLevel);
            runtime.Performance.Blur.Resume((int)settings.shaderQuality);
        }
        // GameSettings normally survives scenes. If it is replaced, the
        // serialized comparisons preserve matching overrides and adopt edits.
        owner = runtime; current = settings;
        Observe(runtime, settings);
        var o = runtime.Performance;
        int particles = o.Particles.Set(settings.particleEffectsLevel, o.NativeParticles);
        int blur = o.Blur.Set((int)settings.shaderQuality, o.NativeBlur);
        Write(gm, settings, particles, blur);
    }
    private static void Write(GameManager gm, GameSettings settings, int particles, int blur)
    {
        bool changeParticles = particles != settings.particleEffectsLevel;
        bool changeBlur = blur != (int)settings.shaderQuality;
        if (changeParticles || changeBlur)
        {
            NativeRenderBudget.Reset();
            settings.particleEffectsLevel = particles;
            settings.shaderQuality = (ShaderQualities)blur;
        }
        if (changeParticles) { pendingParticles = settings; refreshAttempts = 0; nextRefresh = 0f; }
        if (pendingParticles == settings && refreshAttempts < 3 && Time.unscaledTime >= nextRefresh)
        {
            refreshAttempts++;
            try { gm.RefreshParticleSystems(); pendingParticles = null; }
            catch (Exception e)
            {
                nextRefresh = Time.unscaledTime + 1f;
                Diagnostics.Throttled("PERF native particle refresh", e);
            }
        }
    }
    internal static void Restore(Local8Runtime runtime)
    {
        if (runtime == null || runtime.Performance == null) return;
        var gm = GameManager.instance;
        if (!gm || gm.gameSettings == null) return;
        var settings = gm.gameSettings; var o = runtime.Performance;
        Write(gm, settings, o.Particles.Release(settings.particleEffectsLevel), o.Blur.Release((int)settings.shaderQuality));
        if (owner == runtime) { owner = null; current = null; }
    }
    private static void Save(On.GameSettings.orig_SaveVideoSettings orig, GameSettings settings)
    {
        var runtime = owner;
        if (saving || runtime == null || runtime.Performance == null || settings != current)
        { orig(settings); return; }
        Observe(runtime, settings);
        var o = runtime.Performance;
        int particles = settings.particleEffectsLevel;
        ShaderQualities blur = settings.shaderQuality;
        int savedParticles = o.Particles.Saved(particles), savedBlur = o.Blur.Saved((int)blur);
        // Save the original video preferences, then restore the live profile
        // in finally. No particle refresh, settings reload or other UI writes.
        saving = true;
        try
        {
            settings.particleEffectsLevel = savedParticles;
            settings.shaderQuality = (ShaderQualities)savedBlur;
            orig(settings);
        }
        finally
        {
            bool live = installed && owner == runtime && runtime.Enabled.Value;
            bool particlesEdited = settings.particleEffectsLevel != savedParticles;
            bool blurEdited = (int)settings.shaderQuality != savedBlur;
            if (!particlesEdited) settings.particleEffectsLevel = live ? particles : savedParticles;
            if (!blurEdited) settings.shaderQuality = live ? blur : (ShaderQualities)savedBlur;
            saving = false;
            if (live && (particlesEdited || blurEdited)) Observe(runtime, settings);
        }
    }
}

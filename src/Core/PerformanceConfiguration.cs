using System;

namespace KO.HollowKnight8;

// Saved preferences for the F8 profiles. Native overrides retain a separate
// baseline so applying a profile does not replace the user's video settings.
public sealed class PerformanceConfiguration
{
    public bool Initialized;
    public bool Automatic = true;
    public int Preset;
    public int NativeParticles = -1;
    public int NativeBlur = -1;
    public int Detail;
    public int Bloom;
    public bool NormalHitEffects = true;
    public NativeQualityPreference Particles = new NativeQualityPreference();
    public NativeQualityPreference Blur = new NativeQualityPreference();

    public void Ensure(bool legacyMode, bool legacyHitEffects)
    {
        if (!Initialized)
        {
            Initialized = true;
            NormalHitEffects = legacyHitEffects;
            Preset = legacyMode ? 1 : 0;
            Detail = Bloom = legacyMode ? 1 : 0;
        }
        Preset = Clamp(Preset, 0, 3);
        NativeParticles = Clamp(NativeParticles, -1, 1);
        NativeBlur = Clamp(NativeBlur, -1, 2);
        Detail = Clamp(Detail, 0, 2);
        Bloom = Clamp(Bloom, 0, 2);
        if (Particles == null) Particles = new NativeQualityPreference();
        if (Blur == null) Blur = new NativeQualityPreference();
        Particles.Validate(1); Blur.Validate(2);
    }

    internal static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
    internal static int Recommend(int players) => players >= 5 ? 2 : players >= 3 ? 1 : 0;
    internal void Select(int preset)
    {
        Preset = Clamp(preset, 0, 2);
        NativeParticles = NativeBlur = Preset == 0 ? -1 : 0;
        Detail = Bloom = Preset;
    }
    internal void Customize() { Automatic = false; Preset = 3; }
}

// Comparison is live: a native menu or another mod can supersede our value.
// Such a value is never overwritten by a restoration or saved as our backup.
public sealed class NativeQualityPreference
{
    public bool Held;
    public int Original, Applied;
    internal void Validate(int max)
    {
        if (Original < 0 || Original > max || Applied < 0 || Applied > max) Held = false;
    }
    internal bool Observe(int current)
    {
        if (!Held || current == Applied) return false;
        Held = false;
        return true;
    }
    internal void Resume(int current)
    {
        // A normal restart loads the original preferences saved by our hook.
        // This is not a new user edit and must not turn Automatic off.
        if (Held && current == Original) Held = false;
    }
    internal int Set(int current, int target)
    {
        if (target < 0) return Release(current);
        if (!Held) { Original = current; Held = true; }
        Applied = target;
        return target;
    }
    internal int Saved(int current) => Held && current == Applied ? Original : current;
    internal int Release(int current)
    {
        int result = Saved(current);
        Held = false;
        return result;
    }
}

using System;
using System.Collections.Generic;

namespace KO.HollowKnight8;

internal static class AcquisitionRules
{
    internal const float PoseSeconds = 1.65f;
    internal const float TravelSeconds = .8f;
    internal const float EffectSeconds = 3.4f;
    internal static readonly string[] Flags = {
        "hasAcidArmour", "hasDash", "hasWalljump", "hasSuperDash", "hasDoubleJump",
        "hasShadowDash", "hasDreamNail", "hasDreamGate", "hasCyclone", "hasDashSlash", "hasUpwardSlash"
    };
    internal static readonly string[] Levels = { "fireballLevel", "quakeLevel", "screamLevel" };

    internal static bool Flag(string key)
    {
        if (key == null) return false;
        foreach (string name in Flags) if (name == key) return true;
        return key.StartsWith("gotCharm_", StringComparison.Ordinal);
    }

    internal static bool Level(string key)
    {
        foreach (string name in Levels) if (name == key) return true;
        return false;
    }

    internal static bool Gained(string key, int oldValue, int value)
        => value > oldValue && (Flag(key) && value == 1 || Level(key) && value >= 1 && value <= 2);

    internal static string Id(string key, int value) => key + ":" + value;
}

// Action, data-write, animation and polling hooks can all observe the same
// pickup. A visual transaction may start only once, even with eight actors.
internal sealed class AcquisitionLatch
{
    private readonly HashSet<string> seen = new HashSet<string>();
    internal bool First(string key, int value) => seen.Add(AcquisitionRules.Id(key, value));
    internal void Forget(string key, int value) { seen.Remove(AcquisitionRules.Id(key, value)); }
    internal void Clear() => seen.Clear();
}

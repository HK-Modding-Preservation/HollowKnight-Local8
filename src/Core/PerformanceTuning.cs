namespace KO.HollowKnight8;

// Decoration and the reversible native rendering budget use this setting.
// Gameplay, absorption cues, controls and simulation keep their normal timing.
internal static class PerformanceTuning
{
    internal static bool Enabled => Plugin.Self != null && Plugin.Self.PerformanceMode != null && Plugin.Self.PerformanceMode.Value;
    internal static int Detail => !Enabled ? 0 : Plugin.Self.Performance == null ? 1 : Plugin.Self.Performance.Detail;
    internal static int Bloom => !Enabled ? 0 : Plugin.Self.Performance == null ? 1 : Plugin.Self.Performance.Bloom;
    internal static bool ReduceDamage => Enabled && (Plugin.Self.Performance == null || Plugin.Self.ReduceHitEffects.Value);
    internal static float CosmeticRate(float normal) => Detail == 2 ? normal * .2f : Detail == 1 ? normal * .5f : normal;
}

using UnityEngine;

namespace KO.HollowKnight8;

internal static class PerformanceMenu
{
    private static string Text(string key) => UiLocalization.Get("performance." + key);
    private static string Profile(int preset) => Text(preset == 0 ? "normal" : preset == 1 ? "balanced" : preset == 2 ? "maximum" : "custom");

    internal static void Draw(Local8Runtime runtime, float width, ref float row)
    {
        var o = runtime.Performance;
        if (o == null) return;
        int players = PerformancePresets.PlayerCount(runtime);
        MenuTheme.Section(UiLocalization.Get("performance.title"), width, ref row);
        float toggleHeight = ToggleHeight(width, Text("automatic"));
        if (MenuTheme.Toggle(new Rect(5, row, width - 10, toggleHeight), Text("automatic"), o.Automatic, Text("automatic_tip")))
            PerformancePresets.Automatic(runtime, !o.Automatic);
        row += toggleHeight + 7;
        string status = Text("status").Replace("{players}", players.ToString()).Replace("{preset}", Profile(o.Preset))
            .Replace("{recommended}", Profile(PerformanceConfiguration.Recommend(players)));
        row += MenuTheme.Paragraph(5, row, width - 10, status, MenuTheme.Paper, 14) + 9;
        int presetColumns = Columns(width, 3);
        float cell = (width - 10) / presetColumns;
        for (int i = 0; i < 3; i++)
            if (MenuTheme.Choice(new Rect(5 + (i % presetColumns) * cell, row + (i / presetColumns) * 45, cell - 5, 40), new GUIContent(Profile(i), Text("presets_tip")), o.Preset == i))
                PerformancePresets.Apply(runtime, i);
        row += Mathf.CeilToInt(3f / presetColumns) * 45 + 4;
        if (MenuTheme.Button(new Rect(5, row, width - 10, 36), new GUIContent(Text("restore"), Text("restore_tip")), MenuTheme.ButtonStyle))
            PerformancePresets.Restore(runtime);
        row += 45;
        row += MenuTheme.Paragraph(5, row, width - 10, Text("manual_tip"), MenuTheme.Muted) + 10;

        int next = Choices(width, ref row, Text("particles"), Text("particles_tip"), o.NativeParticles + 1,
            new[] { Text("original"), Text("low"), Text("normal") });
        if (next >= 0) { PerformancePresets.Customize(runtime); o.NativeParticles = next - 1; }
        next = Choices(width, ref row, Text("blur"), Text("blur_tip"), o.NativeBlur + 1,
            new[] { Text("original"), Text("low"), Text("medium"), Text("high") });
        if (next >= 0) { PerformancePresets.Customize(runtime); o.NativeBlur = next - 1; }
        next = Choices(width, ref row, Text("decoration"), Text("decoration_tip"), o.Detail,
            new[] { Text("original"), Text("reduced"), Text("minimum") });
        if (next >= 0) { PerformancePresets.Customize(runtime); o.Detail = next; }
        next = Choices(width, ref row, Text("bloom"), Text("bloom_tip"), o.Bloom,
            new[] { Text("original"), Text("reduced"), Text("off") });
        if (next >= 0) { PerformancePresets.Customize(runtime); o.Bloom = next; }
        toggleHeight = ToggleHeight(width, UiLocalization.Get("effects.label"));
        if (MenuTheme.Toggle(new Rect(5, row, width - 10, toggleHeight), UiLocalization.Get("effects.label"), runtime.ReduceHitEffects.Value, UiLocalization.Get("effects.tip")))
        {
            PerformancePresets.Customize(runtime);
            runtime.ReduceHitEffects.Value = !runtime.ReduceHitEffects.Value;
        }
        row += toggleHeight + 7;
        toggleHeight = ToggleHeight(width, Text("hazard_effects"));
        if (MenuTheme.Toggle(new Rect(5, row, width - 10, toggleHeight), Text("hazard_effects"), o.HazardEffects, Text("hazard_effects_tip")))
            o.HazardEffects = !o.HazardEffects;
        row += toggleHeight + 7;
        toggleHeight = ToggleHeight(width, Text("health_effects"));
        if (MenuTheme.Toggle(new Rect(5, row, width - 10, toggleHeight), Text("health_effects"), o.HudHealthEffects, Text("health_effects_tip")))
            o.HudHealthEffects = !o.HudHealthEffects;
        row += toggleHeight + 7;
        var gm = GameManager.instance;
        if (gm && gm.gameSettings != null)
        {
            string actual = Text("actual").Replace("{particles}", Text(gm.gameSettings.particleEffectsLevel == 0 ? "low" : "normal"))
                .Replace("{blur}", Text(gm.gameSettings.shaderQuality == ShaderQualities.Low ? "low" : gm.gameSettings.shaderQuality == ShaderQualities.Medium ? "medium" : "high"));
            row += MenuTheme.Paragraph(5, row, width - 10, actual, MenuTheme.Muted) + 9;
        }
        row += MenuTheme.Paragraph(5, row, width - 10, Text("safe_tip"), MenuTheme.Muted) + 9;
    }

    private static int Choices(float width, ref float row, string label, string tip, int selected, string[] values)
    {
        row += MenuTheme.Paragraph(5, row, width - 10, label, MenuTheme.Paper, 14) + 6;
        int columns = Columns(width, values.Length);
        float cell = (width - 10) / columns;
        int picked = -1;
        for (int i = 0; i < values.Length; i++)
            if (MenuTheme.Choice(new Rect(5 + (i % columns) * cell, row + (i / columns) * 43, cell - 5, 38), new GUIContent(values[i], tip), selected == i) && selected != i) picked = i;
        row += Mathf.CeilToInt(values.Length / (float)columns) * 43 + 8;
        return picked;
    }

    internal static int Columns(float width, int count) => width < 330 ? 1 :
        count == 4 ? (width >= 620 ? 4 : 2) : (width >= 450 ? 3 : 2);

    private static float ToggleHeight(float width, string label)
    {
        float area = width - 10, state = Mathf.Min(82, area * .28f);
        return Mathf.Max(42, MenuTheme.Label.CalcHeight(new GUIContent(label), Mathf.Max(20, area - state - 62)) + 8);
    }
}

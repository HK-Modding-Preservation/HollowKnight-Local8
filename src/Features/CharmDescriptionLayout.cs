using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KO.HollowKnight8;

// Measure the actual translated/provider text. Never replace a mod's
// description with a stock English summary. Full text remains on hover.
internal static class CharmDescriptionLayout
{
    private static readonly Regex Breaks = new Regex(@"<br\s*/?>", RegexOptions.IgnoreCase);
    private static readonly Regex Tags = new Regex(@"<[^>]+>");
    private static readonly Regex Paragraphs = new Regex(@"\n\s*\n+");
    private static readonly string[] raw = new string[8], plain = new string[8];

    internal static string Text(int player, string value)
    {
        if (raw[player] == value && plain[player] != null) return plain[player];
        raw[player] = value;
        return plain[player] = Tags.Replace(Breaks.Replace(value ?? "", "\n"), "").Replace("\r", "").Trim();
    }
    internal static int UtilityParagraph(int id, int paragraphs)
    {
        if (paragraphs < 2 || id < 1 || id > 40) return 0;
        // These native descriptions begin with their effect; the other native
        // descriptions have a short introduction before the effect paragraph.
        switch (id)
        {
            case 1: case 2: case 4: case 5: case 14: case 18:
            case 22: case 23: case 24: case 25: return 0;
            default: return 1;
        }
    }
    private static bool Fit(GUIStyle style, string text, Rect area, int preferred, int minimum)
    {
        for (int size = preferred; size >= minimum; size--)
        {
            style.fontSize = size;
            if (style.CalcHeight(new GUIContent(text), area.width) <= area.height) return true;
        }
        return false;
    }
    internal static string FitText(int id, string full, Rect area, GUIStyle style, int preferred)
    {
        // Smaller paragraph gaps recover room before reducing the font.
        string compact = Paragraphs.Replace(full, "\n");
        if (Fit(style, compact, area, preferred, Math.Max(9, preferred - 1))) return compact;
        string[] parts = Paragraphs.Split(full);
        int priority = UtilityParagraph(id, parts.Length);
        string effect = parts[priority];
        Fit(style, effect, area, preferred, 8);
        // Keep the effect at its fitted size; add complete lore paragraphs
        // only while they fit. Never shrink useful text for extra lore.
        string shown = effect;
        for (int i = 0; i < parts.Length; i++)
        {
            if (i == priority) continue;
            string next = shown + "\n" + parts[i];
            if (style.CalcHeight(new GUIContent(next), area.width) <= area.height) shown = next;
        }
        return shown;
    }
}
